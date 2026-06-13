using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace ZMS.Application.Discovery;

public sealed class DiscoveryStorageService : IDiscoveryStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootDirectory;
    private readonly IDiscoveryExportService _exportService;

    public DiscoveryStorageService(IHostEnvironment hostEnvironment, IDiscoveryExportService exportService)
    {
        _rootDirectory = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "discovery-scans");
        _exportService = exportService;
    }

    public async Task SaveRequestAsync(string scanId, DiscoveryScanRequest request, CancellationToken cancellationToken = default)
    {
        var directory = EnsureScanDirectory(scanId);
        await WriteJsonAsync(Path.Combine(directory, "scan-request.json"), request, cancellationToken);
    }

    public async Task<DiscoveryScanRequest?> GetRequestAsync(string scanId, CancellationToken cancellationToken = default)
    {
        return await ReadJsonAsync<DiscoveryScanRequest>(Path.Combine(GetScanDirectory(scanId), "scan-request.json"), cancellationToken);
    }

    public async Task SaveStatusAsync(DiscoveryScanStatus status, CancellationToken cancellationToken = default)
    {
        var directory = EnsureScanDirectory(status.ScanId);
        await WriteJsonAsync(Path.Combine(directory, "scan-status.json"), status, cancellationToken);
    }

    public async Task<DiscoveryScanStatus?> GetStatusAsync(string scanId, CancellationToken cancellationToken = default)
    {
        return await ReadJsonAsync<DiscoveryScanStatus>(Path.Combine(GetScanDirectory(scanId), "scan-status.json"), cancellationToken);
    }

    public async Task SaveResultAsync(DiscoveryScanResult result, CancellationToken cancellationToken = default)
    {
        var directory = EnsureScanDirectory(result.ScanId);
        await WriteJsonAsync(Path.Combine(directory, "scan-result.json"), result, cancellationToken);

        var exports = new[]
        {
            ("inventory.csv", _exportService.ExportInventoryCsv(result)),
            ("permissions.csv", _exportService.ExportPermissionsCsv(result)),
            ("metadata.csv", _exportService.ExportMetadataCsv(result)),
            ("risks.csv", _exportService.ExportRisksCsv(result))
        };

        foreach (var (fileName, export) in exports)
        {
            await File.WriteAllBytesAsync(Path.Combine(directory, fileName), export.Content, cancellationToken);
        }
    }

    public async Task<DiscoveryScanResult?> GetResultAsync(string scanId, CancellationToken cancellationToken = default)
    {
        return await ReadJsonAsync<DiscoveryScanResult>(Path.Combine(GetScanDirectory(scanId), "scan-result.json"), cancellationToken);
    }

    public async Task<string?> GetLatestCompletedScanIdAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_rootDirectory))
        {
            return null;
        }

        var latest = new List<DiscoveryScanStatus>();
        foreach (var statusPath in Directory.EnumerateFiles(_rootDirectory, "scan-status.json", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await ReadJsonAsync<DiscoveryScanStatus>(statusPath, cancellationToken);
            if (status is not null && string.Equals(status.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                latest.Add(status);
            }
        }

        return latest
            .OrderByDescending(status => status.CompletedAt ?? status.StartedAt)
            .FirstOrDefault()
            ?.ScanId;
    }

    public string GetScanDirectory(string scanId)
    {
        if (!Guid.TryParse(scanId, out _))
        {
            throw new ArgumentException("Invalid discovery scan id.", nameof(scanId));
        }

        return Path.Combine(_rootDirectory, scanId);
    }

    private string EnsureScanDirectory(string scanId)
    {
        var directory = GetScanDirectory(scanId);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }
}
