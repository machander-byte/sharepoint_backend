using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;

namespace ZMS.Application.Services;

public sealed class PreMigrationValidationService : IPreMigrationValidationService
{
    private readonly IMigrationPlanStorageService _planStorage;
    private readonly IPreMigrationStorageService _storage;
    private readonly IPreMigrationCheckEngine _checkEngine;
    private readonly IGoNoGoDecisionService _decisionService;
    private readonly IExecutionSimulationService _simulationService;
    private readonly IPreMigrationExportService _exportService;

    public PreMigrationValidationService(IMigrationPlanStorageService planStorage, IPreMigrationStorageService storage, IPreMigrationCheckEngine checkEngine, IGoNoGoDecisionService decisionService, IExecutionSimulationService simulationService, IPreMigrationExportService exportService)
    {
        _planStorage = planStorage;
        _storage = storage;
        _checkEngine = checkEngine;
        _decisionService = decisionService;
        _simulationService = simulationService;
        _exportService = exportService;
    }

    public async Task<PreMigrationValidationResponse?> ValidateAsync(string planId, CancellationToken cancellationToken)
    {
        var plan = await _planStorage.GetAsync(planId, cancellationToken);
        if (plan is null) return null;
        var checks = _checkEngine.RunChecks(plan);
        var waves = BuildWaveResults(plan, checks);
        var decision = _decisionService.Decide(checks, waves);
        var result = new PreMigrationValidationResult
        {
            ValidationId = Guid.NewGuid().ToString("D"),
            PlanId = plan.PlanId,
            GeneratedAt = DateTimeOffset.UtcNow,
            Decision = decision,
            Checks = checks.ToList(),
            WaveResults = waves.ToList(),
            Blockers = checks.Where(c => c.Status == "failed" && c.RequiredForGoLive).Select(c => c.Title).ToList(),
            Warnings = checks.Where(c => c.Status == "warning").Select(c => c.Title).ToList(),
            Recommendations = checks.Where(c => c.Status != "passed").Select(c => c.RecommendedAction).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList()
        };
        result.Summary = Summary(result);
        result.ExportPaths = new Dictionary<string, string> { ["json"] = "validation-result.json", ["csv"] = "validation-checks.csv", ["markdown"] = "go-no-go-report.md" };
        await _storage.SaveValidationAsync(result, cancellationToken);
        return new PreMigrationValidationResponse { ValidationId = result.ValidationId, PlanId = result.PlanId, Decision = result.Decision, Status = result.Status, Summary = result.Summary };
    }

    public Task<PreMigrationValidationResult?> GetValidationAsync(string validationId, CancellationToken cancellationToken) => _storage.GetValidationAsync(validationId, cancellationToken);
    public Task<PreMigrationValidationResult?> GetLatestValidationAsync(CancellationToken cancellationToken) => _storage.GetLatestValidationAsync(cancellationToken);

    public async Task<ExecutionSimulationResponse?> SimulateAsync(string planId, CancellationToken cancellationToken)
    {
        var plan = await _planStorage.GetAsync(planId, cancellationToken);
        if (plan is null) return null;
        var result = _simulationService.Simulate(plan);
        await _storage.SaveSimulationAsync(result, cancellationToken);
        return new ExecutionSimulationResponse
        {
            SimulationId = result.SimulationId,
            PlanId = result.PlanId,
            Status = result.Status,
            EstimatedDuration = FormatDuration(result.EstimatedDurationMinutes),
            EstimatedFiles = result.EstimatedFiles,
            EstimatedStorage = FormatBytes(result.EstimatedStorageBytes),
            SimulatedWaves = result.Waves.Count,
            ExpectedFailures = result.ExpectedIssues.Count(i => i.Severity == "Failure"),
            ExpectedWarnings = result.ExpectedIssues.Count(i => i.Severity != "Failure")
        };
    }

    public Task<ExecutionSimulationResult?> GetSimulationAsync(string simulationId, CancellationToken cancellationToken) => _storage.GetSimulationAsync(simulationId, cancellationToken);
    public Task<ExecutionSimulationResult?> GetLatestSimulationAsync(CancellationToken cancellationToken) => _storage.GetLatestSimulationAsync(cancellationToken);

