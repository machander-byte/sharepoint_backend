using ZMS.Core.Enums;
using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IMigrationItemRepository
{
    Task<IReadOnlyCollection<MigrationItem>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MigrationItem>> GetNextBatchAsync(
        Guid jobId,
        int batchSize,
        CancellationToken cancellationToken);

    Task<int> CountByStatusAsync(MigrationItemStatus status, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<MigrationItem> items, CancellationToken cancellationToken);
    Task UpdateAsync(MigrationItem item, CancellationToken cancellationToken);
}
