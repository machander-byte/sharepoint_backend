using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Extensions;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/validation")]
public class ValidationController : ControllerBase
{
    private readonly IValidationService _validationService;

    public ValidationController(IValidationService validationService)
    {
        _validationService = validationService;
    }

    [HttpPost("start")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> Start([FromBody] ValidationStartRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var run = await _validationService.StartAsync(request.MigrationJobId, userId, cancellationToken);
        return Accepted($"/api/validation/{run.Id}", run);
    }

    [HttpGet("{validationRunId:guid}")]
    [Authorize]
    public async Task<IActionResult> Get(Guid validationRunId, CancellationToken cancellationToken)
    {
        var run = await _validationService.GetRunAsync(validationRunId, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpGet("{validationRunId:guid}/findings")]
    [Authorize]
    public async Task<IActionResult> GetFindings(Guid validationRunId, CancellationToken cancellationToken)
    {
        return Ok(await _validationService.GetFindingsAsync(validationRunId, cancellationToken));
    }

    [HttpGet("{validationRunId:guid}/items")]
    [Authorize]
    public async Task<IActionResult> GetItems(Guid validationRunId, CancellationToken cancellationToken)
    {
        return Ok(await _validationService.GetItemsAsync(validationRunId, cancellationToken));
    }

    [HttpGet("{validationRunId:guid}/export/{exportType}")]
    [Authorize]
    public async Task<IActionResult> Export(Guid validationRunId, string exportType, CancellationToken cancellationToken)
    {
        var report = await _validationService.ExportAsync(validationRunId, exportType, cancellationToken);
        return report is null ? NotFound() : File(report.Content, report.ContentType, report.FileName);
    }

    [HttpGet("/api/migrations/{jobId:guid}/validation/latest")]
    [Authorize]
    public async Task<IActionResult> GetLatestForJob(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var run = await _validationService.GetLatestForJobAsync(jobId, userId, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }
}
