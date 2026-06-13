using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/demo")]
public sealed class DemoController : ControllerBase
{
    private readonly IDemoService _demo;
    public DemoController(IDemoService demo) => _demo = demo;

    [HttpPost("reset")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Admin)]
    public async Task<ActionResult<DemoStatus>> Reset(CancellationToken cancellationToken) => Ok(await _demo.ResetAsync(cancellationToken));

    [HttpPost("seed")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Admin)]
    public async Task<ActionResult<DemoStatus>> Seed(CancellationToken cancellationToken) => Ok(await _demo.SeedAsync(cancellationToken));

    [HttpPost("run-scripted-chain")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<DemoStatus>> RunScriptedChain(CancellationToken cancellationToken) => Ok(await _demo.RunScriptedChainAsync(cancellationToken));

    [HttpGet("status")]
    public async Task<ActionResult<DemoStatus>> Status(CancellationToken cancellationToken) => Ok(await _demo.GetStatusAsync(cancellationToken));
}
