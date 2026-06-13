namespace ZMS.Application.Contracts;

public sealed class OnPremDiscoveryImportRequest
{
    public string FarmUrl { get; set; } = string.Empty;
    public string Version { get; set; } = "SharePoint2019";
    public string ScanMethod { get; set; } = "ManifestImport";
    public List<ModernizationAssetDto> Assets { get; set; } = [];
}

public sealed class OnPremDiscoveryResult
{
    public string RunId { get; set; } = string.Empty;
    public string SourceType { get; set; } = "SharePointOnPrem";
    public string FarmUrl { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Dictionary<string, int> Summary { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ModernizationAssetDto> Assets { get; set; } = [];
    public List<ModernizationFindingDto> Findings { get; set; } = [];
    public List<ModernizationRecommendationDto> Recommendations { get; set; } = [];
}

public sealed class ModernizationAssetDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ModernizationFindingDto
{
    public string Id { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Complexity { get; set; } = "Medium";
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public bool RequiresHumanReview { get; set; } = true;
}

public sealed class ModernizationRecommendationDto
{
    public string AssetId { get; set; } = string.Empty;
    public string ModernizationTarget { get; set; } = string.Empty;
    public string Feasibility { get; set; } = "Medium";
    public string EstimatedEffort { get; set; } = "Medium";
    public List<string> Blockers { get; set; } = [];
    public bool AutomationEligible { get; set; }
}

public sealed class ModernizationDraftSpec
{
    public string AssetId { get; set; } = string.Empty;
    public string AssetType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public Dictionary<string, object> Specification { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public bool RequiresHumanReview { get; set; } = true;
}

public sealed class TeamsDiscoveryStartRequest
{
    public string Name { get; set; } = "Teams discovery fixture";
    public List<DiscoveredTeamDto> Teams { get; set; } = [];
}

public sealed class TeamsDiscoveryResult
{
    public string RunId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<DiscoveredTeamDto> Teams { get; set; } = [];
    public List<TeamsRiskFindingDto> Risks { get; set; } = [];
    public List<TeamsTopologyEdge> Topology { get; set; } = [];
}

public sealed class DiscoveredTeamDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<string> Owners { get; set; } = [];
    public List<string> Members { get; set; } = [];
    public List<string> Guests { get; set; } = [];
    public List<DiscoveredChannelDto> Channels { get; set; } = [];
    public string SharePointSiteUrl { get; set; } = string.Empty;
}

public sealed class DiscoveredChannelDto
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ChannelType { get; set; } = "Standard";
    public string FilesFolderUrl { get; set; } = string.Empty;
    public List<string> AppsOrTabs { get; set; } = [];
}

public sealed class TeamsRiskFindingDto
{
    public string Id { get; set; } = string.Empty;
    public string TeamId { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
}

public sealed class TeamsTopologyEdge
{
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
}

public interface IEnterprisePlanningService
{
    Task<OnPremDiscoveryResult> ImportOnPremAsync(OnPremDiscoveryImportRequest request, CancellationToken cancellationToken);
    Task<OnPremDiscoveryResult?> GetOnPremAsync(string runId, CancellationToken cancellationToken);
    Task<TeamsDiscoveryResult> StartTeamsDiscoveryAsync(TeamsDiscoveryStartRequest request, CancellationToken cancellationToken);
    Task<TeamsDiscoveryResult?> GetTeamsAsync(string runId, CancellationToken cancellationToken);
    Task<TeamsDiscoveryResult?> GetLatestTeamsAsync(CancellationToken cancellationToken);
    Task<ModernizationDraftSpec?> CreateDraftSpecAsync(string assetId, CancellationToken cancellationToken);
    Task<string> ExplainModernizationAsync(string runId, CancellationToken cancellationToken);
}
