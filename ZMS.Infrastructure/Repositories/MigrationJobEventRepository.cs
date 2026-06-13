using Microsoft.EntityFrameworkCore;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Infrastructure.Repositories;

public class MigrationJobEventRepository : IMigrationJobEventRepository
{
    private readonly ZmsDbContext _dbContext;

    public MigrationJobEventRepository(ZmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(MigrationJobEvent jobEvent, CancellationToken cancellationToken)
    {
        _dbContext.MigrationJobEvents.Add(jobEvent);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<MigrationJobEvent>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken)
    {
        return await _dbContext.MigrationJobEvents
            .AsNoTracking()
            .Where(jobEvent => jobEvent.JobId == jobId)
            .OrderBy(jobEvent => jobEvent.CreatedAt)
            .ToArrayAsync(cancellationToken);
    }
}
