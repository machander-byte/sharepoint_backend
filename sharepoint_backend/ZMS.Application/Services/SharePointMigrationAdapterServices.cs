using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;

namespace ZMS.Application.Services;

public sealed class SharePointMigrationCapabilityService : ISharePointMigrationCapabilityService
{
    private readonly ISharePointMigrationAdapter _adapter;
    public SharePointMigrationCapabilityService(ISharePointMigrationAdapter adapter) => _adapter = adapter;
    public Task<SharePointMigrationCapabilityResult> ValidateAsync(SharePointMigrationCapabilityRequest request, CancellationToken cancellationToken) => _adapter.ValidateCapabilitiesAsync(request, cancellationToken);
}

public sealed class MigrationTransferPreviewService : IMigrationTransferPreviewService
{
    private readonly IMigrationExecutionStorageService _executionStorage;
    private readonly ISharePointMigrationAdapter _adapter;
    private readonly SharePointMigrationStorage _storage;

    public MigrationTransferPreviewService(IMigrationExecutionStorageService executionStorage, ISharePointMigrationAdapter adapter, SharePointMigrationStorage storage)
    {
        _executionStorage = executionStorage;
        _adapter = adapter;
        _storage = storage;
    }

    public async Task<MigrationTransferPreview?> BuildFromJobAsync(string jobId, CancellationToken cancellationToken)
    {
        var job = await _executionStorage.GetAsync(jobId, cancellationToken);
        if (job is null) return null;
        var preview = await _adapter.BuildTransferPreviewAsync(job, cancellationToken);
        await _storage.SavePreviewAsync(preview, cancellationToken);
        return preview;
    }

    public Task<MigrationTransferPreview?> GetAsync(string previewId, CancellationToken cancellationToken) => _storage.GetPreviewAsync(previewId, cancellationToken);
}

public sealed class LivePilotMigrationService : ILivePilotMigrationService
{
    private readonly IMigrationExecutionStorageService _executionStorage;
    private readonly ILivePilotSafetyGate _safetyGate;
    private readonly ISharePointMigrationAdapter _adapter;
    private readonly SharePointMigrationStorage _storage;
    private readonly ISharePointMigrationReportService _reportService;

    public LivePilotMigrationService(IMigrationExecutionStorageService executionStorage, ILivePilotSafetyGate safetyGate, ISharePointMigrationAdapter adapter, SharePointMigrationStorage storage, ISharePointMigrationReportService reportService)
    {
        _executionStorage = executionStorage;
        _safetyGate = safetyGate;
        _adapter = adapter;
        _storage = storage;
        _reportService = reportService;
    }

    public async Task<LivePilotMigrationResult?> RunFromJobAsync(string jobId, LivePilotMigrationRequest request, CancellationToken cancellationToken)
    {
        var job = await _executionStorage.GetAsync(jobId, cancellationToken);
        if (job is null) return null;
        var checks = await _safetyGate.EvaluateAsync(job, request, cancellationToken);
        var result = await _adapter.RunPilotAsync(job, request, checks, cancellationToken);
        await _storage.SavePilotAsync(result, cancellationToken);
        return result;
    }

    public Task<LivePilotMigrationResult?> GetAsync(string pilotRunId, CancellationToken cancellationToken) => _storage.GetPilotAsync(pilotRunId, cancellationToken);

    public async Task<SharePointMigrationExportResult?> ExportPilotAsync(string pilotRunId, string exportType, CancellationToken cancellationToken)
    {
        var result = await _storage.GetPilotAsync(pilotRunId, cancellationToken);
        if (result is null) return null;
        return exportType.ToLowerInvariant() switch
        {
            "json" => _reportService.ExportPilotJson(result),
            "markdown" or "md" => _reportService.ExportPilotMarkdown(result),
            _ => _reportService.ExportPilotCsv(result)
        };
    }

