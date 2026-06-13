using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;
using ZMS.Application.Discovery;

namespace ZMS.Application.Services;

public sealed class WorkflowValidationService : IWorkflowValidationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true, WriteIndented = true };
    private readonly IDiscoveryService _discovery;
    private readonly IReadinessAnalysisService _readiness;
    private readonly IMigrationPlanService _plans;
    private readonly IPreMigrationValidationService _preMigration;
    private readonly IMigrationExecutionService _execution;
    private readonly IMigrationTransferPreviewService _preview;
    private readonly ILivePilotMigrationService _pilotReports;
    private readonly IWorkflowValidationStorageService _storage;
    private readonly IHostEnvironment _environment;

    public WorkflowValidationService(
        IDiscoveryService discovery,
        IReadinessAnalysisService readiness,
        IMigrationPlanService plans,
        IPreMigrationValidationService preMigration,
        IMigrationExecutionService execution,
        IMigrationTransferPreviewService preview,
        ILivePilotMigrationService pilotReports,
        IWorkflowValidationStorageService storage,
        IHostEnvironment environment)
    {
        _discovery = discovery;
        _readiness = readiness;
        _plans = plans;
        _preMigration = preMigration;
        _execution = execution;
        _preview = preview;
        _pilotReports = pilotReports;
        _storage = storage;
        _environment = environment;
    }

    public async Task<WorkflowValidationResponse> RunFullChainAsync(WorkflowValidationRequest request, CancellationToken cancellationToken)
    {
        var run = new WorkflowValidationRun
        {
            WorkflowRunId = Guid.NewGuid().ToString("D"),
            StartedAt = DateTimeOffset.UtcNow,
            Status = "running",
            Source = request.Source,
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "Migration Lead" : request.CreatedBy
        };

        DiscoveryScanResult? scan = null;
        MigrationReadinessAssessment? assessment = null;
        MigrationPlan? plan = null;
        PreMigrationValidationResponse? preValidation = null;
        ExecutionSimulationResponse? simulation = null;
        CreateMigrationExecutionJobResponse? executionResponse = null;
        MigrationExecutionJob? executionJob = null;
        MigrationTransferPreview? transferPreview = null;

        await Step(run, 1, "Discovery Scan", "Load latest completed scan or import sample fallback.", async step =>
        {
            scan = await _discovery.GetLatestCompletedResultAsync(cancellationToken);
            if (scan is null && request.UseSampleFallback)
            {
                scan = await ImportSampleDiscoveryAsync(cancellationToken);
                step.Warnings.Add("No latest discovery scan was available. Imported sample discovery fallback.");
            }
            if (scan is null) throw new InvalidOperationException("No completed discovery scan is available.");
            run.Summary.ScanId = scan.ScanId;
            step.RelatedArtifactId = scan.ScanId;
            AddArtifact(run, "discovery_scan", scan.ScanId, "Discovery scan", scan.Mode);
        });

        await Step(run, 2, "Readiness Analysis", "Run readiness analyzer against the discovery scan.", async step =>
        {
            var response = await _readiness.AnalyzeAsync(run.Summary.ScanId, cancellationToken) ?? throw new InvalidOperationException("Readiness analysis could not be generated.");
            assessment = await _readiness.GetAssessmentAsync(response.AssessmentId, cancellationToken);
            run.Summary.AssessmentId = response.AssessmentId;
            step.RelatedArtifactId = response.AssessmentId;
            if (response.RiskLevel is "High" or "Critical") step.Warnings.Add($"Readiness risk level is {response.RiskLevel}.");
            AddArtifact(run, "readiness_assessment", response.AssessmentId, "Readiness assessment", response.RiskLevel);
        });

        await Step(run, 3, "Migration Plan", "Generate draft migration plan from readiness assessment.", async step =>
        {
            var response = await _plans.CreateFromAssessmentAsync(run.Summary.AssessmentId, cancellationToken) ?? throw new InvalidOperationException("Migration plan could not be generated.");
            plan = await _plans.GetAsync(response.PlanId, cancellationToken);
            run.Summary.PlanId = response.PlanId;
            step.RelatedArtifactId = response.PlanId;
            if (plan?.Status == "blocked") step.Warnings.Add("Migration plan is blocked by current readiness findings.");
            if (string.IsNullOrWhiteSpace(plan?.TargetEnvironment)) step.Warnings.Add("Target environment is missing.");
            AddArtifact(run, "migration_plan", response.PlanId, "Migration plan", response.Status);
        });

        await Step(run, 4, "Plan Validation", "Validate generated migration plan.", async step =>
        {
            var validation = await _plans.ValidateAsync(run.Summary.PlanId, cancellationToken) ?? throw new InvalidOperationException("Migration plan validation failed.");
            if (validation.Errors.Any(error => error.Contains("no waves", StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Migration plan has no waves.");
            step.Warnings.AddRange(validation.Errors.Concat(validation.Warnings).Take(10));
        });

        await Step(run, 5, "Runbook", "Generate planning runbook markdown.", async step =>
        {
            var runbook = await _plans.GenerateRunbookAsync(run.Summary.PlanId, cancellationToken) ?? throw new InvalidOperationException("Runbook could not be generated.");
            if (string.IsNullOrWhiteSpace(runbook.Markdown)) throw new InvalidOperationException("Runbook markdown was empty.");
            step.RelatedArtifactId = runbook.FileName;
            AddArtifact(run, "runbook", runbook.FileName, "Migration runbook", "generated");
        });

        await Step(run, 6, "Pre-Migration Validation", "Run Go/No-Go safety validation.", async step =>
        {
            preValidation = await _preMigration.ValidateAsync(run.Summary.PlanId, cancellationToken) ?? throw new InvalidOperationException("Pre-migration validation could not be generated.");
            run.Summary.ValidationId = preValidation.ValidationId;
            step.RelatedArtifactId = preValidation.ValidationId;
            if (preValidation.Decision == "no_go") step.Warnings.Add("Pre-migration validation decision is no_go. This is expected until approvals/checklist are complete.");
            AddArtifact(run, "pre_migration_validation", preValidation.ValidationId, "Pre-migration validation", preValidation.Decision);
        });

        if (request.IncludeExecutionSimulation)
        {
            await Step(run, 7, "Execution Simulation", "Run execution simulation from migration plan.", async step =>
            {
                simulation = await _preMigration.SimulateAsync(run.Summary.PlanId, cancellationToken) ?? throw new InvalidOperationException("Execution simulation could not be generated.");
                run.Summary.SimulationId = simulation.SimulationId;
                step.RelatedArtifactId = simulation.SimulationId;
                if (simulation.ExpectedFailures > 0) step.Warnings.Add($"Execution simulation expected failures: {simulation.ExpectedFailures}.");
                AddArtifact(run, "execution_simulation", simulation.SimulationId, "Execution simulation", simulation.Status);
            });
        }

        await Step(run, 8, "Execution Job", "Create simulation execution job.", async step =>
        {
            executionResponse = await _execution.CreateFromPlanAsync(run.Summary.PlanId, new MigrationExecutionRequest { Mode = "simulation", RequireGoDecision = false, CreatedBy = run.CreatedBy }, cancellationToken)
                ?? throw new InvalidOperationException("Simulation execution job could not be created.");
            run.Summary.ExecutionJobId = executionResponse.JobId;
            step.RelatedArtifactId = executionResponse.JobId;
            if (executionResponse.Status == "blocked") step.Warnings.Add("Execution job was blocked; workflow validation continues only for safe status reporting.");
            AddArtifact(run, "execution_job", executionResponse.JobId, "Simulation execution job", executionResponse.Status);
        });

        await Step(run, 9, "Start Simulation Execution Job", "Start simulation execution job.", async step =>
        {
            executionJob = await _execution.StartAsync(run.Summary.ExecutionJobId, cancellationToken) ?? throw new InvalidOperationException("Simulation execution job could not be started.");
            step.RelatedArtifactId = executionJob.JobId;
            if (executionJob.Status is "failed" or "completed_with_warnings") step.Warnings.Add($"Simulation execution job finished with status {executionJob.Status}.");
        });

        if (request.IncludeTransferPreview)
        {
            await Step(run, 10, "Transfer Preview", "Generate SharePoint transfer preview from execution job.", async step =>
            {
                transferPreview = await _preview.BuildFromJobAsync(run.Summary.ExecutionJobId, cancellationToken) ?? throw new InvalidOperationException("Transfer preview could not be generated.");
                run.Summary.PreviewId = transferPreview.PreviewId;
                step.RelatedArtifactId = transferPreview.PreviewId;
                if (transferPreview.BlockedItems > 0) step.Warnings.Add($"Transfer preview contains {transferPreview.BlockedItems} blocked items.");
                AddArtifact(run, "transfer_preview", transferPreview.PreviewId, "SharePoint transfer preview", transferPreview.Mode);
            });
        }

        await Step(run, 11, "Export Verification", "Verify key export services return report payloads.", async step =>
        {
            await VerifyExport(step, "readiness", () => _readiness.ExportAsync(run.Summary.AssessmentId, "json", cancellationToken));
            await VerifyExport(step, "migration plan", () => _plans.ExportAsync(run.Summary.PlanId, "json", cancellationToken));
            await VerifyExport(step, "pre-migration validation", () => _preMigration.ExportValidationAsync(run.Summary.ValidationId, "json", cancellationToken));
            if (!string.IsNullOrWhiteSpace(run.Summary.SimulationId)) await VerifyExport(step, "execution simulation", () => _preMigration.ExportSimulationAsync(run.Summary.SimulationId, "json", cancellationToken));
            await VerifyExport(step, "execution job", () => _execution.ExportAsync(run.Summary.ExecutionJobId, "json", cancellationToken));
            if (!string.IsNullOrWhiteSpace(run.Summary.PreviewId)) await VerifySharePointExport(step, "transfer preview", () => _pilotReports.ExportPreviewAsync(run.Summary.PreviewId, "json", cancellationToken));
            AddArtifact(run, "reports", "exports", "Export verification", "verified");
        });

        await Step(run, 12, "Workflow Report", "Generate end-to-end workflow validation report.", step =>
        {
            step.RelatedArtifactId = "workflow-report.md";
            AddArtifact(run, "reports", "workflow-report.md", "Workflow validation report", "generated");
            return Task.CompletedTask;
        });

        run.CompletedAt = DateTimeOffset.UtcNow;
        run.Status = "completed";
        run.OverallResult = run.Steps.Any(s => s.Status == "failed") ? "fail" : run.Steps.Any(s => s.Status == "warning") ? "pass_with_warnings" : "pass";
        run.ReportPaths = new Dictionary<string, string>
        {
            ["json"] = "workflow-result.json",
            ["markdown"] = "workflow-report.md",
            ["artifacts"] = "workflow-artifacts.json",
            ["issues"] = "workflow-issues.csv"
        };
        await _storage.SaveAsync(run, cancellationToken);
        return ToResponse(run);
    }

    public Task<WorkflowValidationRun?> GetAsync(string workflowRunId, CancellationToken cancellationToken) => _storage.GetAsync(workflowRunId, cancellationToken);
    public Task<WorkflowValidationRun?> GetLatestAsync(CancellationToken cancellationToken) => _storage.GetLatestAsync(cancellationToken);

    public async Task<WorkflowValidationExportResult?> ExportAsync(string workflowRunId, string exportType, CancellationToken cancellationToken)
    {
        var run = await _storage.GetAsync(workflowRunId, cancellationToken);
        if (run is null) return null;
        var report = new WorkflowValidationReportService();
        return exportType.ToLowerInvariant() is "markdown" or "md" ? report.ExportMarkdown(run) : report.ExportJson(run);
    }

    private async Task<DiscoveryScanResult?> ImportSampleDiscoveryAsync(CancellationToken cancellationToken)
    {
        var paths = new[]
        {
            Path.Combine(_environment.ContentRootPath, "samples", "discovery-live-import.sample.json"),
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "samples", "discovery-live-import.sample.json"))
        };
        var samplePath = paths.FirstOrDefault(File.Exists);
        if (samplePath is null) return null;
        await using var stream = File.OpenRead(samplePath);
        var sample = await JsonSerializer.DeserializeAsync<DiscoveryScanResult>(stream, JsonOptions, cancellationToken);
        if (sample is null) return null;
        var import = await _discovery.ImportResultAsync(sample, cancellationToken);
        return await _discovery.GetScanResultAsync(import.ScanId, cancellationToken);
    }

    private static async Task VerifyExport<T>(WorkflowValidationStep step, string name, Func<Task<T?>> exporter) where T : class
    {
        var result = await exporter();
        if (result is null)
        {
            step.Warnings.Add($"{name} export returned no payload.");
        }
    }

    private static async Task VerifySharePointExport(WorkflowValidationStep step, string name, Func<Task<SharePointMigrationExportResult?>> exporter)
    {
        var result = await exporter();
        if (result is null || result.Content.Length == 0)
        {
            step.Warnings.Add($"{name} export returned no payload.");
        }
    }

    private static async Task Step(WorkflowValidationRun run, int order, string name, string description, Func<WorkflowValidationStep, Task> action)
    {
        var step = new WorkflowValidationStep
        {
            StepId = $"step-{order:00}",
            Order = order,
            Name = name,
            Description = description,
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow
        };
        var watch = Stopwatch.StartNew();
        run.Steps.Add(step);
        try
        {
            await action(step);
            step.Status = step.Warnings.Count > 0 ? "warning" : "passed";
        }
        catch (Exception ex)
        {
            step.Status = "failed";
            step.Errors.Add(ex.Message);
        }
        finally
        {
            watch.Stop();
            step.CompletedAt = DateTimeOffset.UtcNow;
            step.DurationMs = watch.ElapsedMilliseconds;
            foreach (var warning in step.Warnings) AddIssue(run, "Warning", step.Name, warning, "Review this step before real pilot migration.");
            foreach (var error in step.Errors) AddIssue(run, "Error", step.Name, error, "Fix this failure and rerun workflow validation.");
        }
    }

    private static void AddArtifact(WorkflowValidationRun run, string type, string id, string name, string status)
    {
        run.Artifacts.Add(new WorkflowValidationArtifact { ArtifactId = id, ArtifactType = type, DisplayName = name, Status = status, Location = id });
    }

    private static void AddIssue(WorkflowValidationRun run, string severity, string step, string message, string action)
    {
        run.Issues.Add(new WorkflowValidationIssue { IssueId = Guid.NewGuid().ToString("D"), Severity = severity, StepName = step, Message = message, RecommendedAction = action });
    }

    private static WorkflowValidationResponse ToResponse(WorkflowValidationRun run) => new()
    {
        WorkflowRunId = run.WorkflowRunId,
        Status = run.Status,
        OverallResult = run.OverallResult,
        StepsPassed = run.Steps.Count(s => s.Status == "passed"),
        StepsFailed = run.Steps.Count(s => s.Status == "failed"),
        StepsWarning = run.Steps.Count(s => s.Status == "warning"),
        Summary = run.Summary
    };
}