    public async Task<PreMigrationExportResult?> ExportValidationAsync(string validationId, string exportType, CancellationToken cancellationToken)
    {
        var result = await _storage.GetValidationAsync(validationId, cancellationToken);
        if (result is null) return null;
        return exportType.ToLowerInvariant() switch
        {
            "json" => _exportService.ExportValidationJson(result),
            "markdown" or "md" => _exportService.ExportValidationMarkdown(result),
            _ => _exportService.ExportValidationCsv(result)
        };
    }

    public async Task<PreMigrationExportResult?> ExportSimulationAsync(string simulationId, string exportType, CancellationToken cancellationToken)
    {
        var result = await _storage.GetSimulationAsync(simulationId, cancellationToken);
        if (result is null) return null;
        return exportType.ToLowerInvariant() switch
        {
            "json" => _exportService.ExportSimulationJson(result),
            _ => _exportService.ExportSimulationMarkdown(result)
        };
    }

    private static IReadOnlyCollection<WaveValidationResult> BuildWaveResults(MigrationPlan plan, IReadOnlyCollection<PreMigrationCheck> checks) =>
        plan.Waves.Select(wave =>
        {
            var waveChecks = checks.Where(c => c.AffectedWave == wave.WaveName).ToList();
            var errors = waveChecks.Count(c => c.Status == "failed");
            return new WaveValidationResult
            {
                WaveId = wave.WaveId,
                WaveName = wave.WaveName,
                Status = errors > 0 ? "blocked" : waveChecks.Any(c => c.Status == "warning") ? "warning" : "ready",
                Errors = errors,
                Warnings = waveChecks.Count(c => c.Status == "warning"),
                PassedChecks = waveChecks.Count(c => c.Status == "passed")
            };
        }).ToList();

    private static PreMigrationValidationSummary Summary(PreMigrationValidationResult result) => new()
    {
        Errors = result.Checks.Count(c => c.Status == "failed"),
        Warnings = result.Checks.Count(c => c.Status == "warning"),
        PassedChecks = result.Checks.Count(c => c.Status == "passed"),
        BlockedWaves = result.WaveResults.Count(w => w.Status == "blocked"),
        ReadyWaves = result.WaveResults.Count(w => w.Status == "ready")
    };

    internal static string FormatDuration(int minutes) => $"{minutes / 60}h {minutes % 60}m";
    internal static string FormatBytes(long bytes) => $"{Math.Round(bytes / 1024d / 1024d / 1024d, 2)} GB";
}

