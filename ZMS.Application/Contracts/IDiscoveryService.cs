using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IDiscoveryService
{
    Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(Guid sourceConnectionId, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string userId,
        CancellationToken cancellationToken);

    Task<DiscoverySummary> GetSummaryAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string? libraryName,
        string userId,
        CancellationToken cancellationToken);
}
