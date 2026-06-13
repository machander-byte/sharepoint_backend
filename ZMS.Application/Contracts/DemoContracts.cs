namespace ZMS.Application.Contracts;

public interface IDemoService
{
    Task<DemoStatus> ResetAsync(CancellationToken cancellationToken);
    Task<DemoStatus> SeedAsync(CancellationToken cancellationToken);
    Task<DemoStatus> RunScriptedChainAsync(CancellationToken cancellationToken);
    Task<DemoStatus> GetStatusAsync(CancellationToken cancellationToken);
}

public sealed class DemoStatus
{
    public bool DemoMode { get; set; }
    public bool Seeded { get; set; }
    public string LatestScanId { get; set; } = string.Empty;
    public string LatestAssessmentId { get; set; } = string.Empty;
    public string LatestPlanId { get; set; } = string.Empty;
    public string LatestExecutionJobId { get; set; } = string.Empty;
    public string LatestPreviewId { get; set; } = string.Empty;
    public string LatestWorkflowRunId { get; set; } = string.Empty;
    public string LastDemoChainResult { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
}
