using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Extensions;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IAiAdvisorService _advisorService;

    public AiController(IAiAdvisorService advisorService)
    {
        _advisorService = advisorService;
    }

    [HttpPost("advisor/ask")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Viewer)]
    public async Task<IActionResult> Ask([FromBody] AiAdvisorRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await _advisorService.AskAsync(request, userId, cancellationToken));
    }

    [HttpGet("remediation/discovery/{discoveryRunId}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Viewer)]
    public async Task<IActionResult> DiscoveryRemediation(string discoveryRunId, CancellationToken cancellationToken)
    {
        return Ok(await _advisorService.GetDiscoveryRemediationAsync(discoveryRunId, cancellationToken));
    }

    [HttpGet("remediation/migration/{jobId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Viewer)]
    public async Task<IActionResult> MigrationRemediation(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await _advisorService.GetMigrationRemediationAsync(jobId, userId, cancellationToken));
    }

    [HttpGet("remediation/validation/{validationRunId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Viewer)]
    public async Task<IActionResult> ValidationRemediation(Guid validationRunId, CancellationToken cancellationToken)
    {
        return Ok(await _advisorService.GetValidationRemediationAsync(validationRunId, cancellationToken));
    }

    [HttpGet("/api/migrations/{jobId:guid}/eta")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Viewer)]
    public async Task<IActionResult> MigrationEta(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        return Ok(await _advisorService.GetMigrationEtaAsync(jobId, userId, cancellationToken));
    }

    [HttpGet("/api/discovery/{runId}/eta-estimate")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Viewer)]
    public async Task<IActionResult> DiscoveryEta(string runId, CancellationToken cancellationToken)
    {
        return Ok(await _advisorService.GetDiscoveryEtaAsync(runId, cancellationToken));
    }
}
