using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/migration-plans")]
public sealed class MigrationPlansController : ControllerBase
{
    private readonly IMigrationPlanService _service;

    public MigrationPlansController(IMigrationPlanService service)
    {
        _service = service;
    }

    [HttpPost("from-assessment/{assessmentId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<CreateMigrationPlanResponse>> CreateFromAssessment(string assessmentId, CancellationToken cancellationToken)
    {
        var result = await _service.CreateFromAssessmentAsync(assessmentId, cancellationToken);
        return result is null ? NotFound(new { message = "Readiness assessment was not found." }) : Ok(result);
    }

    [HttpGet("{planId:guid}")]
    public async Task<ActionResult<MigrationPlan>> Get(string planId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(planId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration plan was not found." }) : Ok(result);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<MigrationPlan>> GetLatest(CancellationToken cancellationToken)
    {
        var result = await _service.GetLatestAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No migration plan is available." }) : Ok(result);
    }

    [HttpPut("{planId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<MigrationPlan>> Update(string planId, [FromBody] MigrationPlan plan, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(planId, plan, cancellationToken);
        return result is null ? NotFound(new { message = "Migration plan was not found." }) : Ok(result);
    }

    [HttpPost("{planId:guid}/validate")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<MigrationPlanValidationResult>> Validate(string planId, CancellationToken cancellationToken)
    {
        var result = await _service.ValidateAsync(planId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration plan was not found." }) : Ok(result);
    }

    [HttpPost("{planId:guid}/generate-runbook")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<MigrationRunbook>> GenerateRunbook(string planId, CancellationToken cancellationToken)
    {
        var result = await _service.GenerateRunbookAsync(planId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration plan was not found." }) : Ok(result);
    }

    [HttpGet("{planId:guid}/export/json")]
    public Task<IActionResult> ExportJson(string planId, CancellationToken cancellationToken) => Export(planId, "json", cancellationToken);

    [HttpGet("{planId:guid}/export/csv")]
    public Task<IActionResult> ExportCsv(string planId, CancellationToken cancellationToken) => Export(planId, "csv", cancellationToken);

    [HttpGet("{planId:guid}/export/markdown")]
    public Task<IActionResult> ExportMarkdown(string planId, CancellationToken cancellationToken) => Export(planId, "markdown", cancellationToken);

    private async Task<IActionResult> Export(string planId, string exportType, CancellationToken cancellationToken)
    {
        var export = await _service.ExportAsync(planId, exportType, cancellationToken);
        return export is null ? NotFound(new { message = "Migration plan export was not found." }) : File(export.Content, export.ContentType, export.FileName);
    }
}
