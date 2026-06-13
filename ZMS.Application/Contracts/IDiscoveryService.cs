using ZMS.Core.Models;
using ZMS.Application.Discovery;

namespace ZMS.Application.Contracts;

public interface IDiscoveryService
{
    Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(Guid sourceConnectionId, string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string userId,
        CancellationToken cancellationToken);

    Task<ZMS.Core.Models.DiscoverySummary> GetSummaryAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string? libraryName,
        string userId,
        CancellationToken cancellationToken);

    Task<StartDiscoveryScanResponse> StartScanAsync(DiscoveryScanRequest request, CancellationToken cancellationToken);

    Task<DiscoveryScanStatus?> GetScanStatusAsync(string scanId, CancellationToken cancellationToken);

    Task<DiscoveryScanResult?> GetScanResultAsync(string scanId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DiscoveredInventoryItem>?> GetInventoryAsync(string scanId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PermissionRiskFinding>?> GetPermissionRisksAsync(string scanId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MetadataFinding>?> GetMetadataFindingsAsync(string scanId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<MigrationRiskFinding>?> GetMigrationRisksAsync(string scanId, CancellationToken cancellationToken);

    Task<DiscoveryScanResult?> GetLatestCompletedResultAsync(CancellationToken cancellationToken);

    Task<DiscoveryExportResult?> ExportAsync(string scanId, string exportType, CancellationToken cancellationToken);

    Task<DiscoveryImportResponse> ImportResultAsync(DiscoveryScanResult scanResult, CancellationToken cancellationToken);

    Task<DiscoveryImportResponse> ImportResultFromFolderAsync(string folderPath, CancellationToken cancellationToken);
}
