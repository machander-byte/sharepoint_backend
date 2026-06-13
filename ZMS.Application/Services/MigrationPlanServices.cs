using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;

namespace ZMS.Application.Services;

public sealed class MigrationPlanService : IMigrationPlanService
{
    private readonly IReadinessStorageService _readinessStorage;
    private readonly IMigrationPlanStorageService _storage;
    private readonly IMigrationPlanGenerator _generator;
    private readonly IMigrationPlanValidator _validator;
    private readonly IMigrationRunbookGenerator _runbookGenerator;
    private readonly IMigrationPlanExportService _exportService;

    public MigrationPlanService(
        IReadinessStorageService readinessStorage,
        IMigrationPlanStorageService storage,
        IMigrationPlanGenerator generator,
        IMigrationPlanValidator validator,
        IMigrationRunbookGenerator runbookGenerator,
        IMigrationPlanExportService exportService)
    {
        _readinessStorage = readinessStorage;
        _storage = storage;
        _generator = generator;
        _validator = validator;
        _runbookGenerator = runbookGenerator;
        _exportService = exportService;
    }

    public async Task<CreateMigrationPlanResponse?> CreateFromAssessmentAsync(string assessmentId, CancellationToken cancellationToken)
    {
        var assessment = await _readinessStorage.GetAsync(assessmentId, cancellationToken);
        if (assessment is null)
        {
            return null;
        }

        var plan = _generator.Generate(assessment);
        await _storage.SaveAsync(plan, cancellationToken);
        return new CreateMigrationPlanResponse { PlanId = plan.PlanId, AssessmentId = plan.AssessmentId, Status = plan.Status };
    }

    public Task<MigrationPlan?> GetAsync(string planId, CancellationToken cancellationToken) => _storage.GetAsync(planId, cancellationToken);

    public Task<MigrationPlan?> GetLatestAsync(CancellationToken cancellationToken) => _storage.GetLatestAsync(cancellationToken);

