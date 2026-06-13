using ZMS.Core.Enums;

namespace ZMS.Core.Models;

public class MigrationJobEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? PreviousState { get; set; }
    public string NewState { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public EnterpriseSeverity Severity { get; set; } = EnterpriseSeverity.Info;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CorrelationId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public class ValidationRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MigrationJobId { get; set; }
    public ValidationRunStatus Status { get; set; } = ValidationRunStatus.NOT_STARTED;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int SourceItemCount { get; set; }
    public int TargetItemCount { get; set; }
    public int PassedCount { get; set; }
    public int WarningCount { get; set; }
    public int FailedCount { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

public class ValidationFinding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ValidationRunId { get; set; }
    public EnterpriseSeverity Severity { get; set; } = EnterpriseSeverity.Info;
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public class ValidationItemResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ValidationRunId { get; set; }
    public Guid? MigrationItemId { get; set; }
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public long SourceSizeBytes { get; set; }
    public long TargetSizeBytes { get; set; }
    public string Status { get; set; } = "PASSED";
    public string DifferenceType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
