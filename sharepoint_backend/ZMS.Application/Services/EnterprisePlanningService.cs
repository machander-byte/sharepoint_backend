using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;

namespace ZMS.Application.Services;

public class EnterprisePlanningService : IEnterprisePlanningService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly string _rootPath;
    private readonly IOllamaClient _ollamaClient;

    public EnterprisePlanningService(IHostEnvironment hostEnvironment, IOllamaClient ollamaClient)
    {
        _rootPath = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "planning-intelligence");
        _ollamaClient = ollamaClient;
    }

    public async Task<OnPremDiscoveryResult> ImportOnPremAsync(OnPremDiscoveryImportRequest request, CancellationToken cancellationToken)
    {
        var result = new OnPremDiscoveryResult
        {
            RunId = Guid.NewGuid().ToString("D"),
            FarmUrl = request.FarmUrl,
            Version = request.Version,
            Assets = request.Assets.Count == 0 ? DefaultOnPremAssets() : request.Assets
        };

        result.Findings = result.Assets.Select(Classify).ToList();
        result.Recommendations = result.Assets.Select(Recommend).ToList();
        result.Summary = result.Assets
            .GroupBy(asset => asset.AssetType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        await WriteJsonAsync(OnPremPath(result.RunId), result, cancellationToken);
        return result;
    }

    public Task<OnPremDiscoveryResult?> GetOnPremAsync(string runId, CancellationToken cancellationToken)
    {
        return ReadJsonAsync<OnPremDiscoveryResult>(OnPremPath(runId), cancellationToken);
    }

    public async Task<TeamsDiscoveryResult> StartTeamsDiscoveryAsync(TeamsDiscoveryStartRequest request, CancellationToken cancellationToken)
    {
        var teams = request.Teams.Count == 0 ? DefaultTeams() : request.Teams;
        var result = new TeamsDiscoveryResult
        {
            RunId = Guid.NewGuid().ToString("D"),
            Name = request.Name,
            Teams = teams,
            Risks = teams.SelectMany(AnalyzeTeamsRisks).ToList(),
            Topology = teams.SelectMany(BuildTopology).ToList()
        };

        await WriteJsonAsync(TeamsPath(result.RunId), result, cancellationToken);
        await WriteJsonAsync(Path.Combine(_rootPath, "teams", "latest.json"), result, cancellationToken);
        return result;
    }

    public Task<TeamsDiscoveryResult?> GetTeamsAsync(string runId, CancellationToken cancellationToken)
    {
        return ReadJsonAsync<TeamsDiscoveryResult>(TeamsPath(runId), cancellationToken);
    }

    public Task<TeamsDiscoveryResult?> GetLatestTeamsAsync(CancellationToken cancellationToken)
    {
        return ReadJsonAsync<TeamsDiscoveryResult>(Path.Combine(_rootPath, "teams", "latest.json"), cancellationToken);
    }

    public async Task<ModernizationDraftSpec?> CreateDraftSpecAsync(string assetId, CancellationToken cancellationToken)
    {
        var asset = await FindAssetAsync(assetId, cancellationToken);
        return asset is null ? null : Draft(asset);
    }

    public async Task<string> ExplainModernizationAsync(string runId, CancellationToken cancellationToken)
    {
        var result = await GetOnPremAsync(runId, cancellationToken);
        if (result is null)
        {
            return "Modernization run was not found.";
        }

        var context = new
        {
            result.RunId,
            result.Version,
            result.Summary,
            TopFindings = result.Findings.Take(10),
            Recommendations = result.Recommendations.Take(10)
        };

        var ollama = await _ollamaClient.GenerateAsync(
            "Explain this SharePoint modernization plan. Do not claim automatic conversion. Label all generated specs as drafts requiring human review.",
            "Create an executive and technical modernization explanation.",
            context,
            cancellationToken);

        return ollama.IsAvailable
            ? ollama.Answer ?? FallbackModernizationExplanation(result)
            : FallbackModernizationExplanation(result);
    }

    private async Task<ModernizationAssetDto?> FindAssetAsync(string assetId, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(_rootPath, "onprem");
        if (!Directory.Exists(directory))
        {
            return null;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.json"))
        {
            var result = await ReadJsonAsync<OnPremDiscoveryResult>(path, cancellationToken);
            var asset = result?.Assets.FirstOrDefault(item => item.Id.Equals(assetId, StringComparison.OrdinalIgnoreCase));
            if (asset is not null)
            {
                return asset;
            }
        }

        return null;
    }

    private static ModernizationFindingDto Classify(ModernizationAssetDto asset)
    {
        var recommendation = Recommend(asset);
        return new ModernizationFindingDto
        {
            Id = StableId("finding", asset.Id),
            AssetId = asset.Id,
            AssetType = asset.AssetType,
            Complexity = recommendation.Feasibility == "Low" ? "Critical / Manual rebuild required" : recommendation.EstimatedEffort,
            Description = $"{asset.AssetType} requires modernization assessment before SharePoint Online migration.",
            Recommendation = $"Modernize to {recommendation.ModernizationTarget}.",
            RequiresHumanReview = !recommendation.AutomationEligible || recommendation.EstimatedEffort is "High" or "Critical"
        };
    }

    private static ModernizationRecommendationDto Recommend(ModernizationAssetDto asset)
    {
        var type = asset.AssetType;
        return new ModernizationRecommendationDto
        {
            AssetId = asset.Id,
            ModernizationTarget = type switch
            {
                "SharePointDesignerWorkflow" => "Power Automate",
                "NintexWorkflow" => "Power Automate with unsupported action review",
                "K2Workflow" => "Azure Logic Apps or Power Automate with manual redesign",
                "CustomCSharpWorkflow" => "Azure Logic Apps with manual redesign",
                "InfoPathForm" => "Power Apps / modern SharePoint forms",
                "NintexForm" => "Power Apps with control review",
                "K2SmartForm" => "Power Apps with manual redesign",
                "ASPXPage" => "Modern SharePoint Page or SPFx Web Part",
                "CustomMasterPage" => "Modern SharePoint branding / SPFx review",
                "SSRSReport" or "TableauReport" or "CognosReport" or "ExcelReport" => "Power BI",
                _ => "Manual Redesign"
            },
            Feasibility = type.Contains("Custom", StringComparison.OrdinalIgnoreCase) || type.Contains("K2", StringComparison.OrdinalIgnoreCase) ? "Low" : "Medium",
            EstimatedEffort = type.Contains("Custom", StringComparison.OrdinalIgnoreCase) || type.Contains("K2", StringComparison.OrdinalIgnoreCase) ? "High" : "Medium",
            AutomationEligible = type is "SharePointDesignerWorkflow" or "InfoPathForm" or "ExcelReport",
            Blockers = type.Contains("Custom", StringComparison.OrdinalIgnoreCase) ? ["Custom code dependency"] : []
        };
    }

    private static ModernizationDraftSpec Draft(ModernizationAssetDto asset)
    {
        var recommendation = Recommend(asset);
        return new ModernizationDraftSpec
        {
            AssetId = asset.Id,
            AssetType = asset.AssetType,
            Target = recommendation.ModernizationTarget,
            RequiresHumanReview = true,
            Specification = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceName"] = asset.Name,
                ["sourceLocation"] = asset.Location,
                ["target"] = recommendation.ModernizationTarget,
                ["blockers"] = recommendation.Blockers,
                ["automationEligibility"] = recommendation.AutomationEligible,
                ["draftSections"] = asset.AssetType.Contains("Workflow", StringComparison.OrdinalIgnoreCase)
                    ? new[] { "trigger", "actions", "conditions", "approvals", "connectorsNeeded", "unsupportedActions" }
                    : asset.AssetType.Contains("Form", StringComparison.OrdinalIgnoreCase)
                        ? new[] { "dataSources", "fields", "validationRules", "targetScreens", "unsupportedControls" }
                        : new[] { "dataSources", "metrics", "visuals", "targetPowerBiDataset", "targetReport" }
            }
        };
    }

    private static IEnumerable<TeamsRiskFindingDto> AnalyzeTeamsRisks(DiscoveredTeamDto team)
    {
        if (team.Owners.Count == 0)
        {
            yield return TeamsRisk(team, "NoOwner", "Critical", "Team has no owner.", "Assign at least two accountable owners.");
        }

        if (team.Owners.Count > 10)
        {
            yield return TeamsRisk(team, "TooManyOwners", "Medium", "Team has many owners.", "Review ownership model.");
        }

        if (team.Guests.Count > 0)
        {
            yield return TeamsRisk(team, "GuestAccess", "High", "Team includes guest users.", "Review guest access before migration planning.");
        }

        if (string.IsNullOrWhiteSpace(team.SharePointSiteUrl))
        {
            yield return TeamsRisk(team, "MissingSharePointMapping", "High", "Associated SharePoint site mapping is missing.", "Resolve SharePoint backing site before migration planning.");
        }

        foreach (var channel in team.Channels.Where(channel => !channel.ChannelType.Equals("Standard", StringComparison.OrdinalIgnoreCase)))
        {
            yield return TeamsRisk(team, "PrivateOrSharedChannel", "Medium", $"{channel.ChannelType} channel '{channel.DisplayName}' may have a separate SharePoint dependency.", "Map private/shared channel sites before migration.");
        }
    }

    private static IEnumerable<TeamsTopologyEdge> BuildTopology(DiscoveredTeamDto team)
    {
        yield return new TeamsTopologyEdge { Source = team.Id, Target = $"group:{team.Id}", Relationship = "Team -> Microsoft 365 Group" };
        if (!string.IsNullOrWhiteSpace(team.SharePointSiteUrl))
        {
            yield return new TeamsTopologyEdge { Source = team.Id, Target = team.SharePointSiteUrl, Relationship = "Team -> SharePoint Site" };
        }

        foreach (var channel in team.Channels)
        {
            yield return new TeamsTopologyEdge { Source = team.Id, Target = channel.Id, Relationship = "Team -> Channel" };
            if (!string.IsNullOrWhiteSpace(channel.FilesFolderUrl))
            {
                yield return new TeamsTopologyEdge { Source = channel.Id, Target = channel.FilesFolderUrl, Relationship = "Channel -> Files Folder" };
            }
        }
    }

    private static TeamsRiskFindingDto TeamsRisk(DiscoveredTeamDto team, string category, string severity, string description, string recommendation)
    {
        return new TeamsRiskFindingDto
        {
            Id = StableId("teams-risk", team.Id, category),
            TeamId = team.Id,
            Category = category,
            Severity = severity,
            Description = description,
            Recommendation = recommendation
        };
    }

    private static List<ModernizationAssetDto> DefaultOnPremAssets() =>
    [
        new() { Id = "spd-approval", Name = "Invoice approval", AssetType = "SharePointDesignerWorkflow", Location = "/sites/finance/Invoices" },
        new() { Id = "infopath-hr", Name = "Employee onboarding form", AssetType = "InfoPathForm", Location = "/sites/hr/Forms" },
        new() { Id = "custom-master", Name = "Legacy publishing master", AssetType = "CustomMasterPage", Location = "/sites/corp" }
    ];

    private static List<DiscoveredTeamDto> DefaultTeams() =>
    [
        new()
        {
            Id = "team-finance",
            DisplayName = "Finance Migration Pilot",
            Owners = ["finance.owner@contoso.com"],
            Members = ["finance.user@contoso.com"],
            Guests = ["external.partner@example.com"],
            SharePointSiteUrl = "https://contoso.sharepoint.com/sites/FinanceMigrationPilot",
            Channels =
            [
                new() { Id = "team-finance-general", DisplayName = "General", ChannelType = "Standard", FilesFolderUrl = "https://contoso.sharepoint.com/sites/FinanceMigrationPilot/Shared Documents/General" },
                new() { Id = "team-finance-private", DisplayName = "Leadership", ChannelType = "Private", FilesFolderUrl = "" }
            ]
        }
    ];

    private string OnPremPath(string runId) => Path.Combine(_rootPath, "onprem", $"{runId}.json");
    private string TeamsPath(string runId) => Path.Combine(_rootPath, "teams", $"{runId}.json");

    private async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    private static string FallbackModernizationExplanation(OnPremDiscoveryResult result)
    {
        return $"Modernization run {result.RunId} found {result.Assets.Count} legacy assets. The output is a draft modernization plan requiring human review; high-complexity custom, K2, and master-page assets should be redesigned before migration.";
    }

    private static string StableId(params string[] parts)
    {
        var value = string.Join("-", parts);
        var chars = value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-');
        return string.Join(string.Empty, chars).Replace("--", "-", StringComparison.Ordinal).Trim('-');
    }
}
