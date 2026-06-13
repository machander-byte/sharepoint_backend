using ZMS.Application.Contracts;
using ZMS.Application.Discovery;
using ZMS.Application.Services;

namespace ZMS.Tests;

public class ReadinessAnalysisTests
{
    [Fact]
    public void RiskScoringService_AppliesBlockerAndCategoryPenalties()
    {
        var scoring = new RiskScoringService();

        var result = scoring.Score(
        [
            new ReadinessRiskFinding { Category = "Permissions", Severity = "High", MigrationBlocker = true },
            new ReadinessRiskFinding { Category = "Metadata", Severity = "Medium" },
            new ReadinessRiskFinding { Category = "Path Length", Severity = "High" },
            new ReadinessRiskFinding { Category = "Archived Content", Severity = "Medium" }
        ]);

        Assert.Equal(79, result.Score);
        Assert.Equal("Moderate", result.RiskLevel);
        Assert.Equal(8, result.Breakdown.BlockerPenalty);
    }

    [Fact]
    public void RemediationPlanner_GroupsFindingsByCategory()
    {
        var planner = new RemediationPlanner();

        var actions = planner.BuildPlan(
        [
            new ReadinessRiskFinding { Category = "Permissions", Severity = "High", AffectedLocation = "Payroll", MigrationBlocker = true },
            new ReadinessRiskFinding { Category = "Permissions", Severity = "Medium", AffectedLocation = "Audit" },
            new ReadinessRiskFinding { Category = "Metadata", Severity = "Medium", AffectedLocation = "Contracts" }
        ]);

        Assert.Equal(2, actions.Count);
        Assert.Contains(actions, action => action.Category == "Permissions" && action.Priority == "High");
        Assert.Contains(actions, action => action.Category == "Metadata" && action.OwnerRole == "Information Architect");
    }

    [Fact]
    public void MigrationWavePlanner_CreatesFourSuggestedWaves()
    {
        var planner = new MigrationWavePlanner();
        var scan = new DiscoveryScanResult
        {
            ScanId = Guid.NewGuid().ToString("D"),
            Status = "completed",
            InventoryItems =
            [
                new DiscoveredInventoryItem { Id = "1", SiteCollection = "HR", Library = "Policies Archive", ItemType = "Library", FileCount = 10 },
                new DiscoveredInventoryItem { Id = "2", SiteCollection = "HR", Library = "Payroll Confidential", ItemType = "Library", FileCount = 5 }
            ]
        };

        var waves = planner.BuildWaves(
            scan,
            [new ReadinessRiskFinding { Category = "Permissions", Severity = "High", AffectedSite = "HR", AffectedLibrary = "Payroll Confidential", AffectedLocation = "Payroll Confidential", MigrationBlocker = true }],
            [new RemediationAction { Category = "Permissions", ActionTitle = "Review unique permissions before migration", AffectedLocations = ["Payroll Confidential"] }]);

        Assert.Equal(4, waves.Count);
        Assert.Contains(waves, wave => wave.WaveName.Contains("Low Risk Pilot", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(waves, wave => wave.WaveName.Contains("Restricted Content", StringComparison.OrdinalIgnoreCase));
    }
}
