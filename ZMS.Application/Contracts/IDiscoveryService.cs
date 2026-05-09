using ZMS.Core.Models;

namespace ZMS.Application.Contracts;

public interface IDiscoveryService
{
    Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(Guid sourceConnectionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        CancellationToken cancellationToken);

    Task<DiscoverySummary> GetSummaryAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken);
}
