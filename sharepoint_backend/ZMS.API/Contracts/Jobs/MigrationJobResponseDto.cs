using ZMS.Core.Enums;

namespace ZMS.API.Contracts.Jobs;

public class MigrationJobResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SourceConnectionId { get; set; }
    public Guid TargetConnectionId { get; set; }
    public string SourceLocation { get; set; } = string.Empty;
    public string? SourceLibraryName { get; set; }
    public string TargetSiteUrl { get; set; } = string.Empty;
    public string TargetLibraryName { get; set; } = string.Empty;
    public string? TargetLibraryUrlSegment { get; set; }
    public string? TargetRootPath { get; set; }
    public bool PreserveMetadata { get; set; }
    public int BatchSize { get; set; }
    public int MaxRetryCount { get; set; }
    public JobStatus Status { get; set; }
    public EnterpriseJobState EnterpriseState { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public string? LastError { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? StartedUtc { get; set; }
    public DateTimeOffset? FinishedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
