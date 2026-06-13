namespace ZMS.Application.Contracts;

public interface IPreMigrationValidationService
{
    Task<PreMigrationValidationResponse?> ValidateAsync(string planId, CancellationToken cancellationToken);
    Task<PreMigrationValidationResult?> GetValidationAsync(string validationId, CancellationToken cancellationToken);
    Task<PreMigrationValidationResult?> GetLatestValidationAsync(CancellationToken cancellationToken);
    Task<ExecutionSimulationResponse?> SimulateAsync(string planId, CancellationToken cancellationToken);
    Task<ExecutionSimulationResult?> GetSimulationAsync(string simulationId, CancellationToken cancellationToken);
    Task<ExecutionSimulationResult?> GetLatestSimulationAsync(CancellationToken cancellationToken);
    Task<PreMigrationExportResult?> ExportValidationAsync(string validationId, string exportType, CancellationToken cancellationToken);
    Task<PreMigrationExportResult?> ExportSimulationAsync(string simulationId, string exportType, CancellationToken cancellationToken);
}

public interface IPreMigrationStorageService
{
    Task SaveValidationAsync(PreMigrationValidationResult result, CancellationToken cancellationToken);
    Task<PreMigrationValidationResult?> GetValidationAsync(string validationId, CancellationToken cancellationToken);
    Task<PreMigrationValidationResult?> GetLatestValidationAsync(CancellationToken cancellationToken);
    Task SaveSimulationAsync(ExecutionSimulationResult result, CancellationToken cancellationToken);
    Task<ExecutionSimulationResult?> GetSimulationAsync(string simulationId, CancellationToken cancellationToken);
    Task<ExecutionSimulationResult?> GetLatestSimulationAsync(CancellationToken cancellationToken);
}

public interface IPreMigrationCheckEngine
{
    IReadOnlyCollection<PreMigrationCheck> RunChecks(MigrationPlan plan);
}

public interface IExecutionSimulationService
{
    ExecutionSimulationResult Simulate(MigrationPlan plan);
}

public interface IExecutionEstimator
{
    ExecutionEstimate Estimate(MigrationPlanWave wave);
}

public interface IGoNoGoDecisionService
{
    string Decide(IReadOnlyCollection<PreMigrationCheck> checks, IReadOnlyCollection<WaveValidationResult> waveResults);
}

public interface IPreMigrationExportService
{
    PreMigrationExportResult ExportValidationJson(PreMigrationValidationResult result);
    PreMigrationExportResult ExportValidationCsv(PreMigrationValidationResult result);
    PreMigrationExportResult ExportValidationMarkdown(PreMigrationValidationResult result);
    PreMigrationExportResult ExportSimulationJson(ExecutionSimulationResult result);
    PreMigrationExportResult ExportSimulationMarkdown(ExecutionSimulationResult result);
}

public sealed class PreMigrationValidationResponse
{
    public string ValidationId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string Status { get; set; } = "completed";
    public string Decision { get; set; } = "no_go";
    public PreMigrationValidationSummary Summary { get; set; } = new();
}

public sealed class ExecutionSimulationResponse
{
    public string SimulationId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string Status { get; set; } = "completed";
    public string EstimatedDuration { get; set; } = string.Empty;
    public int EstimatedFiles { get; set; }
    public string EstimatedStorage { get; set; } = string.Empty;
    public int SimulatedWaves { get; set; }
    public int ExpectedFailures { get; set; }
    public int ExpectedWarnings { get; set; }
}

public sealed class PreMigrationValidationResult
{
    public string ValidationId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public string Status { get; set; } = "completed";
    public string Decision { get; set; } = "no_go";
    public PreMigrationValidationSummary Summary { get; set; } = new();
    public List<PreMigrationCheck> Checks { get; set; } = [];
    public List<WaveValidationResult> WaveResults { get; set; } = [];
    public List<string> Blockers { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Recommendations { get; set; } = [];
    public Dictionary<string, string> ExportPaths { get; set; } = [];
}

public sealed class PreMigrationValidationSummary
{
    public int Errors { get; set; }
    public int Warnings { get; set; }
    public int PassedChecks { get; set; }
    public int BlockedWaves { get; set; }
    public int ReadyWaves { get; set; }
}

public sealed class PreMigrationCheck
{
    public string CheckId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "passed";
    public string Severity { get; set; } = "Info";
    public string AffectedWave { get; set; } = string.Empty;
    public string AffectedItem { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool RequiredForGoLive { get; set; }
}

public sealed class WaveValidationResult
{
    public string WaveId { get; set; } = string.Empty;
    public string WaveName { get; set; } = string.Empty;
    public string Status { get; set; } = "ready";
    public int PassedChecks { get; set; }
    public int Warnings { get; set; }
    public int Errors { get; set; }
}

public sealed class ExecutionSimulationResult
{
    public string SimulationId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public string Status { get; set; } = "completed";
    public int EstimatedDurationMinutes { get; set; }
    public int EstimatedFiles { get; set; }
    public long EstimatedStorageBytes { get; set; }
    public List<ExecutionSimulationWave> Waves { get; set; } = [];
    public List<ExecutionSimulationIssue> ExpectedIssues { get; set; } = [];
    public List<string> Checkpoints { get; set; } = [];
    public List<string> Assumptions { get; set; } = [];
    public List<string> Recommendations { get; set; } = [];
}

public sealed class ExecutionSimulationWave
{
    public string WaveId { get; set; } = string.Empty;
    public string WaveName { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ItemCount { get; set; }
    public int EstimatedFiles { get; set; }
    public long EstimatedStorageBytes { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public int ReadinessScore { get; set; }
    public int ExpectedWarnings { get; set; }
    public int ExpectedFailures { get; set; }
    public List<ExecutionSimulationStep> Steps { get; set; } = [];
}

public sealed class ExecutionSimulationStep
{
    public string StepId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public string Status { get; set; } = "simulated";
    public List<string> Dependencies { get; set; } = [];
    public List<string> ExpectedIssues { get; set; } = [];
}

public sealed class ExecutionSimulationIssue
{
    public string IssueId { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string WaveName { get; set; } = string.Empty;
    public string Item { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public sealed class ExecutionEstimate
{
    public int DurationMinutes { get; set; }
    public int ExpectedWarnings { get; set; }
    public int ExpectedFailures { get; set; }
}

public sealed class PreMigrationExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] Content { get; set; } = [];
}
