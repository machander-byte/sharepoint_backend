using ZMS.Application.Contracts;
using ZMS.Application.Services;

namespace ZMS.Tests;

public class MigrationPlanTests
{
    [Fact]
    public void MigrationPlanGenerator_CreatesDraftPlanFromReadinessAssessment()
    {
        var assessment = new MigrationReadinessAssessment
        {
            AssessmentId = Guid.NewGuid().ToString("D"),
            ScanId = Guid.NewGuid().ToString("D"),
            GeneratedAt = DateTimeOffset.UtcNow,
            MigrationWaves =
            [
                new MigrationWaveSuggestion
                {
                    WaveId = "wave-1",
                    WaveName = "Wave 1 - Low Risk Pilot",
                    RecommendedOrder = 1,
                    IncludedSites = ["HR"],
                    IncludedLibraries = ["Policies"],
                    EstimatedFiles = 100,
                    EstimatedStorage = 1024,
                    ReadinessScore = 95,
                    RiskLevel = "Low"
                }
            ],
            RemediationActions = [new RemediationAction { ActionTitle = "Review metadata", Category = "Metadata" }]
        };

        var plan = new MigrationPlanGenerator().Generate(assessment);

        Assert.Equal(assessment.AssessmentId, plan.AssessmentId);
        Assert.Single(plan.Waves);
        Assert.NotEmpty(plan.Options);
        Assert.NotEmpty(plan.Checklist);
        Assert.Equal("migrate", plan.Waves[0].IncludedItems[0].MigrationAction);
    }

    [Fact]
    public void MigrationPlanValidator_FlagsRestrictedContentWithoutApproval()
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
                    WaveName = "Restricted",
                    ApprovalStatus = "not_started",
                    IncludedItems = [new MigrationPlanItem { Library = "Payroll", MigrationAction = "manual_review", IncludeInMigration = true }]
                }
            ]
        };

        var result = new MigrationPlanValidator().Validate(plan);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("without approval", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MigrationRunbookGenerator_ProducesPlanningRunbookMarkdown()
    {
        var plan = new MigrationPlan
        {
            PlanId = Guid.NewGuid().ToString("D"),
            PlanName = "Draft Plan",
            SourceEnvironment = "Source",
            TargetEnvironment = "Target",
            Waves = [new MigrationPlanWave { WaveName = "Wave 1", IncludedItems = [new MigrationPlanItem { Library = "Policies" }] }],
            Checklist = MigrationPlanGenerator.DefaultChecklist(),
            Approvals = [new MigrationPlanApproval { Role = "Migration Lead" }]
        };

        var runbook = new MigrationRunbookGenerator().Generate(plan, new MigrationPlanValidationResult { IsValid = true });

        Assert.Contains("planning runbook only", runbook.Markdown, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Wave 1", runbook.Markdown);
    }
}