    public async Task<MigrationPlan?> UpdateAsync(string planId, MigrationPlan plan, CancellationToken cancellationToken)
    {
        var existing = await _storage.GetAsync(planId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        plan.PlanId = existing.PlanId;
        plan.AssessmentId = existing.AssessmentId;
        plan.ScanId = existing.ScanId;
        plan.CreatedAt = existing.CreatedAt;
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await _storage.SaveAsync(plan, cancellationToken);
        return plan;
    }

    public async Task<MigrationPlanValidationResult?> ValidateAsync(string planId, CancellationToken cancellationToken)
    {
        var plan = await _storage.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var result = _validator.Validate(plan);
        await _storage.SaveValidationAsync(plan.PlanId, result, cancellationToken);
        return result;
    }

    public async Task<MigrationRunbook?> GenerateRunbookAsync(string planId, CancellationToken cancellationToken)
    {
        var plan = await _storage.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        var validation = _validator.Validate(plan);
        var runbook = _runbookGenerator.Generate(plan, validation);
        plan.RunbookPath = "migration-runbook.md";
        plan.UpdatedAt = DateTimeOffset.UtcNow;
        await _storage.SaveAsync(plan, cancellationToken);
        await _storage.SaveRunbookAsync(plan.PlanId, runbook, cancellationToken);
        return runbook;
    }

    public async Task<MigrationPlanExportResult?> ExportAsync(string planId, string exportType, CancellationToken cancellationToken)
    {
        var plan = await _storage.GetAsync(planId, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        return exportType.ToLowerInvariant() switch
        {
            "json" => _exportService.ExportJson(plan),
            "markdown" or "md" => _exportService.ExportMarkdown(plan),
            _ => _exportService.ExportCsv(plan)
        };
    }
}

public sealed class MigrationPlanGenerator : IMigrationPlanGenerator
{
    public MigrationPlan Generate(MigrationReadinessAssessment assessment)
    {
        var plan = new MigrationPlan
        {
            PlanId = Guid.NewGuid().ToString("D"),
            AssessmentId = assessment.AssessmentId,
            ScanId = assessment.ScanId,
            PlanName = $"Migration plan from readiness {assessment.GeneratedAt:yyyy-MM-dd}",
            Description = "Planning-only migration plan generated from readiness assessment. It does not execute migration.",
            Status = assessment.Summary.Blockers > 0 ? "blocked" : "draft",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            SourceEnvironment = "Discovery scan " + assessment.ScanId,
            TargetEnvironment = "Target SharePoint Online",
            Options = DefaultOptions(),
            Checklist = DefaultChecklist(),
            Risks = assessment.RiskFindings.ToList(),
            RemediationPrerequisites = assessment.RemediationActions.ToList(),
            Approvals =
            [
                new MigrationPlanApproval { Role = "Migration Lead" },
                new MigrationPlanApproval { Role = "SharePoint Admin / Security Owner" },
                new MigrationPlanApproval { Role = "Business Owner" }
            ],
            Warnings = assessment.Warnings.ToList(),
            Errors = assessment.Errors.ToList()
        };

        plan.Waves = assessment.MigrationWaves.OrderBy(w => w.RecommendedOrder).Select(wave => ToPlanWave(wave, assessment)).ToList();
        return plan;
    }

    private static MigrationPlanWave ToPlanWave(MigrationWaveSuggestion wave, MigrationReadinessAssessment assessment)
    {
        var blockers = assessment.RiskFindings.Where(r => r.MigrationBlocker).ToList();
        var highRiskLibraries = assessment.RiskFindings
            .Where(r => r.Severity is "High" or "Critical")
            .Select(r => r.AffectedLibrary)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var archiveLibraries = assessment.RiskFindings
            .Where(r => r.Category == "Archived Content")
            .Select(r => r.AffectedLibrary)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var restrictedLibraries = assessment.RiskFindings
            .Where(r => r.Category is "Restricted Content" or "Permissions")
            .Select(r => r.AffectedLibrary)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = wave.IncludedLibraries.Select(library =>
        {
            var site = wave.IncludedSites.FirstOrDefault() ?? "";
            var blocker = blockers.FirstOrDefault(r => r.AffectedLibrary.Equals(library, StringComparison.OrdinalIgnoreCase));
            var action = blocker is not null
                ? "exclude"
                : archiveLibraries.Contains(library)
                    ? "archive"
                    : restrictedLibraries.Contains(library)
                        ? "manual_review"
                        : highRiskLibraries.Contains(library)
                            ? "remediate_first"
                            : "migrate";
            return new MigrationPlanItem
            {
                ItemId = Guid.NewGuid().ToString("D"),
                SiteCollection = site,
                Library = library,
                Path = library,
                ItemType = "Library",
                SourceUrl = library,
                TargetUrl = library,
                FileCount = Math.Max(0, wave.EstimatedFiles / Math.Max(1, wave.IncludedLibraries.Count)),
                StorageBytes = Math.Max(0, wave.EstimatedStorage / Math.Max(1, wave.IncludedLibraries.Count)),
                PermissionRisk = restrictedLibraries.Contains(library) ? "High" : wave.RiskLevel,
                MigrationAction = action,
                IncludeInMigration = action != "exclude",
                Reason = blocker?.RecommendedAction ?? (action == "migrate" ? "Ready for draft planning." : "Requires review before execution planning.")
            };
        }).ToList();

        return new MigrationPlanWave
        {
            WaveId = wave.WaveId,
            WaveName = wave.WaveName,
            Order = wave.RecommendedOrder,
            Description = wave.Description,
            RiskLevel = wave.RiskLevel,
            ReadinessScore = wave.ReadinessScore,
            IncludedItems = items.Where(item => item.IncludeInMigration).ToList(),
            ExcludedItems = items.Where(item => !item.IncludeInMigration).ToList(),
            Prerequisites = wave.Prerequisites,
            EstimatedFiles = wave.EstimatedFiles,
            EstimatedStorage = wave.EstimatedStorage,
            EstimatedDuration = EstimateDuration(wave.EstimatedFiles),
            OwnerRole = wave.RiskLevel is "High" or "Critical" ? "Migration Lead / Security Owner" : "Migration Lead",
            ApprovalStatus = "not_started",
            Notes = "Generated from readiness assessment. Review before execution design."
        };
    }

    public static List<MigrationPlanOption> DefaultOptions() =>
    [
        Option("preservePermissions", "Preserve permissions", true),
        Option("preserveMetadata", "Preserve metadata", true),
        Option("includeVersionHistory", "Include version history", true),
        Option("includeSubsites", "Include subsites", true),
        Option("skipArchivedContent", "Skip archived content", false),
        Option("renameInvalidFiles", "Rename invalid files", true),
        Option("flattenLongPaths", "Flatten long paths", false),
        Option("includeLargeFiles", "Include large files", true),
        Option("validateAfterMigration", "Validate after migration", true),
        Option("generatePreMigrationReport", "Generate pre-migration report", true),
        Option("generatePostMigrationReport", "Generate post-migration report", true)
    ];

    public static List<MigrationPlanChecklistItem> DefaultChecklist() =>
    [
        Check("source-access", "Confirm source SharePoint access", "Access", "SharePoint Admin"),
        Check("target-access", "Confirm target SharePoint access", "Access", "SharePoint Admin"),
        Check("graph-pnp", "Confirm Microsoft Graph/PnP permissions", "Access", "SharePoint Admin"),
        Check("broken-permissions", "Review broken permission areas", "Security", "Security Owner"),
        Check("metadata", "Review metadata mapping", "Information Architecture", "Information Architect"),
        Check("long-path", "Review long path risks", "Content", "Content Owner"),
        Check("large-files", "Review large file risks", "Content", "Migration Engineer"),
        Check("archive", "Confirm archive strategy", "Governance", "Business Owner"),
        Check("restricted", "Confirm restricted content approvals", "Security", "Security Owner"),
        Check("owners", "Confirm migration wave owners", "Governance", "Migration Lead"),
        Check("pre-report", "Generate pre-migration report", "Reporting", "Migration Lead"),
        Check("rollback", "Confirm rollback/restore plan", "Operations", "Migration Lead"),
        Check("validation", "Confirm post-migration validation plan", "Validation", "Migration Lead")
    ];

    private static MigrationPlanOption Option(string key, string label, bool value) => new() { Key = key, Label = label, Value = value, Description = "Planning option only. No migration execution is performed." };
    private static MigrationPlanChecklistItem Check(string id, string title, string category, string ownerRole) => new() { Id = id, Title = title, Description = title, Category = category, Required = true, Status = "not_started", OwnerRole = ownerRole };
    private static string EstimateDuration(int files) => $"PT{Math.Max(30, files / 25)}M";
}

public sealed class MigrationPlanValidator : IMigrationPlanValidator
{
    public MigrationPlanValidationResult Validate(MigrationPlan plan)
    {
        var result = new MigrationPlanValidationResult { Checklist = plan.Checklist };
        if (plan.Waves.Count == 0) result.Errors.Add("Plan has no migration waves.");
        if (string.IsNullOrWhiteSpace(plan.SourceEnvironment)) result.Errors.Add("Source environment is missing.");
        if (string.IsNullOrWhiteSpace(plan.TargetEnvironment)) result.Errors.Add("Target environment is missing.");
        foreach (var wave in plan.Waves)
        {
            if (wave.IncludedItems.Count == 0) result.Errors.Add($"{wave.WaveName} has no included items.");
            if (string.IsNullOrWhiteSpace(wave.OwnerRole)) result.Warnings.Add($"{wave.WaveName} has no owner assigned.");
            foreach (var item in wave.IncludedItems)
            {
                if (item.MigrationAction == "exclude") result.Errors.Add($"{item.Library} is excluded but still included in a wave.");
                if (item.MigrationAction == "manual_review" && wave.ApprovalStatus != "approved") result.Errors.Add($"{item.Library} is restricted/manual review content without approval.");
                if (item.MigrationAction == "remediate_first") result.Warnings.Add($"{item.Library} is high risk and requires remediation before execution.");
                if (item.MigrationAction == "archive") result.Warnings.Add($"{item.Library} is archive-heavy and included in the plan.");
                if (item.PermissionRisk is "High" or "Critical" && PreservePermissions(plan)) result.Warnings.Add($"{item.Library} has permission risk while preserve permissions is enabled.");
            }
        }

        if (plan.Checklist.All(item => item.Status != "completed")) result.Warnings.Add("No validation checklist items are completed.");
        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private static bool PreservePermissions(MigrationPlan plan) =>
        plan.Options.FirstOrDefault(option => option.Key == "preservePermissions")?.Value == true;
}

public sealed class MigrationRunbookGenerator : IMigrationRunbookGenerator
{
    public MigrationRunbook Generate(MigrationPlan plan, MigrationPlanValidationResult validation)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Migration Planning Runbook");
        builder.AppendLine();
        builder.AppendLine("> This is a planning runbook only. It is not an execution script and does not modify SharePoint.");
        builder.AppendLine();
        builder.AppendLine("## 1. Migration Plan Overview");
        builder.AppendLine($"- Plan: {plan.PlanName}");
        builder.AppendLine($"- Status: {plan.Status}");
        builder.AppendLine($"- Assessment: {plan.AssessmentId}");
        builder.AppendLine();
        builder.AppendLine("## 2. Source and Target Environment");
        builder.AppendLine($"- Source: {plan.SourceEnvironment}");
        builder.AppendLine($"- Target: {plan.TargetEnvironment}");
        builder.AppendLine();
        builder.AppendLine("## 3. Migration Scope");
        builder.AppendLine($"- Waves: {plan.Waves.Count}");
        builder.AppendLine($"- Included items: {plan.Waves.Sum(w => w.IncludedItems.Count)}");
        builder.AppendLine($"- Excluded items: {plan.Waves.Sum(w => w.ExcludedItems.Count)}");
        builder.AppendLine();
        builder.AppendLine("## 4. Wave Plan");
        foreach (var wave in plan.Waves.OrderBy(w => w.Order))
        {
            builder.AppendLine($"### {wave.WaveName}");
            builder.AppendLine($"- Risk: {wave.RiskLevel}");
            builder.AppendLine($"- Readiness: {wave.ReadinessScore}");
            builder.AppendLine($"- Owner: {wave.OwnerRole}");
            builder.AppendLine($"- Libraries: {string.Join(", ", wave.IncludedItems.Select(i => i.Library).DefaultIfEmpty("None"))}");
        }
        builder.AppendLine();
        builder.AppendLine("## 5. Excluded Content");
        foreach (var item in plan.Waves.SelectMany(w => w.ExcludedItems)) builder.AppendLine($"- {item.Library}: {item.Reason}");
        builder.AppendLine();
        builder.AppendLine("## 6. Pre-Migration Checklist");
        foreach (var item in plan.Checklist) builder.AppendLine($"- [{(item.Status == "completed" ? "x" : " ")}] {item.Title} ({item.OwnerRole})");
        builder.AppendLine();
        builder.AppendLine("## 7. Remediation Prerequisites");
        foreach (var action in plan.RemediationPrerequisites) builder.AppendLine($"- {action.Priority}: {action.ActionTitle}");
        builder.AppendLine();
        builder.AppendLine("## 8. Permission Review");
        builder.AppendLine("Review restricted and broken inheritance content before future execution.");
        builder.AppendLine();
        builder.AppendLine("## 9. Metadata Mapping Review");
        builder.AppendLine("Confirm required metadata mappings and unresolved metadata risk findings.");
        builder.AppendLine();
        builder.AppendLine("## 10. Risk Summary");
        foreach (var risk in plan.Risks.Take(20)) builder.AppendLine($"- {risk.Severity} {risk.Category}: {risk.Title} at {risk.AffectedLocation}");
        builder.AppendLine();
        builder.AppendLine("## 11. Execution Notes");
        builder.AppendLine("No execution is performed by this runbook. Use it to prepare a future approved migration job.");
        builder.AppendLine();
        builder.AppendLine("## 12. Validation Plan");
        builder.AppendLine("Run pre-migration inventory export and post-migration validation once execution exists.");
        builder.AppendLine();
        builder.AppendLine("## 13. Rollback Notes");
        builder.AppendLine("Confirm source retention, target cleanup approach, and restore ownership before execution.");
        builder.AppendLine();
        builder.AppendLine("## 14. Approval Summary");
        foreach (var approval in plan.Approvals) builder.AppendLine($"- {approval.Role}: {approval.Status}");
        builder.AppendLine();
        builder.AppendLine("## Validation");
        builder.AppendLine($"- Valid: {validation.IsValid}");
        foreach (var error in validation.Errors) builder.AppendLine($"- Error: {error}");
        foreach (var warning in validation.Warnings) builder.AppendLine($"- Warning: {warning}");

        return new MigrationRunbook { PlanId = plan.PlanId, Markdown = builder.ToString(), GeneratedAt = DateTimeOffset.UtcNow };
    }
}

public sealed class MigrationPlanStorageService : IMigrationPlanStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _rootDirectory;
    private readonly IMigrationPlanExportService _exportService;

    public MigrationPlanStorageService(IHostEnvironment hostEnvironment, IMigrationPlanExportService exportService)
    {
        _rootDirectory = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "migration-plans");
        _exportService = exportService;
    }

