using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IConnectionService
{
    Task<IReadOnlyCollection<ConnectionProfile>> ListAsync(CancellationToken cancellationToken);
    Task<ConnectionProfile> CreateAsync(CreateConnectionRequest request, CancellationToken cancellationToken);
    Task<ConnectionTestResult> TestConnectionAsync(Guid connectionId, CancellationToken cancellationToken);
}
