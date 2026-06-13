namespace ZMS.Application.Contracts;

public sealed class AiAdvisorRequest
{
    public string Question { get; set; } = string.Empty;
    public string? DiscoveryRunId { get; set; }
    public Guid? MigrationJobId { get; set; }
    public Guid? ValidationRunId { get; set; }
}

public sealed class AiAdvisorResponse
{
    public string Answer { get; set; } = string.Empty;
    public bool UsedOllama { get; set; }
    public string Model { get; set; } = string.Empty;
    public string? Warning { get; set; }
    public object ContextSummary { get; set; } = new();
}

public sealed class RemediationItem
{
    public string Issue { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string RecommendedFix { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public bool AutomationEligible { get; set; }
    public double Confidence { get; set; }
    public string SourceFindingId { get; set; } = string.Empty;
}

public sealed class EtaEstimate
{
    public TimeSpan EstimatedDuration { get; set; }
    public double Confidence { get; set; }
    public string BottleneckExplanation { get; set; } = string.Empty;
    public List<string> Assumptions { get; set; } = [];
    public List<string> OptimizationRecommendations { get; set; } = [];
}
