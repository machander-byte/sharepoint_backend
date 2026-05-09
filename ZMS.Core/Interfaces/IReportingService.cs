using ZMS.Core.Models;

namespace ZMS.Core.Interfaces;

public interface IReportingService
{
    Task<JobReport?> GetJobReportAsync(Guid jobId, CancellationToken cancellationToken);
    Task<ReportFile> ExportJobsCsvAsync(CancellationToken cancellationToken);
    Task<ReportFile?> ExportJobSummaryCsvAsync(Guid jobId, CancellationToken cancellationToken);
    Task<ReportFile?> ExportJobItemsCsvAsync(Guid jobId, CancellationToken cancellationToken);
    Task<ReportFile?> ExportJobLogsCsvAsync(Guid jobId, CancellationToken cancellationToken);
}
