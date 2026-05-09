using Microsoft.AspNetCore.Mvc;
using ZMS.API.Contracts;
using ZMS.API.Contracts.Connections;
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
    public async Task<ActionResult<IReadOnlyCollection<ConnectionResponseDto>>> GetAll(CancellationToken cancellationToken)
    {
        var connections = await _connectionService.ListAsync(cancellationToken);
        return Ok(connections.Select(connection => connection.ToResponse()).ToArray());
    }

    [HttpPost]
    public async Task<ActionResult<ConnectionResponseDto>> Create(
        [FromBody] CreateConnectionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var connection = await _connectionService.CreateAsync(request.ToApplicationRequest(), cancellationToken);
            return Created($"/api/connections/{connection.Id}", connection.ToResponse());
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("{connectionId:guid}/test")]
    public async Task<ActionResult<ConnectionTestResponseDto>> Test(Guid connectionId, CancellationToken cancellationToken)
    {
        var result = await _connectionService.TestConnectionAsync(connectionId, cancellationToken);
        return Ok(result.ToResponse());
    }
}
