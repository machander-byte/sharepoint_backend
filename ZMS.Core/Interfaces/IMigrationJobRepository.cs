using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IMigrationJobRepository
{
    Task<IReadOnlyCollection<MigrationJob>> ListAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationJob>> ListAsync(CancellationToken cancellationToken);
    Task<MigrationJob?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken);
    Task<MigrationJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(MigrationJob job, CancellationToken cancellationToken);
    Task UpdateAsync(MigrationJob job, CancellationToken cancellationToken);
}
