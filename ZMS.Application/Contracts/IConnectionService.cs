using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IConnectionService
{
    Task<IReadOnlyCollection<ConnectionProfile>> ListAsync(string userId, CancellationToken cancellationToken);
    Task<ConnectionProfile> CreateAsync(CreateConnectionRequest request, string userId, CancellationToken cancellationToken);
    Task<ConnectionTestResult> TestConnectionAsync(Guid connectionId, string userId, CancellationToken cancellationToken);
}
