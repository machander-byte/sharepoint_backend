using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Extensions;
using ZMS.API.Security;
using ZMS.Application.Contracts;
using ZMS.Application.Discovery;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/discovery")]
public class DiscoveryController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IDiscoveryService _discoveryService;
    private readonly IWebHostEnvironment _hostEnvironment;

    public DiscoveryController(IDiscoveryService discoveryService, IWebHostEnvironment hostEnvironment)
    {
        _discoveryService = discoveryService;
        _hostEnvironment = hostEnvironment;
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

    [HttpPost("start")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<StartDiscoveryScanResponse>> Start(
        [FromBody] DiscoveryScanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _discoveryService.StartScanAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{scanId:guid}/status")]
    public async Task<ActionResult<DiscoveryScanStatus>> GetScanStatus(string scanId, CancellationToken cancellationToken)
    {
        var status = await _discoveryService.GetScanStatusAsync(scanId, cancellationToken);
        return status is null ? NotFound(new { message = "Discovery scan was not found." }) : Ok(status);
    }

    [HttpGet("{scanId:guid}/results")]
    public async Task<ActionResult<DiscoveryScanResult>> GetScanResults(string scanId, CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetScanResultAsync(scanId, cancellationToken);
        return result is null ? NotFound(new { message = "Discovery scan results were not found." }) : Ok(result);
    }

    [HttpGet("latest/results")]
    public async Task<ActionResult<DiscoveryScanResult>> GetLatestCompletedResults(CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetLatestCompletedResultAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No completed discovery scan was found." }) : Ok(result);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<DiscoveryScanResult>> GetLatest(CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetLatestCompletedResultAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No completed discovery scan was found." }) : Ok(result);
    }

    [HttpGet("{scanId:guid}/inventory")]
    public async Task<ActionResult<IReadOnlyCollection<DiscoveredInventoryItem>>> GetInventory(
        string scanId,
        CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetInventoryAsync(scanId, cancellationToken);
        return result is null ? NotFound(new { message = "Discovery inventory was not found." }) : Ok(result);
    }

    [HttpGet("{scanId:guid}/permissions")]
    public async Task<ActionResult<IReadOnlyCollection<PermissionRiskFinding>>> GetPermissions(
        string scanId,
        CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetPermissionRisksAsync(scanId, cancellationToken);
        return result is null ? NotFound(new { message = "Discovery permissions were not found." }) : Ok(result);
    }

    [HttpGet("latest/permissions")]
    public async Task<ActionResult<IReadOnlyCollection<PermissionRiskFinding>>> GetLatestPermissions(CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetLatestCompletedResultAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No completed discovery scan was found." }) : Ok(result.PermissionRisks);
    }

    [HttpGet("{scanId:guid}/metadata")]
    public async Task<ActionResult<IReadOnlyCollection<MetadataFinding>>> GetMetadata(
        string scanId,
        CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetMetadataFindingsAsync(scanId, cancellationToken);
        return result is null ? NotFound(new { message = "Discovery metadata was not found." }) : Ok(result);
    }

    [HttpGet("latest/metadata")]
    public async Task<ActionResult<IReadOnlyCollection<MetadataFinding>>> GetLatestMetadata(CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetLatestCompletedResultAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No completed discovery scan was found." }) : Ok(result.MetadataFindings);
    }

    [HttpGet("{scanId:guid}/risks")]
    public async Task<ActionResult<IReadOnlyCollection<MigrationRiskFinding>>> GetRisks(
        string scanId,
        CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetMigrationRisksAsync(scanId, cancellationToken);
        return result is null ? NotFound(new { message = "Discovery risks were not found." }) : Ok(result);
    }

    [HttpGet("latest/risks")]
    public async Task<ActionResult<IReadOnlyCollection<MigrationRiskFinding>>> GetLatestRisks(CancellationToken cancellationToken)
    {
        var result = await _discoveryService.GetLatestCompletedResultAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No completed discovery scan was found." }) : Ok(result.MigrationRisks);
    }

    [HttpPost("import")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<DiscoveryImportResponse>> Import(CancellationToken cancellationToken)
    {
        try
        {
            DiscoveryScanResult? scanResult;
            if (Request.HasFormContentType)
            {
                var form = await Request.ReadFormAsync(cancellationToken);
                var file = form.Files["scanResult"] ?? form.Files["file"] ?? form.Files.FirstOrDefault();
                if (file is null || file.Length == 0)
                {
                    return BadRequest(new { message = "Upload scan-result.json as multipart field 'scanResult'." });
                }

                await using var stream = file.OpenReadStream();
                scanResult = await JsonSerializer.DeserializeAsync<DiscoveryScanResult>(stream, JsonOptions, cancellationToken);
            }
            else
            {
                scanResult = await JsonSerializer.DeserializeAsync<DiscoveryScanResult>(Request.Body, JsonOptions, cancellationToken);
            }

            if (scanResult is null)
            {
                return BadRequest(new { message = "Discovery scan result payload is required." });
            }

            var response = await _discoveryService.ImportResultAsync(scanResult, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or JsonException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("import-folder")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<DiscoveryImportResponse>> ImportFolder(
        [FromBody] DiscoveryImportFolderRequest request,
        CancellationToken cancellationToken)
    {
        if (!_hostEnvironment.IsDevelopment())
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Folder import is available only in local development environments." });
        }

        try
        {
            var response = await _discoveryService.ImportResultFromFolderAsync(request.FolderPath, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or IOException or JsonException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{scanId:guid}/export/csv")]
    public async Task<IActionResult> ExportInventoryCsv(string scanId, CancellationToken cancellationToken)
    {
        return await Export(scanId, "csv", cancellationToken);
    }

    [HttpGet("{scanId:guid}/export/json")]
    public async Task<IActionResult> ExportJson(string scanId, CancellationToken cancellationToken)
    {
        return await Export(scanId, "json", cancellationToken);
    }

    [HttpGet("{scanId:guid}/export/permissions.csv")]
    public async Task<IActionResult> ExportPermissionsCsv(string scanId, CancellationToken cancellationToken)
    {
        return await Export(scanId, "permissions", cancellationToken);
    }

    [HttpGet("{scanId:guid}/export/metadata.csv")]
    public async Task<IActionResult> ExportMetadataCsv(string scanId, CancellationToken cancellationToken)
    {
        return await Export(scanId, "metadata", cancellationToken);
    }

    [HttpGet("{scanId:guid}/export/risks.csv")]
    public async Task<IActionResult> ExportRisksCsv(string scanId, CancellationToken cancellationToken)
    {
        return await Export(scanId, "risks", cancellationToken);
    }

    private async Task<IActionResult> Export(string scanId, string exportType, CancellationToken cancellationToken)
    {
        var export = await _discoveryService.ExportAsync(scanId, exportType, cancellationToken);
        if (export is null)
        {
            return NotFound(new { message = "Discovery export was not found." });
        }

        return File(export.Content, export.ContentType, export.FileName);
    }
}
