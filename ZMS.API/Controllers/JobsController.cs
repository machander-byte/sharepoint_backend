using Microsoft.AspNetCore.Mvc;
using ZMS.API.Contracts;
using ZMS.API.Contracts.Jobs;
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
    public async Task<ActionResult<IReadOnlyCollection<MigrationJobResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var jobs = await _migrationService.ListJobsAsync(cancellationToken);
        return Ok(jobs.Select(job => job.ToResponse()).ToArray());
    }

    [HttpGet("{jobId:guid}")]
    public async Task<ActionResult<MigrationJobResponseDto>> Get(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _migrationService.GetJobAsync(jobId, cancellationToken);
        if (job is null)
        {
            return NotFound();
        }

        return Ok(job.ToResponse());
    }

    [HttpGet("{jobId:guid}/items")]
    public async Task<ActionResult<IReadOnlyCollection<MigrationItemResponseDto>>> GetItems(Guid jobId, CancellationToken cancellationToken)
    {
        var items = await _migrationService.GetJobItemsAsync(jobId, cancellationToken);
        return Ok(items.Select(item => item.ToResponse()).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<MigrationJobResponseDto>> Create(
        [FromBody] CreateMigrationJobRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var job = await _migrationService.CreateJobAsync(request.ToApplicationRequest(), cancellationToken);
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
    public async Task<IActionResult> Start(Guid jobId, CancellationToken cancellationToken)
    {
        await _migrationService.StartJobAsync(jobId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{jobId:guid}/pause")]
    public async Task<IActionResult> Pause(Guid jobId, CancellationToken cancellationToken)
    {
        await _migrationService.PauseJobAsync(jobId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{jobId:guid}/resume")]
    public async Task<IActionResult> Resume(Guid jobId, CancellationToken cancellationToken)
    {
        await _migrationService.ResumeJobAsync(jobId, cancellationToken);
        return Accepted();
    }
}
