using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.EnvironmentBridge;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/environment-package")]
public sealed class EnvironmentPackageController : ControllerBase
{
    private readonly IEnvironmentConfigValidator _validator;
    private readonly IEnvironmentPackageGenerator _packageGenerator;

    public EnvironmentPackageController(
        IEnvironmentConfigValidator validator,
        IEnvironmentPackageGenerator packageGenerator)
    {
        _validator = validator;
        _packageGenerator = packageGenerator;
    }

    [HttpPost("generate")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<GeneratedPackageResult>> Generate([FromBody] EnvironmentConfig config, CancellationToken cancellationToken)
    {
        var validation = _validator.Validate(config);
        if (!validation.IsValid)
        {
            return BadRequest(validation);
        }

        var result = await _packageGenerator.GenerateAsync(config, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{packageId}/manifest")]
    public async Task<ActionResult<PackageManifest>> GetManifest(string packageId, CancellationToken cancellationToken)
    {
        var manifest = await _packageGenerator.GetManifestAsync(packageId, cancellationToken);
        if (manifest is null)
        {
            return NotFound(new { message = "Environment package manifest was not found." });
        }

        return Ok(manifest);
    }

    [HttpGet("{packageId}/download")]
    public IActionResult Download(string packageId)
    {
        var zipPath = _packageGenerator.GetPackageZipPath(packageId);
        if (zipPath is null)
        {
            return NotFound(new { message = "Environment package was not found." });
        }

        return PhysicalFile(zipPath, "application/zip", $"zms-sharepoint-environment-package-{packageId}.zip");
    }
}