    public async Task SaveAsync(MigrationPlan plan, CancellationToken cancellationToken)
    {
        var directory = GetDirectory(plan.PlanId);
        Directory.CreateDirectory(directory);
        await WriteJsonAsync(Path.Combine(directory, "plan.json"), plan, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "migration-plan.csv"), _exportService.ExportCsv(plan).Content, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "wave-summary.csv"), Encoding.UTF8.GetBytes(Csv(plan.Waves.Select(w => new[] { w.WaveName, w.RiskLevel, w.ReadinessScore.ToString(), w.EstimatedFiles.ToString(), w.EstimatedStorage.ToString(), w.ApprovalStatus }))), cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(directory, "excluded-items.csv"), Encoding.UTF8.GetBytes(Csv(plan.Waves.SelectMany(w => w.ExcludedItems).Select(i => new[] { i.SiteCollection, i.Library, i.Path, i.MigrationAction, i.Reason }))), cancellationToken);
        await WriteJsonAsync(Path.Combine(directory, "validation-checklist.json"), plan.Checklist, cancellationToken);
    }

    public Task<MigrationPlan?> GetAsync(string planId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(planId, out _)) return Task.FromResult<MigrationPlan?>(null);
        return ReadJsonAsync<MigrationPlan>(Path.Combine(GetDirectory(planId), "plan.json"), cancellationToken);
    }

    public async Task<MigrationPlan?> GetLatestAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_rootDirectory)) return null;
        var plans = new List<MigrationPlan>();
        foreach (var path in Directory.EnumerateFiles(_rootDirectory, "plan.json", SearchOption.AllDirectories))
        {
            var plan = await ReadJsonAsync<MigrationPlan>(path, cancellationToken);
            if (plan is not null) plans.Add(plan);
        }
        return plans.OrderByDescending(p => p.UpdatedAt).FirstOrDefault();
    }

    public Task SaveValidationAsync(string planId, MigrationPlanValidationResult result, CancellationToken cancellationToken) =>
        WriteJsonAsync(Path.Combine(GetDirectory(planId), "validation-result.json"), result, cancellationToken);

    public async Task SaveRunbookAsync(string planId, MigrationRunbook runbook, CancellationToken cancellationToken)
    {
        var directory = GetDirectory(planId);
        await File.WriteAllTextAsync(Path.Combine(directory, "migration-runbook.md"), runbook.Markdown, cancellationToken);
    }

    private string GetDirectory(string planId) => Path.Combine(_rootDirectory, planId);
    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken) { await using var stream = File.Create(path); await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken); }
    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken cancellationToken) { if (!File.Exists(path)) return default; await using var stream = File.OpenRead(path); return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken); }
    internal static string Csv(IEnumerable<string[]> rows) { static string Escape(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\""; return string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(Escape)))); }
}

