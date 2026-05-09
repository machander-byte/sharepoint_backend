using Microsoft.EntityFrameworkCore;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Infrastructure.Repositories;

public class MigrationItemRepository : IMigrationItemRepository
{
    private readonly ZmsDbContext _dbContext;

    public MigrationItemRepository(ZmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<MigrationItem>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var items = await _dbContext.MigrationItems
            .AsNoTracking()
            .Where(item => item.JobId == jobId)
            .ToListAsync(cancellationToken);

        return items
            .OrderBy(item => item.CreatedUtc)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<MigrationItem>> GetNextBatchAsync(
        Guid jobId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var items = await _dbContext.MigrationItems
            .AsNoTracking()
            .Where(item => item.JobId == jobId
                && (item.Status == MigrationItemStatus.Pending || item.Status == MigrationItemStatus.RetryQueued))
            .ToListAsync(cancellationToken);

        return items
            .OrderBy(item => item.CreatedUtc)
            .Take(batchSize)
            .ToArray();
    }

    public Task<int> CountByStatusAsync(string userId, MigrationItemStatus status, CancellationToken cancellationToken)
    {
        return (from item in _dbContext.MigrationItems.AsNoTracking()
                join job in _dbContext.MigrationJobs.AsNoTracking() on item.JobId equals job.Id
                where item.Status == status && job.UserId == userId
                select item)
            .CountAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<MigrationItem> items, CancellationToken cancellationToken)
    {
        _dbContext.MigrationItems.AddRange(items);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MigrationItem item, CancellationToken cancellationToken)
    {
        var trackedItem = _dbContext.MigrationItems.Local.FirstOrDefault(localItem => localItem.Id == item.Id);
        if (trackedItem is not null && !ReferenceEquals(trackedItem, item))
        {
            _dbContext.Entry(trackedItem).State = EntityState.Detached;
        }

        _dbContext.MigrationItems.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.Entry(item).State = EntityState.Detached;
    }
}
