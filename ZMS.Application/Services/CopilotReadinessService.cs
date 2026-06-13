using ZMS.Application.Contracts;
using ZMS.Application.Discovery;

namespace ZMS.Application.Services;

public class CopilotReadinessService : ICopilotReadinessService
{
    private readonly IDiscoveryService _discoveryService;

    public CopilotReadinessService(IDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    public async Task<CopilotReadinessResult?> AnalyzeAsync(string discoveryRunId, CancellationToken cancellationToken)
    {
        var discovery = await _discoveryService.GetScanResultAsync(discoveryRunId, cancellationToken);
        return discovery is null ? null : Analyze(discovery);
    }

    public async Task<CopilotReadinessResult?> AnalyzeLatestAsync(CancellationToken cancellationToken)
    {
        var discovery = await _discoveryService.GetLatestCompletedResultAsync(cancellationToken);
        return discovery is null ? null : Analyze(discovery);
    }

    private static CopilotReadinessResult Analyze(DiscoveryScanResult discovery)
    {
        var findings = new List<CopilotFinding>();
        var broadAccessCount = 0;
        var guestCount = 0;
        var brokenInheritanceCount = discovery.Summary.BrokenInheritanceCount;

        foreach (var permission in discovery.PermissionRisks)
        {
            if (permission.Groups.Any(IsBroadGroup))
            {
                broadAccessCount++;
                findings.Add(new CopilotFinding
                {
                    Category = "BroadGroupAccess",
                    Severity = "High",
                    Location = $"{permission.Site}/{permission.LibraryOrFolder}",
                    Description = "Everyone or broad group access was found.",
                    Recommendation = "Replace broad access with least-privilege Microsoft 365 groups."
                });
            }

            if (permission.Users.Any(IsGuestOrExternal))
            {
                guestCount++;
                findings.Add(new CopilotFinding
                {
                    Category = "GuestAccess",
                    Severity = "High",
                    Location = $"{permission.Site}/{permission.LibraryOrFolder}",
                    Description = "Guest or external user access was found.",
                    Recommendation = "Review external access before enabling Copilot over this content."
                });
            }

            if (permission.InheritanceStatus.Contains("broken", StringComparison.OrdinalIgnoreCase)
                || permission.InheritanceStatus.Contains("unique", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new CopilotFinding
                {
                    Category = "BrokenInheritance",
                    Severity = permission.RiskLevel,
                    Location = $"{permission.Site}/{permission.LibraryOrFolder}",
                    Description = "Broken permission inheritance creates oversharing review risk.",
                    Recommendation = "Review unique permissions and normalize access where possible."
                });
            }
        }

        foreach (var risk in discovery.MigrationRisks)
        {
            if (risk.RiskType.Contains("archive", StringComparison.OrdinalIgnoreCase)
                || risk.RiskType.Contains("stale", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new CopilotFinding
                {
                    Category = "StaleContent",
                    Severity = risk.RiskLevel,
                    Location = risk.Path,
                    Description = risk.Description,
                    Recommendation = "Archive, label, or exclude stale content before Copilot rollout."
                });
            }
        }

        var oversharingScore = Clamp(100 - broadAccessCount * 15 - guestCount * 12 - brokenInheritanceCount * 3);
        var staleScore = Clamp(100 - findings.Count(item => item.Category == "StaleContent") * 5);
        var metadataScore = Clamp(100 - discovery.Summary.MissingMetadataIssues * 2);
        var labelScore = 0;
        var overall = Clamp((oversharingScore * 45 + staleScore * 20 + metadataScore * 25 + labelScore * 10) / 100);

        return new CopilotReadinessResult
        {
            DiscoveryRunId = discovery.ScanId,
            OverallScore = overall,
            RiskTier = Tier(overall),
            Summary = "Copilot readiness is calculated from discovered access, inheritance, stale content, and metadata signals. Sensitivity label data is not scanned / unavailable unless supplied by discovery.",
            CategoryScores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Oversharing"] = oversharingScore,
                ["StaleOrOwnerlessContent"] = staleScore,
                ["MetadataGovernance"] = metadataScore,
                ["SensitivityLabels"] = labelScore
            },
            TopFindings = findings.OrderByDescending(item => SeverityRank(item.Severity)).Take(25).ToList(),
            RecommendedActions =
            [
                "Review broad Everyone and guest access before Copilot enablement.",
                "Normalize broken inheritance hotspots.",
                "Archive or exclude stale content from high-value Copilot scopes.",
                "Run sensitivity-label discovery before using label status as a readiness input."
            ]
        };
    }

    private static bool IsBroadGroup(string value)
    {
        return value.Contains("Everyone", StringComparison.OrdinalIgnoreCase)
            || value.Contains("All Users", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGuestOrExternal(string value)
    {
        return value.Contains("guest", StringComparison.OrdinalIgnoreCase)
            || value.Contains("external", StringComparison.OrdinalIgnoreCase)
            || value.Contains("#ext#", StringComparison.OrdinalIgnoreCase);
    }

    private static int Clamp(int value) => Math.Max(0, Math.Min(100, value));

    private static string Tier(int score)
    {
        return score switch
        {
            >= 85 => "Low",
            >= 70 => "Medium",
            >= 50 => "High",
            _ => "Critical"
        };
    }

    private static int SeverityRank(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "warning" => 2,
            "low" => 1,
            _ => 0
        };
    }
}
