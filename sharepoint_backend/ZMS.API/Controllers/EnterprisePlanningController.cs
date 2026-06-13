using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
public class EnterprisePlanningController : ControllerBase
{
    private readonly IEnterprisePlanningService _planningService;

    public EnterprisePlanningController(IEnterprisePlanningService planningService)
    {
        _planningService = planningService;
    }

    [HttpPost("api/onprem/discovery/import")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> ImportOnPrem([FromBody] OnPremDiscoveryImportRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _planningService.ImportOnPremAsync(request, cancellationToken));
    }

    [HttpGet("api/onprem/discovery/{runId}")]
    public async Task<IActionResult> GetOnPrem(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetOnPremAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("api/modernization/{runId}/findings")]
    public async Task<IActionResult> ModernizationFindings(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetOnPremAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result.Findings);
    }

    [HttpGet("api/modernization/{runId}/summary")]
    public async Task<IActionResult> ModernizationSummary(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetOnPremAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result.Summary);
    }

    [HttpGet("api/modernization/{runId}/assets")]
    public async Task<IActionResult> ModernizationAssets(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetOnPremAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result.Assets);
    }

    [HttpGet("api/modernization/{runId}/recommendations")]
    public async Task<IActionResult> ModernizationRecommendations(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetOnPremAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result.Recommendations);
    }

    [HttpPost("api/modernization/{assetId}/draft-spec")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> DraftSpec(string assetId, CancellationToken cancellationToken)
    {
        var result = await _planningService.CreateDraftSpecAsync(assetId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("api/modernization/{runId}/explain")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> Explain(string runId, CancellationToken cancellationToken)
    {
        return Ok(new { explanation = await _planningService.ExplainModernizationAsync(runId, cancellationToken) });
    }

    [HttpPost("api/teams/discovery/start")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<IActionResult> StartTeams([FromBody] TeamsDiscoveryStartRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _planningService.StartTeamsDiscoveryAsync(request, cancellationToken));
    }

    [HttpGet("api/teams/discovery/{runId}")]
    public async Task<IActionResult> GetTeams(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetTeamsAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("api/teams/discovery/latest")]
    public async Task<IActionResult> LatestTeams(CancellationToken cancellationToken)
    {
        var result = await _planningService.GetLatestTeamsAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("api/teams/discovery/{runId}/topology")]
    public async Task<IActionResult> TeamsTopology(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetTeamsAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result.Topology);
    }

    [HttpGet("api/teams/discovery/{runId}/risks")]
    public async Task<IActionResult> TeamsRisks(string runId, CancellationToken cancellationToken)
    {
        var result = await _planningService.GetTeamsAsync(runId, cancellationToken);
        return result is null ? NotFound() : Ok(result.Risks);
    }
}
