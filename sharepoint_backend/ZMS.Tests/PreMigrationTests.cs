using ZMS.Application.Contracts;
using ZMS.Application.Services;

namespace ZMS.Tests;

public class PreMigrationTests
{
    [Fact]
    public void CheckEngine_FailsManualReviewItemsWithoutApproval()
    {
        var plan = new MigrationPlan
        {
            PlanId = Guid.NewGuid().ToString("D"),
            SourceEnvironment = "Source",
            TargetEnvironment = "Target",
            Options = MigrationPlanGenerator.DefaultOptions(),
            Checklist = MigrationPlanGenerator.DefaultChecklist(),
            Waves =
            [
                new MigrationPlanWave
                {
                    WaveId = "wave-3",
                    WaveName = "Wave 3 - Restricted Content",
                    RiskLevel = "High",
                    IncludedItems = [new MigrationPlanItem { ItemId = "item-1", Library = "Payroll", MigrationAction = "manual_review", IncludeInMigration = true }]
                }
            ]
        };

        var checks = new PreMigrationCheckEngine().RunChecks(plan);

        Assert.Contains(checks, check => check.Category == "Restricted Content" && check.Status == "failed");
    }

    [Fact]
    public void DecisionService_ReturnsNoGoForRequiredFailures()
    {
        var decision = new GoNoGoDecisionService().Decide(
            [new PreMigrationCheck { Status = "failed", RequiredForGoLive = true }],
            [new WaveValidationResult { Status = "blocked" }]);

        Assert.Equal("no_go", decision);
    }

    [Fact]
    public void ExecutionEstimator_IncreasesDurationForRisk()
    {
        var estimate = new ExecutionEstimator().Estimate(new MigrationPlanWave
        {
            EstimatedFiles = 1000,
            EstimatedStorage = 5L * 1024 * 1024 * 1024,
            IncludedItems = [new MigrationPlanItem { MigrationAction = "manual_review" }]
        });

        Assert.True(estimate.DurationMinutes >= 30);
        Assert.True(estimate.ExpectedWarnings > 0);
    }
}
