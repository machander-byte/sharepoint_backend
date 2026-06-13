namespace ZMS.Application.Contracts;

public interface IMigrationExecutionService
{
    Task<CreateMigrationExecutionJobResponse?> CreateFromPlanAsync(string planId, MigrationExecutionRequest request, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> GetAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> GetLatestAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationExecutionJob>> GetAllAsync(CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> StartAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> PauseAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> ResumeAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> CancelAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> RetryFailedAsync(string jobId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationExecutionTimelineEvent>?> GetTimelineAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationExecutionExportResult?> ExportAsync(string jobId, string exportType, CancellationToken cancellationToken);
}

public interface IMigrationExecutionStorageService
{
    Task SaveAsync(MigrationExecutionJob job, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> GetAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationExecutionJob?> GetLatestAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationExecutionJob>> GetAllAsync(CancellationToken cancellationToken);
}

public interface IMigrationExecutionJobFactory
{
    Task<MigrationExecutionJob?> CreateAsync(MigrationPlan plan, MigrationExecutionRequest request, CancellationToken cancellationToken);
}

public interface IMigrationExecutionOrchestrator
{
    MigrationExecutionJob Start(MigrationExecutionJob job);
    MigrationExecutionJob Pause(MigrationExecutionJob job);
    MigrationExecutionJob Resume(MigrationExecutionJob job);
    MigrationExecutionJob Cancel(MigrationExecutionJob job);
    MigrationExecutionJob RetryFailed(MigrationExecutionJob job);
}

public interface IMigrationExecutionAdapter
{
    MigrationExecutionItem ProcessItem(MigrationExecutionItem item, MigrationExecutionWave wave);
}

public interface IMigrationExecutionTimelineService
{
    void Add(MigrationExecutionJob job, string eventType, string message, string severity = "Info", string waveExecutionId = "", string itemExecutionId = "");
}

public interface IMigrationExecutionReportService
{
    MigrationExecutionExportResult ExportJson(MigrationExecutionJob job);
    MigrationExecutionExportResult ExportCsv(MigrationExecutionJob job);
    MigrationExecutionExportResult ExportMarkdown(MigrationExecutionJob job);
    string BuildMarkdown(MigrationExecutionJob job);
}

public sealed class MigrationExecutionRequest
{
    public string Mode { get; set; } = "simulation";
    public bool RequireGoDecision { get; set; } = true;
    public List<string> SelectedWaveIds { get; set; } = [];
    public string CreatedBy { get; set; } = "Migration Lead";
}

public sealed class CreateMigrationExecutionJobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string Status { get; set; } = "created";
    public string Mode { get; set; } = "simulation";
    public string Message { get; set; } = "Migration execution job created in simulation mode";
}

public sealed class MigrationExecutionJob
{
    public string JobId { get; set; } = string.Empty;
    public string PlanId { get; set; } = string.Empty;
    public string ValidationId { get; set; } = string.Empty;
    public string SimulationId { get; set; } = string.Empty;
    public string Mode { get; set; } = "simulation";
    public string Status { get; set; } = "created";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string CreatedBy { get; set; } = "Migration Lead";
    public MigrationExecutionSummary Summary { get; set; } = new();
    public List<MigrationExecutionWave> Waves { get; set; } = [];
    public List<MigrationExecutionCheckpoint> Checkpoints { get; set; } = [];
    public List<MigrationExecutionTimelineEvent> Timeline { get; set; } = [];
    public List<MigrationExecutionError> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public Dictionary<string, string> ReportPaths { get; set; } = [];
}

public sealed class MigrationExecutionWave
{
    public string WaveExecutionId { get; set; } = string.Empty;
    public string SourceWaveId { get; set; } = string.Empty;
    public string WaveName { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Status { get; set; } = "created";
    public int ProgressPercent { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public int SkippedItems { get; set; }
    public int EstimatedFiles { get; set; }
    public long EstimatedStorageBytes { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<MigrationExecutionItem> Items { get; set; } = [];
    public List<MigrationExecutionCheckpoint> Checkpoints { get; set; } = [];
    public List<MigrationExecutionError> Errors { get; set; } = [];
}

public sealed class MigrationExecutionItem
{
    public string ItemExecutionId { get; set; } = string.Empty;
    public string SourceItemId { get; set; } = string.Empty;
    public string SiteCollection { get; set; } = string.Empty;
    public string Library { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Library";
    public string Action { get; set; } = "migrate";
    public string Status { get; set; } = "pending";
    public int ProgressPercent { get; set; }
    public string SimulatedSourceUrl { get; set; } = string.Empty;
    public string SimulatedTargetUrl { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class MigrationExecutionCheckpoint
{
    public string CheckpointId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "pending";
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
}

public sealed class MigrationExecutionTimelineEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string WaveExecutionId { get; set; } = string.Empty;
    public string ItemExecutionId { get; set; } = string.Empty;
}

public sealed class MigrationExecutionError
{
    public string ErrorId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public string Severity { get; set; } = "Error";
    public string WaveExecutionId { get; set; } = string.Empty;
    public string ItemExecutionId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public sealed class MigrationExecutionSummary
{
    public int ProgressPercent { get; set; }
    public int TotalWaves { get; set; }
    public int CompletedWaves { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public int SkippedItems { get; set; }
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
}

public sealed class MigrationExecutionExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] Content { get; set; } = [];
}
