using ZMS.Application.Contracts;
using ZMS.Application.Services;

namespace ZMS.Tests;

public class MigrationExecutionTests
{
    [Fact]
    public async Task JobFactory_CreatesSimulationJobFromPlanWaves()
    {
        var timeline = new MigrationExecutionTimelineService();
        var factory = new MigrationExecutionJobFactory(new EmptyPreMigrationStorage(), timeline);
        var plan = BuildPlan();

        var job = await factory.CreateAsync(plan, new MigrationExecutionRequest { RequireGoDecision = false }, CancellationToken.None);

        Assert.NotNull(job);
        Assert.Equal("simulation", job.Mode);
        Assert.Equal("created", job.Status);
        Assert.Single(job.Waves);
        Assert.Single(job.Waves[0].Items);
        Assert.Contains(job.Timeline, entry => entry.EventType == "JobCreated");
    }

    [Fact]
    public async Task Orchestrator_StartsAndCompletesSafeSimulation()
    {
        var timeline = new MigrationExecutionTimelineService();
        var factory = new MigrationExecutionJobFactory(new EmptyPreMigrationStorage(), timeline);
        var job = (await factory.CreateAsync(BuildPlan(), new MigrationExecutionRequest { RequireGoDecision = false }, CancellationToken.None))!;
        var orchestrator = new MigrationExecutionOrchestrator(new MigrationSimulationAdapter(), timeline, new MigrationExecutionReportService());

        var completed = orchestrator.Start(job);

        Assert.Equal("completed", completed.Status);
        Assert.Equal(100, completed.Summary.ProgressPercent);
        Assert.Equal(1, completed.Summary.CompletedItems);
        Assert.Contains(completed.Timeline, entry => entry.EventType == "JobCompleted");
    }

    [Fact]
    public void Orchestrator_RetryFailedConvertsUnresolvedFailureToSkipped()
    {
        var timeline = new MigrationExecutionTimelineService();
        var job = new MigrationExecutionJob
        {
            JobId = Guid.NewGuid().ToString("D"),
            PlanId = Guid.NewGuid().ToString("D"),
            Mode = "simulation",
            Status = "failed",
            CreatedAt = DateTimeOffset.UtcNow,
            Waves =
            [
                new MigrationExecutionWave
                {
                    WaveExecutionId = Guid.NewGuid().ToString("D"),
                    WaveName = "Wave 3 - Restricted Content",
                    TotalItems = 1,
                    Items = [new MigrationExecutionItem { ItemExecutionId = Guid.NewGuid().ToString("D"), Library = "Payroll", Action = "remediate_first", Status = "failed", Errors = ["Unresolved"] }]
                }
            ]
        };
        var orchestrator = new MigrationExecutionOrchestrator(new MigrationSimulationAdapter(), timeline, new MigrationExecutionReportService());

        var retried = orchestrator.RetryFailed(job);

        Assert.Equal("skipped", retried.Waves[0].Items[0].Status);
        Assert.Equal("completed_with_warnings", retried.Status);
        Assert.Contains(retried.Timeline, entry => entry.EventType == "ItemRetried");
    }

    private static MigrationPlan BuildPlan() => new()
    {
        PlanId = Guid.NewGuid().ToString("D"),
        SourceEnvironment = "Source",
        TargetEnvironment = "Target",
        Waves =
        [
            new MigrationPlanWave
            {
                WaveId = "wave-1",
                WaveName = "Wave 1 - Low Risk Pilot",
                Order = 1,
                EstimatedFiles = 100,
                EstimatedStorage = 1024,
                IncludedItems =
                [
                    new MigrationPlanItem
                    {
                        ItemId = "item-1",
                        SiteCollection = "HR",
                        Library = "Policies",
                        Path = "/Policies",
                        SourceUrl = "https://source/sites/hr/Policies",
                        TargetUrl = "https://target/sites/hr/Policies",
                        MigrationAction = "migrate",
                        IncludeInMigration = true
                    }
                ]
            }
        ]
    };

    private sealed class EmptyPreMigrationStorage : IPreMigrationStorageService
    {
        public Task SaveValidationAsync(PreMigrationValidationResult result, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<PreMigrationValidationResult?> GetValidationAsync(string validationId, CancellationToken cancellationToken) => Task.FromResult<PreMigrationValidationResult?>(null);
        public Task<PreMigrationValidationResult?> GetLatestValidationAsync(CancellationToken cancellationToken) => Task.FromResult<PreMigrationValidationResult?>(null);
        public Task SaveSimulationAsync(ExecutionSimulationResult result, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<ExecutionSimulationResult?> GetSimulationAsync(string simulationId, CancellationToken cancellationToken) => Task.FromResult<ExecutionSimulationResult?>(null);
        public Task<ExecutionSimulationResult?> GetLatestSimulationAsync(CancellationToken cancellationToken) => Task.FromResult<ExecutionSimulationResult?>(null);
    }
}
