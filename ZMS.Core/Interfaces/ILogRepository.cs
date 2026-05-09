using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface ILogRepository
{
    Task AddAsync(LogEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LogEntry>> ListByJobIdAsync(Guid jobId, int take, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<LogEntry>> ListRecentAsync(int take, CancellationToken cancellationToken);
}
