namespace ZMS.Application.Discovery;

public interface IDiscoveryStorageService
{
    Task SaveRequestAsync(string scanId, DiscoveryScanRequest request, CancellationToken cancellationToken = default);
    Task<DiscoveryScanRequest?> GetRequestAsync(string scanId, CancellationToken cancellationToken = default);
    Task SaveStatusAsync(DiscoveryScanStatus status, CancellationToken cancellationToken = default);
    Task<DiscoveryScanStatus?> GetStatusAsync(string scanId, CancellationToken cancellationToken = default);
    Task SaveResultAsync(DiscoveryScanResult result, CancellationToken cancellationToken = default);
    Task<DiscoveryScanResult?> GetResultAsync(string scanId, CancellationToken cancellationToken = default);
    Task<string?> GetLatestCompletedScanIdAsync(CancellationToken cancellationToken = default);
    string GetScanDirectory(string scanId);
}

public interface IConfigModeDiscoveryScanner
{
    Task<DiscoveryScanResult> ScanAsync(
        string scanId,
        DiscoveryScanRequest request,
        Func<int, string, Task> reportProgress,
        CancellationToken cancellationToken = default);
}

public interface ILiveSharePointDiscoveryScanner
{
    Task<DiscoveryScanResult> ScanAsync(
        string scanId,
        DiscoveryScanRequest request,
        Func<int, string, Task> reportProgress,
        CancellationToken cancellationToken = default);
}

public interface IPermissionRiskAnalyzer
{
    IReadOnlyCollection<PermissionRiskFinding> Analyze(DiscoveryScanResult result);
}

public interface IMetadataAnalyzer
{
    IReadOnlyCollection<MetadataFinding> Analyze(DiscoveryScanResult result);
}

public interface IMigrationRiskAnalyzer
{
    IReadOnlyCollection<MigrationRiskFinding> Analyze(DiscoveryScanResult result);
    int CalculateReadinessScore(
        IReadOnlyCollection<PermissionRiskFinding> permissionRisks,
        IReadOnlyCollection<MetadataFinding> metadataFindings,
        IReadOnlyCollection<MigrationRiskFinding> migrationRisks);
}

public interface IDiscoveryExportService
{
    DiscoveryExportResult ExportInventoryCsv(DiscoveryScanResult result);
    DiscoveryExportResult ExportPermissionsCsv(DiscoveryScanResult result);
    DiscoveryExportResult ExportMetadataCsv(DiscoveryScanResult result);
    DiscoveryExportResult ExportRisksCsv(DiscoveryScanResult result);
    DiscoveryExportResult ExportJson(DiscoveryScanResult result);
}
