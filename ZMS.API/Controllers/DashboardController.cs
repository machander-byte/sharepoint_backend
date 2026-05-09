using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Extensions;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [Authorize]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var summary = await _dashboardService.GetSummaryAsync(userId, cancellationToken);
        return Ok(summary);
    }
}
