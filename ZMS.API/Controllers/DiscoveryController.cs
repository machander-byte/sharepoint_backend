using Microsoft.AspNetCore.Mvc;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiscoveryController : ControllerBase
{
    private readonly IDiscoveryService _discoveryService;

    public DiscoveryController(IDiscoveryService discoveryService)
    {
        _discoveryService = discoveryService;
    }

    [HttpGet("{sourceConnectionId:guid}/sites")]
    public async Task<IActionResult> GetSites(Guid sourceConnectionId, CancellationToken cancellationToken)
    {
        var sites = await _discoveryService.GetSitesAsync(sourceConnectionId, cancellationToken);
        return Ok(sites);
    }

    [HttpGet("{sourceConnectionId:guid}/libraries")]
    public async Task<IActionResult> GetLibraries(
        Guid sourceConnectionId,
        [FromQuery] string sourceLocation,
        CancellationToken cancellationToken)
    {
        var libraries = await _discoveryService.GetLibrariesAsync(sourceConnectionId, sourceLocation, cancellationToken);
        return Ok(libraries);
    }

    [HttpGet("{sourceConnectionId:guid}/summary")]
    public async Task<IActionResult> GetSummary(
        Guid sourceConnectionId,
        [FromQuery] string sourceLocation,
        [FromQuery] string? libraryName,
        CancellationToken cancellationToken)
    {
        var summary = await _discoveryService.GetSummaryAsync(sourceConnectionId, sourceLocation, libraryName, cancellationToken);
        return Ok(summary);
    }
}
