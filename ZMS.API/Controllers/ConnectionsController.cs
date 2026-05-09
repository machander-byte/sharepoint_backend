using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Contracts;
using ZMS.API.Contracts.Connections;
using ZMS.API.Extensions;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectionsController : ControllerBase
{
    private readonly IConnectionService _connectionService;

    public ConnectionsController(IConnectionService connectionService)
    {
        _connectionService = connectionService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<ConnectionResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var connections = await _connectionService.ListAsync(userId, cancellationToken);
        return Ok(connections.Select(connection => connection.ToResponse()).ToArray());
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ConnectionResponseDto>> Create(
        [FromBody] CreateConnectionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var connection = await _connectionService.CreateAsync(request.ToApplicationRequest(), userId, cancellationToken);
            return Created($"/api/connections/{connection.Id}", connection.ToResponse());
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("{connectionId:guid}/test")]
    [Authorize]
    public async Task<ActionResult<ConnectionTestResponseDto>> Test(Guid connectionId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        var result = await _connectionService.TestConnectionAsync(connectionId, userId, cancellationToken);
        return Ok(result.ToResponse());
    }
}
