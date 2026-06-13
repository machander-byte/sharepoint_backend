using ZMS.Application.Discovery;

namespace ZMS.Application.Contracts;

public interface IReadinessAnalysisService
{
    Task<ReadinessAnalyzeResponse?> AnalyzeAsync(string scanId, CancellationToken cancellationToken);
    Task<MigrationReadinessAssessment?> GetAssessmentAsync(string assessmentId, CancellationToken cancellationToken);
    Task<MigrationReadinessAssessment?> GetLatestAssessmentAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<RemediationAction>?> GetRemediationPlanAsync(string assessmentId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<MigrationWaveSuggestion>?> GetMigrationWavesAsync(string assessmentId, CancellationToken cancellationToken);
    Task<ReadinessExportResult?> ExportAsync(string assessmentId, string exportType, CancellationToken cancellationToken);
}

public interface IReadinessStorageService
{
    Task SaveAsync(MigrationReadinessAssessment assessment, CancellationToken cancellationToken);
    Task<MigrationReadinessAssessment?> GetAsync(string assessmentId, CancellationToken cancellationToken);
    Task<MigrationReadinessAssessment?> GetLatestAsync(CancellationToken cancellationToken);
}

public interface IRiskScoringService
{
    ReadinessScoreResult Score(IReadOnlyCollection<ReadinessRiskFinding> findings);
}

public interface IRemediationPlanner
{
    IReadOnlyCollection<RemediationAction> BuildPlan(IReadOnlyCollection<ReadinessRiskFinding> findings);
}

public interface IMigrationWavePlanner
{
    IReadOnlyCollection<MigrationWaveSuggestion> BuildWaves(
        DiscoveryScanResult scanResult,
        IReadOnlyCollection<ReadinessRiskFinding> findings,
        IReadOnlyCollection<RemediationAction> actions);
}

public interface IModernizationOpportunityDetector
{
    IReadOnlyCollection<ModernizationOpportunity> Detect(DiscoveryScanResult scanResult);
}

public interface IReadinessExportService
{
    ReadinessExportResult ExportJson(MigrationReadinessAssessment assessment);
    ReadinessExportResult ExportCsv(MigrationReadinessAssessment assessment);
    ReadinessExportResult ExportMarkdown(MigrationReadinessAssessment assessment);
}

public sealed class ReadinessAnalyzeResponse
{
    public string AssessmentId { get; set; } = string.Empty;
    public string ScanId { get; set; } = string.Empty;
    public string Status { get; set; } = "completed";
    public int ReadinessScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public ReadinessSummary Summary { get; set; } = new();
}

public sealed class MigrationReadinessAssessment
{
    public string AssessmentId { get; set; } = string.Empty;
    public string ScanId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public string Status { get; set; } = "completed";
    public int ReadinessScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public ReadinessSummary Summary { get; set; } = new();
    public ReadinessScoreBreakdown ScoreBreakdown { get; set; } = new();
    public List<SiteReadinessProfile> SiteProfiles { get; set; } = [];
    public List<LibraryReadinessProfile> LibraryProfiles { get; set; } = [];
    public List<ReadinessRiskFinding> RiskFindings { get; set; } = [];
    public List<RemediationAction> RemediationActions { get; set; } = [];
    public List<MigrationWaveSuggestion> MigrationWaves { get; set; } = [];
    public List<ModernizationOpportunity> ModernizationOpportunities { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public sealed class ReadinessSummary
{
    public int Blockers { get; set; }
    public int HighRisks { get; set; }
    public int MediumRisks { get; set; }
    public int LowRisks { get; set; }
    public int RemediationActions { get; set; }
    public int SuggestedWaves { get; set; }
}

public sealed class ReadinessRiskFinding
{
    public string Id { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AffectedLocation { get; set; } = string.Empty;
    public string AffectedSite { get; set; } = string.Empty;
    public string AffectedLibrary { get; set; } = string.Empty;
    public string AffectedPath { get; set; } = string.Empty;
    public string Evidence { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool CanAutoRemediate { get; set; }
    public bool MigrationBlocker { get; set; }
}

public sealed class RemediationAction
{
    public string Id { get; set; } = string.Empty;
    public string Priority { get; set; } = "Low";
    public string Category { get; set; } = string.Empty;
    public string ActionTitle { get; set; } = string.Empty;
    public string ActionDescription { get; set; } = string.Empty;
    public List<string> AffectedLocations { get; set; } = [];
    public string EstimatedEffort { get; set; } = "Low";
    public string OwnerRole { get; set; } = "Content Owner";
    public string Status { get; set; } = "Open";
    public List<string> DependsOn { get; set; } = [];
    public string ExpectedBenefit { get; set; } = string.Empty;
}

public sealed class MigrationWaveSuggestion
{
    public string WaveId { get; set; } = string.Empty;
    public string WaveName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RecommendedOrder { get; set; }
    public List<string> IncludedSites { get; set; } = [];
    public List<string> IncludedLibraries { get; set; } = [];
    public List<string> ExcludedRisks { get; set; } = [];
    public int EstimatedFiles { get; set; }
    public long EstimatedStorage { get; set; }
    public int ReadinessScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public List<string> Prerequisites { get; set; } = [];
}

public sealed class SiteReadinessProfile
{
    public string Site { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int ReadinessScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public int Libraries { get; set; }
    public int Files { get; set; }
    public long StorageBytes { get; set; }
    public int Blockers { get; set; }
    public int RiskCount { get; set; }
}

public sealed class LibraryReadinessProfile
{
    public string Site { get; set; } = string.Empty;
    public string Library { get; set; } = string.Empty;
    public int ReadinessScore { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public int Files { get; set; }
    public long StorageBytes { get; set; }
    public int MetadataIssues { get; set; }
    public bool PermissionSensitive { get; set; }
}

public sealed class ReadinessScoreBreakdown
{
    public int StartingScore { get; set; } = 100;
    public int PermissionPenalty { get; set; }
    public int MetadataPenalty { get; set; }
    public int PathLengthPenalty { get; set; }
    public int LargeFilePenalty { get; set; }
    public int RestrictedContentPenalty { get; set; }
    public int ArchivedContentPenalty { get; set; }
    public int DuplicateContentPenalty { get; set; }
    public int BlockerPenalty { get; set; }
    public int FinalScore { get; set; }
}

public sealed class ReadinessScoreResult
{
    public int Score { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public ReadinessScoreBreakdown Breakdown { get; set; } = new();
}

public sealed class ModernizationOpportunity
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string PotentialTarget { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public string EstimatedEffort { get; set; } = "Medium";
}

public sealed class ReadinessExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] Content { get; set; } = [];
}
