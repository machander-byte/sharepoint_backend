using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Contracts;
using ZMS.API.Contracts.Jobs;
using ZMS.API.Extensions;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobsController : ControllerBase
{
    private readonly IMigrationService _migrationService;

    public JobsController(IMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<MigrationJobResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var jobs = await _migrationService.ListJobsAsync(userId, cancellationToken);
        return Ok(jobs.Select(job => job.ToResponse()).ToArray());
    }

    [HttpGet("{jobId:guid}")]
    [Authorize]
    public async Task<ActionResult<MigrationJobResponseDto>> Get(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var job = await _migrationService.GetJobAsync(jobId, userId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(job.ToResponse());
    }

    [HttpGet("{jobId:guid}/items")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<MigrationItemResponseDto>>> GetItems(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var items = await _migrationService.GetJobItemsAsync(jobId, userId, cancellationToken);
        return Ok(items.Select(item => item.ToResponse()).ToArray());
    }

    [HttpPost]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<MigrationJobResponseDto>> Create(
        [FromBody] CreateMigrationJobRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var job = await _migrationService.CreateJobAsync(request.ToApplicationRequest(), userId, cancellationToken);
            return Created($"/api/jobs/{job.Id}", job.ToResponse());
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpPost("{jobId:guid}/start")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> Start(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _migrationService.StartJobAsync(jobId, userId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{jobId:guid}/pause")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> Pause(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _migrationService.PauseJobAsync(jobId, userId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{jobId:guid}/resume")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> Resume(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _migrationService.ResumeJobAsync(jobId, userId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{jobId:guid}/cancel")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> Cancel(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _migrationService.CancelJobAsync(jobId, userId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{jobId:guid}/retry")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> Retry(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        await _migrationService.RetryJobAsync(jobId, userId, cancellationToken);
        return Accepted();
    }

    [HttpGet("{jobId:guid}/timeline")]
    [Authorize]
    public async Task<IActionResult> Timeline(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var timeline = await _migrationService.GetTimelineAsync(jobId, userId, cancellationToken);
        return Ok(timeline);
    }
}
