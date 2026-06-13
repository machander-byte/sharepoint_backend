namespace ZMS.Application.Discovery;

public sealed class PermissionRiskAnalyzer : IPermissionRiskAnalyzer
{
    public IReadOnlyCollection<PermissionRiskFinding> Analyze(DiscoveryScanResult result)
    {
        var findings = new List<PermissionRiskFinding>();

        foreach (var site in result.SiteCollections)
        {
            foreach (var permission in site.Permissions)
            {
                AddFinding(findings, permission);
            }

            foreach (var library in site.Libraries)
            {
                foreach (var permission in library.Permissions)
                {
                    AddFinding(findings, permission);
                }

                if (library.BrokenInheritance && library.Permissions.Count == 0)
                {
                    AddFinding(findings, new DiscoveredPermissionEntry
                    {
                        Site = site.Title,
                        LibraryOrFolder = library.Title,
                        InheritanceStatus = "Broken",
                        RiskLevel = "High",
                        RecommendedAction = "Validate unique permissions and map target groups before migration."
                    });
                }
            }
        }

        if (findings.Count == 0)
        {
            foreach (var item in result.InventoryItems)
            {
                if (IsInherited(item.PermissionStatus) && IsLow(item.RiskLevel))
                {
                    continue;
                }

                AddFinding(findings, new DiscoveredPermissionEntry
                {
                    Site = item.SiteCollection,
                    LibraryOrFolder = string.IsNullOrWhiteSpace(item.Path) ? item.Library : item.Path,
                    InheritanceStatus = item.PermissionStatus.Contains("broken", StringComparison.OrdinalIgnoreCase)
                        || item.PermissionStatus.Contains("unique", StringComparison.OrdinalIgnoreCase)
                        || item.PermissionStatus.Contains("restricted", StringComparison.OrdinalIgnoreCase)
                            ? "Broken"
                            : item.PermissionStatus,
                    RiskLevel = NormalizePermissionRisk(item.PermissionStatus, item.RiskLevel, [], []),
                    RecommendedAction = Recommend(item.PermissionStatus, item.RiskLevel, [], [])
                });
            }
        }

        return findings
            .GroupBy(item => $"{item.Site}|{item.LibraryOrFolder}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => RiskRank(item.RiskLevel)).First())
            .OrderByDescending(item => RiskRank(item.RiskLevel))
            .ThenBy(item => item.Site)
            .ThenBy(item => item.LibraryOrFolder)
            .ToList();
    }

    private static void AddFinding(List<PermissionRiskFinding> findings, DiscoveredPermissionEntry permission)
    {
        var riskLevel = NormalizePermissionRisk(
            permission.InheritanceStatus,
            permission.RiskLevel,
            permission.Groups,
            permission.Users);

        if (IsLow(riskLevel) && IsInherited(permission.InheritanceStatus))
        {
            return;
        }

        findings.Add(new PermissionRiskFinding
        {
            Id = StableId(permission.Site, permission.LibraryOrFolder, "permission"),
            Site = permission.Site,
            LibraryOrFolder = permission.LibraryOrFolder,
            InheritanceStatus = permission.InheritanceStatus,
            Groups = [.. permission.Groups],
            Users = [.. permission.Users],
            AccessLevels = [.. permission.AccessLevels],
            RiskLevel = riskLevel,
            RecommendedAction = string.IsNullOrWhiteSpace(permission.RecommendedAction)
                ? Recommend(permission.InheritanceStatus, riskLevel, permission.Groups, permission.Users)
                : permission.RecommendedAction
        });
    }

    private static string NormalizePermissionRisk(
        string inheritanceStatus,
        string currentRisk,
        IReadOnlyCollection<string> groups,
        IReadOnlyCollection<string> users)
    {
        if (inheritanceStatus.Contains("broken", StringComparison.OrdinalIgnoreCase)
            || inheritanceStatus.Contains("unique", StringComparison.OrdinalIgnoreCase)
            || inheritanceStatus.Contains("restricted", StringComparison.OrdinalIgnoreCase)
            || inheritanceStatus.Contains("confidential", StringComparison.OrdinalIgnoreCase)
            || currentRisk.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            || currentRisk.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        if (groups.Count > 3
            || users.Any(user => user.Contains("external", StringComparison.OrdinalIgnoreCase)
                || user.Contains("#ext#", StringComparison.OrdinalIgnoreCase)
                || user.Contains("unknown", StringComparison.OrdinalIgnoreCase))
            || currentRisk.Equals("Medium", StringComparison.OrdinalIgnoreCase))
        {
            return "Medium";
        }

        return "Low";
    }

    private static string Recommend(
        string inheritanceStatus,
        string riskLevel,
        IReadOnlyCollection<string> groups,
        IReadOnlyCollection<string> users)
    {
        if (inheritanceStatus.Contains("broken", StringComparison.OrdinalIgnoreCase)
            || inheritanceStatus.Contains("unique", StringComparison.OrdinalIgnoreCase))
        {
            return "Validate unique permissions and map target groups before migration.";
        }

        if (users.Any(user => user.Contains("external", StringComparison.OrdinalIgnoreCase)
            || user.Contains("#ext#", StringComparison.OrdinalIgnoreCase)))
        {
            return "Review guest and external users before migration.";
        }

        if (groups.Count > 3)
        {
            return "Consolidate or validate group membership before migration.";
        }

        return riskLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase)
            ? "Review access assignments before migration."
            : "No action required";
    }

    private static bool IsInherited(string value)
    {
        return value.Equals("Inherited", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLow(string value)
    {
        return value.Equals("Low", StringComparison.OrdinalIgnoreCase);
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

    private static string StableId(string site, string path, string suffix)
    {
        var value = $"{site}-{path}-{suffix}";
        var chars = value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        return string.Join(string.Empty, chars).Replace("--", "-", StringComparison.Ordinal).Trim('-');
    }
}
