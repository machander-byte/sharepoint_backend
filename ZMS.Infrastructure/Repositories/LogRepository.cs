using Microsoft.EntityFrameworkCore;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Infrastructure.Repositories;

public class LogRepository : ILogRepository
{
    private readonly ZmsDbContext _dbContext;

    public LogRepository(ZmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(LogEntry entry, CancellationToken cancellationToken)
    {
        _dbContext.Logs.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<LogEntry>> ListByJobIdAsync(Guid jobId, int take, CancellationToken cancellationToken)
    {
        var logs = await _dbContext.Logs
            .AsNoTracking()
            .Where(log => log.JobId == jobId)
            .ToListAsync(cancellationToken);

        var orderedLogs = logs
            .OrderByDescending(log => log.CreatedUtc)
            .ToArray();

        return take > 0
            ? orderedLogs.Take(take).ToArray()
            : orderedLogs;
    }

    public async Task<IReadOnlyCollection<LogEntry>> ListRecentAsync(int take, CancellationToken cancellationToken)
    {
        var logs = await _dbContext.Logs
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return logs
            .OrderByDescending(log => log.CreatedUtc)
            .Take(take)
            .ToArray();
    }
}
