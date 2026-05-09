using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Contracts;
using ZMS.API.Contracts.Reports;
using ZMS.API.Extensions;
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
    [Authorize]
    public async Task<ActionResult<JobReportResponseDto>> GetJobReport(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var report = await _reportingService.GetJobReportAsync(jobId, userId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return Ok(report.ToResponse());
    }

    [HttpGet("jobs.csv")]
    [Authorize]
    public async Task<IActionResult> DownloadJobsCsv(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var report = await _reportingService.ExportJobsCsvAsync(userId, cancellationToken);
        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("jobs/{jobId:guid}/summary.csv")]
    [Authorize]
    public async Task<IActionResult> DownloadJobSummaryCsv(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var report = await _reportingService.ExportJobSummaryCsvAsync(jobId, userId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("jobs/{jobId:guid}/items.csv")]
    [Authorize]
    public async Task<IActionResult> DownloadJobItemsCsv(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var report = await _reportingService.ExportJobItemsCsvAsync(jobId, userId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("jobs/{jobId:guid}/logs.csv")]
    [Authorize]
    public async Task<IActionResult> DownloadJobLogsCsv(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var report = await _reportingService.ExportJobLogsCsvAsync(jobId, userId, cancellationToken);
        if (report is null)
        {
            return NotFound();
        }

        return File(report.Content, report.ContentType, report.FileName);
    }
}
