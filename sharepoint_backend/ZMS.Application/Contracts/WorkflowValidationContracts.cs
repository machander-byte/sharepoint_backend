namespace ZMS.Application.Contracts;

public interface IWorkflowValidationService
{
    Task<WorkflowValidationResponse> RunFullChainAsync(WorkflowValidationRequest request, CancellationToken cancellationToken);
    Task<WorkflowValidationRun?> GetAsync(string workflowRunId, CancellationToken cancellationToken);
    Task<WorkflowValidationRun?> GetLatestAsync(CancellationToken cancellationToken);
    Task<WorkflowValidationExportResult?> ExportAsync(string workflowRunId, string exportType, CancellationToken cancellationToken);
}

public interface IWorkflowValidationStorageService
{
    Task SaveAsync(WorkflowValidationRun run, CancellationToken cancellationToken);
    Task<WorkflowValidationRun?> GetAsync(string workflowRunId, CancellationToken cancellationToken);
    Task<WorkflowValidationRun?> GetLatestAsync(CancellationToken cancellationToken);
}

public interface IWorkflowValidationReportService
{
    WorkflowValidationExportResult ExportJson(WorkflowValidationRun run);
    WorkflowValidationExportResult ExportMarkdown(WorkflowValidationRun run);
    string BuildMarkdown(WorkflowValidationRun run);
}

public sealed class WorkflowValidationRequest
{
    public string Source { get; set; } = "latest_scan";
    public bool UseSampleFallback { get; set; } = true;
    public string CreatedBy { get; set; } = "Migration Lead";
    public bool IncludeExecutionSimulation { get; set; } = true;
    public bool IncludeTransferPreview { get; set; } = true;
}

public sealed class WorkflowValidationResponse
{
    public string WorkflowRunId { get; set; } = string.Empty;
    public string Status { get; set; } = "completed";
    public string OverallResult { get; set; } = "pass";
    public int StepsPassed { get; set; }
    public int StepsFailed { get; set; }
    public int StepsWarning { get; set; }
    public WorkflowValidationSummary Summary { get; set; } = new();
}

public sealed class WorkflowValidationRun
{
    public string WorkflowRunId { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Status { get; set; } = "running";
    public string OverallResult { get; set; } = "pass";
    public string Source { get; set; } = "latest_scan";
    public string CreatedBy { get; set; } = "Migration Lead";
    public List<WorkflowValidationStep> Steps { get; set; } = [];
    public List<WorkflowValidationArtifact> Artifacts { get; set; } = [];
    public List<WorkflowValidationIssue> Issues { get; set; } = [];
    public WorkflowValidationSummary Summary { get; set; } = new();
    public Dictionary<string, string> ReportPaths { get; set; } = [];
}

public sealed class WorkflowValidationStep
{
    public string StepId { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public long DurationMs { get; set; }
    public string RelatedArtifactId { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}

public sealed class WorkflowValidationSummary
{
    public string ScanId { get; set; } = string.Empty;
    public string AssessmentId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string ValidationId { get; set; } = string.Empty;
    public string SimulationId { get; set; } = string.Empty;
    public string ExecutionJobId { get; set; } = string.Empty;
    public string PreviewId { get; set; } = string.Empty;
}

public sealed class WorkflowValidationArtifact
{
    public string ArtifactId { get; set; } = string.Empty;
    public string ArtifactType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "created";
    public string Location { get; set; } = string.Empty;
}

public sealed class WorkflowValidationIssue
{
    public string IssueId { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string StepName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public sealed class WorkflowValidationExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/json";
    public byte[] Content { get; set; } = [];
}
