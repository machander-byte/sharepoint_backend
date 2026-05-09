using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IReportingService
{
    Task<JobReport?> GetJobReportAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task<ReportFile> ExportJobsCsvAsync(string userId, CancellationToken cancellationToken);
    Task<ReportFile?> ExportJobSummaryCsvAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task<ReportFile?> ExportJobItemsCsvAsync(Guid jobId, string userId, CancellationToken cancellationToken);
    Task<ReportFile?> ExportJobLogsCsvAsync(Guid jobId, string userId, CancellationToken cancellationToken);
}
