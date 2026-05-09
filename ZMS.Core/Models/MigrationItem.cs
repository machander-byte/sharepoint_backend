using ZMS.Core.Enums;

namespace ZMS.Core.Models;

public class MigrationItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string? TargetPath { get; set; }
    public long FileSizeInBytes { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public MigrationItemStatus Status { get; set; } = MigrationItemStatus.Pending;
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? StartedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
}
