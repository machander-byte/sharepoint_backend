using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;
using ZMS.Application.Discovery;

namespace ZMS.Application.Services;

public sealed class DemoService : IDemoService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly IDiscoveryService _discovery;
    private readonly IReadinessAnalysisService _readiness;
    private readonly IMigrationPlanService _plans;
    private readonly IMigrationExecutionService _execution;
    private readonly IWorkflowValidationService _workflow;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly string _statusPath;

    public DemoService(IDiscoveryService discovery, IReadinessAnalysisService readiness, IMigrationPlanService plans, IMigrationExecutionService execution, IWorkflowValidationService workflow, IConfiguration configuration, IHostEnvironment environment)
    {
        _discovery = discovery;
        _readiness = readiness;
        _plans = plans;
        _execution = execution;
        _workflow = workflow;
        _configuration = configuration;
        _environment = environment;
        _statusPath = Path.Combine(environment.ContentRootPath, "App_Data", "demo-status.json");
    }

    public async Task<DemoStatus> ResetAsync(CancellationToken cancellationToken)
    {
        var status = new DemoStatus
        {
            DemoMode = IsDemoMode(),
            Seeded = false,
            LastDemoChainResult = "reset",
            Warnings = ["Demo reset clears demo status only. Existing workflow artifacts are retained for audit safety."]
        };
        await SaveStatusAsync(status, cancellationToken);
        return status;
    }

    public async Task<DemoStatus> SeedAsync(CancellationToken cancellationToken)
    {
        var existing = await _discovery.GetLatestCompletedResultAsync(cancellationToken);
        if (existing is null)
        {
            await ImportSampleDiscoveryAsync(cancellationToken);
        }
        var status = await BuildStatusAsync(cancellationToken);
        status.Seeded = true;
        status.LastDemoChainResult = "seeded";
        await SaveStatusAsync(status, cancellationToken);
        return status;
    }

    public async Task<DemoStatus> RunScriptedChainAsync(CancellationToken cancellationToken)
    {
        await SeedAsync(cancellationToken);
        var response = await _workflow.RunFullChainAsync(new WorkflowValidationRequest
        {
            Source = "latest_scan",
            UseSampleFallback = true,
            CreatedBy = "Demo Operator",
            IncludeExecutionSimulation = true,
            IncludeTransferPreview = true
        }, cancellationToken);
        var status = await BuildStatusAsync(cancellationToken);
        status.Seeded = true;
        status.LatestWorkflowRunId = response.WorkflowRunId;
        status.LastDemoChainResult = response.OverallResult;
        await SaveStatusAsync(status, cancellationToken);
        return status;
    }

    public async Task<DemoStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        var status = await ReadStatusAsync(cancellationToken) ?? await BuildStatusAsync(cancellationToken);
        status.DemoMode = IsDemoMode();
        return status;
    }

    private async Task<DemoStatus> BuildStatusAsync(CancellationToken cancellationToken)
    {
        var latestScan = await _discovery.GetLatestCompletedResultAsync(cancellationToken);
        var latestAssessment = await _readiness.GetLatestAssessmentAsync(cancellationToken);
        var latestPlan = await _plans.GetLatestAsync(cancellationToken);
        var latestExecution = await _execution.GetLatestAsync(cancellationToken);
        var latestWorkflow = await _workflow.GetLatestAsync(cancellationToken);
        return new DemoStatus
        {
            DemoMode = IsDemoMode(),
            Seeded = latestScan is not null,
            LatestScanId = latestScan?.ScanId ?? string.Empty,
            LatestAssessmentId = latestAssessment?.AssessmentId ?? string.Empty,
            LatestPlanId = latestPlan?.PlanId ?? string.Empty,
            LatestExecutionJobId = latestExecution?.JobId ?? string.Empty,
            LatestPreviewId = latestWorkflow?.Summary.PreviewId ?? string.Empty,
            LatestWorkflowRunId = latestWorkflow?.WorkflowRunId ?? string.Empty,
            LastDemoChainResult = latestWorkflow?.OverallResult ?? string.Empty
        };
    }

    private async Task ImportSampleDiscoveryAsync(CancellationToken cancellationToken)
    {
        var samplePath = Path.Combine(_environment.ContentRootPath, "samples", "discovery-live-import.sample.json");
        if (!File.Exists(samplePath))
        {
            samplePath = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "samples", "discovery-live-import.sample.json"));
        }
        if (!File.Exists(samplePath))
        {
            throw new FileNotFoundException("Demo sample discovery file was not found.", samplePath);
        }
        await using var stream = File.OpenRead(samplePath);
        var sample = await JsonSerializer.DeserializeAsync<DiscoveryScanResult>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Demo sample discovery file could not be parsed.");
        await _discovery.ImportResultAsync(sample, cancellationToken);
    }

    private bool IsDemoMode() =>
        string.Equals(Environment.GetEnvironmentVariable("ZMS_DEMO_MODE") ?? _configuration["ZMS_DEMO_MODE"], "true", StringComparison.OrdinalIgnoreCase);

    private async Task SaveStatusAsync(DemoStatus status, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statusPath)!);
        await using var stream = File.Create(_statusPath);
        await JsonSerializer.SerializeAsync(stream, status, JsonOptions, cancellationToken);
    }

    private async Task<DemoStatus?> ReadStatusAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_statusPath)) return null;
        await using var stream = File.OpenRead(_statusPath);
        return await JsonSerializer.DeserializeAsync<DemoStatus>(stream, JsonOptions, cancellationToken);
    }
}
