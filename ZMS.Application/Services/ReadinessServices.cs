using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;
using ZMS.Application.Discovery;

namespace ZMS.Application.Services;

public sealed class ReadinessAnalysisService : IReadinessAnalysisService
{
    private readonly IDiscoveryService _discoveryService;
    private readonly IReadinessStorageService _storage;
    private readonly IRiskScoringService _scoring;
    private readonly IRemediationPlanner _remediationPlanner;
    private readonly IMigrationWavePlanner _wavePlanner;
    private readonly IModernizationOpportunityDetector _modernizationDetector;
    private readonly IReadinessExportService _exportService;

    public ReadinessAnalysisService(
        IDiscoveryService discoveryService,
        IReadinessStorageService storage,
        IRiskScoringService scoring,
        IRemediationPlanner remediationPlanner,
        IMigrationWavePlanner wavePlanner,
        IModernizationOpportunityDetector modernizationDetector,
        IReadinessExportService exportService)
    {
        _discoveryService = discoveryService;
        _storage = storage;
        _scoring = scoring;
        _remediationPlanner = remediationPlanner;
        _wavePlanner = wavePlanner;
        _modernizationDetector = modernizationDetector;
        _exportService = exportService;
    }

    public async Task<ReadinessAnalyzeResponse?> AnalyzeAsync(string scanId, CancellationToken cancellationToken)
    {
        var scan = await _discoveryService.GetScanResultAsync(scanId, cancellationToken);
        if (scan is null || !IsCompleted(scan.Status))
        {
            return null;
        }

        var findings = BuildRiskFindings(scan);
        var score = _scoring.Score(findings);
        var remediations = _remediationPlanner.BuildPlan(findings);
        var waves = _wavePlanner.BuildWaves(scan, findings, remediations);
        var modernization = _modernizationDetector.Detect(scan);

        var assessment = new MigrationReadinessAssessment
        {
            AssessmentId = Guid.NewGuid().ToString("D"),
            ScanId = scan.ScanId,
            GeneratedAt = DateTimeOffset.UtcNow,
            Status = "completed",
            ReadinessScore = score.Score,
            RiskLevel = score.RiskLevel,
            ScoreBreakdown = score.Breakdown,
            RiskFindings = findings.ToList(),
            RemediationActions = remediations.ToList(),
            MigrationWaves = waves.ToList(),
            ModernizationOpportunities = modernization.ToList(),
            Warnings = scan.Warnings.ToList(),
            Errors = scan.Errors.ToList()
        };
        assessment.SiteProfiles = BuildSiteProfiles(scan, findings).ToList();
        assessment.LibraryProfiles = BuildLibraryProfiles(scan, findings).ToList();
        assessment.Summary = BuildSummary(assessment);

        await _storage.SaveAsync(assessment, cancellationToken);

        return new ReadinessAnalyzeResponse
        {
            AssessmentId = assessment.AssessmentId,
            ScanId = assessment.ScanId,
            Status = assessment.Status,
            ReadinessScore = assessment.ReadinessScore,
            RiskLevel = assessment.RiskLevel,
            Summary = assessment.Summary
        };
    }

    public Task<MigrationReadinessAssessment?> GetAssessmentAsync(string assessmentId, CancellationToken cancellationToken) =>
        _storage.GetAsync(assessmentId, cancellationToken);

    public Task<MigrationReadinessAssessment?> GetLatestAssessmentAsync(CancellationToken cancellationToken) =>
        _storage.GetLatestAsync(cancellationToken);

    public async Task<IReadOnlyCollection<RemediationAction>?> GetRemediationPlanAsync(string assessmentId, CancellationToken cancellationToken) =>
        (await _storage.GetAsync(assessmentId, cancellationToken))?.RemediationActions;

    public async Task<IReadOnlyCollection<MigrationWaveSuggestion>?> GetMigrationWavesAsync(string assessmentId, CancellationToken cancellationToken) =>
        (await _storage.GetAsync(assessmentId, cancellationToken))?.MigrationWaves;