public sealed class WorkflowValidationStorageService : IWorkflowValidationStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly IWorkflowValidationReportService _reports;

    public WorkflowValidationStorageService(IHostEnvironment environment, IWorkflowValidationReportService reports)
    {
        _root = Path.Combine(environment.ContentRootPath, "App_Data", "workflow-validations");
        _reports = reports;
    }

    public async Task SaveAsync(WorkflowValidationRun run, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(_root, run.WorkflowRunId);
        Directory.CreateDirectory(dir);
        await WriteJsonAsync(Path.Combine(dir, "workflow-result.json"), run, cancellationToken);
        await WriteJsonAsync(Path.Combine(dir, "workflow-artifacts.json"), run.Artifacts, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(dir, "workflow-report.md"), _reports.ExportMarkdown(run).Content, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(dir, "workflow-issues.csv"), Encoding.UTF8.GetBytes(Csv(run.Issues.Select(i => new[] { i.Severity, i.StepName, i.Message, i.RecommendedAction }))), cancellationToken);
    }

    public Task<WorkflowValidationRun?> GetAsync(string workflowRunId, CancellationToken cancellationToken) =>
        Guid.TryParse(workflowRunId, out _) ? ReadJsonAsync<WorkflowValidationRun>(Path.Combine(_root, workflowRunId, "workflow-result.json"), cancellationToken) : Task.FromResult<WorkflowValidationRun?>(null);

    public async Task<WorkflowValidationRun?> GetLatestAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_root)) return null;
        var runs = new List<WorkflowValidationRun>();
        foreach (var path in Directory.EnumerateFiles(_root, "workflow-result.json", SearchOption.AllDirectories))
        {
            var run = await ReadJsonAsync<WorkflowValidationRun>(path, cancellationToken);
            if (run is not null) runs.Add(run);
        }
        return runs.OrderByDescending(r => r.StartedAt).FirstOrDefault();
    }

    internal static string Csv(IEnumerable<string[]> rows)
    {
        static string Escape(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        return string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(Escape))));
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }
}

