using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.EnvironmentBridge;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/environment-config")]
public sealed class EnvironmentConfigController : ControllerBase
{
    private readonly IEnvironmentConfigValidator _validator;
    private readonly IEnvironmentConfigStorageService _storageService;

    public EnvironmentConfigController(
        IEnvironmentConfigValidator validator,
        IEnvironmentConfigStorageService storageService)
    {
        _validator = validator;
        _storageService = storageService;
    }

    [HttpPost("validate")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public ActionResult<ValidationResult> Validate([FromBody] EnvironmentConfig config)
    {
        var validation = _validator.Validate(config);
        return Ok(validation);
    }

    [HttpPost("save")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<SaveConfigResponse>> Save([FromBody] EnvironmentConfig config, CancellationToken cancellationToken)
    {
        var validation = _validator.Validate(config);
        if (!validation.IsValid)
        {
            return BadRequest(validation);
        }

        var result = await _storageService.SaveAsync(config, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{configId}")]
    public async Task<ActionResult<EnvironmentConfig>> Get(string configId, CancellationToken cancellationToken)
    {
        var config = await _storageService.GetAsync(configId, cancellationToken);
        if (config is null)
        {
            return NotFound(new { message = "Environment config was not found." });
        }

        return Ok(config);
    }
}
