using System.Text;
using System.Text.Json;

namespace ZMS.Application.Discovery;

public sealed class DiscoveryExportService : IDiscoveryExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public DiscoveryExportResult ExportInventoryCsv(DiscoveryScanResult result)
    {
        var rows = new List<string[]>
        {
            new[]
            {
                "siteCollection",
                "subsite",
                "library",
                "itemType",
                "path",
                "fileCount",
                "sizeBytes",
                "metadataCount",
                "permissionStatus",
                "riskLevel",
                "readinessStatus"
            }
        };

        rows.AddRange(result.InventoryItems.Select(item => new[]
        {
            item.SiteCollection,
            item.Subsite,
            item.Library,
            item.ItemType,
            item.Path,
            item.FileCount.ToString(),
            item.SizeBytes.ToString(),
            item.MetadataCount.ToString(),
            item.PermissionStatus,
            item.RiskLevel,
            item.ReadinessStatus
        }));

        return Csv($"{result.ScanId}-inventory.csv", rows);
    }

    public DiscoveryExportResult ExportPermissionsCsv(DiscoveryScanResult result)
    {
        var rows = new List<string[]>
        {
            new[] { "site", "libraryOrFolder", "inheritanceStatus", "groups", "users", "accessLevels", "riskLevel", "recommendedAction" }
        };

        rows.AddRange(result.PermissionRisks.Select(item => new[]
        {
            item.Site,
            item.LibraryOrFolder,
            item.InheritanceStatus,
            string.Join("; ", item.Groups),
            string.Join("; ", item.Users),
            string.Join("; ", item.AccessLevels),
            item.RiskLevel,
            item.RecommendedAction
        }));

        return Csv($"{result.ScanId}-permissions.csv", rows);
    }

    public DiscoveryExportResult ExportMetadataCsv(DiscoveryScanResult result)
    {
        var rows = new List<string[]>
        {
            new[] { "site", "library", "fieldName", "fieldType", "required", "missingValueCount", "mappedTargetField", "mappingRisk" }
        };

        rows.AddRange(result.MetadataFindings.Select(item => new[]
        {
            item.Site,
            item.Library,
            item.FieldName,
            item.FieldType,
            item.Required.ToString(),
            item.MissingValueCount.ToString(),
            item.MappedTargetField,
            item.MappingRisk
        }));

        return Csv($"{result.ScanId}-metadata.csv", rows);
    }

    public DiscoveryExportResult ExportRisksCsv(DiscoveryScanResult result)
    {
        var rows = new List<string[]>
        {
            new[] { "riskType", "site", "libraryOrPath", "path", "riskLevel", "description", "recommendedAction" }
        };

        rows.AddRange(result.MigrationRisks.Select(item => new[]
        {
            item.RiskType,
            item.Site,
            item.LibraryOrPath,
            item.Path,
            item.RiskLevel,
            item.Description,
            item.RecommendedAction
        }));

        return Csv($"{result.ScanId}-risks.csv", rows);
    }

    public DiscoveryExportResult ExportJson(DiscoveryScanResult result)
    {
        return new DiscoveryExportResult
        {
            FileName = $"{result.ScanId}-discovery-results.json",
            ContentType = "application/json",
            Content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result, JsonOptions))
        };
    }

    private static DiscoveryExportResult Csv(string fileName, IEnumerable<string[]> rows)
    {
        var builder = new StringBuilder();
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(EscapeCsv)));
        }

        return new DiscoveryExportResult
        {
            FileName = fileName,
            ContentType = "text/csv",
            Content = Encoding.UTF8.GetBytes(builder.ToString())
        };
    }

    private static string EscapeCsv(string? value)
    {
        value ??= string.Empty;
        var mustQuote = value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');
        var escaped = value.Replace("\"", "\"\"");
        return mustQuote ? $"\"{escaped}\"" : escaped;
    }
}
