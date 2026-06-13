namespace ZMS.Application.Discovery;

public sealed class MigrationRiskAnalyzer : IMigrationRiskAnalyzer
{
    private const int MediumPathRiskLength = 250;
    private const int HighPathRiskLength = 350;
    private const long MediumFileRiskBytes = 100L * 1024L * 1024L;
    private const long HighFileRiskBytes = 500L * 1024L * 1024L;

    public IReadOnlyCollection<MigrationRiskFinding> Analyze(DiscoveryScanResult result)
    {
        var findings = new List<MigrationRiskFinding>();

        foreach (var configuredRisk in result.SiteCollections.SelectMany(site => site.ConfiguredRisks))
        {
            findings.Add(configuredRisk);
        }

        foreach (var item in result.InventoryItems)
        {
            AddPathRisk(findings, item.SiteCollection, item.Library, item.Path);

            if (item.Path.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new MigrationRiskFinding
                {
                    Id = StableId("duplicate", item.SiteCollection, item.Path),
                    RiskType = "Duplicate Content",
                    Site = item.SiteCollection,
                    LibraryOrPath = item.Library,
                    Path = item.Path,
                    RiskLevel = "Medium",
                    Description = "Duplicate folder or file naming indicator found.",
                    RecommendedAction = "Review duplicate candidates before migration waves."
                });
            }

            if (item.Path.Contains("archive", StringComparison.OrdinalIgnoreCase)
                || item.Path.Contains("archived", StringComparison.OrdinalIgnoreCase)
                || item.ReadinessStatus.Contains("archive", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new MigrationRiskFinding
                {
                    Id = StableId("archive", item.SiteCollection, item.Path),
                    RiskType = "Archived Content",
                    Site = item.SiteCollection,
                    LibraryOrPath = item.Library,
                    Path = item.Path,
                    RiskLevel = "Medium",
                    Description = "Archived content indicator found.",
                    RecommendedAction = "Decide whether archived content should migrate or remain excluded."
                });
            }
        }

        foreach (var site in result.SiteCollections)
        {
            foreach (var library in site.Libraries)
            {
                foreach (var folder in library.Folders)
                {
                    AddPathRisk(findings, site.Title, library.Title, folder.Path, folder.LongPathRisk);

                    if (folder.Archived)
                    {
                        findings.Add(new MigrationRiskFinding
                        {
                            Id = StableId("archive", site.Title, folder.Path),
                            RiskType = "Archived Content",
                            Site = site.Title,
                            LibraryOrPath = library.Title,
                            Path = folder.Path,
                            RiskLevel = "Medium",
                            Description = "Folder is marked as archived.",
                            RecommendedAction = "Confirm archive migration or exclusion rule."
                        });
                    }

                    if (folder.DuplicateIndicator)
                    {
                        findings.Add(new MigrationRiskFinding
                        {
                            Id = StableId("duplicate", site.Title, folder.Path),
                            RiskType = "Duplicate Content",
                            Site = site.Title,
                            LibraryOrPath = library.Title,
                            Path = folder.Path,
                            RiskLevel = "Medium",
                            Description = "Folder is marked as a duplicate content indicator.",
                            RecommendedAction = "Review folder structure before migration."
                        });
                    }
                }

                foreach (var file in library.Files)
                {
                    AddPathRisk(findings, site.Title, library.Title, file.Path, file.LongPathRisk);
                    AddFileRisk(findings, site.Title, library.Title, file.Path, file.SizeBytes);

                    if (file.DuplicateIndicator)
                    {
                        findings.Add(new MigrationRiskFinding
                        {
                            Id = StableId("duplicate-file", site.Title, file.Path),
                            RiskType = "Duplicate Content",
                            Site = site.Title,
                            LibraryOrPath = library.Title,
                            Path = file.Path,
                            RiskLevel = "Medium",
                            Description = "File is marked as a duplicate candidate.",
                            RecommendedAction = "Review duplicate file candidates before migration."
                        });
                    }
                }
            }
        }

        foreach (var permissionRisk in result.PermissionRisks)
        {
            findings.Add(new MigrationRiskFinding
            {
                Id = StableId("permission", permissionRisk.Site, permissionRisk.LibraryOrFolder),
                RiskType = permissionRisk.InheritanceStatus.Contains("broken", StringComparison.OrdinalIgnoreCase)
                    ? "Broken Permissions"
                    : "Restricted Content",
                Site = permissionRisk.Site,
                LibraryOrPath = permissionRisk.LibraryOrFolder,
                Path = permissionRisk.LibraryOrFolder,
                RiskLevel = permissionRisk.RiskLevel,
                Description = "Permission mapping requires validation before migration.",
                RecommendedAction = permissionRisk.RecommendedAction
            });
        }

