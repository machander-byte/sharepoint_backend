using ZMS.Core.Enums;

namespace ZMS.Core.Models;

public class JobReport
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public int RetryQueuedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public IReadOnlyCollection<MigrationItem> FailedItemsList { get; set; } = Array.Empty<MigrationItem>();
    public IReadOnlyCollection<LogEntry> RecentLogs { get; set; } = Array.Empty<LogEntry>();
}