    public async Task<ReadinessExportResult?> ExportAsync(string assessmentId, string exportType, CancellationToken cancellationToken)
    {
        var assessment = await _storage.GetAsync(assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        return exportType.ToLowerInvariant() switch
        {
            "json" => _exportService.ExportJson(assessment),
            "markdown" or "md" => _exportService.ExportMarkdown(assessment),
            _ => _exportService.ExportCsv(assessment)
        };
    }

    private static bool IsCompleted(string status) =>
        status.Equals("completed", StringComparison.OrdinalIgnoreCase)
        || status.Equals("partial", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyCollection<ReadinessRiskFinding> BuildRiskFindings(DiscoveryScanResult scan)
    {
        var findings = new List<ReadinessRiskFinding>();

        if (scan.Errors.Count > 0)
        {
            foreach (var error in scan.Errors)
            {
                findings.Add(NewFinding("Connectivity", "High", "Discovery error requires review", error, "", "", "", error, true));
            }
        }

        foreach (var site in scan.SiteCollections.Where(site => string.IsNullOrWhiteSpace(site.Url) || !Uri.TryCreate(site.Url, UriKind.Absolute, out _)))
        {
            findings.Add(NewFinding("Connectivity", "High", "Site URL is missing or invalid", "A discovered site has no valid absolute URL.", site.Title, "", site.Url, "Correct the source inventory before planning migration.", true));
        }

        foreach (var risk in scan.PermissionRisks)
        {
            var restricted = IsRestricted($"{risk.LibraryOrFolder} {string.Join(' ', risk.Groups)}");
            var blocker = restricted && (risk.Groups.Count == 0 || risk.Groups.Any(group => group.Contains("unknown", StringComparison.OrdinalIgnoreCase)));
            findings.Add(NewFinding("Permissions", NormalizeSeverity(risk.RiskLevel), "Review unique permissions before migration", $"Permission inheritance is {risk.InheritanceStatus}.", risk.Site, risk.LibraryOrFolder, risk.LibraryOrFolder, risk.RecommendedAction, blocker));
        }

        foreach (var finding in scan.MetadataFindings)
        {
            var blocker = finding.Required
                && finding.MissingValueCount > 0
                && IsRestricted(finding.Library)
                && finding.MappingRisk.Equals("Critical", StringComparison.OrdinalIgnoreCase);
            findings.Add(NewFinding("Metadata", NormalizeSeverity(finding.MappingRisk), "Standardize metadata fields", $"{finding.FieldName} has {finding.MissingValueCount} missing values.", finding.Site, finding.Library, finding.Library, "Standardize and map required metadata before migration.", blocker));
        }

        foreach (var risk in scan.MigrationRisks)
        {
            var category = CategoryForRisk(risk.RiskType);
            var severity = NormalizeSeverity(risk.RiskLevel);
            var blocker = IsBlocker(category, severity, risk);
            findings.Add(NewFinding(category, severity, TitleForCategory(category), risk.Description, risk.Site, risk.LibraryOrPath, string.IsNullOrWhiteSpace(risk.Path) ? risk.LibraryOrPath : risk.Path, risk.RecommendedAction, blocker));
        }

        foreach (var item in scan.InventoryItems)
        {
            if (item.Path.Length > 350)
            {
                findings.Add(NewFinding("Path Length", "High", "Shorten deep folder paths", "Path length is above 350 characters.", item.SiteCollection, item.Library, item.Path, "Flatten or rename the path before migration.", true));
            }
            else if (item.Path.Length > 250)
            {
                findings.Add(NewFinding("Path Length", "Medium", "Review long folder paths", "Path length is above 250 characters.", item.SiteCollection, item.Library, item.Path, "Shorten path segments where practical.", false));
            }

            if (item.SizeBytes > 500L * 1024 * 1024)
            {
                findings.Add(NewFinding("Large Files", "High", "Review large file migration approach", "Item size is above 500 MB.", item.SiteCollection, item.Library, item.Path, "Confirm chunk upload support and migration window.", false));
            }
            else if (item.SizeBytes > 100L * 1024 * 1024)
            {
                findings.Add(NewFinding("Large Files", "Medium", "Review large file migration approach", "Item size is above 100 MB.", item.SiteCollection, item.Library, item.Path, "Plan throughput and retry handling.", false));
            }

            if (IsRestricted($"{item.Library} {item.Path}") && item.PermissionStatus.Contains("Broken", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(NewFinding("Restricted Content", "High", "Validate restricted content access", "Restricted content has broken or unique permissions.", item.SiteCollection, item.Library, item.Path, "Map target SharePoint security groups before migration.", false));
            }

            if (IsArchive(item.Path))
            {
                findings.Add(NewFinding("Archived Content", "Medium", "Decide archive vs migrate strategy", "Archived content was detected.", item.SiteCollection, item.Library, item.Path, "Review whether archive content should be migrated or retained separately.", false));
            }
        }

        return findings
            .GroupBy(f => $"{f.Category}|{f.Title}|{f.AffectedSite}|{f.AffectedLibrary}|{f.AffectedPath}")
            .Select(group => group.First())
            .ToList();
    }

    private static ReadinessRiskFinding NewFinding(string category, string severity, string title, string description, string site, string library, string path, string action, bool blocker)
    {
        return new ReadinessRiskFinding
        {
            Id = Guid.NewGuid().ToString("D"),
            Category = category,
            Severity = blocker ? "High" : severity,
            Title = title,
            Description = description,
            AffectedLocation = string.IsNullOrWhiteSpace(path) ? library : path,
            AffectedSite = site,
            AffectedLibrary = library,
            AffectedPath = path,
            Evidence = description,
            Impact = blocker ? "Blocks reliable migration planning until remediated." : "Can increase migration risk, validation effort, or cutover time.",
            RecommendedAction = action,
            CanAutoRemediate = category is "Metadata" or "Path Length" && !blocker,
            MigrationBlocker = blocker
        };
    }

    private static IReadOnlyCollection<SiteReadinessProfile> BuildSiteProfiles(DiscoveryScanResult scan, IReadOnlyCollection<ReadinessRiskFinding> findings)
    {
        return scan.SiteCollections.Select(site =>
        {
            var siteFindings = findings.Where(f => f.AffectedSite.Equals(site.Title, StringComparison.OrdinalIgnoreCase)).ToList();
            var score = Math.Max(0, 100 - siteFindings.Sum(f => f.MigrationBlocker ? 8 : SeverityPenalty(f.Severity)));
            return new SiteReadinessProfile
            {
                Site = site.Title,
                Url = site.Url,
                Libraries = site.Libraries.Count,
                Files = site.FileCount,
                StorageBytes = site.SizeBytes,
                Blockers = siteFindings.Count(f => f.MigrationBlocker),
                RiskCount = siteFindings.Count,
                ReadinessScore = score,
                RiskLevel = RiskLevelForScore(score)
            };
        }).ToList();
    }

    private static IReadOnlyCollection<LibraryReadinessProfile> BuildLibraryProfiles(DiscoveryScanResult scan, IReadOnlyCollection<ReadinessRiskFinding> findings)
    {
        var rows = scan.InventoryItems.Where(item => item.ItemType.Equals("Library", StringComparison.OrdinalIgnoreCase));
        return rows.Select(item =>
        {
            var libraryFindings = findings.Where(f =>
                f.AffectedSite.Equals(item.SiteCollection, StringComparison.OrdinalIgnoreCase)
                && f.AffectedLibrary.Equals(item.Library, StringComparison.OrdinalIgnoreCase)).ToList();
            var score = Math.Max(0, 100 - libraryFindings.Sum(f => f.MigrationBlocker ? 8 : SeverityPenalty(f.Severity)));
            return new LibraryReadinessProfile
            {
                Site = item.SiteCollection,
                Library = item.Library,
                Files = item.FileCount,
                StorageBytes = item.SizeBytes,
                MetadataIssues = libraryFindings.Count(f => f.Category == "Metadata"),
                PermissionSensitive = libraryFindings.Any(f => f.Category is "Permissions" or "Restricted Content"),
                ReadinessScore = score,
                RiskLevel = RiskLevelForScore(score)
            };
        }).ToList();
    }

    private static ReadinessSummary BuildSummary(MigrationReadinessAssessment assessment) => new()
    {
        Blockers = assessment.RiskFindings.Count(f => f.MigrationBlocker),
        HighRisks = assessment.RiskFindings.Count(f => f.Severity.Equals("High", StringComparison.OrdinalIgnoreCase) || f.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)),
        MediumRisks = assessment.RiskFindings.Count(f => f.Severity.Equals("Medium", StringComparison.OrdinalIgnoreCase)),
        LowRisks = assessment.RiskFindings.Count(f => f.Severity.Equals("Low", StringComparison.OrdinalIgnoreCase)),
        RemediationActions = assessment.RemediationActions.Count,
        SuggestedWaves = assessment.MigrationWaves.Count
    };

    private static string CategoryForRisk(string riskType)
    {
        var value = riskType.ToLowerInvariant();
        if (value.Contains("permission") || value.Contains("inheritance")) return "Permissions";
        if (value.Contains("metadata")) return "Metadata";
        if (value.Contains("path")) return "Path Length";
        if (value.Contains("large") || value.Contains("size")) return "Large Files";
        if (value.Contains("duplicate")) return "Duplicate Content";
        if (value.Contains("archive") || value.Contains("stale")) return "Archived Content";
        if (IsRestricted(value)) return "Restricted Content";
        return "Governance";
    }

    private static bool IsBlocker(string category, string severity, MigrationRiskFinding risk) =>
        category == "Path Length" && (risk.Path.Length > 350 || risk.Description.Contains("350", StringComparison.OrdinalIgnoreCase))
        || category == "Large Files" && risk.Description.Contains("configured maximum", StringComparison.OrdinalIgnoreCase)
        || category == "Restricted Content" && risk.RecommendedAction.Contains("target permission groups are missing", StringComparison.OrdinalIgnoreCase)
        || severity == "Critical";

    private static string TitleForCategory(string category) => category switch
    {
        "Permissions" => "Review unique permissions before migration",
        "Metadata" => "Standardize metadata fields",
        "Path Length" => "Shorten deep folder paths",
        "Large Files" => "Review large file migration approach",
        "Duplicate Content" => "Resolve duplicate content",
        "Archived Content" => "Decide archive vs migrate strategy",
        "Restricted Content" => "Validate restricted content access",
        _ => "Review governance risk"
    };

    internal static string NormalizeSeverity(string value)
    {
        if (value.Equals("Critical", StringComparison.OrdinalIgnoreCase)) return "Critical";
        if (value.Equals("High", StringComparison.OrdinalIgnoreCase)) return "High";
        if (value.Equals("Medium", StringComparison.OrdinalIgnoreCase) || value.Equals("Moderate", StringComparison.OrdinalIgnoreCase)) return "Medium";
        return "Low";
    }

    internal static int SeverityPenalty(string severity) => NormalizeSeverity(severity) switch
    {
        "Critical" => 6,
        "High" => 5,
        "Medium" => 3,
        _ => 1
    };

    internal static string RiskLevelForScore(int score) => score switch
    {
        >= 90 => "Low",
        >= 75 => "Moderate",
        >= 60 => "Medium",
        >= 40 => "High",
        _ => "Critical"
    };

    internal static bool IsRestricted(string value) =>
        value.Contains("confidential", StringComparison.OrdinalIgnoreCase)
        || value.Contains("restricted", StringComparison.OrdinalIgnoreCase)
        || value.Contains("payroll", StringComparison.OrdinalIgnoreCase)
        || value.Contains("security", StringComparison.OrdinalIgnoreCase)
        || value.Contains("audit", StringComparison.OrdinalIgnoreCase)
        || value.Contains("tax", StringComparison.OrdinalIgnoreCase)
        || value.Contains("contract", StringComparison.OrdinalIgnoreCase);

    internal static bool IsArchive(string value) =>
        value.Contains("archive", StringComparison.OrdinalIgnoreCase)
        || value.Contains("archived", StringComparison.OrdinalIgnoreCase)
        || value.Contains("2021", StringComparison.OrdinalIgnoreCase)
        || value.Contains("2022", StringComparison.OrdinalIgnoreCase)
        || value.Contains("2023", StringComparison.OrdinalIgnoreCase);
}

public sealed class RiskScoringService : IRiskScoringService
{
    public ReadinessScoreResult Score(IReadOnlyCollection<ReadinessRiskFinding> findings)
    {
        var breakdown = new ReadinessScoreBreakdown();

        foreach (var finding in findings)
        {
            var penalty = Penalty(finding);
            switch (finding.Category)
            {
                case "Permissions": breakdown.PermissionPenalty += penalty; break;
                case "Metadata": breakdown.MetadataPenalty += penalty; break;
                case "Path Length": breakdown.PathLengthPenalty += penalty; break;
                case "Large Files": breakdown.LargeFilePenalty += penalty; break;
                case "Restricted Content": breakdown.RestrictedContentPenalty += penalty; break;
                case "Archived Content": breakdown.ArchivedContentPenalty += penalty; break;
                case "Duplicate Content": breakdown.DuplicateContentPenalty += penalty; break;
            }

            if (finding.MigrationBlocker)
            {
                breakdown.BlockerPenalty += 8;
            }
        }

        var final = Math.Max(0, breakdown.StartingScore
            - breakdown.PermissionPenalty
            - breakdown.MetadataPenalty
            - breakdown.PathLengthPenalty
            - breakdown.LargeFilePenalty
            - breakdown.RestrictedContentPenalty
            - breakdown.ArchivedContentPenalty
            - breakdown.DuplicateContentPenalty
            - breakdown.BlockerPenalty);
        breakdown.FinalScore = final;
        return new ReadinessScoreResult { Score = final, RiskLevel = ReadinessAnalysisService.RiskLevelForScore(final), Breakdown = breakdown };
    }

    private static int Penalty(ReadinessRiskFinding finding)
    {
        var severity = ReadinessAnalysisService.NormalizeSeverity(finding.Severity);
        return finding.Category switch
        {
            "Permissions" => severity == "High" || severity == "Critical" ? 5 : severity == "Medium" ? 3 : 1,
            "Metadata" => severity == "High" || severity == "Critical" ? 4 : severity == "Medium" ? 2 : 1,
            "Path Length" => severity == "High" || severity == "Critical" ? 5 : severity == "Medium" ? 3 : 1,
            "Large Files" => severity == "High" || severity == "Critical" ? 4 : severity == "Medium" ? 2 : 1,
            "Restricted Content" => severity == "High" || severity == "Critical" ? 4 : 2,
            "Archived Content" => severity == "Medium" ? 1 : 0,
            "Duplicate Content" => severity == "Medium" ? 2 : 1,
            _ => ReadinessAnalysisService.SeverityPenalty(severity)
        };
    }
}

public sealed class RemediationPlanner : IRemediationPlanner
{
    public IReadOnlyCollection<RemediationAction> BuildPlan(IReadOnlyCollection<ReadinessRiskFinding> findings)
    {
        return findings
            .GroupBy(finding => finding.Category)
            .Select(group =>
            {
                var priority = group.Any(f => f.MigrationBlocker) || group.Any(f => f.Severity is "High" or "Critical") ? "High" : group.Any(f => f.Severity == "Medium") ? "Medium" : "Low";
                return new RemediationAction
                {
                    Id = Guid.NewGuid().ToString("D"),
                    Priority = priority,
                    Category = group.Key,
                    ActionTitle = ActionTitle(group.Key),
                    ActionDescription = ActionDescription(group.Key, group.First()),
                    AffectedLocations = group.Select(f => f.AffectedLocation).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().Take(10).ToList(),
                    EstimatedEffort = priority == "High" ? "High" : priority == "Medium" ? "Medium" : "Low",
                    OwnerRole = OwnerFor(group.Key),
                    Status = "Open",
                    DependsOn = group.Where(f => f.MigrationBlocker).Select(f => f.Id).Take(10).ToList(),
                    ExpectedBenefit = ExpectedBenefit(group.Key)
                };
            })
            .OrderBy(action => action.Priority == "High" ? 0 : action.Priority == "Medium" ? 1 : 2)
            .ToList();
    }

    private static string ActionTitle(string category) => category switch
    {
        "Permissions" or "Restricted Content" => "Review unique permissions before migration",
        "Metadata" => "Standardize metadata fields",
        "Path Length" => "Shorten deep folder paths",
        "Archived Content" => "Decide archive vs migrate strategy",
        "Large Files" => "Plan large file migration handling",
        "Duplicate Content" => "Resolve duplicate content",
        _ => "Review governance risk"
    };

    private static string ActionDescription(string category, ReadinessRiskFinding sample) => category switch
    {
        "Permissions" or "Restricted Content" => $"{sample.AffectedLocation} has sensitive or unique permissions. Validate target SharePoint groups before migration.",
        "Metadata" => $"{sample.AffectedLibrary} has required or risky metadata. Standardize mappings and fill required values.",
        "Path Length" => $"{sample.AffectedLocation} exceeds recommended migration path length. Flatten or rename folders before migration.",
        "Archived Content" => $"{sample.AffectedLocation} appears archived. Decide whether to migrate, exclude, or move to archive storage.",
        "Large Files" => $"{sample.AffectedLocation} contains large files. Confirm chunking, retry, and migration window.",
        _ => sample.RecommendedAction
    };

    private static string OwnerFor(string category) => category switch
    {
        "Permissions" or "Restricted Content" => "SharePoint Admin / Security Owner",
        "Metadata" => "Information Architect",
        "Path Length" or "Archived Content" or "Duplicate Content" => "Business Owner",
        "Large Files" => "Migration Engineer",
        _ => "Migration Lead"
    };

    private static string ExpectedBenefit(string category) => category switch
    {
        "Permissions" => "Reduces access validation risk and post-migration permission defects.",
        "Metadata" => "Improves search, filtering, retention, and migration validation.",
        "Path Length" => "Prevents failed or skipped items during migration.",
        "Archived Content" => "Reduces migrated payload and shortens cutover windows.",
        _ => "Improves migration predictability."
    };
}

public sealed class MigrationWavePlanner : IMigrationWavePlanner
{
    private sealed class WaveLibrary
    {
        public string SiteCollection { get; init; } = string.Empty;
        public string Library { get; init; } = string.Empty;
        public int Files { get; init; }
        public long Storage { get; init; }
        public List<ReadinessRiskFinding> Risks { get; init; } = [];
    }

    public IReadOnlyCollection<MigrationWaveSuggestion> BuildWaves(
        DiscoveryScanResult scanResult,
        IReadOnlyCollection<ReadinessRiskFinding> findings,
        IReadOnlyCollection<RemediationAction> actions)
    {
        var libraries = scanResult.InventoryItems
            .Where(item => item.ItemType.Equals("Library", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(item.Library))
            .GroupBy(item => new { item.SiteCollection, item.Library })
            .Select(group => new WaveLibrary
            {
                SiteCollection = group.Key.SiteCollection,
                Library = group.Key.Library,
                Files = group.Sum(item => item.FileCount),
                Storage = group.Sum(item => item.SizeBytes),
                Risks = findings.Where(f => f.AffectedSite == group.Key.SiteCollection && f.AffectedLibrary == group.Key.Library).ToList()
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Library))
            .ToList();

        return new[]
        {
            BuildWave("wave-1", "Wave 1 - Low Risk Pilot", 1, "Low-risk, simple libraries for pilot validation.", libraries.Where(l => !l.Risks.Any()).ToList(), actions),
            BuildWave("wave-2", "Wave 2 - Business Content", 2, "Medium-risk business libraries with manageable metadata.", libraries.Where(l => l.Risks.Any(r => r.Severity == "Medium") && !l.Risks.Any(r => r.MigrationBlocker)).ToList(), actions),
            BuildWave("wave-3", "Wave 3 - Restricted Content", 3, "Permission-sensitive or restricted content after remediation.", libraries.Where(l => l.Risks.Any(r => r.Category is "Permissions" or "Restricted Content")).ToList(), actions),
            BuildWave("wave-4", "Wave 4 - Archive and Cleanup", 4, "Archive-heavy, long-path, or complex content after cleanup.", libraries.Where(l => l.Risks.Any(r => r.Category is "Archived Content" or "Path Length" or "Large Files")).ToList(), actions)
        };
    }

    private static MigrationWaveSuggestion BuildWave(string id, string name, int order, string description, List<WaveLibrary> libraries, IReadOnlyCollection<RemediationAction> actions)
    {
        var riskScore = libraries.SelectMany(l => l.Risks).Sum(r => r.MigrationBlocker ? 8 : ReadinessAnalysisService.SeverityPenalty(r.Severity));
        var score = Math.Max(0, 100 - riskScore);
        return new MigrationWaveSuggestion
        {
            WaveId = id,
            WaveName = name,
            Description = description,
            RecommendedOrder = order,
            IncludedSites = libraries.Select(l => l.SiteCollection).Distinct().ToList(),
            IncludedLibraries = libraries.Select(l => l.Library).Distinct().ToList(),
            ExcludedRisks = libraries.SelectMany(l => l.Risks).Where(r => r.MigrationBlocker).Select(r => r.Title).Distinct().ToList(),
            EstimatedFiles = libraries.Sum(l => l.Files),
            EstimatedStorage = libraries.Sum(l => l.Storage),
            ReadinessScore = score,
            RiskLevel = ReadinessAnalysisService.RiskLevelForScore(score),
            Prerequisites = actions.Where(action => libraries.Any(l => action.AffectedLocations.Any(location => l.Library.Contains(location, StringComparison.OrdinalIgnoreCase) || location.Contains(l.Library, StringComparison.OrdinalIgnoreCase)))).Select(action => action.ActionTitle).Distinct().ToList()
        };
    }
}

public sealed class ModernizationOpportunityDetector : IModernizationOpportunityDetector
{
    public IReadOnlyCollection<ModernizationOpportunity> Detect(DiscoveryScanResult scanResult)
    {
        var names = scanResult.InventoryItems.Select(item => new { Name = string.IsNullOrWhiteSpace(item.Library) ? item.Path : item.Library, item.Path, item.SiteCollection });
        return names.SelectMany(item => DetectFor(item.SiteCollection, item.Name, item.Path)).GroupBy(item => $"{item.Type}|{item.SourceName}|{item.Location}").Select(group => group.First()).ToList();
    }

    private static IEnumerable<ModernizationOpportunity> DetectFor(string site, string name, string path)
    {
        var value = $"{name} {path}";
        if (ContainsAny(value, "Workflow", "Approval", "Request", "Tracker"))
        {
            yield return Opportunity("Workflow Modernization", name, site, "Power Automate approval workflow", "Name indicates workflow or request routing.");
        }
        if (ContainsAny(value, "Form", "InfoPath", "Nintex", "K2"))
        {
            yield return Opportunity("Forms Modernization", name, site, "Power Apps / modern SharePoint forms", "Name indicates legacy form dependency.");
        }
        if (ContainsAny(value, "Report", "SSRS", "Excel"))
        {
            yield return Opportunity("Reporting Modernization", name, site, "Power BI", "Name indicates reporting or spreadsheet dependency.");
        }
        if (ContainsAny(value, "Policy", "Audit", "Compliance", "Security"))
        {
            yield return Opportunity("Governance Modernization", name, site, "Retention labels and governance review", "Name indicates governed or sensitive content.");
        }
    }

    private static ModernizationOpportunity Opportunity(string type, string sourceName, string location, string target, string rationale) => new()
    {
        Id = Guid.NewGuid().ToString("D"),
        Type = type,
        SourceName = sourceName,
        Location = location,
        PotentialTarget = target,
        Rationale = rationale,
        EstimatedEffort = type == "Governance Modernization" ? "Medium" : "High"
    };

    private static bool ContainsAny(string value, params string[] tokens) =>
        tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
}

public sealed class ReadinessStorageService : IReadinessStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _rootDirectory;
    private readonly IReadinessExportService _exportService;

    public ReadinessStorageService(IHostEnvironment hostEnvironment, IReadinessExportService exportService)
    {
        _rootDirectory = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "readiness-assessments");
        _exportService = exportService;
    }

    public async Task SaveAsync(MigrationReadinessAssessment assessment, CancellationToken cancellationToken)
    {
        var directory = GetDirectory(assessment.AssessmentId);
        Directory.CreateDirectory(directory);
        await WriteJsonAsync(Path.Combine(directory, "assessment.json"), assessment, cancellationToken);
        await WriteJsonAsync(Path.Combine(directory, "score-breakdown.json"), assessment.ScoreBreakdown, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "risk-findings.csv"), _exportService.ExportCsv(assessment).Content, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "remediation-plan.csv"), Encoding.UTF8.GetBytes(Csv(assessment.RemediationActions.Select(a => new[] { a.Priority, a.Category, a.ActionTitle, a.OwnerRole, a.EstimatedEffort, string.Join("; ", a.AffectedLocations) }))), cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "migration-waves.csv"), Encoding.UTF8.GetBytes(Csv(assessment.MigrationWaves.Select(w => new[] { w.WaveName, w.RiskLevel, w.ReadinessScore.ToString(), w.EstimatedFiles.ToString(), w.EstimatedStorage.ToString(), string.Join("; ", w.IncludedLibraries) }))), cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "readiness-report.md"), _exportService.ExportMarkdown(assessment).Content, cancellationToken);
    }

    public Task<MigrationReadinessAssessment?> GetAsync(string assessmentId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(assessmentId, out _))
        {
            return Task.FromResult<MigrationReadinessAssessment?>(null);
        }

        return ReadJsonAsync<MigrationReadinessAssessment>(Path.Combine(GetDirectory(assessmentId), "assessment.json"), cancellationToken);
    }

    public async Task<MigrationReadinessAssessment?> GetLatestAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_rootDirectory))
        {
            return null;
        }

        var assessments = new List<MigrationReadinessAssessment>();
        foreach (var path in Directory.EnumerateFiles(_rootDirectory, "assessment.json", SearchOption.AllDirectories))
        {
            var assessment = await ReadJsonAsync<MigrationReadinessAssessment>(path, cancellationToken);
            if (assessment is not null && assessment.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
            {
                assessments.Add(assessment);
            }
        }

        return assessments.OrderByDescending(item => item.GeneratedAt).FirstOrDefault();
    }

    private string GetDirectory(string assessmentId) => Path.Combine(_rootDirectory, assessmentId);

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return default;
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
    }

    internal static string Csv(IEnumerable<string[]> rows)
    {
        static string Escape(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        return string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(Escape))));
    }
}

