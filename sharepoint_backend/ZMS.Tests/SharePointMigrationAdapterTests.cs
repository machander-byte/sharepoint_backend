using Microsoft.Extensions.Configuration;
using ZMS.Application.Contracts;
using ZMS.Application.Services;

namespace ZMS.Tests;

public class SharePointMigrationAdapterTests
{
    [Fact]
    public async Task CapabilityValidation_DisabledLiveMigration_IsNotReady()
    {
        Environment.SetEnvironmentVariable("ZMS_ENABLE_LIVE_MIGRATION", null);
        var result = await new SharePointMigrationAdapter().ValidateCapabilitiesAsync(new SharePointMigrationCapabilityRequest
        {
            SourceSiteUrl = "https://tenant.sharepoint.com/sites/source",
            TargetSiteUrl = "https://tenant.sharepoint.com/sites/target",
            ClientId = "client-id",
            Mode = "validate_only"
        }, CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.False(result.Capabilities.CanUploadFiles);
        Assert.Contains(result.Checks, c => c.CheckId == "live-flag" && c.Status == "failed");
    }

    [Fact]
    public async Task TransferPreview_ClassifiesManualReviewAsBlocked()
    {
        var job = new MigrationExecutionJob
        {
            JobId = Guid.NewGuid().ToString("D"),
            Waves =
            [
                new MigrationExecutionWave
                {
                    WaveExecutionId = "wave-exec",
                    SourceWaveId = "wave-1",
                    WaveName = "Wave 1",
                    Items =
                    [
                        new MigrationExecutionItem { ItemExecutionId = "item-1", Action = "migrate", Status = "completed", SimulatedSourceUrl = "https://source/doc.docx", SimulatedTargetUrl = "https://target/doc.docx" },
                        new MigrationExecutionItem { ItemExecutionId = "item-2", Action = "manual_review", Status = "skipped", SimulatedSourceUrl = "https://source/payroll.docx", SimulatedTargetUrl = "https://target/payroll.docx" }
                    ]
                }
            ]
        };

        var preview = await new SharePointMigrationAdapter().BuildTransferPreviewAsync(job, CancellationToken.None);

        Assert.Equal(2, preview.TotalItems);
        Assert.Equal(1, preview.EligibleItems);
        Assert.Equal(1, preview.BlockedItems);
    }

    [Fact]
    public async Task SafetyGate_BlocksWrongConfirmation()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ZMS_ENABLE_LIVE_MIGRATION"] = "true" }).Build();
        var gate = new LivePilotSafetyGate(config, new EmptyPreMigrationStorage());
        var checks = await gate.EvaluateAsync(new MigrationExecutionJob
        {
            PlanId = "plan-1",
            Mode = "dry_run",
            Waves = [new MigrationExecutionWave { SourceWaveId = "wave-1" }]
        }, new LivePilotMigrationRequest
        {
            Mode = "live_pilot",
            ConfirmationText = "WRONG",
            SelectedWaveId = "wave-1",
            MaxFiles = 1,
            TargetSiteUrl = "https://tenant.sharepoint.com/sites/target",
            TargetLibrary = "Pilot",
            PreservePermissions = false,
            OverwriteExisting = false
        }, CancellationToken.None);

        Assert.Contains(checks, c => c.CheckId == "confirmation" && c.Status == "failed");
    }

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
