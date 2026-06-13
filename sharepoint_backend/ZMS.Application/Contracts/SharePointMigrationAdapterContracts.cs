namespace ZMS.Application.Contracts;

public interface ISharePointMigrationCapabilityService
{
    Task<SharePointMigrationCapabilityResult> ValidateAsync(SharePointMigrationCapabilityRequest request, CancellationToken cancellationToken);
}

public interface IMigrationTransferPreviewService
{
    Task<MigrationTransferPreview?> BuildFromJobAsync(string jobId, CancellationToken cancellationToken);
    Task<MigrationTransferPreview?> GetAsync(string previewId, CancellationToken cancellationToken);
}

public interface ILivePilotMigrationService
{
    Task<LivePilotMigrationResult?> RunFromJobAsync(string jobId, LivePilotMigrationRequest request, CancellationToken cancellationToken);
    Task<LivePilotMigrationResult?> GetAsync(string pilotRunId, CancellationToken cancellationToken);
    Task<SharePointMigrationExportResult?> ExportPilotAsync(string pilotRunId, string exportType, CancellationToken cancellationToken);
    Task<SharePointMigrationExportResult?> ExportPreviewAsync(string previewId, string exportType, CancellationToken cancellationToken);
}

public interface ILivePilotSafetyGate
{
    Task<IReadOnlyCollection<LivePilotSafetyCheck>> EvaluateAsync(MigrationExecutionJob job, LivePilotMigrationRequest request, CancellationToken cancellationToken);
}

public interface ISharePointMigrationAdapter
{
    Task<SharePointMigrationCapabilityResult> ValidateCapabilitiesAsync(SharePointMigrationCapabilityRequest request, CancellationToken cancellationToken);
    Task<MigrationTransferPreview> BuildTransferPreviewAsync(MigrationExecutionJob job, CancellationToken cancellationToken);
    Task<LivePilotMigrationResult> RunPilotAsync(MigrationExecutionJob job, LivePilotMigrationRequest request, IReadOnlyCollection<LivePilotSafetyCheck> safetyChecks, CancellationToken cancellationToken);
}

public interface ISharePointMigrationReportService
{
    SharePointMigrationExportResult ExportPreviewJson(MigrationTransferPreview preview);
    SharePointMigrationExportResult ExportPreviewCsv(MigrationTransferPreview preview);
    SharePointMigrationExportResult ExportPilotJson(LivePilotMigrationResult result);
    SharePointMigrationExportResult ExportPilotCsv(LivePilotMigrationResult result);
    SharePointMigrationExportResult ExportPilotMarkdown(LivePilotMigrationResult result);
}

public sealed class SharePointMigrationCapabilityRequest
{
    public string SourceSiteUrl { get; set; } = string.Empty;
    public string TargetSiteUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string Mode { get; set; } = "validate_only";
    public bool IncludePermissions { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
}

public sealed class SharePointMigrationCapabilityResult
{
    public bool IsReady { get; set; }
    public string Mode { get; set; } = "validate_only";
    public List<SharePointMigrationCapabilityCheck> Checks { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public SharePointMigrationCapabilities Capabilities { get; set; } = new();
}

public sealed class SharePointMigrationCapabilityCheck
{
    public string CheckId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "warning";
    public string Severity { get; set; } = "Medium";
    public string Message { get; set; } = string.Empty;
}

public sealed class SharePointMigrationCapabilities
{
    public bool CanReadSource { get; set; }
    public bool CanReadTarget { get; set; }
    public bool CanWriteTarget { get; set; }
    public bool CanUploadFiles { get; set; }
    public bool CanCreateFolders { get; set; }
    public bool CanApplyMetadata { get; set; }
    public bool CanApplyPermissions { get; set; }
}

public sealed class MigrationTransferPreview
{
    public string PreviewId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Mode { get; set; } = "preview_only";
    public DateTimeOffset GeneratedAt { get; set; }
    public int TotalItems { get; set; }
    public int EligibleItems { get; set; }
    public int BlockedItems { get; set; }
    public List<MetadataMappingPreview> MetadataMappings { get; set; } = [];
    public List<PermissionMappingPreview> PermissionMappings { get; set; } = [];
    public List<MigrationTransferPlanItem> TransferPlan { get; set; } = [];
    public List<MigrationBlockedItem> Blocked { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public sealed class MigrationTransferPlanItem
{
    public string ItemId { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string ItemType { get; set; } = "Library";
    public long EstimatedSizeBytes { get; set; }
    public string MetadataMappingStatus { get; set; } = "not_required";
    public string PermissionMappingStatus { get; set; } = "not_applied";
    public string Eligibility { get; set; } = "eligible";
    public string Reason { get; set; } = string.Empty;
}

public sealed class MetadataMappingPreview
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string MappingStatus { get; set; } = "mapped";
    public string Issue { get; set; } = string.Empty;
}

public sealed class PermissionMappingPreview
{
    public string SourcePrincipal { get; set; } = string.Empty;
    public string TargetPrincipal { get; set; } = string.Empty;
    public string PermissionLevel { get; set; } = string.Empty;
    public string MappingStatus { get; set; } = "not_applied";
    public string Issue { get; set; } = "Permission writeback is disabled for pilot mode.";
}

public sealed class MigrationBlockedItem
{
    public string ItemId { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public sealed class LivePilotMigrationRequest
{
    public string Mode { get; set; } = "live_pilot";
    public string ConfirmationText { get; set; } = string.Empty;
    public string SelectedWaveId { get; set; } = string.Empty;
    public string SelectedLibrary { get; set; } = string.Empty;
    public int MaxFiles { get; set; } = 10;
    public string SourceSiteUrl { get; set; } = string.Empty;
    public string TargetSiteUrl { get; set; } = string.Empty;
    public string TargetLibrary { get; set; } = string.Empty;
    public bool PreserveMetadata { get; set; } = true;
    public bool PreservePermissions { get; set; }
    public bool OverwriteExisting { get; set; }
}

public sealed class LivePilotMigrationResult
{
    public string PilotRunId { get; set; } = string.Empty;
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "blocked";
    public string Mode { get; set; } = "live_pilot";
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public int FilesAttempted { get; set; }
    public int FilesCopied { get; set; }
    public int FilesSkipped { get; set; }
    public List<LivePilotSafetyCheck> SafetyChecks { get; set; } = [];
    public List<LivePilotTransferResult> Items { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}

public sealed class LivePilotSafetyCheck
{
    public string CheckId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "failed";
    public string Severity { get; set; } = "High";
    public string Message { get; set; } = string.Empty;
}

public sealed class LivePilotTransferResult
{
    public string ItemId { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Status { get; set; } = "skipped";
    public string Message { get; set; } = string.Empty;
}

public sealed class SharePointMigrationExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] Content { get; set; } = [];
}