public sealed class ReadinessExportService : IReadinessExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public ReadinessExportResult ExportJson(MigrationReadinessAssessment assessment) => new()
    {
        FileName = $"readiness-assessment-{assessment.AssessmentId}.json",
        ContentType = "application/json",
        Content = JsonSerializer.SerializeToUtf8Bytes(assessment, JsonOptions)
    };

    public ReadinessExportResult ExportCsv(MigrationReadinessAssessment assessment)
    {
        var rows = new List<string[]> { new[] { "Category", "Severity", "Title", "Site", "Library", "Path", "Blocker", "Recommended Action" } };
        rows.AddRange(assessment.RiskFindings.Select(f => new[] { f.Category, f.Severity, f.Title, f.AffectedSite, f.AffectedLibrary, f.AffectedPath, f.MigrationBlocker.ToString(), f.RecommendedAction }));
        return new ReadinessExportResult
        {
            FileName = $"readiness-risk-findings-{assessment.AssessmentId}.csv",
            ContentType = "text/csv",
            Content = Encoding.UTF8.GetBytes(ReadinessStorageService.Csv(rows))
        };
    }

    public ReadinessExportResult ExportMarkdown(MigrationReadinessAssessment assessment)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Executive Readiness Summary");
        builder.AppendLine();
        builder.AppendLine($"Assessment: `{assessment.AssessmentId}`");
        builder.AppendLine($"Scan: `{assessment.ScanId}`");
        builder.AppendLine($"Generated: {assessment.GeneratedAt:o}");
        builder.AppendLine($"Readiness Score: {assessment.ReadinessScore}");
        builder.AppendLine($"Risk Level: {assessment.RiskLevel}");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine($"- Blockers: {assessment.Summary.Blockers}");
        builder.AppendLine($"- High risks: {assessment.Summary.HighRisks}");
        builder.AppendLine($"- Medium risks: {assessment.Summary.MediumRisks}");
        builder.AppendLine($"- Remediation actions: {assessment.Summary.RemediationActions}");
        builder.AppendLine($"- Suggested waves: {assessment.Summary.SuggestedWaves}");
        builder.AppendLine();
        builder.AppendLine("## Top Remediation Actions");
        foreach (var action in assessment.RemediationActions.Take(10))
        {
            builder.AppendLine($"- **{action.Priority}** {action.ActionTitle}: {action.ActionDescription}");
        }
        builder.AppendLine();
        builder.AppendLine("## Migration Waves");
        foreach (var wave in assessment.MigrationWaves)
        {
            builder.AppendLine($"- **{wave.WaveName}** ({wave.RiskLevel}, {wave.ReadinessScore}%): {string.Join(", ", wave.IncludedLibraries.DefaultIfEmpty("No libraries assigned"))}");
        }

        return new ReadinessExportResult
        {
            FileName = $"executive-readiness-summary-{assessment.AssessmentId}.md",
            ContentType = "text/markdown",
            Content = Encoding.UTF8.GetBytes(builder.ToString())
        };
    }
}