public sealed class PreMigrationCheckEngine : IPreMigrationCheckEngine
{
    public IReadOnlyCollection<PreMigrationCheck> RunChecks(MigrationPlan plan)
    {
        var checks = new List<PreMigrationCheck>();
        Add(checks, "plan-waves", "Governance", "Migration plan has waves", plan.Waves.Count > 0, "Add migration waves.", true);
        Add(checks, "source", "Source Access", "Source environment is defined", !string.IsNullOrWhiteSpace(plan.SourceEnvironment), "Define source environment.", true);
        Add(checks, "target", "Target Access", "Target environment is defined", !string.IsNullOrWhiteSpace(plan.TargetEnvironment), "Define target environment.", true);
        Add(checks, "pre-report", "Reports", "Pre-migration report planned", Option(plan, "generatePreMigrationReport"), "Enable pre-migration report generation.", true);
        Add(checks, "validation-plan", "Reports", "Post-migration validation plan defined", Option(plan, "validateAfterMigration"), "Enable post-migration validation.", true);
        Add(checks, "rollback", "Governance", "Rollback notes exist", plan.Checklist.Any(c => c.Id.Contains("rollback") && c.Status == "completed"), "Confirm rollback/restore plan.", true, warningOnly: true);
        foreach (var item in plan.Checklist.Where(c => c.Required))
        {
            Add(checks, $"check-{item.Id}", "Checklist", item.Title, item.Status == "completed", $"Complete checklist item: {item.Title}.", true, warningOnly: item.Category is not "Security" and not "Access");
        }
        foreach (var wave in plan.Waves)
        {
            Add(checks, $"wave-{wave.WaveId}-items", "Governance", $"{wave.WaveName} has included items", wave.IncludedItems.Count > 0, "Add items or remove this wave.", true, wave.WaveName);
            Add(checks, $"wave-{wave.WaveId}-owner", "Governance", $"{wave.WaveName} has owner", !string.IsNullOrWhiteSpace(wave.OwnerRole), "Assign wave owner.", true, wave.WaveName, warningOnly: true);
            if (wave.RiskLevel is "High" or "Critical")
            {
                Add(checks, $"wave-{wave.WaveId}-prereq", "Governance", $"{wave.WaveName} has prerequisites", wave.Prerequisites.Count > 0, "Add remediation prerequisites for high-risk wave.", true, wave.WaveName);
            }
            foreach (var item in wave.IncludedItems)
            {
                if (item.MigrationAction == "manual_review") Add(checks, $"restricted-{item.ItemId}", "Restricted Content", $"Approve restricted content {item.Library}", wave.ApprovalStatus == "approved", "Approve restricted content before execution.", true, wave.WaveName, item.Library);
                if (item.MigrationAction == "remediate_first") Add(checks, $"remediate-{item.ItemId}", "Governance", $"Remediate high-risk item {item.Library}", false, "Complete remediation before execution.", true, wave.WaveName, item.Library);
                if (item.MigrationAction == "archive") Add(checks, $"archive-{item.ItemId}", "Archive Strategy", $"Archive strategy for {item.Library}", plan.Checklist.Any(c => c.Id.Contains("archive") && c.Status == "completed"), "Confirm archive strategy.", true, wave.WaveName, item.Library, warningOnly: true);
                if (item.Path.Length > 250) Add(checks, $"path-{item.ItemId}", "Path Length", $"Long path review for {item.Library}", false, "Resolve or accept long path risk.", true, wave.WaveName, item.Library, warningOnly: item.Path.Length <= 350);
                if (item.StorageBytes > 500L * 1024 * 1024) Add(checks, $"large-{item.ItemId}", "Large Files", $"Large file strategy for {item.Library}", Option(plan, "includeLargeFiles"), "Define large file strategy.", true, wave.WaveName, item.Library, warningOnly: true);
            }
        }
        if (Option(plan, "preservePermissions")) Add(checks, "permissions-review", "Permissions", "Permission mapping reviewed", plan.Checklist.Any(c => c.Id.Contains("broken-permissions") && c.Status == "completed"), "Complete permission mapping review.", true);
        if (Option(plan, "preserveMetadata")) Add(checks, "metadata-review", "Metadata", "Metadata mapping reviewed", plan.Checklist.Any(c => c.Id.Contains("metadata") && c.Status == "completed"), "Complete metadata mapping review.", true, warningOnly: true);
        Add(checks, "connections", "Connections", "Connections verified", false, "Verify source and target connections before execution.", true, warningOnly: true);
        return checks;
    }

    private static bool Option(MigrationPlan plan, string key) => plan.Options.FirstOrDefault(o => o.Key == key)?.Value == true;
    private static void Add(List<PreMigrationCheck> checks, string id, string category, string title, bool passed, string action, bool required, string wave = "", string item = "", bool warningOnly = false)
    {
        checks.Add(new PreMigrationCheck
        {
            CheckId = id,
            Category = category,
            Title = title,
            Description = title,
            Status = passed ? "passed" : warningOnly ? "warning" : "failed",
            Severity = passed ? "Info" : warningOnly ? "Medium" : "High",
            AffectedWave = wave,
            AffectedItem = item,
            Evidence = passed ? "Satisfied by migration plan." : "Not satisfied by current migration plan.",
            RecommendedAction = passed ? "No action required." : action,
            RequiredForGoLive = required
        });
    }
}

