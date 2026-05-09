namespace ZMS.Core.Models;

public class DashboardSummary
{
    public int TotalConnections { get; set; }
    public int TotalJobs { get; set; }
    public int QueuedJobs { get; set; }
    public int RunningJobs { get; set; }
    public int CompletedJobs { get; set; }
    public int FailedJobs { get; set; }
    public int PendingRetryItems { get; set; }
    public DateTimeOffset GeneratedUtc { get; set; } = DateTimeOffset.UtcNow;
}