public sealed class MigrationPlanExportService : IMigrationPlanExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public MigrationPlanExportResult ExportJson(MigrationPlan plan) => new() { FileName = $"migration-plan-{plan.PlanId}.json", ContentType = "application/json", Content = JsonSerializer.SerializeToUtf8Bytes(plan, JsonOptions) };
    public MigrationPlanExportResult ExportCsv(MigrationPlan plan)
    {
        var rows = new List<string[]> { new[] { "Wave", "Site", "Library", "Action", "Included", "Files", "Storage", "Reason" } };
        rows.AddRange(plan.Waves.SelectMany(w => w.IncludedItems.Concat(w.ExcludedItems).Select(i => new[] { w.WaveName, i.SiteCollection, i.Library, i.MigrationAction, i.IncludeInMigration.ToString(), i.FileCount.ToString(), i.StorageBytes.ToString(), i.Reason })));
        return new MigrationPlanExportResult { FileName = $"migration-plan-{plan.PlanId}.csv", ContentType = "text/csv", Content = Encoding.UTF8.GetBytes(MigrationPlanStorageService.Csv(rows)) };
    }
    public MigrationPlanExportResult ExportMarkdown(MigrationPlan plan)
    {
        var runbook = new MigrationRunbookGenerator().Generate(plan, new MigrationPlanValidator().Validate(plan));
        return new MigrationPlanExportResult { FileName = $"migration-runbook-{plan.PlanId}.md", ContentType = "text/markdown", Content = Encoding.UTF8.GetBytes(runbook.Markdown) };
    }
}
