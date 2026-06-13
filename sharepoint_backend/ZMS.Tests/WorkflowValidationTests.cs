using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;
using ZMS.Application.Discovery;
using ZMS.Application.Services;
using ZMS.Core.Models;

namespace ZMS.Tests;

public class WorkflowValidationTests
{
    [Fact]
    public async Task RunFullChain_WithSampleFallback_CreatesAllMajorArtifacts()
    {
        var storage = new InMemoryWorkflowStorage();
        var service = new WorkflowValidationService(
            new FakeDiscoveryService(),
            new FakeReadinessService(),
            new FakeMigrationPlanService(),
            new FakePreMigrationService(),
            new FakeExecutionService(),
            new FakePreviewService(),
            new FakePilotReports(),
            storage,
            new TestEnvironment { ContentRootPath = FindBackendRoot() });

        var response = await service.RunFullChainAsync(new WorkflowValidationRequest { UseSampleFallback = true }, CancellationToken.None);
        var latest = await storage.GetLatestAsync(CancellationToken.None);

        Assert.Equal("completed", response.Status);
        Assert.NotEmpty(response.Summary.ScanId);
        Assert.NotEmpty(response.Summary.AssessmentId);
        Assert.NotEmpty(response.Summary.PlanId);
        Assert.NotEmpty(response.Summary.ValidationId);
        Assert.NotEmpty(response.Summary.SimulationId);
        Assert.NotEmpty(response.Summary.ExecutionJobId);
        Assert.NotEmpty(response.Summary.PreviewId);
        Assert.NotNull(latest);
        Assert.Contains(latest!.Artifacts, artifact => artifact.ArtifactType == "transfer_preview");
    }

