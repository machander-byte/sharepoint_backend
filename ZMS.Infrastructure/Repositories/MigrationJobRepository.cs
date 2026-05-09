using Microsoft.EntityFrameworkCore;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Infrastructure.Repositories;

public class MigrationJobRepository : IMigrationJobRepository
{
    private readonly ZmsDbContext _dbContext;

    public MigrationJobRepository(ZmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<MigrationJob>> ListAsync(string userId, CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.MigrationJobs
            .AsNoTracking()
            .Where(job => job.UserId == userId)
            .ToListAsync(cancellationToken);

        return jobs
            .OrderByDescending(job => job.CreatedUtc)
            .ToArray();
    }

    public async Task<IReadOnlyCollection<MigrationJob>> ListAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.MigrationJobs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return jobs
            .OrderByDescending(job => job.CreatedUtc)
            .ToArray();
    }

    public Task<MigrationJob?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken)
    {
        return _dbContext.MigrationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == id && job.UserId == userId, cancellationToken);
    }

    public Task<MigrationJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.MigrationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == id, cancellationToken);
    }

    public async Task AddAsync(MigrationJob job, CancellationToken cancellationToken)
    {
        _dbContext.MigrationJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(MigrationJob job, CancellationToken cancellationToken)
    {
        var trackedJob = _dbContext.MigrationJobs.Local.FirstOrDefault(localJob => localJob.Id == job.Id);
        if (trackedJob is not null && !ReferenceEquals(trackedJob, job))
        {
            _dbContext.Entry(trackedJob).State = EntityState.Detached;
        }

        _dbContext.MigrationJobs.Update(job);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.Entry(job).State = EntityState.Detached;
    }
}
