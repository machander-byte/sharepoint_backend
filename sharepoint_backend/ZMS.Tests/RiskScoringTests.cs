using ZMS.Application.Discovery;

namespace ZMS.Tests;

public class RiskScoringTests
{
    [Fact]
    public void MigrationRiskAnalyzer_CalculatesLowerReadinessForHighRisks()
    {
        var analyzer = new MigrationRiskAnalyzer();

        var score = analyzer.CalculateReadinessScore(
            [
                new PermissionRiskFinding { RiskLevel = "High", Site = "Finance", LibraryOrFolder = "Contracts" }
            ],
            [
                new MetadataFinding { MappingRisk = "High", MissingValueCount = 12 }
            ],
            [
                new MigrationRiskFinding { RiskType = "Long Paths", RiskLevel = "High" },
                new MigrationRiskFinding { RiskType = "Large Files", RiskLevel = "Medium" }
            ]);

        Assert.InRange(score, 1, 95);
    }
}