    public async Task<SharePointMigrationExportResult?> ExportPreviewAsync(string previewId, string exportType, CancellationToken cancellationToken)
    {
        var preview = await _storage.GetPreviewAsync(previewId, cancellationToken);
        if (preview is null) return null;
        return exportType.ToLowerInvariant() == "json" ? _reportService.ExportPreviewJson(preview) : _reportService.ExportPreviewCsv(preview);
    }
}

public sealed class LivePilotSafetyGate : ILivePilotSafetyGate
{
    private readonly IConfiguration _configuration;
    private readonly IPreMigrationStorageService _preMigrationStorage;

    public LivePilotSafetyGate(IConfiguration configuration, IPreMigrationStorageService preMigrationStorage)
    {
        _configuration = configuration;
        _preMigrationStorage = preMigrationStorage;
    }

    public async Task<IReadOnlyCollection<LivePilotSafetyCheck>> EvaluateAsync(MigrationExecutionJob job, LivePilotMigrationRequest request, CancellationToken cancellationToken)
    {
        var latestValidation = await _preMigrationStorage.GetLatestValidationAsync(cancellationToken);
        var enabled = string.Equals(Environment.GetEnvironmentVariable("ZMS_ENABLE_LIVE_MIGRATION") ?? _configuration["ZMS_ENABLE_LIVE_MIGRATION"], "true", StringComparison.OrdinalIgnoreCase);
        var maxFiles = int.TryParse(Environment.GetEnvironmentVariable("ZMS_LIVE_PILOT_MAX_FILES") ?? _configuration["ZMS_LIVE_PILOT_MAX_FILES"], out var configuredMax) ? configuredMax : 10;
        var checks = new List<LivePilotSafetyCheck>();
        Add(checks, "env-flag", "Live migration flag enabled", enabled, "Set ZMS_ENABLE_LIVE_MIGRATION=true only for a test tenant.");
        Add(checks, "mode", "Request mode is live_pilot", request.Mode == "live_pilot", "Use mode live_pilot.");
        Add(checks, "confirmation", "Explicit confirmation text supplied", request.ConfirmationText == "ENABLE LIVE PILOT MIGRATION", "Confirmation text must exactly match ENABLE LIVE PILOT MIGRATION.");
        Add(checks, "job-mode", "Execution job is not simulation-only", job.Mode != "simulation", "Create a non-simulation job only after explicit approval.");
        Add(checks, "validation", "Latest Go/No-Go allows pilot", latestValidation?.PlanId == job.PlanId && latestValidation.Decision is "go" or "conditional_go", "Run pre-migration validation and resolve no_go blockers.");
        Add(checks, "wave", "Selected wave exists", job.Waves.Any(w => w.SourceWaveId == request.SelectedWaveId || w.WaveExecutionId == request.SelectedWaveId), "Select one valid wave.");
        Add(checks, "scope-files", "Pilot file limit respected", request.MaxFiles > 0 && request.MaxFiles <= maxFiles, $"Max files must be between 1 and {maxFiles}.");
        Add(checks, "target-site", "Target site URL supplied", Uri.TryCreate(request.TargetSiteUrl, UriKind.Absolute, out _), "Provide explicit target site URL.");
        Add(checks, "target-library", "Target library supplied", !string.IsNullOrWhiteSpace(request.TargetLibrary), "Provide explicit target library.");
        Add(checks, "permissions", "Permission writeback disabled", !request.PreservePermissions, "Permission preservation is disabled for this pilot phase.");
        Add(checks, "overwrite", "Overwrite disabled", !request.OverwriteExisting, "Overwrite existing files must remain false.");
        Add(checks, "delete", "No delete operations requested", true, "Deletes are not supported.");
        return checks;
    }

    private static void Add(List<LivePilotSafetyCheck> checks, string id, string title, bool passed, string message)
    {
        checks.Add(new LivePilotSafetyCheck { CheckId = id, Title = title, Status = passed ? "passed" : "failed", Severity = passed ? "Info" : "High", Message = passed ? "Satisfied." : message });
    }
}

