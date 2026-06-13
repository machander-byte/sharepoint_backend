using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/migration-execution")]
public sealed class MigrationExecutionController : ControllerBase
{
    private readonly IMigrationExecutionService _service;
    public MigrationExecutionController(IMigrationExecutionService service) => _service = service;

    [HttpPost("jobs/from-plan/{planId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<CreateMigrationExecutionJobResponse>> CreateFromPlan(string planId, [FromBody] MigrationExecutionRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.CreateFromPlanAsync(planId, request ?? new MigrationExecutionRequest(), cancellationToken);
        return result is null ? NotFound(new { message = "Migration plan was not found." }) : Ok(result);
    }

    [HttpGet("jobs/{jobId:guid}")]
    public async Task<ActionResult<MigrationExecutionJob>> Get(string jobId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(jobId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration execution job was not found." }) : Ok(result);
    }

    [HttpGet("jobs/latest")]
    public async Task<ActionResult<MigrationExecutionJob>> GetLatest(CancellationToken cancellationToken)
    {
        var result = await _service.GetLatestAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No migration execution job is available." }) : Ok(result);
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<IReadOnlyCollection<MigrationExecutionJob>>> GetAll(CancellationToken cancellationToken) => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpPost("jobs/{jobId:guid}/start")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public Task<ActionResult<MigrationExecutionJob>> Start(string jobId, CancellationToken cancellationToken) => Mutate(jobId, _service.StartAsync, cancellationToken);

    [HttpPost("jobs/{jobId:guid}/pause")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public Task<ActionResult<MigrationExecutionJob>> Pause(string jobId, CancellationToken cancellationToken) => Mutate(jobId, _service.PauseAsync, cancellationToken);

    [HttpPost("jobs/{jobId:guid}/resume")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public Task<ActionResult<MigrationExecutionJob>> Resume(string jobId, CancellationToken cancellationToken) => Mutate(jobId, _service.ResumeAsync, cancellationToken);

    [HttpPost("jobs/{jobId:guid}/cancel")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public Task<ActionResult<MigrationExecutionJob>> Cancel(string jobId, CancellationToken cancellationToken) => Mutate(jobId, _service.CancelAsync, cancellationToken);

    [HttpPost("jobs/{jobId:guid}/retry-failed")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public Task<ActionResult<MigrationExecutionJob>> RetryFailed(string jobId, CancellationToken cancellationToken) => Mutate(jobId, _service.RetryFailedAsync, cancellationToken);

    [HttpGet("jobs/{jobId:guid}/timeline")]
    public async Task<ActionResult<IReadOnlyCollection<MigrationExecutionTimelineEvent>>> Timeline(string jobId, CancellationToken cancellationToken)
    {
        var result = await _service.GetTimelineAsync(jobId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration execution timeline was not found." }) : Ok(result);
    }

    [HttpGet("jobs/{jobId:guid}/report/json")]
    public Task<IActionResult> ReportJson(string jobId, CancellationToken cancellationToken) => Export(jobId, "json", cancellationToken);

    [HttpGet("jobs/{jobId:guid}/report/csv")]
    public Task<IActionResult> ReportCsv(string jobId, CancellationToken cancellationToken) => Export(jobId, "csv", cancellationToken);

    [HttpGet("jobs/{jobId:guid}/report/markdown")]
    public Task<IActionResult> ReportMarkdown(string jobId, CancellationToken cancellationToken) => Export(jobId, "markdown", cancellationToken);

    private async Task<ActionResult<MigrationExecutionJob>> Mutate(string jobId, Func<string, CancellationToken, Task<MigrationExecutionJob?>> action, CancellationToken cancellationToken)
    {
        var result = await action(jobId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration execution job was not found." }) : Ok(result);
    }

    private async Task<IActionResult> Export(string jobId, string exportType, CancellationToken cancellationToken)
    {
        var export = await _service.ExportAsync(jobId, exportType, cancellationToken);
        return export is null ? NotFound(new { message = "Migration execution report was not found." }) : File(export.Content, export.ContentType, export.FileName);
    }
}
