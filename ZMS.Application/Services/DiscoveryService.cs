using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ZMS.Application.Contracts;
using ZMS.Application.Discovery;
using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;
using ZMS.Core.Security;

namespace ZMS.Application.Services;

public class DiscoveryService : IDiscoveryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly IConnectionRepository _connectionRepository;
    private readonly ConnectorResolver _connectorResolver;
    private readonly ISecretProtector _secretProtector;
    private readonly IDiscoveryStorageService _storageService;
    private readonly IConfigModeDiscoveryScanner _configModeScanner;
    private readonly ILiveSharePointDiscoveryScanner _liveScanner;
    private readonly IPermissionRiskAnalyzer _permissionRiskAnalyzer;
    private readonly IMetadataAnalyzer _metadataAnalyzer;
    private readonly IMigrationRiskAnalyzer _migrationRiskAnalyzer;
    private readonly IDiscoveryExportService _exportService;
    private readonly IDiscoveryGraphRepository _discoveryGraphRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(
        IConnectionRepository connectionRepository,
        ConnectorResolver connectorResolver,
        ISecretProtector secretProtector,
        IDiscoveryStorageService storageService,
        IConfigModeDiscoveryScanner configModeScanner,
        ILiveSharePointDiscoveryScanner liveScanner,
        IPermissionRiskAnalyzer permissionRiskAnalyzer,
        IMetadataAnalyzer metadataAnalyzer,
        IMigrationRiskAnalyzer migrationRiskAnalyzer,
        IDiscoveryExportService exportService,
        IDiscoveryGraphRepository discoveryGraphRepository,
        IConfiguration configuration,
        ILogger<DiscoveryService> logger)
    {
        _connectionRepository = connectionRepository;
        _connectorResolver = connectorResolver;
        _secretProtector = secretProtector;
        _storageService = storageService;
        _configModeScanner = configModeScanner;
        _liveScanner = liveScanner;
        _permissionRiskAnalyzer = permissionRiskAnalyzer;
        _metadataAnalyzer = metadataAnalyzer;
        _migrationRiskAnalyzer = migrationRiskAnalyzer;
        _exportService = exportService;
        _discoveryGraphRepository = discoveryGraphRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(Guid sourceConnectionId, string userId, CancellationToken cancellationToken)
    {
        var connection = await GetSourceConnectionAsync(sourceConnectionId, userId, cancellationToken);
        var connector = _connectorResolver.ResolveSource(connection);
        return await connector.GetSitesAsync(connection, cancellationToken);
    }

    public async Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string userId,
        CancellationToken cancellationToken)
    {
        var connection = await GetSourceConnectionAsync(sourceConnectionId, userId, cancellationToken);
        var connector = _connectorResolver.ResolveSource(connection);
        return await connector.GetLibrariesAsync(connection, sourceLocation, cancellationToken);
    }

    public async Task<ZMS.Core.Models.DiscoverySummary> GetSummaryAsync(
        Guid sourceConnectionId,
        string sourceLocation,
        string? libraryName,
        string userId,
        CancellationToken cancellationToken)
    {
        var connection = await GetSourceConnectionAsync(sourceConnectionId, userId, cancellationToken);
        var connector = _connectorResolver.ResolveSource(connection);
        var sites = await connector.GetSitesAsync(connection, cancellationToken);
        var libraries = await connector.GetLibrariesAsync(connection, sourceLocation, cancellationToken);
        var files = await connector.GetFilesAsync(connection, sourceLocation, libraryName, cancellationToken);

        return new ZMS.Core.Models.DiscoverySummary
        {
            SiteCount = sites.Count,
            LibraryCount = libraries.Count,
            FileCount = files.Count,
            TotalBytes = files.Sum(file => file.SizeInBytes)
        };
    }

    public async Task<StartDiscoveryScanResponse> StartScanAsync(DiscoveryScanRequest request, CancellationToken cancellationToken)
    {
        var scanId = Guid.NewGuid().ToString("D");
        var status = new DiscoveryScanStatus
        {
            ScanId = scanId,
            Status = "queued",
            Progress = 0,
            CurrentStep = "Queued",
            StartedAt = DateTimeOffset.UtcNow
        };

        request.Mode = ResolveDiscoveryMode(request.Mode, _configuration);
        await _storageService.SaveRequestAsync(scanId, request, cancellationToken);
        await _storageService.SaveStatusAsync(status, cancellationToken);
        _logger.LogInformation("Discovery run {DiscoveryRunId} queued in {DiscoveryMode} mode.", scanId, request.Mode);

        _ = Task.Run(() => ExecuteScanAsync(scanId, request, status.StartedAt, CancellationToken.None), CancellationToken.None);

        return new StartDiscoveryScanResponse
        {
            ScanId = scanId,
            Status = "queued",
            Message = "Discovery scan started"
        };
    }

    public async Task<DiscoveryScanStatus?> GetScanStatusAsync(string scanId, CancellationToken cancellationToken)
    {
        return await _storageService.GetStatusAsync(scanId, cancellationToken);
    }

    public async Task<DiscoveryScanResult?> GetScanResultAsync(string scanId, CancellationToken cancellationToken)
    {
        return await _storageService.GetResultAsync(scanId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<DiscoveredInventoryItem>?> GetInventoryAsync(string scanId, CancellationToken cancellationToken)
    {
        return (await _storageService.GetResultAsync(scanId, cancellationToken))?.InventoryItems;
    }

    public async Task<IReadOnlyCollection<PermissionRiskFinding>?> GetPermissionRisksAsync(string scanId, CancellationToken cancellationToken)
    {
        return (await _storageService.GetResultAsync(scanId, cancellationToken))?.PermissionRisks;
    }

    public async Task<IReadOnlyCollection<MetadataFinding>?> GetMetadataFindingsAsync(string scanId, CancellationToken cancellationToken)
    {
        return (await _storageService.GetResultAsync(scanId, cancellationToken))?.MetadataFindings;
    }

    public async Task<IReadOnlyCollection<MigrationRiskFinding>?> GetMigrationRisksAsync(string scanId, CancellationToken cancellationToken)
    {
        return (await _storageService.GetResultAsync(scanId, cancellationToken))?.MigrationRisks;
    }

    public async Task<DiscoveryScanResult?> GetLatestCompletedResultAsync(CancellationToken cancellationToken)
    {
        var scanId = await _storageService.GetLatestCompletedScanIdAsync(cancellationToken);
        return scanId is null ? null : await _storageService.GetResultAsync(scanId, cancellationToken);
    }

    public async Task<DiscoveryExportResult?> ExportAsync(string scanId, string exportType, CancellationToken cancellationToken)
    {
        var result = await _storageService.GetResultAsync(scanId, cancellationToken);
        if (result is null)
        {
            return null;
        }

        return exportType.ToLowerInvariant() switch
        {
            "csv" or "inventory" or "inventory.csv" => _exportService.ExportInventoryCsv(result),
            "json" => _exportService.ExportJson(result),
            "permissions" or "permissions.csv" => _exportService.ExportPermissionsCsv(result),
            "metadata" or "metadata.csv" => _exportService.ExportMetadataCsv(result),
            "risks" or "risks.csv" => _exportService.ExportRisksCsv(result),
            _ => null
        };
    }

    public async Task<DiscoveryImportResponse> ImportResultAsync(DiscoveryScanResult scanResult, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scanResult);
        ValidateImportedResult(scanResult);

        var scanId = Guid.NewGuid().ToString("D");
        var now = DateTimeOffset.UtcNow;
        var imported = NormalizeImportedResult(scanResult, scanId, now);

        await _storageService.SaveRequestAsync(
            scanId,
            new DiscoveryScanRequest
            {
                ScanName = imported.ScanName,
                Mode = "live-import",
                TenantUrl = imported.SiteCollections.FirstOrDefault()?.Url ?? string.Empty,
                SiteUrls = imported.SiteCollections.Select(site => site.Url).Where(url => !string.IsNullOrWhiteSpace(url)).ToList(),
                IncludeFiles = true,
                IncludePermissions = true,
                IncludeMetadata = true,
                IncludeSubsites = true
            },
            cancellationToken);

        await _storageService.SaveResultAsync(imported, cancellationToken);
        await PersistDiscoveryGraphAsync(imported, new DiscoveryScanRequest
        {
            ScanName = imported.ScanName,
            Mode = "live-import",
            TenantUrl = imported.SiteCollections.FirstOrDefault()?.Url ?? string.Empty,
            SiteUrls = imported.SiteCollections.Select(site => site.Url).Where(url => !string.IsNullOrWhiteSpace(url)).ToList(),
            IncludeFiles = true,
            IncludePermissions = true,
            IncludeMetadata = true,
            IncludeSubsites = true
        }, cancellationToken);
        await _storageService.SaveStatusAsync(
            new DiscoveryScanStatus
            {
                ScanId = scanId,
                Status = "completed",
                Progress = 100,
                CurrentStep = imported.Status.Equals("partial", StringComparison.OrdinalIgnoreCase)
                    ? "Imported live discovery result with partial scan errors"
                    : "Imported live discovery result",
                StartedAt = imported.StartedAt,
                CompletedAt = imported.CompletedAt ?? now,
                Errors = imported.Errors,
                Warnings = imported.Warnings
            },
            cancellationToken);

        return new DiscoveryImportResponse
        {
            ScanId = imported.ScanId,
            Status = "completed",
            Message = "Discovery result imported successfully",
            Summary = imported.Summary
        };
    }

    public async Task<DiscoveryImportResponse> ImportResultFromFolderAsync(string folderPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path is required.", nameof(folderPath));
        }

        var resultPath = Path.Combine(folderPath, "scan-result.json");
        if (!File.Exists(resultPath))
        {
            throw new FileNotFoundException("scan-result.json was not found in the provided discovery output folder.", resultPath);
        }

        await using var stream = File.OpenRead(resultPath);
        var result = await JsonSerializer.DeserializeAsync<DiscoveryScanResult>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("scan-result.json could not be parsed as a discovery scan result.");

        return await ImportResultAsync(result, cancellationToken);
    }

    private async Task<ConnectionProfile> GetSourceConnectionAsync(Guid sourceConnectionId, string userId, CancellationToken cancellationToken)
    {
        var connection = await _connectionRepository.GetByIdAsync(sourceConnectionId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Source connection '{sourceConnectionId}' was not found.");

        if (!_connectorResolver.CanResolveSource(connection.Type))
        {
            throw new InvalidOperationException($"Connection '{connection.Name}' is not configured as a source connector.");
        }

        return connection.WithUnprotectedSecrets(_secretProtector);
    }

    private async Task ExecuteScanAsync(
        string scanId,
        DiscoveryScanRequest request,
        DateTimeOffset startedAt,
        CancellationToken cancellationToken)
    {
        var status = new DiscoveryScanStatus
        {
            ScanId = scanId,
            Status = "running",
            Progress = 5,
            CurrentStep = "Starting discovery scan",
            StartedAt = startedAt
        };

        try
        {
            await _storageService.SaveStatusAsync(status, cancellationToken);
            _logger.LogInformation("Discovery run {DiscoveryRunId} started in {DiscoveryMode} mode.", scanId, request.Mode);

            Task ReportProgress(int progress, string currentStep)
            {
                status.Status = "running";
                status.Progress = Math.Clamp(progress, 0, 99);
                status.CurrentStep = currentStep;
                return _storageService.SaveStatusAsync(status, cancellationToken);
            }

            DiscoveryScanResult result;
            if (string.Equals(request.Mode, "live", StringComparison.OrdinalIgnoreCase))
            {
                result = await _liveScanner.ScanAsync(scanId, request, ReportProgress, cancellationToken);
            }
            else if (string.Equals(request.Mode, "config", StringComparison.OrdinalIgnoreCase))
            {
                result = await _configModeScanner.ScanAsync(scanId, request, ReportProgress, cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("Discovery mode must be 'config' or 'live'.");
            }

            await ReportProgress(82, "Analyzing permissions");
            result.PermissionRisks = _permissionRiskAnalyzer.Analyze(result).ToList();

            await ReportProgress(88, "Analyzing metadata");
            result.MetadataFindings = _metadataAnalyzer.Analyze(result).ToList();

            await ReportProgress(93, "Analyzing migration risks");
            result.MigrationRisks = _migrationRiskAnalyzer.Analyze(result).ToList();
            result.Summary = BuildFinalSummary(result);
            result.Status = result.IsPartial || result.Errors.Count > 0 ? "partial" : "completed";
            result.StartedAt = startedAt;
            result.CompletedAt = DateTimeOffset.UtcNow;

            await ReportProgress(98, "Writing discovery results");
            await _storageService.SaveResultAsync(result, cancellationToken);
            await PersistDiscoveryGraphAsync(result, request, cancellationToken);

            status.Status = result.Status;
            status.Progress = 100;
            status.CurrentStep = result.Status.Equals("partial", StringComparison.OrdinalIgnoreCase)
                ? "Discovery scan completed with partial results"
                : "Discovery scan completed";
            status.CompletedAt = result.CompletedAt;
            status.Warnings = result.Warnings;
            status.Errors = result.Errors;
            await _storageService.SaveStatusAsync(status, cancellationToken);
            _logger.LogInformation(
                "Discovery run {DiscoveryRunId} completed with status {DiscoveryStatus}, {TotalFiles} files, {RiskCount} risks.",
                scanId,
                result.Status,
                result.Summary.Files,
                result.MigrationRisks.Count + result.PermissionRisks.Count + result.MetadataFindings.Count);
        }
        catch (Exception ex)
        {
            status.Status = "failed";
            status.CurrentStep = "Discovery scan failed";
            status.CompletedAt = DateTimeOffset.UtcNow;
            status.Errors.Add(SecretRedactor.Redact(ex.Message));
            _logger.LogError("Discovery run {DiscoveryRunId} failed: {Message}", scanId, SecretRedactor.Redact(ex.Message));
            await _storageService.SaveStatusAsync(status, CancellationToken.None);
        }
    }

    private static void ValidateImportedResult(DiscoveryScanResult scanResult)
    {
        if (scanResult.Summary is null)
        {
            throw new InvalidOperationException("Imported discovery result must include summary.");
        }

        if (scanResult.InventoryItems is null)
        {
            throw new InvalidOperationException("Imported discovery result must include inventoryItems.");
        }

        if (scanResult.PermissionRisks is null)
        {
            throw new InvalidOperationException("Imported discovery result must include permissionRisks.");
        }

        if (scanResult.MetadataFindings is null)
        {
            throw new InvalidOperationException("Imported discovery result must include metadataFindings.");
        }

        if (scanResult.MigrationRisks is null)
        {
            throw new InvalidOperationException("Imported discovery result must include migrationRisks.");
        }

        if (!scanResult.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)
            && !scanResult.Status.Equals("partial", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Imported discovery result status must be completed or partial.");
        }
    }

    private DiscoveryScanResult NormalizeImportedResult(DiscoveryScanResult source, string scanId, DateTimeOffset importedAt)
    {
        source.ScanId = scanId;
        source.ScanName = string.IsNullOrWhiteSpace(source.ScanName) ? "Imported Live SharePoint Discovery" : source.ScanName;
        source.Mode = "live-import";
        source.Status = source.Status.Equals("partial", StringComparison.OrdinalIgnoreCase) ? "partial" : "completed";
        source.StartedAt = source.StartedAt == default ? importedAt : source.StartedAt;
        source.CompletedAt ??= importedAt;
        source.Warnings ??= [];
        source.Errors ??= [];
        source.SiteCollections ??= [];
        source.InventoryItems ??= [];
        source.PermissionRisks ??= [];
        source.MetadataFindings ??= [];
        source.MigrationRisks ??= [];
        source.Summary = BuildImportedSummary(source);

        return source;
    }

    private ZMS.Application.Discovery.DiscoverySummary BuildImportedSummary(DiscoveryScanResult result)
    {
        var computed = BuildFinalSummary(result);

        return new ZMS.Application.Discovery.DiscoverySummary
        {
            SiteCollections = result.Summary.SiteCollections > 0 ? result.Summary.SiteCollections : computed.SiteCollections,
            Subsites = result.Summary.Subsites > 0 ? result.Summary.Subsites : computed.Subsites,
            Libraries = result.Summary.Libraries > 0 ? result.Summary.Libraries : computed.Libraries,
            Lists = result.Summary.Lists > 0 ? result.Summary.Lists : computed.Lists,
            Files = result.Summary.Files > 0 ? result.Summary.Files : computed.Files,
            Folders = result.Summary.Folders > 0 ? result.Summary.Folders : computed.Folders,
            TotalStorageBytes = result.Summary.TotalStorageBytes > 0 ? result.Summary.TotalStorageBytes : computed.TotalStorageBytes,
            MetadataFields = result.Summary.MetadataFields > 0 ? result.Summary.MetadataFields : computed.MetadataFields,
            PermissionGroups = result.Summary.PermissionGroups > 0 ? result.Summary.PermissionGroups : computed.PermissionGroups,
            BrokenInheritanceCount = result.Summary.BrokenInheritanceCount > 0 ? result.Summary.BrokenInheritanceCount : computed.BrokenInheritanceCount,
            LongPathRisks = result.Summary.LongPathRisks > 0 ? result.Summary.LongPathRisks : computed.LongPathRisks,
            LargeFileRisks = result.Summary.LargeFileRisks > 0 ? result.Summary.LargeFileRisks : computed.LargeFileRisks,
            MissingMetadataIssues = result.Summary.MissingMetadataIssues > 0 ? result.Summary.MissingMetadataIssues : computed.MissingMetadataIssues,
            ReadinessScore = result.Summary.ReadinessScore > 0 ? result.Summary.ReadinessScore : computed.ReadinessScore
        };
    }

    private ZMS.Application.Discovery.DiscoverySummary BuildFinalSummary(DiscoveryScanResult result)
    {
        var coreSummary = new ZMS.Application.Discovery.DiscoverySummary
        {
            SiteCollections = result.SiteCollections.Count,
            Subsites = result.SiteCollections.Sum(site => site.Subsites.Count),
            Libraries = result.SiteCollections.Sum(site => site.Libraries.Count),
            Lists = result.SiteCollections.Sum(site => site.Lists.Count),
            Files = result.SiteCollections.Sum(site => site.Libraries.Sum(library => library.FileCount)),
            Folders = result.SiteCollections.Sum(site => site.Libraries.Sum(library => library.FolderCount)),
            TotalStorageBytes = result.SiteCollections.Sum(site => site.SizeBytes),
            MetadataFields = result.SiteCollections.Sum(site => site.MetadataFields.Count),
            PermissionGroups = result.SiteCollections.Sum(site => site.SharePointGroups.Count),
            BrokenInheritanceCount = result.PermissionRisks.Count(item =>
                item.InheritanceStatus.Contains("broken", StringComparison.OrdinalIgnoreCase)
                || item.InheritanceStatus.Contains("unique", StringComparison.OrdinalIgnoreCase)),
            LongPathRisks = result.MigrationRisks.Count(item => item.RiskType.Equals("Long Paths", StringComparison.OrdinalIgnoreCase)),
            LargeFileRisks = result.MigrationRisks.Count(item => item.RiskType.Equals("Large Files", StringComparison.OrdinalIgnoreCase)),
            MissingMetadataIssues = result.MetadataFindings.Count(item =>
                item.MissingValueCount > 0
                || item.MappingRisk.Equals("High", StringComparison.OrdinalIgnoreCase)
                || item.MappingRisk.Equals("Critical", StringComparison.OrdinalIgnoreCase))
        };

        coreSummary.ReadinessScore = string.Equals(result.Mode, "live", StringComparison.OrdinalIgnoreCase)
            && result.Warnings.Any(warning => warning.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
                ? 0
                : _migrationRiskAnalyzer.CalculateReadinessScore(
                    result.PermissionRisks,
                    result.MetadataFindings,
                    result.MigrationRisks);

        return coreSummary;
    }

    private async Task PersistDiscoveryGraphAsync(
        DiscoveryScanResult result,
        DiscoveryScanRequest request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(result.ScanId, out var runId))
        {
            runId = Guid.NewGuid();
        }

        var sites = new List<ZMS.Core.Models.DiscoveredSite>();
        var webs = new List<ZMS.Core.Models.DiscoveredWeb>();
        var libraries = new List<ZMS.Core.Models.DiscoveredLibrary>();
        var lists = new List<DiscoveredListEntity>();
        var folders = new List<DiscoveredFolderEntity>();
        var files = new List<DiscoveredFileEntity>();
        var permissions = new List<ZMS.Core.Models.DiscoveredPermission>();
        var sharingLinks = new List<DiscoveredSharingLink>();
        var metadataFields = new List<DiscoveredMetadataFieldEntity>();
        var contentTypes = new List<DiscoveredContentType>();
        var riskFindings = BuildPersistentRiskFindings(runId, result).ToList();

        foreach (var site in result.SiteCollections)
        {
            var siteId = Guid.NewGuid();
            sites.Add(new ZMS.Core.Models.DiscoveredSite
            {
                Id = siteId,
                DiscoveryRunId = runId,
                ExternalId = site.Id,
                Title = site.Title,
                Url = site.Url,
                Department = site.Department,
                Description = site.Description,
                FileCount = site.FileCount,
                FolderCount = site.FolderCount,
                SizeBytes = site.SizeBytes
            });

            foreach (var subsite in site.Subsites)
            {
                webs.Add(new ZMS.Core.Models.DiscoveredWeb
                {
                    DiscoveryRunId = runId,
                    SiteId = siteId,
                    ExternalId = subsite.Id,
                    Title = subsite.Title,
                    Url = subsite.Url,
                    Description = subsite.Description
                });
            }

            foreach (var siteField in site.MetadataFields)
            {
                metadataFields.Add(ToPersistentMetadataField(runId, null, site.Title, string.Empty, siteField));
            }

            foreach (var group in site.SharePointGroups)
            {
                permissions.Add(new ZMS.Core.Models.DiscoveredPermission
                {
                    DiscoveryRunId = runId,
                    Site = site.Title,
                    Scope = site.Url,
                    Principal = group.Name,
                    PrincipalType = "SharePointGroup",
                    Role = group.Role,
                    IsBroadAccess = IsBroadAccess(group.Name)
                });
            }

            foreach (var permission in site.Permissions)
            {
                AddPersistentPermissions(permissions, sharingLinks, runId, permission);
            }

            foreach (var library in site.Libraries)
            {
                var libraryId = Guid.NewGuid();
                libraries.Add(new ZMS.Core.Models.DiscoveredLibrary
                {
                    Id = libraryId,
                    DiscoveryRunId = runId,
                    SiteId = siteId,
                    ExternalId = library.Id,
                    Title = library.Title,
                    Type = library.Type,
                    Url = library.Url,
                    FileCount = library.FileCount,
                    FolderCount = library.FolderCount,
                    SizeBytes = library.SizeBytes,
                    BrokenInheritance = library.BrokenInheritance
                });

                foreach (var contentType in library.ContentTypes)
                {
                    contentTypes.Add(new DiscoveredContentType
                    {
                        DiscoveryRunId = runId,
                        LibraryId = libraryId,
                        Name = contentType,
                        Scope = library.Url
                    });
                }

                foreach (var field in library.MetadataFields)
                {
                    metadataFields.Add(ToPersistentMetadataField(runId, libraryId, site.Title, library.Title, field));
                }

                foreach (var permission in library.Permissions)
                {
                    AddPersistentPermissions(permissions, sharingLinks, runId, permission);
                }

                foreach (var folder in library.Folders)
                {
                    folders.Add(new DiscoveredFolderEntity
                    {
                        DiscoveryRunId = runId,
                        LibraryId = libraryId,
                        ExternalId = folder.Id,
                        Name = folder.Name,
                        Path = folder.Path,
                        Depth = folder.Depth,
                        FileCount = folder.FileCount,
                        SizeBytes = folder.SizeBytes,
                        Archived = folder.Archived,
                        LongPathRisk = folder.LongPathRisk,
                        DuplicateIndicator = folder.DuplicateIndicator
                    });
                }

                foreach (var file in library.Files)
                {
                    files.Add(new DiscoveredFileEntity
                    {
                        DiscoveryRunId = runId,
                        LibraryId = libraryId,
                        Name = file.Name,
                        Path = file.Path,
                        Url = file.Url,
                        SizeBytes = file.SizeBytes,
                        CreatedAt = file.CreatedAt,
                        ModifiedAt = file.ModifiedAt,
                        LargeFileRisk = file.LargeFileRisk,
                        LongPathRisk = file.LongPathRisk,
                        DuplicateIndicator = file.DuplicateIndicator
                    });
                }
            }

            foreach (var list in site.Lists)
            {
                lists.Add(new DiscoveredListEntity
                {
                    DiscoveryRunId = runId,
                    SiteId = siteId,
                    ExternalId = list.Id,
                    Title = list.Title,
                    Description = list.Description,
                    ItemCount = list.ItemCount
                });

                foreach (var field in list.Fields)
                {
                    metadataFields.Add(ToPersistentMetadataField(runId, null, site.Title, list.Title, field));
                }
            }
        }

        var run = new DiscoveryRun
        {
            Id = runId,
            Name = string.IsNullOrWhiteSpace(result.ScanName) ? request.ScanName : result.ScanName,
            SourceType = ResolveSourceType(result.Mode),
            Status = result.Status,
            StartedAt = result.StartedAt,
            CompletedAt = result.CompletedAt,
            TotalSites = result.Summary.SiteCollections,
            TotalWebs = result.Summary.Subsites,
            TotalLibraries = result.Summary.Libraries,
            TotalLists = result.Summary.Lists,
            TotalFolders = result.Summary.Folders,
            TotalFiles = result.Summary.Files,
            TotalPermissions = permissions.Count,
            TotalSharingLinks = sharingLinks.Count,
            TotalRiskFindings = riskFindings.Count,
            ReadinessScore = result.Summary.ReadinessScore,
            ErrorMessage = result.Errors.FirstOrDefault()
        };

        await _discoveryGraphRepository.SaveRunAsync(
            run,
            sites,
            webs,
            libraries,
            lists,
            folders,
            files,
            permissions,
            sharingLinks,
            metadataFields,
            contentTypes,
            riskFindings,
            cancellationToken);
    }

    private static DiscoveredMetadataFieldEntity ToPersistentMetadataField(
        Guid runId,
        Guid? libraryId,
        string site,
        string library,
        ZMS.Application.Discovery.DiscoveredMetadataField field)
    {
        return new DiscoveredMetadataFieldEntity
        {
            DiscoveryRunId = runId,
            LibraryId = libraryId,
            Site = site,
            Library = library,
            Name = field.Name,
            FieldType = field.FieldType,
            Required = field.Required,
            MissingValueCount = field.MissingValueCount,
            MappingRisk = field.MappingRisk
        };
    }

    private static void AddPersistentPermissions(
        List<ZMS.Core.Models.DiscoveredPermission> permissions,
        List<DiscoveredSharingLink> sharingLinks,
        Guid runId,
        ZMS.Application.Discovery.DiscoveredPermissionEntry permission)
    {
        foreach (var group in permission.Groups.DefaultIfEmpty("Unspecified principal"))
        {
            permissions.Add(new ZMS.Core.Models.DiscoveredPermission
            {
                DiscoveryRunId = runId,
                Site = permission.Site,
                Scope = permission.LibraryOrFolder,
                Principal = group,
                PrincipalType = "Group",
                Role = string.Join(",", permission.AccessLevels),
                HasBrokenInheritance = IsBrokenInheritance(permission.InheritanceStatus),
                IsBroadAccess = IsBroadAccess(group)
            });
        }

        foreach (var user in permission.Users)
        {
            permissions.Add(new ZMS.Core.Models.DiscoveredPermission
            {
                DiscoveryRunId = runId,
                Site = permission.Site,
                Scope = permission.LibraryOrFolder,
                Principal = user,
                PrincipalType = "User",
                Role = string.Join(",", permission.AccessLevels),
                HasBrokenInheritance = IsBrokenInheritance(permission.InheritanceStatus),
                IsExternal = IsExternal(user)
            });
        }

        if (permission.AccessLevels.Any(access => access.Contains("anonymous", StringComparison.OrdinalIgnoreCase)
            || access.Contains("external", StringComparison.OrdinalIgnoreCase)))
        {
            sharingLinks.Add(new DiscoveredSharingLink
            {
                DiscoveryRunId = runId,
                Scope = permission.LibraryOrFolder,
                Path = permission.LibraryOrFolder,
                LinkType = string.Join(",", permission.AccessLevels),
                AllowsAnonymousAccess = permission.AccessLevels.Any(access => access.Contains("anonymous", StringComparison.OrdinalIgnoreCase)),
                AllowsExternalAccess = permission.AccessLevels.Any(access => access.Contains("external", StringComparison.OrdinalIgnoreCase))
            });
        }
    }

    private static IEnumerable<RiskFinding> BuildPersistentRiskFindings(Guid runId, DiscoveryScanResult result)
    {
        foreach (var risk in result.MigrationRisks)
        {
            yield return new RiskFinding
            {
                DiscoveryRunId = runId,
                SourceFindingId = risk.Id,
                Category = NormalizeRiskCategory(risk.RiskType),
                Severity = ParseSeverity(risk.RiskLevel),
                Title = risk.RiskType,
                Description = risk.Description,
                RecommendedAction = risk.RecommendedAction,
                Site = risk.Site,
                Location = risk.LibraryOrPath,
                Path = risk.Path
            };
        }

        foreach (var risk in result.PermissionRisks)
        {
            yield return new RiskFinding
            {
                DiscoveryRunId = runId,
                SourceFindingId = risk.Id,
                Category = "PermissionRisk",
                Severity = ParseSeverity(risk.RiskLevel),
                Title = "Permission risk",
                Description = $"{risk.InheritanceStatus} permissions require review.",
                RecommendedAction = risk.RecommendedAction,
                Site = risk.Site,
                Location = risk.LibraryOrFolder,
                Path = risk.LibraryOrFolder
            };
        }

        foreach (var finding in result.MetadataFindings)
        {
            yield return new RiskFinding
            {
                DiscoveryRunId = runId,
                SourceFindingId = finding.Id,
                Category = "MetadataRisk",
                Severity = ParseSeverity(finding.MappingRisk),
                Title = "Metadata risk",
                Description = $"{finding.FieldName} has {finding.MissingValueCount} missing values.",
                RecommendedAction = "Clean or map metadata before migration.",
                Site = finding.Site,
                Location = finding.Library,
                Path = finding.FieldName
            };
        }
    }

    private static string NormalizeRiskCategory(string riskType)
    {
        if (riskType.Contains("permission", StringComparison.OrdinalIgnoreCase)) return "PermissionRisk";
        if (riskType.Contains("sharing", StringComparison.OrdinalIgnoreCase)) return "SharingRisk";
        if (riskType.Contains("metadata", StringComparison.OrdinalIgnoreCase)) return "MetadataRisk";
        if (riskType.Contains("path", StringComparison.OrdinalIgnoreCase)) return "PathLengthRisk";
        if (riskType.Contains("large", StringComparison.OrdinalIgnoreCase)) return "LargeFileRisk";
        if (riskType.Contains("stale", StringComparison.OrdinalIgnoreCase)
            || riskType.Contains("archive", StringComparison.OrdinalIgnoreCase)) return "StaleContentRisk";
        if (riskType.Contains("governance", StringComparison.OrdinalIgnoreCase)) return "GovernanceRisk";
        return "MigrationComplexityRisk";
    }

    private static EnterpriseSeverity ParseSeverity(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "critical" => EnterpriseSeverity.Critical,
            "high" => EnterpriseSeverity.High,
            "medium" => EnterpriseSeverity.Medium,
            "warning" => EnterpriseSeverity.Warning,
            "low" => EnterpriseSeverity.Low,
            _ => EnterpriseSeverity.Info
        };
    }

    private static string ResolveSourceType(string mode)
    {
        return mode.Equals("live", StringComparison.OrdinalIgnoreCase)
            || mode.Equals("live-import", StringComparison.OrdinalIgnoreCase)
                ? "SharePointOnline"
                : "ConfigDemo";
    }

    private static string ResolveDiscoveryMode(string requestedMode, IConfiguration configuration)
    {
        var configuredMode = configuration["DISCOVERY_MODE"] ?? configuration["Discovery:Mode"];
        var mode = string.IsNullOrWhiteSpace(configuredMode) ? requestedMode : configuredMode;

        return mode.ToLowerInvariant() switch
        {
            "livegraph" or "live" => "live",
            "demo" or "config" => "config",
            _ => string.IsNullOrWhiteSpace(requestedMode) ? "config" : requestedMode
        };
    }

    private static bool IsBrokenInheritance(string value)
    {
        return value.Contains("broken", StringComparison.OrdinalIgnoreCase)
            || value.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBroadAccess(string value)
    {
        return value.Contains("Everyone", StringComparison.OrdinalIgnoreCase)
            || value.Contains("All Users", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExternal(string value)
    {
        return value.Contains("external", StringComparison.OrdinalIgnoreCase)
            || value.Contains("#ext#", StringComparison.OrdinalIgnoreCase)
            || value.Contains("guest", StringComparison.OrdinalIgnoreCase);
    }
}