public sealed class GoNoGoDecisionService : IGoNoGoDecisionService
{
    public string Decide(IReadOnlyCollection<PreMigrationCheck> checks, IReadOnlyCollection<WaveValidationResult> waveResults)
    {
        if (checks.Any(c => c.Status == "failed" && c.RequiredForGoLive) || waveResults.Any(w => w.Status == "blocked")) return "no_go";
        if (checks.Any(c => c.Status == "warning")) return "conditional_go";
        return "go";
    }
}

public sealed class ExecutionSimulationService : IExecutionSimulationService
{
    private readonly IExecutionEstimator _estimator;
    public ExecutionSimulationService(IExecutionEstimator estimator) => _estimator = estimator;
    public ExecutionSimulationResult Simulate(MigrationPlan plan)
    {
        var waves = plan.Waves.Select(w =>
        {
            var estimate = _estimator.Estimate(w);
            return new ExecutionSimulationWave
            {
                WaveId = w.WaveId,
                WaveName = w.WaveName,
                Order = w.Order,
                ItemCount = w.IncludedItems.Count,
                EstimatedFiles = w.EstimatedFiles,
                EstimatedStorageBytes = w.EstimatedStorage,
                EstimatedDurationMinutes = estimate.DurationMinutes,
                RiskLevel = w.RiskLevel,
                ReadinessScore = w.ReadinessScore,
                ExpectedWarnings = estimate.ExpectedWarnings,
                ExpectedFailures = estimate.ExpectedFailures,
                Steps = Steps(w)
            };
        }).ToList();
        var issues = waves.SelectMany(w => BuildIssues(plan.Waves.First(pw => pw.WaveId == w.WaveId), w)).ToList();
        return new ExecutionSimulationResult
        {
            SimulationId = Guid.NewGuid().ToString("D"),
            PlanId = plan.PlanId,
            GeneratedAt = DateTimeOffset.UtcNow,
            EstimatedDurationMinutes = waves.Sum(w => w.EstimatedDurationMinutes),
            EstimatedFiles = waves.Sum(w => w.EstimatedFiles),
            EstimatedStorageBytes = waves.Sum(w => w.EstimatedStorageBytes),
            Waves = waves,
            ExpectedIssues = issues,
            Checkpoints = ["Pre-wave validation", "Source accessibility check", "Target accessibility check", "Metadata mapping check", "Permission mapping check", "Content copy simulation", "Post-wave validation simulation", "Report generation"],
            Assumptions = ["Simulation only; no files are copied.", "Throughput estimate uses files, storage, risk, and wave overhead."],
            Recommendations = issues.Select(i => i.RecommendedAction).Distinct().ToList()
        };
    }

    private static List<ExecutionSimulationStep> Steps(MigrationPlanWave wave) =>
        new[] { "Pre-wave validation", "Source accessibility check", "Target accessibility check", "Metadata mapping check", "Permission mapping check", "Content copy simulation", "Post-wave validation simulation", "Report generation" }
        .Select((name, index) => new ExecutionSimulationStep { StepId = $"{wave.WaveId}-step-{index + 1}", StepName = name, Order = index + 1, Description = $"{name} for {wave.WaveName}.", EstimatedDurationMinutes = index is 0 or 7 ? 5 : 10, Dependencies = index == 0 ? [] : [$"{wave.WaveId}-step-{index}"] }).ToList();

    private static IEnumerable<ExecutionSimulationIssue> BuildIssues(MigrationPlanWave planWave, ExecutionSimulationWave wave)
    {
        foreach (var item in planWave.IncludedItems)
        {
            if (item.MigrationAction is "manual_review" or "remediate_first") yield return Issue(wave.WaveName, item.Library, "Warning", $"{item.Library} requires review/remediation.", "Complete prerequisite before execution.");
            if (item.PermissionRisk is "High" or "Critical") yield return Issue(wave.WaveName, item.Library, "Warning", $"{item.Library} has permission risk.", "Validate permission mapping.");
        }
        if (wave.RiskLevel is "High" or "Critical") yield return Issue(wave.WaveName, "", "Warning", "High-risk wave may require extra retries.", "Resolve high-risk findings first.");
    }

    private static ExecutionSimulationIssue Issue(string wave, string item, string severity, string description, string action) => new() { IssueId = Guid.NewGuid().ToString("D"), WaveName = wave, Item = item, Severity = severity, Description = description, RecommendedAction = action };
}