        foreach (var metadataIssue in result.MetadataFindings.Where(item => item.MissingValueCount > 0 || IsHigh(item.MappingRisk)))
        {
            findings.Add(new MigrationRiskFinding
            {
                Id = StableId("metadata", metadataIssue.Site, $"{metadataIssue.Library}-{metadataIssue.FieldName}"),
                RiskType = "Missing Metadata",
                Site = metadataIssue.Site,
                LibraryOrPath = metadataIssue.Library,
                Path = metadataIssue.FieldName,
                RiskLevel = metadataIssue.MappingRisk,
                Description = $"{metadataIssue.FieldName} has {metadataIssue.MissingValueCount} missing or risky values.",
                RecommendedAction = "Clean or map metadata before migration."
            });
        }

        return findings
            .GroupBy(item => $"{item.RiskType}|{item.Site}|{item.LibraryOrPath}|{item.Path}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => RiskRank(item.RiskLevel)).First())
            .OrderByDescending(item => RiskRank(item.RiskLevel))
            .ThenBy(item => item.RiskType)
            .ThenBy(item => item.Site)
            .ToList();
    }

    public int CalculateReadinessScore(
        IReadOnlyCollection<PermissionRiskFinding> permissionRisks,
        IReadOnlyCollection<MetadataFinding> metadataFindings,
        IReadOnlyCollection<MigrationRiskFinding> migrationRisks)
    {
        var highPermissionIssues = permissionRisks.Count(item => IsHigh(item.RiskLevel));
        var longPathRisks = migrationRisks.Count(item => item.RiskType.Equals("Long Paths", StringComparison.OrdinalIgnoreCase));
        var largeFileRisks = migrationRisks.Count(item => item.RiskType.Equals("Large Files", StringComparison.OrdinalIgnoreCase));
        var metadataIssues = metadataFindings.Count(item => item.MissingValueCount > 0 || IsHigh(item.MappingRisk));

        var score = 100
            - (3 * highPermissionIssues)
            - (2 * longPathRisks)
            - metadataIssues
            - (2 * largeFileRisks);

        return Math.Max(0, score);
    }

    private static void AddPathRisk(
        List<MigrationRiskFinding> findings,
        string site,
        string library,
        string path,
        bool forceRisk = false)
    {
        var pathLength = path.Length;
        if (!forceRisk && pathLength <= MediumPathRiskLength)
        {
            return;
        }

        findings.Add(new MigrationRiskFinding
        {
            Id = StableId("long-path", site, path),
            RiskType = "Long Paths",
            Site = site,
            LibraryOrPath = library,
            Path = path,
            RiskLevel = pathLength > HighPathRiskLength || forceRisk && pathLength > MediumPathRiskLength ? "High" : "Medium",
            Description = $"Path length is {pathLength} characters.",
            RecommendedAction = "Shorten or flatten folder paths before migration."
        });
    }

    private static void AddFileRisk(List<MigrationRiskFinding> findings, string site, string library, string path, long sizeBytes)
    {
        if (sizeBytes <= MediumFileRiskBytes)
        {
            return;
        }

        findings.Add(new MigrationRiskFinding
        {
            Id = StableId("large-file", site, path),
            RiskType = "Large Files",
            Site = site,
            LibraryOrPath = library,
            Path = path,
            RiskLevel = sizeBytes > HighFileRiskBytes ? "High" : "Medium",
            Description = $"File size is {sizeBytes} bytes.",
            RecommendedAction = "Review large file migration handling and throttling exceptions."
        });
    }

    private static bool IsHigh(string riskLevel)
    {
        return riskLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || riskLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsElevated(string riskLevel)
    {
        return riskLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase)
            || IsHigh(riskLevel);
    }

    private static int RiskRank(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }

    private static string StableId(string prefix, string site, string value)
    {
        return Slug($"{prefix}-{site}-{value}");
    }

    private static string Slug(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join(string.Empty, chars).Replace("--", "-", StringComparison.Ordinal).Trim('-');
    }
}
