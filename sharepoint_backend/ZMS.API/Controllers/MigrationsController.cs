using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Extensions;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/migrations")]
public class MigrationsController : ControllerBase
{
    private readonly IMigrationService _migrationService;

    public MigrationsController(IMigrationService migrationService)
    {
        _migrationService = migrationService;
    }

    [HttpGet("{jobId:guid}/timeline")]
    [Authorize]
    public async Task<IActionResult> GetTimeline(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var events = await _migrationService.GetTimelineAsync(jobId, userId, cancellationToken);
        return Ok(events);
    }

    [HttpGet("{jobId:guid}/state")]
    [Authorize]
    public async Task<IActionResult> GetState(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var job = await _migrationService.GetJobAsync(jobId, userId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            job.Id,
            job.Name,
            LegacyStatus = job.Status.ToString(),
            State = job.EnterpriseState.ToString(),
            job.TotalItems,
            job.CompletedItems,
            job.FailedItems,
            job.RetryCount,
            job.LastError,
            job.FailureReason,
            job.CorrelationId,
            job.UpdatedUtc
        });
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
}
