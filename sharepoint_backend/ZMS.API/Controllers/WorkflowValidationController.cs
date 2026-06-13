using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZMS.API.Security;
using ZMS.Application.Contracts;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/workflow-validation")]
public sealed class WorkflowValidationController : ControllerBase
{
    private readonly IWorkflowValidationService _service;
    public WorkflowValidationController(IWorkflowValidationService service) => _service = service;

    [HttpPost("run-full-chain")]
    [Authorize(Policy = ZmsAuthorizationPolicies.Operator)]
    public async Task<ActionResult<WorkflowValidationResponse>> RunFullChain([FromBody] WorkflowValidationRequest request, CancellationToken cancellationToken) =>
        Ok(await _service.RunFullChainAsync(request ?? new WorkflowValidationRequest(), cancellationToken));

    [HttpGet("{workflowRunId:guid}")]
    public async Task<ActionResult<WorkflowValidationRun>> Get(string workflowRunId, CancellationToken cancellationToken)
    {
        var result = await _service.GetAsync(workflowRunId, cancellationToken);
        return result is null ? NotFound(new { message = "Workflow validation run was not found." }) : Ok(result);
    }

    [HttpGet("latest")]
    public async Task<ActionResult<WorkflowValidationRun>> Latest(CancellationToken cancellationToken)
    {
        var result = await _service.GetLatestAsync(cancellationToken);
        return result is null ? NotFound(new { message = "No workflow validation run is available." }) : Ok(result);
    }

    [HttpGet("{workflowRunId:guid}/export/markdown")]
    public Task<IActionResult> ExportMarkdown(string workflowRunId, CancellationToken cancellationToken) => Export(workflowRunId, "markdown", cancellationToken);

    [HttpGet("{workflowRunId:guid}/export/json")]
    public Task<IActionResult> ExportJson(string workflowRunId, CancellationToken cancellationToken) => Export(workflowRunId, "json", cancellationToken);

    private async Task<IActionResult> Export(string workflowRunId, string exportType, CancellationToken cancellationToken)
    {
        var export = await _service.ExportAsync(workflowRunId, exportType, cancellationToken);
        return export is null ? NotFound(new { message = "Workflow validation export was not found." }) : File(export.Content, export.ContentType, export.FileName);
    }
}
