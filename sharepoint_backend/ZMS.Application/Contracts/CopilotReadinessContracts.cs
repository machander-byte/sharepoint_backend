namespace ZMS.Application.Contracts;

public sealed class CopilotReadinessResult
{
    public string DiscoveryRunId { get; set; } = string.Empty;
    public int OverallScore { get; set; }
    public string RiskTier { get; set; } = "Low";
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, int> CategoryScores { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<CopilotFinding> TopFindings { get; set; } = [];
    public List<string> RecommendedActions { get; set; } = [];
}

public sealed class CopilotFinding
{
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low";
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public interface ICopilotReadinessService
{
    Task<CopilotReadinessResult?> AnalyzeAsync(string discoveryRunId, CancellationToken cancellationToken);
    Task<CopilotReadinessResult?> AnalyzeLatestAsync(CancellationToken cancellationToken);
}
