using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/sharepoint-migration")]
public sealed class SharePointMigrationController : ControllerBase
{
    private readonly ISharePointMigrationCapabilityService _capabilities;
    private readonly IMigrationTransferPreviewService _preview;
    private readonly ILivePilotMigrationService _pilot;

    public SharePointMigrationController(ISharePointMigrationCapabilityService capabilities, IMigrationTransferPreviewService preview, ILivePilotMigrationService pilot)
    {
        _capabilities = capabilities;
        _preview = preview;
        _pilot = pilot;
    }

    [HttpPost("capabilities/validate")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<SharePointMigrationCapabilityResult>> ValidateCapabilities([FromBody] SharePointMigrationCapabilityRequest request, CancellationToken cancellationToken) =>
        Ok(await _capabilities.ValidateAsync(request, cancellationToken));

    [HttpPost("preview/from-job/{jobId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<MigrationTransferPreview>> PreviewFromJob(string jobId, CancellationToken cancellationToken)
    {
        var result = await _preview.BuildFromJobAsync(jobId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration execution job was not found." }) : Ok(result);
    }

    [HttpGet("preview/{previewId:guid}")]
    public async Task<ActionResult<MigrationTransferPreview>> GetPreview(string previewId, CancellationToken cancellationToken)
    {
        var result = await _preview.GetAsync(previewId, cancellationToken);
        return result is null ? NotFound(new { message = "Transfer preview was not found." }) : Ok(result);
    }

    [HttpPost("pilot/from-job/{jobId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<LivePilotMigrationResult>> PilotFromJob(string jobId, [FromBody] LivePilotMigrationRequest request, CancellationToken cancellationToken)
    {
        var result = await _pilot.RunFromJobAsync(jobId, request, cancellationToken);
        return result is null ? NotFound(new { message = "Migration execution job was not found." }) : Ok(result);
    }

    [HttpGet("pilot/{pilotRunId:guid}")]
    public async Task<ActionResult<LivePilotMigrationResult>> GetPilot(string pilotRunId, CancellationToken cancellationToken)
    {
        var result = await _pilot.GetAsync(pilotRunId, cancellationToken);
        return result is null ? NotFound(new { message = "Pilot run was not found." }) : Ok(result);
    }

    [HttpGet("pilot/{pilotRunId:guid}/report/json")]
    public Task<IActionResult> PilotJson(string pilotRunId, CancellationToken cancellationToken) => ExportPilot(pilotRunId, "json", cancellationToken);
    [HttpGet("pilot/{pilotRunId:guid}/report/csv")]
    public Task<IActionResult> PilotCsv(string pilotRunId, CancellationToken cancellationToken) => ExportPilot(pilotRunId, "csv", cancellationToken);
    [HttpGet("pilot/{pilotRunId:guid}/report/markdown")]
    public Task<IActionResult> PilotMarkdown(string pilotRunId, CancellationToken cancellationToken) => ExportPilot(pilotRunId, "markdown", cancellationToken);
    [HttpGet("preview/{previewId:guid}/report/json")]
    public Task<IActionResult> PreviewJson(string previewId, CancellationToken cancellationToken) => ExportPreview(previewId, "json", cancellationToken);
    [HttpGet("preview/{previewId:guid}/report/csv")]
    public Task<IActionResult> PreviewCsv(string previewId, CancellationToken cancellationToken) => ExportPreview(previewId, "csv", cancellationToken);

    private async Task<IActionResult> ExportPilot(string id, string type, CancellationToken cancellationToken)
    {
        var export = await _pilot.ExportPilotAsync(id, type, cancellationToken);
        return export is null ? NotFound(new { message = "Pilot report was not found." }) : File(export.Content, export.ContentType, export.FileName);
    }

    private async Task<IActionResult> ExportPreview(string id, string type, CancellationToken cancellationToken)
    {
        var export = await _pilot.ExportPreviewAsync(id, type, cancellationToken);
        return export is null ? NotFound(new { message = "Transfer preview report was not found." }) : File(export.Content, export.ContentType, export.FileName);
    }
}
