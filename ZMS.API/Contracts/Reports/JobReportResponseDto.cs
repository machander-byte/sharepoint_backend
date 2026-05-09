using ZMS.Core.Enums;
using ZMS.API.Contracts.Jobs;

namespace ZMS.API.Contracts.Reports;

public class JobReportResponseDto
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public int TotalItems { get; set; }
    public int CompletedItems { get; set; }
    public int FailedItems { get; set; }
    public int RetryQueuedItems { get; set; }
    public double ProgressPercentage { get; set; }
    public IReadOnlyCollection<MigrationItemResponseDto> FailedItemsList { get; set; } = Array.Empty<MigrationItemResponseDto>();
    public IReadOnlyCollection<LogEntryResponseDto> RecentLogs { get; set; } = Array.Empty<LogEntryResponseDto>();
}
