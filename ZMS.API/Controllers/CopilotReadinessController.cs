using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/copilot-readiness")]
public class CopilotReadinessController : ControllerBase
{
    private readonly ICopilotReadinessService _readinessService;

    public CopilotReadinessController(ICopilotReadinessService readinessService)
    {
        _readinessService = readinessService;
    }

    [HttpGet("{discoveryRunId}")]
    public async Task<IActionResult> Get(string discoveryRunId, CancellationToken cancellationToken)
    {
        var result = await _readinessService.AnalyzeAsync(discoveryRunId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("latest")]
    public async Task<IActionResult> Latest(CancellationToken cancellationToken)
    {
        var result = await _readinessService.AnalyzeLatestAsync(cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