    [Fact]
    public void WorkflowReport_IncludesSafetyStatement()
    {
        var report = new WorkflowValidationReportService().BuildMarkdown(new WorkflowValidationRun
        {
            WorkflowRunId = Guid.NewGuid().ToString("D"),
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            Status = "completed",
            OverallResult = "pass"
        });

        Assert.Contains("does not perform real SharePoint migration", report, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeDiscoveryService : IDiscoveryService
    {
        private DiscoveryScanResult? _scan;
        public Task<DiscoveryScanResult?> GetLatestCompletedResultAsync(CancellationToken cancellationToken) => Task.FromResult(_scan);
        public Task<DiscoveryScanResult?> GetScanResultAsync(string scanId, CancellationToken cancellationToken) => Task.FromResult(_scan);
        public Task<DiscoveryImportResponse> ImportResultAsync(DiscoveryScanResult scanResult, CancellationToken cancellationToken)
        {
            scanResult.ScanId = Guid.NewGuid().ToString("D");
            scanResult.Status = "completed";
            _scan = scanResult;
            return Task.FromResult(new DiscoveryImportResponse { ScanId = scanResult.ScanId, Status = "completed", Summary = scanResult.Summary });
        }
        public Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(Guid sourceConnectionId, string userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<SiteInfo>>([]);
        public Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(Guid sourceConnectionId, string sourceLocation, string userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<LibraryInfo>>([]);
        public Task<ZMS.Core.Models.DiscoverySummary> GetSummaryAsync(Guid sourceConnectionId, string sourceLocation, string? libraryName, string userId, CancellationToken cancellationToken) => Task.FromResult(new ZMS.Core.Models.DiscoverySummary());
        public Task<StartDiscoveryScanResponse> StartScanAsync(DiscoveryScanRequest request, CancellationToken cancellationToken) => Task.FromResult(new StartDiscoveryScanResponse());
        public Task<DiscoveryScanStatus?> GetScanStatusAsync(string scanId, CancellationToken cancellationToken) => Task.FromResult<DiscoveryScanStatus?>(null);
        public Task<IReadOnlyCollection<DiscoveredInventoryItem>?> GetInventoryAsync(string scanId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<DiscoveredInventoryItem>?>([]);
        public Task<IReadOnlyCollection<PermissionRiskFinding>?> GetPermissionRisksAsync(string scanId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<PermissionRiskFinding>?>([]);
        public Task<IReadOnlyCollection<MetadataFinding>?> GetMetadataFindingsAsync(string scanId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<MetadataFinding>?>([]);
        public Task<IReadOnlyCollection<MigrationRiskFinding>?> GetMigrationRisksAsync(string scanId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<MigrationRiskFinding>?>([]);
        public Task<DiscoveryExportResult?> ExportAsync(string scanId, string exportType, CancellationToken cancellationToken) => Task.FromResult<DiscoveryExportResult?>(new DiscoveryExportResult { Content = [1] });
        public Task<DiscoveryImportResponse> ImportResultFromFolderAsync(string folderPath, CancellationToken cancellationToken) => throw new NotImplementedException();
    }

    private sealed class FakeReadinessService : IReadinessAnalysisService
    {
        private MigrationReadinessAssessment? _assessment;
        public Task<ReadinessAnalyzeResponse?> AnalyzeAsync(string scanId, CancellationToken cancellationToken)
        {
            _assessment = new MigrationReadinessAssessment { AssessmentId = Guid.NewGuid().ToString("D"), ScanId = scanId, Status = "completed", RiskLevel = "Medium", GeneratedAt = DateTimeOffset.UtcNow };
            return Task.FromResult<ReadinessAnalyzeResponse?>(new ReadinessAnalyzeResponse { AssessmentId = _assessment.AssessmentId, ScanId = scanId, Status = "completed", RiskLevel = "Medium" });
        }
        public Task<MigrationReadinessAssessment?> GetAssessmentAsync(string assessmentId, CancellationToken cancellationToken) => Task.FromResult(_assessment);
        public Task<MigrationReadinessAssessment?> GetLatestAssessmentAsync(CancellationToken cancellationToken) => Task.FromResult(_assessment);
        public Task<IReadOnlyCollection<RemediationAction>?> GetRemediationPlanAsync(string assessmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<RemediationAction>?>([]);
        public Task<IReadOnlyCollection<MigrationWaveSuggestion>?> GetMigrationWavesAsync(string assessmentId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<MigrationWaveSuggestion>?>([]);
        public Task<ReadinessExportResult?> ExportAsync(string assessmentId, string exportType, CancellationToken cancellationToken) => Task.FromResult<ReadinessExportResult?>(new ReadinessExportResult { Content = [1] });
    }

    private sealed class FakeMigrationPlanService : IMigrationPlanService
    {
        private MigrationPlan? _plan;
        public Task<CreateMigrationPlanResponse?> CreateFromAssessmentAsync(string assessmentId, CancellationToken cancellationToken)
        {
            _plan = new MigrationPlan { PlanId = Guid.NewGuid().ToString("D"), AssessmentId = assessmentId, SourceEnvironment = "Source", TargetEnvironment = "Target", Waves = [new MigrationPlanWave { WaveId = "wave-1", WaveName = "Wave 1", IncludedItems = [new MigrationPlanItem { ItemId = "item-1", Library = "Docs", MigrationAction = "migrate", IncludeInMigration = true }] }] };
            return Task.FromResult<CreateMigrationPlanResponse?>(new CreateMigrationPlanResponse { PlanId = _plan.PlanId, AssessmentId = assessmentId, Status = "draft" });
        }
        public Task<MigrationPlan?> GetAsync(string planId, CancellationToken cancellationToken) => Task.FromResult(_plan);
        public Task<MigrationPlan?> GetLatestAsync(CancellationToken cancellationToken) => Task.FromResult(_plan);
        public Task<MigrationPlan?> UpdateAsync(string planId, MigrationPlan plan, CancellationToken cancellationToken) => Task.FromResult<MigrationPlan?>(plan);
        public Task<MigrationPlanValidationResult?> ValidateAsync(string planId, CancellationToken cancellationToken) => Task.FromResult<MigrationPlanValidationResult?>(new MigrationPlanValidationResult { IsValid = true });
        public Task<MigrationRunbook?> GenerateRunbookAsync(string planId, CancellationToken cancellationToken) => Task.FromResult<MigrationRunbook?>(new MigrationRunbook { PlanId = planId, Markdown = "# Runbook", FileName = "migration-runbook.md" });
        public Task<MigrationPlanExportResult?> ExportAsync(string planId, string exportType, CancellationToken cancellationToken) => Task.FromResult<MigrationPlanExportResult?>(new MigrationPlanExportResult { Content = [1] });
    }

    private sealed class FakePreMigrationService : IPreMigrationValidationService
    {
        public Task<PreMigrationValidationResponse?> ValidateAsync(string planId, CancellationToken cancellationToken) => Task.FromResult<PreMigrationValidationResponse?>(new PreMigrationValidationResponse { PlanId = planId, ValidationId = Guid.NewGuid().ToString("D"), Decision = "no_go" });
        public Task<ExecutionSimulationResponse?> SimulateAsync(string planId, CancellationToken cancellationToken) => Task.FromResult<ExecutionSimulationResponse?>(new ExecutionSimulationResponse { PlanId = planId, SimulationId = Guid.NewGuid().ToString("D"), Status = "completed" });
        public Task<PreMigrationValidationResult?> GetValidationAsync(string validationId, CancellationToken cancellationToken) => Task.FromResult<PreMigrationValidationResult?>(null);
        public Task<PreMigrationValidationResult?> GetLatestValidationAsync(CancellationToken cancellationToken) => Task.FromResult<PreMigrationValidationResult?>(null);
        public Task<ExecutionSimulationResult?> GetSimulationAsync(string simulationId, CancellationToken cancellationToken) => Task.FromResult<ExecutionSimulationResult?>(null);
        public Task<ExecutionSimulationResult?> GetLatestSimulationAsync(CancellationToken cancellationToken) => Task.FromResult<ExecutionSimulationResult?>(null);
        public Task<PreMigrationExportResult?> ExportValidationAsync(string validationId, string exportType, CancellationToken cancellationToken) => Task.FromResult<PreMigrationExportResult?>(new PreMigrationExportResult { Content = [1] });
        public Task<PreMigrationExportResult?> ExportSimulationAsync(string simulationId, string exportType, CancellationToken cancellationToken) => Task.FromResult<PreMigrationExportResult?>(new PreMigrationExportResult { Content = [1] });
    }

    private sealed class FakeExecutionService : IMigrationExecutionService
    {
        public Task<CreateMigrationExecutionJobResponse?> CreateFromPlanAsync(string planId, MigrationExecutionRequest request, CancellationToken cancellationToken) => Task.FromResult<CreateMigrationExecutionJobResponse?>(new CreateMigrationExecutionJobResponse { PlanId = planId, JobId = Guid.NewGuid().ToString("D"), Status = "created", Mode = "simulation" });
        public Task<MigrationExecutionJob?> StartAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionJob?>(new MigrationExecutionJob { JobId = jobId, Status = "completed_with_warnings" });
        public Task<MigrationExecutionJob?> GetAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionJob?>(null);
        public Task<MigrationExecutionJob?> GetLatestAsync(CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionJob?>(null);
        public Task<IReadOnlyCollection<MigrationExecutionJob>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<MigrationExecutionJob>>([]);
        public Task<MigrationExecutionJob?> PauseAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionJob?>(null);
        public Task<MigrationExecutionJob?> ResumeAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionJob?>(null);
        public Task<MigrationExecutionJob?> CancelAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionJob?>(null);
        public Task<MigrationExecutionJob?> RetryFailedAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionJob?>(null);
        public Task<IReadOnlyCollection<MigrationExecutionTimelineEvent>?> GetTimelineAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyCollection<MigrationExecutionTimelineEvent>?>([]);
        public Task<MigrationExecutionExportResult?> ExportAsync(string jobId, string exportType, CancellationToken cancellationToken) => Task.FromResult<MigrationExecutionExportResult?>(new MigrationExecutionExportResult { Content = [1] });
    }

    private sealed class FakePreviewService : IMigrationTransferPreviewService
    {
        public Task<MigrationTransferPreview?> BuildFromJobAsync(string jobId, CancellationToken cancellationToken) => Task.FromResult<MigrationTransferPreview?>(new MigrationTransferPreview { PreviewId = Guid.NewGuid().ToString("D"), JobId = jobId, TotalItems = 1, EligibleItems = 1 });
        public Task<MigrationTransferPreview?> GetAsync(string previewId, CancellationToken cancellationToken) => Task.FromResult<MigrationTransferPreview?>(null);
    }

    private sealed class FakePilotReports : ILivePilotMigrationService
    {
        public Task<SharePointMigrationExportResult?> ExportPreviewAsync(string previewId, string exportType, CancellationToken cancellationToken) => Task.FromResult<SharePointMigrationExportResult?>(new SharePointMigrationExportResult { Content = [1] });
        public Task<LivePilotMigrationResult?> RunFromJobAsync(string jobId, LivePilotMigrationRequest request, CancellationToken cancellationToken) => Task.FromResult<LivePilotMigrationResult?>(null);
        public Task<LivePilotMigrationResult?> GetAsync(string pilotRunId, CancellationToken cancellationToken) => Task.FromResult<LivePilotMigrationResult?>(null);
        public Task<SharePointMigrationExportResult?> ExportPilotAsync(string pilotRunId, string exportType, CancellationToken cancellationToken) => Task.FromResult<SharePointMigrationExportResult?>(null);
    }

    private sealed class InMemoryWorkflowStorage : IWorkflowValidationStorageService
    {
        private WorkflowValidationRun? _run;
        public Task SaveAsync(WorkflowValidationRun run, CancellationToken cancellationToken) { _run = run; return Task.CompletedTask; }
        public Task<WorkflowValidationRun?> GetAsync(string workflowRunId, CancellationToken cancellationToken) => Task.FromResult(_run);
        public Task<WorkflowValidationRun?> GetLatestAsync(CancellationToken cancellationToken) => Task.FromResult(_run);
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "ZMS.Tests";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static string FindBackendRoot()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "samples", "discovery-live-import.sample.json")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        return Directory.GetCurrentDirectory();
    }
}