public sealed class SharePointMigrationAdapter : ISharePointMigrationAdapter
{
    public Task<SharePointMigrationCapabilityResult> ValidateCapabilitiesAsync(SharePointMigrationCapabilityRequest request, CancellationToken cancellationToken)
    {
        var liveEnabled = string.Equals(Environment.GetEnvironmentVariable("ZMS_ENABLE_LIVE_MIGRATION"), "true", StringComparison.OrdinalIgnoreCase);
        var result = new SharePointMigrationCapabilityResult { Mode = request.Mode };
        Add(result, "source-url", "Source SharePoint URL format", Uri.TryCreate(request.SourceSiteUrl, UriKind.Absolute, out _), "Provide a valid source site URL.");
        Add(result, "target-url", "Target SharePoint URL format", Uri.TryCreate(request.TargetSiteUrl, UriKind.Absolute, out _), "Provide a valid target site URL.");
        Add(result, "client-id", "Client ID present", !string.IsNullOrWhiteSpace(request.ClientId), "Provide approved PnP/Entra client ID.");
        Add(result, "live-flag", "Live migration flag", liveEnabled, "Live migration disabled by default.");
        result.Capabilities = new SharePointMigrationCapabilities
        {
            CanReadSource = Uri.TryCreate(request.SourceSiteUrl, UriKind.Absolute, out _),
            CanReadTarget = Uri.TryCreate(request.TargetSiteUrl, UriKind.Absolute, out _),
            CanWriteTarget = liveEnabled,
            CanUploadFiles = liveEnabled,
            CanCreateFolders = liveEnabled,
            CanApplyMetadata = liveEnabled && request.IncludeMetadata,
            CanApplyPermissions = false
        };
        result.Warnings.Add("Real tenant connectivity is not attempted during validate_only mode.");
        result.Warnings.Add("Permission writeback is disabled for locked pilot mode.");
        result.Errors = result.Checks.Where(c => c.Status == "failed" && c.CheckId is "source-url" or "target-url" or "client-id").Select(c => c.Message).ToList();
        result.IsReady = result.Errors.Count == 0 && liveEnabled;
        return Task.FromResult(result);
    }

    public Task<MigrationTransferPreview> BuildTransferPreviewAsync(MigrationExecutionJob job, CancellationToken cancellationToken)
    {
        var plan = job.Waves.SelectMany(w => w.Items.Select(item => Classify(job, w, item))).ToList();
        var preview = new MigrationTransferPreview
        {
            PreviewId = Guid.NewGuid().ToString("D"),
            JobId = job.JobId,
            GeneratedAt = DateTimeOffset.UtcNow,
            TransferPlan = plan,
            TotalItems = plan.Count,
            EligibleItems = plan.Count(p => p.Eligibility == "eligible"),
            BlockedItems = plan.Count(p => p.Eligibility == "blocked"),
            Blocked = plan.Where(p => p.Eligibility == "blocked").Select(p => new MigrationBlockedItem { ItemId = p.ItemId, Path = p.SourcePath, Reason = p.Reason, RecommendedAction = "Resolve blocker before live pilot." }).ToList(),
            MetadataMappings = [new MetadataMappingPreview { SourceField = "Title", TargetField = "Title", MappingStatus = "mapped" }],
            PermissionMappings = [new PermissionMappingPreview { SourcePrincipal = "Source Owners", TargetPrincipal = "Target Owners", PermissionLevel = "Owner", MappingStatus = "preview_only" }],
            Warnings = ["Preview only. No SharePoint tenant changes performed.", "Permission mappings are preview-only and will not be applied in pilot mode."]
        };
        return Task.FromResult(preview);
    }

