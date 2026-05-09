using Microsoft.AspNetCore.Mvc;
using ZMS.API.Contracts;
using ZMS.API.Contracts.Reports;
using ZMS.Core.Interfaces;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("jobs/{jobId:guid}")]
    public async Task<ActionResult<JobReportResponseDto>> GetJobReport(Guid jobId, CancellationToken cancellationToken)
    {
        var report = await _reportingService.GetJobReportAsync(jobId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return Ok(report.ToResponse());
    }

    [HttpGet("jobs.csv")]
    public async Task<IActionResult> DownloadJobsCsv(CancellationToken cancellationToken)
    {
        var report = await _reportingService.ExportJobsCsvAsync(cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("jobs/{jobId:guid}/summary.csv")]
    public async Task<IActionResult> DownloadJobSummaryCsv(Guid jobId, CancellationToken cancellationToken)
    {
        var report = await _reportingService.ExportJobSummaryCsvAsync(jobId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("jobs/{jobId:guid}/items.csv")]
    public async Task<IActionResult> DownloadJobItemsCsv(Guid jobId, CancellationToken cancellationToken)
    {
        var report = await _reportingService.ExportJobItemsCsvAsync(jobId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("jobs/{jobId:guid}/logs.csv")]
    public async Task<IActionResult> DownloadJobLogsCsv(Guid jobId, CancellationToken cancellationToken)
    {
        var report = await _reportingService.ExportJobLogsCsvAsync(jobId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(report.Content, report.ContentType, report.FileName);
    }
}
