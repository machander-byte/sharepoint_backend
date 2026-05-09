using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IConnectionRepository
{
    Task<IReadOnlyCollection<ConnectionProfile>> ListAsync(CancellationToken cancellationToken);
    Task<ConnectionProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(ConnectionProfile connection, CancellationToken cancellationToken);
    Task UpdateAsync(ConnectionProfile connection, CancellationToken cancellationToken);
}
