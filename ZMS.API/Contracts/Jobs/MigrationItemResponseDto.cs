using ZMS.Core.Enums;

namespace ZMS.API.Contracts.Jobs;

public class MigrationItemResponseDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string? TargetPath { get; set; }
    public long FileSizeInBytes { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public MigrationItemStatus Status { get; set; }
    public int RetryCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? StartedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
}
