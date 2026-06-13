using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IMigrationJobEventRepository
{
    Task AddAsync(MigrationJobEvent jobEvent, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationJobEvent>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken);
}
