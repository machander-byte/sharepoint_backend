using ZMS.Core.Enums;

namespace ZMS.Core.Models;

public class MigrationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty; // Supabase sub claim from JWT
    public string Name { get; set; } = string.Empty;
    public Guid SourceConnectionId { get; set; }
    public Guid TargetConnectionId { get; set; }
    public string SourceLocation { get; set; } = string.Empty;
    public string? SourceLibraryName { get; set; }
    public string TargetSiteUrl { get; set; } = string.Empty;
    public string TargetLibraryName { get; set; } = string.Empty;
    public string? TargetLibraryUrlSegment { get; set; }
    public string? TargetRootPath { get; set; }
    public bool PreserveMetadata { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int MaxRetryCount { get; set; } = 3;
    public JobStatus Status { get; set; } = JobStatus.Draft;
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedUtc { get; set; }
    public DateTimeOffset? FinishedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