public sealed class WorkflowValidationReportService : IWorkflowValidationReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public WorkflowValidationExportResult ExportJson(WorkflowValidationRun run) => new() { FileName = $"workflow-validation-{run.WorkflowRunId}.json", ContentType = "application/json", Content = JsonSerializer.SerializeToUtf8Bytes(run, JsonOptions) };
    public WorkflowValidationExportResult ExportMarkdown(WorkflowValidationRun run) => new() { FileName = $"workflow-validation-{run.WorkflowRunId}.md", ContentType = "text/markdown", Content = Encoding.UTF8.GetBytes(BuildMarkdown(run)) };

    public string BuildMarkdown(WorkflowValidationRun run)
    {
        var md = new StringBuilder("# End-to-End ZMS Workflow Validation Report\n\n")
            .AppendLine("> This workflow validation does not perform real SharePoint migration.")
            .AppendLine("## Executive Summary")
            .AppendLine($"Overall result: **{run.OverallResult}**")
            .AppendLine($"Steps passed: {run.Steps.Count(s => s.Status == "passed")}")
            .AppendLine($"Warnings: {run.Steps.Count(s => s.Status == "warning")}")
            .AppendLine($"Failures: {run.Steps.Count(s => s.Status == "failed")}")
            .AppendLine("\n## Workflow Run Details")
            .AppendLine($"Run ID: `{run.WorkflowRunId}`")
            .AppendLine($"Created by: {run.CreatedBy}")
            .AppendLine($"Started: {run.StartedAt:o}")
            .AppendLine($"Completed: {run.CompletedAt:o}")
            .AppendLine("\n## Step-by-Step Results");
        foreach (var step in run.Steps.OrderBy(s => s.Order))
        {
            md.AppendLine($"- {step.Order}. **{step.Name}** - {step.Status} ({step.DurationMs} ms) artifact `{step.RelatedArtifactId}`");
            foreach (var warning in step.Warnings) md.AppendLine($"  - Warning: {warning}");
            foreach (var error in step.Errors) md.AppendLine($"  - Error: {error}");
        }
        md.AppendLine("\n## Generated Artifacts");
        foreach (var artifact in run.Artifacts) md.AppendLine($"- {artifact.ArtifactType}: `{artifact.ArtifactId}` ({artifact.Status})");
        md.AppendLine("\n## Risk Summary")
            .AppendLine($"Issues recorded: {run.Issues.Count}")
            .AppendLine("\n## Go/No-Go Result")
            .AppendLine($"Pre-migration validation: `{run.Summary.ValidationId}`")
            .AppendLine("\n## Simulation Summary")
            .AppendLine($"Execution simulation: `{run.Summary.SimulationId}`")
            .AppendLine("\n## Execution Job Summary")
            .AppendLine($"Execution job: `{run.Summary.ExecutionJobId}`")
            .AppendLine("\n## Transfer Preview Summary")
            .AppendLine($"Transfer preview: `{run.Summary.PreviewId}`")
            .AppendLine("\n## Export Verification")
            .AppendLine("Export services were checked during the Export Verification step.")
            .AppendLine("\n## Issues and Warnings");
        foreach (var issue in run.Issues) md.AppendLine($"- **{issue.Severity}** {issue.StepName}: {issue.Message}");
        md.AppendLine("\n## Recommended Next Actions")
            .AppendLine("- Complete approval checklist before execution.")
            .AppendLine("- Resolve blocked transfer preview items.")
            .AppendLine("- Review No-Go decision before pilot migration.")
            .AppendLine("- Run live discovery before final pilot decision.");
        return md.ToString();
    }
}
