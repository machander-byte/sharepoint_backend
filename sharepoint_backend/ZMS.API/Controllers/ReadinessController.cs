using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/readiness")]
public sealed class ReadinessController : ControllerBase
{
    private readonly IReadinessAnalysisService _readinessService;

    public ReadinessController(IReadinessAnalysisService readinessService)
    {
        _readinessService = readinessService;
    }

    [HttpPost("analyze/{scanId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<ReadinessAnalyzeResponse>> Analyze(string scanId, CancellationToken cancellationToken)
    {
        var result = await _readinessService.AnalyzeAsync(scanId, cancellationToken);
        return result is null
            ? NotFound(new { message = "Completed discovery scan was not found." })
            : Ok(result);
    }

    [HttpGet("{assessmentId:guid}")]
    public async Task<ActionResult<MigrationReadinessAssessment>> Get(string assessmentId, CancellationToken cancellationToken)
    {
        var result = await _readinessService.GetAssessmentAsync(assessmentId, cancellationToken);
        return result is null
            ? NotFound(new { message = "Readiness assessment was not found." })
            : Ok(result);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<MigrationReadinessAssessment>> GetLatest(CancellationToken cancellationToken)
    {
        var result = await _readinessService.GetLatestAssessmentAsync(cancellationToken);
        return result is null
            ? NotFound(new { message = "No completed readiness assessment is available." })
            : Ok(result);
    }

    [HttpGet("{assessmentId:guid}/remediation-plan")]
    public async Task<ActionResult<IReadOnlyCollection<RemediationAction>>> GetRemediationPlan(string assessmentId, CancellationToken cancellationToken)
    {
        var result = await _readinessService.GetRemediationPlanAsync(assessmentId, cancellationToken);
        return result is null
            ? NotFound(new { message = "Readiness assessment was not found." })
            : Ok(result);
    }

    [HttpGet("{assessmentId:guid}/migration-waves")]
    public async Task<ActionResult<IReadOnlyCollection<MigrationWaveSuggestion>>> GetMigrationWaves(string assessmentId, CancellationToken cancellationToken)
    {
        var result = await _readinessService.GetMigrationWavesAsync(assessmentId, cancellationToken);
        return result is null
            ? NotFound(new { message = "Readiness assessment was not found." })
            : Ok(result);
    }

    [HttpGet("{assessmentId:guid}/export/json")]
    public Task<IActionResult> ExportJson(string assessmentId, CancellationToken cancellationToken) =>
        Export(assessmentId, "json", cancellationToken);

    [HttpGet("{assessmentId:guid}/export/csv")]
    public Task<IActionResult> ExportCsv(string assessmentId, CancellationToken cancellationToken) =>
        Export(assessmentId, "csv", cancellationToken);

    [HttpGet("{assessmentId:guid}/export/markdown")]
    public Task<IActionResult> ExportMarkdown(string assessmentId, CancellationToken cancellationToken) =>
        Export(assessmentId, "markdown", cancellationToken);

    private async Task<IActionResult> Export(string assessmentId, string exportType, CancellationToken cancellationToken)
    {
        var export = await _readinessService.ExportAsync(assessmentId, exportType, cancellationToken);
        return export is null
            ? NotFound(new { message = "Readiness export was not found." })
            : File(export.Content, export.ContentType, export.FileName);
    }
}