public sealed class ExecutionEstimator : IExecutionEstimator
{
    public ExecutionEstimate Estimate(MigrationPlanWave wave)
    {
        var gb = wave.EstimatedStorage / 1024d / 1024d / 1024d;
        var restricted = wave.IncludedItems.Count(i => i.MigrationAction == "manual_review");
        var highRisk = wave.IncludedItems.Count(i => i.MigrationAction is "manual_review" or "remediate_first");
        var metadataHeavy = wave.IncludedItems.Count(i => i.MetadataCount > 5);
        var minutes = (int)Math.Ceiling(wave.EstimatedFiles / 100d + gb + 10 + restricted * 5 + highRisk * 3 + metadataHeavy * 2);
        return new ExecutionEstimate { DurationMinutes = Math.Max(10, minutes), ExpectedWarnings = highRisk + restricted + (wave.RiskLevel is "High" or "Critical" ? 1 : 0), ExpectedFailures = wave.ExcludedItems.Count };
    }
}

public sealed class PreMigrationStorageService : IPreMigrationStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _validationRoot;
    private readonly string _simulationRoot;
    private readonly IPreMigrationExportService _exportService;
    public PreMigrationStorageService(IHostEnvironment env, IPreMigrationExportService exportService)
    {
        _validationRoot = Path.Combine(env.ContentRootPath, "App_Data", "pre-migration");
        _simulationRoot = Path.Combine(env.ContentRootPath, "App_Data", "pre-migration-simulations");
        _exportService = exportService;
    }
    public async Task SaveValidationAsync(PreMigrationValidationResult result, CancellationToken ct)
    {
        var dir = Path.Combine(_validationRoot, result.ValidationId); Directory.CreateDirectory(dir);
        await WriteJsonAsync(Path.Combine(dir, "validation-result.json"), result, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "validation-checks.csv"), _exportService.ExportValidationCsv(result).Content, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "go-no-go-report.md"), _exportService.ExportValidationMarkdown(result).Content, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "blocked-items.csv"), Encoding.UTF8.GetBytes(Csv(result.Checks.Where(c => c.Status == "failed").Select(c => new[] { c.Category, c.Title, c.AffectedWave, c.AffectedItem, c.RecommendedAction }))), ct);
    }
    public Task<PreMigrationValidationResult?> GetValidationAsync(string id, CancellationToken ct) => Guid.TryParse(id, out _) ? ReadJsonAsync<PreMigrationValidationResult>(Path.Combine(_validationRoot, id, "validation-result.json"), ct) : Task.FromResult<PreMigrationValidationResult?>(null);
    public async Task<PreMigrationValidationResult?> GetLatestValidationAsync(CancellationToken ct) => await LatestAsync<PreMigrationValidationResult>(_validationRoot, "validation-result.json", r => r.GeneratedAt, ct);
    public async Task SaveSimulationAsync(ExecutionSimulationResult result, CancellationToken ct)
    {
        var dir = Path.Combine(_simulationRoot, result.SimulationId); Directory.CreateDirectory(dir);
        await WriteJsonAsync(Path.Combine(dir, "simulation-result.json"), result, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "simulation-report.md"), _exportService.ExportSimulationMarkdown(result).Content, ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "wave-simulation.csv"), Encoding.UTF8.GetBytes(Csv(result.Waves.Select(w => new[] { w.WaveName, w.RiskLevel, w.EstimatedFiles.ToString(), w.EstimatedStorageBytes.ToString(), w.EstimatedDurationMinutes.ToString(), w.ExpectedWarnings.ToString(), w.ExpectedFailures.ToString() }))), ct);
        await File.WriteAllBytesAsync(Path.Combine(dir, "expected-issues.csv"), Encoding.UTF8.GetBytes(Csv(result.ExpectedIssues.Select(i => new[] { i.Severity, i.WaveName, i.Item, i.Description, i.RecommendedAction }))), ct);
    }
    public Task<ExecutionSimulationResult?> GetSimulationAsync(string id, CancellationToken ct) => Guid.TryParse(id, out _) ? ReadJsonAsync<ExecutionSimulationResult>(Path.Combine(_simulationRoot, id, "simulation-result.json"), ct) : Task.FromResult<ExecutionSimulationResult?>(null);
    public async Task<ExecutionSimulationResult?> GetLatestSimulationAsync(CancellationToken ct) => await LatestAsync<ExecutionSimulationResult>(_simulationRoot, "simulation-result.json", r => r.GeneratedAt, ct);
    private static async Task<T?> LatestAsync<T>(string root, string file, Func<T, DateTimeOffset> date, CancellationToken ct) { if (!Directory.Exists(root)) return default; var items = new List<T>(); foreach (var path in Directory.EnumerateFiles(root, file, SearchOption.AllDirectories)) { var item = await ReadJsonAsync<T>(path, ct); if (item is not null) items.Add(item); } return items.OrderByDescending(date).FirstOrDefault(); }
    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken ct) { await using var stream = File.Create(path); await JsonSerializer.SerializeAsync(stream, value, JsonOptions, ct); }
    private static async Task<T?> ReadJsonAsync<T>(string path, CancellationToken ct) { if (!File.Exists(path)) return default; await using var stream = File.OpenRead(path); return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, ct); }
    internal static string Csv(IEnumerable<string[]> rows) { static string Escape(string value) => $"\"{(value ?? "").Replace("\"", "\"\"")}\""; return string.Join(Environment.NewLine, rows.Select(r => string.Join(",", r.Select(Escape)))); }
}

