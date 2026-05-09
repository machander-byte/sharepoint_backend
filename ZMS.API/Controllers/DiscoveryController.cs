using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Extensions;
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
    [Authorize]
    public async Task<IActionResult> GetSites(Guid sourceConnectionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var sites = await _discoveryService.GetSitesAsync(sourceConnectionId, userId, cancellationToken);
        return Ok(sites);
    }

    [HttpGet("{sourceConnectionId:guid}/libraries")]
    [Authorize]
    public async Task<IActionResult> GetLibraries(
        Guid sourceConnectionId,
        [FromQuery] string sourceLocation,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var libraries = await _discoveryService.GetLibrariesAsync(sourceConnectionId, sourceLocation, userId, cancellationToken);
        return Ok(libraries);
    }

    [HttpGet("{sourceConnectionId:guid}/summary")]
    [Authorize]
    public async Task<IActionResult> GetSummary(
        Guid sourceConnectionId,
        [FromQuery] string sourceLocation,
        [FromQuery] string? libraryName,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var summary = await _discoveryService.GetSummaryAsync(sourceConnectionId, sourceLocation, libraryName, userId, cancellationToken);
        return Ok(summary);
    }
}
