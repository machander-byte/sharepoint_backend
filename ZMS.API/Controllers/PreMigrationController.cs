using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/pre-migration")]
public sealed class PreMigrationController : ControllerBase
{
    private readonly IPreMigrationValidationService _service;
    public PreMigrationController(IPreMigrationValidationService service) => _service = service;

    [HttpPost("validate/{planId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<PreMigrationValidationResponse>> Validate(string planId, CancellationToken cancellationToken)
    {
        var result = await _service.ValidateAsync(planId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration plan was not found." }) : Ok(result);
    }

    [HttpGet("validations/{validationId:guid}")]
    public async Task<ActionResult<PreMigrationValidationResult>> GetValidation(string validationId, CancellationToken cancellationToken)
    {
        var result = await _service.GetValidationAsync(validationId, cancellationToken);
        return result is null ? NotFound(new { message = "Pre-migration validation was not found." }) : Ok(result);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<PreMigrationValidationResult>> GetLatestValidation(CancellationToken cancellationToken)
    {
        var result = await _service.GetLatestValidationAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No pre-migration validation is available." }) : Ok(result);
    }

    [HttpPost("simulate/{planId:guid}")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<ExecutionSimulationResponse>> Simulate(string planId, CancellationToken cancellationToken)
    {
        var result = await _service.SimulateAsync(planId, cancellationToken);
        return result is null ? NotFound(new { message = "Migration plan was not found." }) : Ok(result);
    }

    [HttpGet("simulations/{simulationId:guid}")]
    public async Task<ActionResult<ExecutionSimulationResult>> GetSimulation(string simulationId, CancellationToken cancellationToken)
    {
        var result = await _service.GetSimulationAsync(simulationId, cancellationToken);
        return result is null ? NotFound(new { message = "Execution simulation was not found." }) : Ok(result);
    }

    [HttpGet("simulations/latest")]
    public async Task<ActionResult<ExecutionSimulationResult>> GetLatestSimulation(CancellationToken cancellationToken)
    {
        var result = await _service.GetLatestSimulationAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No execution simulation is available." }) : Ok(result);
    }

    [HttpGet("{validationId:guid}/export/json")]
    public Task<IActionResult> ExportValidationJson(string validationId, CancellationToken cancellationToken) => ExportValidation(validationId, "json", cancellationToken);
    [HttpGet("{validationId:guid}/export/csv")]
    public Task<IActionResult> ExportValidationCsv(string validationId, CancellationToken cancellationToken) => ExportValidation(validationId, "csv", cancellationToken);
    [HttpGet("{validationId:guid}/export/markdown")]
    public Task<IActionResult> ExportValidationMarkdown(string validationId, CancellationToken cancellationToken) => ExportValidation(validationId, "markdown", cancellationToken);
    [HttpGet("simulations/{simulationId:guid}/export/json")]
    public Task<IActionResult> ExportSimulationJson(string simulationId, CancellationToken cancellationToken) => ExportSimulation(simulationId, "json", cancellationToken);
    [HttpGet("simulations/{simulationId:guid}/export/markdown")]
    public Task<IActionResult> ExportSimulationMarkdown(string simulationId, CancellationToken cancellationToken) => ExportSimulation(simulationId, "markdown", cancellationToken);

    private async Task<IActionResult> ExportValidation(string id, string type, CancellationToken cancellationToken)
    {
        var export = await _service.ExportValidationAsync(id, type, cancellationToken);
        return export is null ? NotFound(new { message = "Validation export was not found." }) : File(export.Content, export.ContentType, export.FileName);
    }
    private async Task<IActionResult> ExportSimulation(string id, string type, CancellationToken cancellationToken)
    {
        var export = await _service.ExportSimulationAsync(id, type, cancellationToken);
        return export is null ? NotFound(new { message = "Simulation export was not found." }) : File(export.Content, export.ContentType, export.FileName);
    }
}