public sealed class PreMigrationExportService : IPreMigrationExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public PreMigrationExportResult ExportValidationJson(PreMigrationValidationResult result) => new() { FileName = $"pre-migration-validation-{result.ValidationId}.json", ContentType = "application/json", Content = JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions) };
    public PreMigrationExportResult ExportValidationCsv(PreMigrationValidationResult result) => new() { FileName = $"pre-migration-checks-{result.ValidationId}.csv", ContentType = "text/csv", Content = Encoding.UTF8.GetBytes(PreMigrationStorageService.Csv(result.Checks.Select(c => new[] { c.Category, c.Title, c.Status, c.Severity, c.AffectedWave, c.AffectedItem, c.RecommendedAction }))) };
    public PreMigrationExportResult ExportValidationMarkdown(PreMigrationValidationResult result)
    {
        var md = new StringBuilder("# Go/No-Go Validation Report\n\n").AppendLine($"Decision: **{result.Decision}**").AppendLine($"Errors: {result.Summary.Errors}").AppendLine($"Warnings: {result.Summary.Warnings}").AppendLine("\n## Recommendations");
        foreach (var rec in result.Recommendations) md.AppendLine($"- {rec}");
        return new() { FileName = $"go-no-go-report-{result.ValidationId}.md", ContentType = "text/markdown", Content = Encoding.UTF8.GetBytes(md.ToString()) };
    }
    public PreMigrationExportResult ExportSimulationJson(ExecutionSimulationResult result) => new() { FileName = $"execution-simulation-{result.SimulationId}.json", ContentType = "application/json", Content = JsonSerializer.SerializeToUtf8Bytes(result, JsonOptions) };
    public PreMigrationExportResult ExportSimulationMarkdown(ExecutionSimulationResult result)
    {
        var md = new StringBuilder("# Execution Simulation Report\n\n").AppendLine("> Simulation only. No migration has run.").AppendLine($"Estimated duration: {PreMigrationValidationService.FormatDuration(result.EstimatedDurationMinutes)}").AppendLine($"Files: {result.EstimatedFiles}").AppendLine("\n## Waves");
        foreach (var wave in result.Waves) md.AppendLine($"- {wave.WaveName}: {wave.EstimatedDurationMinutes} min, warnings {wave.ExpectedWarnings}, failures {wave.ExpectedFailures}");
        return new() { FileName = $"execution-simulation-{result.SimulationId}.md", ContentType = "text/markdown", Content = Encoding.UTF8.GetBytes(md.ToString()) };
    }
}