    public Task<LivePilotMigrationResult> RunPilotAsync(MigrationExecutionJob job, LivePilotMigrationRequest request, IReadOnlyCollection<LivePilotSafetyCheck> safetyChecks, CancellationToken cancellationToken)
    {
        var blocked = safetyChecks.Any(c => c.Status == "failed");
        var result = new LivePilotMigrationResult
        {
            PilotRunId = Guid.NewGuid().ToString("D"),
            JobId = job.JobId,
            Mode = request.Mode,
            GeneratedAt = DateTimeOffset.UtcNow,
            SafetyChecks = safetyChecks.ToList(),
            Status = "blocked",
            Message = "Live migration is disabled. Set ZMS_ENABLE_LIVE_MIGRATION=true and pass all safety gates.",
            Warnings = ["No SharePoint tenant changes performed."]
        };
        if (blocked) return Task.FromResult(result);

        result.Message = "Safety gates passed, but real file copy is not implemented in this foundation phase.";
        result.Errors.Add("Guarded placeholder adapter: real small-file pilot copy must be implemented and tested in a dedicated test tenant.");
        return Task.FromResult(result);
    }

    private static MigrationTransferPlanItem Classify(MigrationExecutionJob job, MigrationExecutionWave wave, MigrationExecutionItem item)
    {
        var blocked = item.Status is "failed" or "skipped" || item.Action is "manual_review" or "remediate_first" || string.IsNullOrWhiteSpace(item.SimulatedTargetUrl);
        var reason = blocked
            ? item.Status == "failed" ? "Execution item failed in simulation."
              : item.Action is "manual_review" ? "Manual review required."
              : item.Action is "remediate_first" ? "Remediation required before pilot."
              : string.IsNullOrWhiteSpace(item.SimulatedTargetUrl) ? "No target path available."
              : "Item skipped in simulation."
            : "Eligible for future pilot planning.";
        return new MigrationTransferPlanItem
        {
            ItemId = item.ItemExecutionId,
            SourcePath = item.SimulatedSourceUrl.Length > 0 ? item.SimulatedSourceUrl : item.Path,
            TargetPath = item.SimulatedTargetUrl,
            ItemType = item.ItemType,
            MetadataMappingStatus = "previewed",
            PermissionMappingStatus = "not_applied",
            Eligibility = blocked ? "blocked" : item.Warnings.Count > 0 ? "warning" : "eligible",
            Reason = reason
        };
    }

    private static void Add(SharePointMigrationCapabilityResult result, string id, string title, bool passed, string message)
    {
        result.Checks.Add(new SharePointMigrationCapabilityCheck { CheckId = id, Title = title, Status = passed ? "passed" : "failed", Severity = passed ? "Info" : "High", Message = passed ? "Satisfied." : message });
    }
}

