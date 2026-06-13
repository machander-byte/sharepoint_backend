namespace ZMS.Application.Contracts;

public interface IMigrationPlanService
{
    Task<CreateMigrationPlanResponse?> CreateFromAssessmentAsync(string assessmentId, CancellationToken cancellationToken);
    Task<MigrationPlan?> GetAsync(string planId, CancellationToken cancellationToken);
    Task<MigrationPlan?> GetLatestAsync(CancellationToken cancellationToken);
    Task<MigrationPlan?> UpdateAsync(string planId, MigrationPlan plan, CancellationToken cancellationToken);
    Task<MigrationPlanValidationResult?> ValidateAsync(string planId, CancellationToken cancellationToken);
    Task<MigrationRunbook?> GenerateRunbookAsync(string planId, CancellationToken cancellationToken);
    Task<MigrationPlanExportResult?> ExportAsync(string planId, string exportType, CancellationToken cancellationToken);
}

public interface IMigrationPlanStorageService
{
    Task SaveAsync(MigrationPlan plan, CancellationToken cancellationToken);
    Task<MigrationPlan?> GetAsync(string planId, CancellationToken cancellationToken);
    Task<MigrationPlan?> GetLatestAsync(CancellationToken cancellationToken);
    Task SaveValidationAsync(string planId, MigrationPlanValidationResult result, CancellationToken cancellationToken);
    Task SaveRunbookAsync(string planId, MigrationRunbook runbook, CancellationToken cancellationToken);
}

public interface IMigrationPlanGenerator
{
    MigrationPlan Generate(MigrationReadinessAssessment assessment);
}

public interface IMigrationPlanValidator
{
    MigrationPlanValidationResult Validate(MigrationPlan plan);
}

public interface IMigrationRunbookGenerator
{
    MigrationRunbook Generate(MigrationPlan plan, MigrationPlanValidationResult validation);
}

public interface IMigrationPlanExportService
{
    MigrationPlanExportResult ExportJson(MigrationPlan plan);
    MigrationPlanExportResult ExportCsv(MigrationPlan plan);
    MigrationPlanExportResult ExportMarkdown(MigrationPlan plan);
}

public sealed class CreateMigrationPlanResponse
{
    public string PlanId { get; set; } = string.Empty;
    public string AssessmentId { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public string Message { get; set; } = "Migration plan generated successfully";
}

public sealed class MigrationPlan
{
    public string PlanId { get; set; } = string.Empty;
    public string AssessmentId { get; set; } = string.Empty;
    public string ScanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "draft";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = "ZMS Planner";
    public string SourceEnvironment { get; set; } = "Discovered SharePoint source";
    public string TargetEnvironment { get; set; } = "Target SharePoint Online";
    public List<MigrationPlanWave> Waves { get; set; } = [];
    public List<MigrationPlanOption> Options { get; set; } = [];
    public List<MigrationPlanChecklistItem> Checklist { get; set; } = [];
    public List<ReadinessRiskFinding> Risks { get; set; } = [];
    public List<RemediationAction> RemediationPrerequisites { get; set; } = [];
    public List<MigrationPlanApproval> Approvals { get; set; } = [];
    public string RunbookPath { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public sealed class MigrationPlanWave
{
    public string WaveId { get; set; } = string.Empty;
    public string WaveName { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low";
    public int ReadinessScore { get; set; }
    public List<MigrationPlanItem> IncludedItems { get; set; } = [];
    public List<MigrationPlanItem> ExcludedItems { get; set; } = [];
    public List<string> Prerequisites { get; set; } = [];
    public int EstimatedFiles { get; set; }
    public long EstimatedStorage { get; set; }
    public string EstimatedDuration { get; set; } = "Not estimated";
    public string OwnerRole { get; set; } = "Migration Lead";
    public string ApprovalStatus { get; set; } = "not_started";
    public string Notes { get; set; } = string.Empty;
}

public sealed class MigrationPlanItem
{
    public string ItemId { get; set; } = string.Empty;
    public string SiteCollection { get; set; } = string.Empty;
    public string Library { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Library";
    public string SourceUrl { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public long StorageBytes { get; set; }
    public int MetadataCount { get; set; }
    public string PermissionRisk { get; set; } = "Low";
    public string MigrationAction { get; set; } = "migrate";
    public bool IncludeInMigration { get; set; } = true;
    public string Reason { get; set; } = string.Empty;
}

public sealed class MigrationPlanOption
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Value { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class MigrationPlanChecklistItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public string Status { get; set; } = "not_started";
    public string OwnerRole { get; set; } = "Migration Lead";
}

public sealed class MigrationPlanValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<MigrationPlanChecklistItem> Checklist { get; set; } = [];
}

public sealed class MigrationRunbook
{
    public string PlanId { get; set; } = string.Empty;
    public string FileName { get; set; } = "migration-runbook.md";
    public string Markdown { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
}

public sealed class MigrationPlanApproval
{
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = "not_started";
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTimeOffset? ApprovedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public sealed class MigrationPlanExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] Content { get; set; } = [];
}