public sealed class SharePointMigrationStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _previewRoot;
    private readonly string _pilotRoot;
    private readonly ISharePointMigrationReportService _reports;

    public SharePointMigrationStorage(IHostEnvironment env, ISharePointMigrationReportService reports)
    {
        _previewRoot = Path.Combine(env.ContentRootPath, "App_Data", "sharepoint-migration-previews");
        _pilotRoot = Path.Combine(env.ContentRootPath, "App_Data", "sharepoint-migration-pilots");
        _reports = reports;
    }

    public async Task SavePreviewAsync(MigrationTransferPreview preview, CancellationToken ct)
    {
        var dir = Path.Combine(_previewRoot, preview.PreviewId); Directory.CreateDirectory(dir);
        await WriteJsonAsync(Path.Combine(dir, "transfer-preview.json"), preview, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "transfer-plan.csv"), _reports.ExportPreviewCsv(preview).Content, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "metadata-mapping-preview.csv"), Encoding.UTF8.GetBytes(Csv(preview.MetadataMappings.Select(m => new[] { m.SourceField, m.TargetField, m.MappingStatus, m.Issue }))), ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "permission-mapping-preview.csv"), Encoding.UTF8.GetBytes(Csv(preview.PermissionMappings.Select(p => new[] { p.SourcePrincipal, p.TargetPrincipal, p.PermissionLevel, p.MappingStatus, p.Issue }))), ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "blocked-items.csv"), Encoding.UTF8.GetBytes(Csv(preview.Blocked.Select(b => new[] { b.ItemId, b.Path, b.Reason, b.RecommendedAction }))), ct);
    }

    public Task<MigrationTransferPreview?> GetPreviewAsync(string id, CancellationToken ct) => Guid.TryParse(id, out _) ? ReadJsonAsync<MigrationTransferPreview>(Path.Combine(_previewRoot, id, "transfer-preview.json"), ct) : Task.FromResult<MigrationTransferPreview?>(null);

    public async Task SavePilotAsync(LivePilotMigrationResult result, CancellationToken ct)
    {
        var dir = Path.Combine(_pilotRoot, result.PilotRunId); Directory.CreateDirectory(dir);
        await WriteJsonAsync(Path.Combine(dir, "pilot-result.json"), result, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "pilot-report.md"), _reports.ExportPilotMarkdown(result).Content, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "pilot-items.csv"), _reports.ExportPilotCsv(result).Content, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "pilot-errors.csv"), Encoding.UTF8.GetBytes(Csv(result.Errors.Select(e => new[] { "Error", e }))), ct);
    }

    public Task<LivePilotMigrationResult?> GetPilotAsync(string id, CancellationToken ct) => Guid.TryParse(id, out _) ? ReadJsonAsync<LivePilotMigrationResult>(Path.Combine(_pilotRoot, id, "pilot-result.json"), ct) : Task.FromResult<LivePilotMigrationResult?>(null);

    internal static string Csv(IEnumerable<string[]> rows) { static string E(string v) => $"\"{(v ?? "").Replace("\"", "\"\"")}\""; return string.Join(Environment.NewLine, rows.Select(r => string.Join(",", r.Select(E)))); }
    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken ct) { await using var stream = File.Create(path); await JsonSerializer.SerializeAsync(stream, value, JsonOptions, ct); }
    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken ct) { if (!File.Exists(path)) return default; await using var stream = File.OpenRead(path); return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct); }
}

public sealed class SharePointMigrationReportService : ISharePointMigrationReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public SharePointMigrationExportResult ExportPreviewJson(MigrationTransferPreview preview) => new() { FileName = $"transfer-preview-{preview.PreviewId}.json", ContentType = "application/json", Content = JsonSerializer.SerializeToUtf8Bytes(preview, JsonOptions) };
    public SharePointMigrationExportResult ExportPreviewCsv(MigrationTransferPreview preview) => new() { FileName = $"transfer-plan-{preview.PreviewId}.csv", ContentType = "text/csv", Content = Encoding.UTF8.GetBytes(SharePointMigrationStorage.Csv(preview.TransferPlan.Select(i => new[] { i.ItemId, i.SourcePath, i.TargetPath, i.ItemType, i.Eligibility, i.MetadataMappingStatus, i.PermissionMappingStatus, i.Reason }))) };
    public SharePointMigrationExportResult ExportPilotJson(LivePilotMigrationResult result) => new() { FileName = $"pilot-result-{result.PilotRunId}.json", ContentType = "application/json", Content = JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions) };
    public SharePointMigrationExportResult ExportPilotCsv(LivePilotMigrationResult result) => new() { FileName = $"pilot-items-{result.PilotRunId}.csv", ContentType = "text/csv", Content = Encoding.UTF8.GetBytes(SharePointMigrationStorage.Csv(result.Items.Select(i => new[] { i.ItemId, i.SourcePath, i.TargetPath, i.Status, i.Message }))) };
    public SharePointMigrationExportResult ExportPilotMarkdown(LivePilotMigrationResult result)
    {
        var md = new StringBuilder("# Locked Live Pilot Migration Report\n\n").AppendLine("> Live pilot is disabled by default. No tenant changes are performed unless all safety gates pass.").AppendLine($"Status: **{result.Status}**").AppendLine(result.Message).AppendLine("\n## Safety Checks");
        foreach (var check in result.SafetyChecks) md.AppendLine($"- {check.Status}: {check.Title} - {check.Message}");
        return new() { FileName = $"pilot-report-{result.PilotRunId}.md", ContentType = "text/markdown", Content = Encoding.UTF8.GetBytes(md.ToString()) };
    }
}
