using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ZMS.Application.Contracts;

namespace ZMS.Application.Services;

public sealed class MigrationExecutionService : IMigrationExecutionService
{
    private readonly IMigrationPlanStorageService _planStorage;
    private readonly IMigrationExecutionStorageService _storage;
    private readonly IMigrationExecutionJobFactory _factory;
    private readonly IMigrationExecutionOrchestrator _orchestrator;
    private readonly IMigrationExecutionReportService _reportService;

    public MigrationExecutionService(IMigrationPlanStorageService planStorage, IMigrationExecutionStorageService storage, IMigrationExecutionJobFactory factory, IMigrationExecutionOrchestrator orchestrator, IMigrationExecutionReportService reportService)
    {
        _planStorage = planStorage;
        _storage = storage;
        _factory = factory;
        _orchestrator = orchestrator;
        _reportService = reportService;
    }

    public async Task<CreateMigrationExecutionJobResponse?> CreateFromPlanAsync(string planId, MigrationExecutionRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planStorage.GetAsync(planId, cancellationToken);
        if (plan is null) return null;

        var job = await _factory.CreateAsync(plan, request, cancellationToken);
        if (job is null) return null;

        await _storage.SaveAsync(job, cancellationToken);
        return new CreateMigrationExecutionJobResponse
        {
            JobId = job.JobId,
            PlanId = job.PlanId,
            Status = job.Status,
            Mode = job.Mode,
            Message = job.Status == "blocked"
                ? "Migration execution job blocked by latest Go/No-Go decision. Simulation mode only."
                : "Migration execution job created in simulation mode"
        };
    }

    public Task<MigrationExecutionJob?> GetAsync(string jobId, CancellationToken cancellationToken) => _storage.GetAsync(jobId, cancellationToken);
    public Task<MigrationExecutionJob?> GetLatestAsync(CancellationToken cancellationToken) => _storage.GetLatestAsync(cancellationToken);
    public Task<IReadOnlyCollection<MigrationExecutionJob>> GetAllAsync(CancellationToken cancellationToken) => _storage.GetAllAsync(cancellationToken);

    public async Task<MigrationExecutionJob?> StartAsync(string jobId, CancellationToken cancellationToken) => await MutateAsync(jobId, _orchestrator.Start, cancellationToken);
    public async Task<MigrationExecutionJob?> PauseAsync(string jobId, CancellationToken cancellationToken) => await MutateAsync(jobId, _orchestrator.Pause, cancellationToken);
    public async Task<MigrationExecutionJob?> ResumeAsync(string jobId, CancellationToken cancellationToken) => await MutateAsync(jobId, _orchestrator.Resume, cancellationToken);
    public async Task<MigrationExecutionJob?> CancelAsync(string jobId, CancellationToken cancellationToken) => await MutateAsync(jobId, _orchestrator.Cancel, cancellationToken);
    public async Task<MigrationExecutionJob?> RetryFailedAsync(string jobId, CancellationToken cancellationToken) => await MutateAsync(jobId, _orchestrator.RetryFailed, cancellationToken);

    public async Task<IReadOnlyCollection<MigrationExecutionTimelineEvent>?> GetTimelineAsync(string jobId, CancellationToken cancellationToken)
    {
        var job = await _storage.GetAsync(jobId, cancellationToken);
        return job?.Timeline.OrderBy(e => e.CreatedAt).ToList();
    }

    public async Task<MigrationExecutionExportResult?> ExportAsync(string jobId, string exportType, CancellationToken cancellationToken)
    {
        var job = await _storage.GetAsync(jobId, cancellationToken);
        if (job is null) return null;
        return exportType.ToLowerInvariant() switch
        {
            "json" => _reportService.ExportJson(job),
            "markdown" or "md" => _reportService.ExportMarkdown(job),
            _ => _reportService.ExportCsv(job)
        };
    }

    private async Task<MigrationExecutionJob?> MutateAsync(string jobId, Func<MigrationExecutionJob, MigrationExecutionJob> mutate, CancellationToken cancellationToken)
    {
        var job = await _storage.GetAsync(jobId, cancellationToken);
        if (job is null) return null;
        var updated = mutate(job);
        await _storage.SaveAsync(updated, cancellationToken);
        return updated;
    }
}

public sealed class MigrationExecutionJobFactory : IMigrationExecutionJobFactory
{
    private readonly IPreMigrationStorageService _preMigrationStorage;
    private readonly IMigrationExecutionTimelineService _timeline;

    public MigrationExecutionJobFactory(IPreMigrationStorageService preMigrationStorage, IMigrationExecutionTimelineService timeline)
    {
        _preMigrationStorage = preMigrationStorage;
        _timeline = timeline;
    }

    public async Task<MigrationExecutionJob?> CreateAsync(MigrationPlan plan, MigrationExecutionRequest request, CancellationToken cancellationToken)
    {
        if (!request.Mode.Equals("simulation", StringComparison.OrdinalIgnoreCase))
        {
            return BlockedJob(plan, request, "Only simulation mode is executable in this phase.");
        }

        var latestValidation = await _preMigrationStorage.GetLatestValidationAsync(cancellationToken);
        var latestSimulation = await _preMigrationStorage.GetLatestSimulationAsync(cancellationToken);
        if (request.RequireGoDecision && latestValidation?.PlanId == plan.PlanId && latestValidation.Decision == "no_go")
        {
            return BlockedJob(plan, request, "Latest Go/No-Go decision is no_go. Resolve blockers before execution simulation.");
        }

        var selectedWaveIds = request.SelectedWaveIds.Count == 0 ? null : request.SelectedWaveIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var waves = plan.Waves
            .Where(w => selectedWaveIds is null || selectedWaveIds.Contains(w.WaveId))
            .OrderBy(w => w.Order)
            .Select(ToExecutionWave)
            .ToList();

        var job = new MigrationExecutionJob
        {
            JobId = Guid.NewGuid().ToString("D"),
            PlanId = plan.PlanId,
            ValidationId = latestValidation?.PlanId == plan.PlanId ? latestValidation.ValidationId : string.Empty,
            SimulationId = latestSimulation?.PlanId == plan.PlanId ? latestSimulation.SimulationId : string.Empty,
            Mode = "simulation",
            Status = "created",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "Migration Lead" : request.CreatedBy,
            Waves = waves,
            Checkpoints = JobCheckpoints(),
            Warnings = latestValidation?.Decision == "conditional_go" ? ["Latest Go/No-Go decision is conditional_go. Simulation can run with warnings."] : [],
            ReportPaths = new Dictionary<string, string>
            {
                ["json"] = "execution-job.json",
                ["timeline"] = "execution-timeline.json",
                ["markdown"] = "execution-report.md",
                ["items"] = "execution-items.csv",
                ["errors"] = "execution-errors.csv",
                ["waves"] = "wave-summary.csv"
            }
        };
        UpdateSummary(job);
        _timeline.Add(job, "JobCreated", "Simulation Mode - No tenant changes performed. Job created.");
        return job;
    }

    private MigrationExecutionJob BlockedJob(MigrationPlan plan, MigrationExecutionRequest request, string message)
    {
        var job = new MigrationExecutionJob
        {
            JobId = Guid.NewGuid().ToString("D"),
            PlanId = plan.PlanId,
            Mode = request.Mode,
            Status = "blocked",
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "Migration Lead" : request.CreatedBy,
            Warnings = ["Simulation Mode - No tenant changes performed."],
            Errors = [new MigrationExecutionError { ErrorId = Guid.NewGuid().ToString("D"), CreatedAt = DateTimeOffset.UtcNow, Severity = "High", Message = message, RecommendedAction = "Run pre-migration validation and resolve blockers." }]
        };
        _timeline.Add(job, "JobBlocked", message, "High");
        UpdateSummary(job);
        return job;
    }

    private static MigrationExecutionWave ToExecutionWave(MigrationPlanWave wave)
    {
        var items = wave.IncludedItems
            .Where(i => i.IncludeInMigration)
            .Select(item => new MigrationExecutionItem
            {
                ItemExecutionId = Guid.NewGuid().ToString("D"),
                SourceItemId = item.ItemId,
                SiteCollection = item.SiteCollection,
                Library = item.Library,
                Path = item.Path,
                ItemType = item.ItemType,
                Action = item.MigrationAction,
                Status = item.MigrationAction is "manual_review" or "remediate_first" ? "retry_pending" : "pending",
                SimulatedSourceUrl = item.SourceUrl,
                SimulatedTargetUrl = item.TargetUrl,
                Warnings = item.MigrationAction is "manual_review" or "remediate_first" ? [$"{item.MigrationAction} must be resolved before future live execution."] : []
            })
            .ToList();

        return new MigrationExecutionWave
        {
            WaveExecutionId = Guid.NewGuid().ToString("D"),
            SourceWaveId = wave.WaveId,
            WaveName = wave.WaveName,
            Order = wave.Order,
            Status = items.Any(i => i.Status == "retry_pending") ? "blocked" : "created",
            TotalItems = items.Count,
            EstimatedFiles = wave.EstimatedFiles,
            EstimatedStorageBytes = wave.EstimatedStorage,
            Items = items,
            Checkpoints = WaveCheckpoints(wave.WaveName)
        };
    }

    internal static List<MigrationExecutionCheckpoint> JobCheckpoints() =>
    [
        Checkpoint("Plan loaded"),
        Checkpoint("Go/No-Go validation checked"),
        Checkpoint("Simulation mode confirmed"),
        Checkpoint("Waves generated"),
        Checkpoint("Execution report initialized")
    ];

    internal static List<MigrationExecutionCheckpoint> WaveCheckpoints(string waveName) =>
    [
        Checkpoint("Pre-wave validation", waveName),
        Checkpoint("Source accessibility simulation", waveName),
        Checkpoint("Target accessibility simulation", waveName),
        Checkpoint("Metadata mapping simulation", waveName),
        Checkpoint("Permission mapping simulation", waveName),
        Checkpoint("Content transfer simulation", waveName),
        Checkpoint("Post-wave validation simulation", waveName),
        Checkpoint("Wave report generated", waveName)
    ];

    private static MigrationExecutionCheckpoint Checkpoint(string name, string scope = "") => new()
    {
        CheckpointId = Guid.NewGuid().ToString("D"),
        Name = name,
        Status = "pending",
        Message = string.IsNullOrWhiteSpace(scope) ? name : $"{name} for {scope}."
    };

    internal static void UpdateSummary(MigrationExecutionJob job)
    {
        var items = job.Waves.SelectMany(w => w.Items).ToList();
        var totalItems = items.Count;
        job.Summary = new MigrationExecutionSummary
        {
            TotalWaves = job.Waves.Count,
            CompletedWaves = job.Waves.Count(w => w.Status is "completed" or "completed_with_warnings"),
            TotalItems = totalItems,
            CompletedItems = items.Count(i => i.Status == "completed"),
            FailedItems = items.Count(i => i.Status == "failed"),
            SkippedItems = items.Count(i => i.Status == "skipped"),
            WarningCount = job.Warnings.Count + job.Waves.SelectMany(w => w.Items).Sum(i => i.Warnings.Count),
            ErrorCount = job.Errors.Count + job.Waves.Sum(w => w.Errors.Count) + items.Sum(i => i.Errors.Count),
            ProgressPercent = totalItems == 0 ? 0 : (int)Math.Round(items.Count(i => i.Status is "completed" or "failed" or "skipped") * 100d / totalItems)
        };
    }
}

public sealed class MigrationExecutionOrchestrator : IMigrationExecutionOrchestrator
{
    private readonly IMigrationExecutionAdapter _adapter;
    private readonly IMigrationExecutionTimelineService _timeline;
    private readonly IMigrationExecutionReportService _reportService;

    public MigrationExecutionOrchestrator(IMigrationExecutionAdapter adapter, IMigrationExecutionTimelineService timeline, IMigrationExecutionReportService reportService)
    {
        _adapter = adapter;
        _timeline = timeline;
        _reportService = reportService;
    }

    public MigrationExecutionJob Start(MigrationExecutionJob job)
    {
        if (job.Status is "cancelled" or "completed" or "completed_with_warnings" or "failed" or "blocked") return job;
        job.Status = "running";
        job.StartedAt ??= DateTimeOffset.UtcNow;
        _timeline.Add(job, "JobStarted", "Simulated migration execution started. Simulation Mode - No tenant changes performed.");
        PassCheckpoints(job.Checkpoints, "Job checkpoint passed.", job);

        foreach (var wave in job.Waves.OrderBy(w => w.Order))
        {
            if (job.Status is "paused" or "cancelled") break;
            ProcessWave(job, wave);
        }

        CompleteJob(job);
        return job;
    }

    public MigrationExecutionJob Pause(MigrationExecutionJob job)
    {
        if (job.Status == "running")
        {
            job.Status = "paused";
            _timeline.Add(job, "JobPaused", "Simulated execution paused.");
        }
        return job;
    }

    public MigrationExecutionJob Resume(MigrationExecutionJob job)
    {
        if (job.Status == "paused")
        {
            job.Status = "running";
            _timeline.Add(job, "JobResumed", "Simulated execution resumed.");
            return Start(job);
        }
        return job;
    }

    public MigrationExecutionJob Cancel(MigrationExecutionJob job)
    {
        if (job.Status is "running" or "paused" or "created" or "queued")
        {
            job.Status = "cancelled";
            job.CompletedAt = DateTimeOffset.UtcNow;
            _timeline.Add(job, "JobCancelled", "Simulated execution cancelled.", "Warning");
            MigrationExecutionJobFactory.UpdateSummary(job);
        }
        return job;
    }

    public MigrationExecutionJob RetryFailed(MigrationExecutionJob job)
    {
        var failedItems = job.Waves.SelectMany(w => w.Items.Select(i => new { Wave = w, Item = i })).Where(x => x.Item.Status == "failed").ToList();
        foreach (var entry in failedItems)
        {
            entry.Item.Status = "retry_pending";
            entry.Item.Errors.Clear();
            _timeline.Add(job, "ItemRetryPending", $"Retry queued for {entry.Item.Library}.", "Warning", entry.Wave.WaveExecutionId, entry.Item.ItemExecutionId);
            var processed = _adapter.ProcessItem(entry.Item, entry.Wave);
            if (processed.Status == "failed")
            {
                processed.Status = "skipped";
                processed.Warnings.Add("Retry remained risky in simulation and was skipped for future remediation.");
            }
            _timeline.Add(job, "ItemRetried", $"Retry simulation completed for {processed.Library} with status {processed.Status}.", processed.Status == "completed" ? "Info" : "Warning", entry.Wave.WaveExecutionId, processed.ItemExecutionId);
        }
        CompleteJob(job);
        return job;
    }

    private void ProcessWave(MigrationExecutionJob job, MigrationExecutionWave wave)
    {
        if (wave.Status is "completed" or "completed_with_warnings") return;
        wave.Status = "running";
        wave.StartedAt ??= DateTimeOffset.UtcNow;
        _timeline.Add(job, "WaveStarted", $"{wave.WaveName} simulation started.", "Info", wave.WaveExecutionId);
        PassCheckpoints(wave.Checkpoints, "Wave checkpoint passed.", job, wave.WaveExecutionId);

        foreach (var item in wave.Items)
        {
            if (job.Status is "paused" or "cancelled") return;
            if (item.Status == "completed") continue;
            _timeline.Add(job, "ItemStarted", $"{item.Library} simulation started.", "Info", wave.WaveExecutionId, item.ItemExecutionId);
            _adapter.ProcessItem(item, wave);
            _timeline.Add(job, item.Status == "failed" ? "ItemFailed" : item.Status == "skipped" ? "ItemSkipped" : "ItemCompleted", $"{item.Library} simulation {item.Status}.", item.Status == "completed" ? "Info" : "Warning", wave.WaveExecutionId, item.ItemExecutionId);
        }

        wave.CompletedItems = wave.Items.Count(i => i.Status == "completed");
        wave.FailedItems = wave.Items.Count(i => i.Status == "failed");
        wave.SkippedItems = wave.Items.Count(i => i.Status == "skipped");
        wave.ProgressPercent = wave.TotalItems == 0 ? 100 : (int)Math.Round(wave.Items.Count(i => i.Status is "completed" or "failed" or "skipped") * 100d / wave.TotalItems);
        wave.Status = wave.FailedItems > 0 ? "failed" : wave.SkippedItems > 0 || wave.Items.Any(i => i.Warnings.Count > 0) ? "completed_with_warnings" : "completed";
        wave.CompletedAt = DateTimeOffset.UtcNow;
        _timeline.Add(job, "WaveCompleted", $"{wave.WaveName} simulation finished with status {wave.Status}.", wave.Status == "completed" ? "Info" : "Warning", wave.WaveExecutionId);
    }

    private void CompleteJob(MigrationExecutionJob job)
    {
        if (job.Status is "paused" or "cancelled") return;
        foreach (var wave in job.Waves)
        {
            wave.CompletedItems = wave.Items.Count(i => i.Status == "completed");
            wave.FailedItems = wave.Items.Count(i => i.Status == "failed");
            wave.SkippedItems = wave.Items.Count(i => i.Status == "skipped");
            wave.ProgressPercent = wave.TotalItems == 0 ? 100 : (int)Math.Round(wave.Items.Count(i => i.Status is "completed" or "failed" or "skipped") * 100d / wave.TotalItems);
        }
        job.CompletedAt = DateTimeOffset.UtcNow;
        MigrationExecutionJobFactory.UpdateSummary(job);
        job.Status = job.Summary.FailedItems > 0 ? "failed" : job.Summary.WarningCount > 0 || job.Summary.SkippedItems > 0 ? "completed_with_warnings" : "completed";
        _timeline.Add(job, "JobCompleted", $"Simulated execution completed with status {job.Status}.", job.Status == "completed" ? "Info" : "Warning");
        _ = _reportService.BuildMarkdown(job);
    }

    private void PassCheckpoints(List<MigrationExecutionCheckpoint> checkpoints, string message, MigrationExecutionJob job, string waveExecutionId = "")
    {
        foreach (var checkpoint in checkpoints)
        {
            checkpoint.Status = "passed";
            checkpoint.StartedAt ??= DateTimeOffset.UtcNow;
            checkpoint.CompletedAt = DateTimeOffset.UtcNow;
            checkpoint.Message = message;
            _timeline.Add(job, "CheckpointPassed", checkpoint.Name, "Info", waveExecutionId);
        }
    }
}

public sealed class MigrationSimulationAdapter : IMigrationExecutionAdapter
{
    public MigrationExecutionItem ProcessItem(MigrationExecutionItem item, MigrationExecutionWave wave)
    {
        item.StartedAt ??= DateTimeOffset.UtcNow;
        item.ProgressPercent = 100;
        if (item.Action is "manual_review")
        {
            item.Status = "skipped";
            item.Warnings.Add("Manual review item skipped in simulation.");
        }
        else if (item.Action is "remediate_first")
        {
            item.Status = "failed";
            item.Errors.Add("Remediation prerequisite is unresolved.");
        }
        else if (item.Action is "archive")
        {
            item.Status = "skipped";
            item.Warnings.Add("Archive item skipped until archive strategy is confirmed.");
        }
        else if (wave.WaveName.Contains("Restricted", StringComparison.OrdinalIgnoreCase) || item.Library.Contains("Payroll", StringComparison.OrdinalIgnoreCase))
        {
            item.Status = "completed";
            item.Warnings.Add("Restricted-content permission review should be repeated before live execution.");
        }
        else
        {
            item.Status = "completed";
        }
        item.CompletedAt = DateTimeOffset.UtcNow;
        return item;
    }
}

public sealed class MigrationExecutionTimelineService : IMigrationExecutionTimelineService
{
    public void Add(MigrationExecutionJob job, string eventType, string message, string severity = "Info", string waveExecutionId = "", string itemExecutionId = "")
    {
        job.Timeline.Add(new MigrationExecutionTimelineEvent
        {
            EventId = Guid.NewGuid().ToString("D"),
            CreatedAt = DateTimeOffset.UtcNow,
            EventType = eventType,
            Message = message,
            Severity = severity,
            WaveExecutionId = waveExecutionId,
            ItemExecutionId = itemExecutionId
        });
    }
}

public sealed class MigrationExecutionStorageService : IMigrationExecutionStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly string _root;
    private readonly IMigrationExecutionReportService _reportService;

    public MigrationExecutionStorageService(IHostEnvironment env, IMigrationExecutionReportService reportService)
    {
        _root = Path.Combine(env.ContentRootPath, "App_Data", "migration-executions");
        _reportService = reportService;
    }

    public async Task SaveAsync(MigrationExecutionJob job, CancellationToken cancellationToken)
    {
        var dir = Path.Combine(_root, job.JobId);
        Directory.CreateDirectory(dir);
        await WriteJsonAsync(Path.Combine(dir, "execution-job.json"), job, cancellationToken);
        await WriteJsonAsync(Path.Combine(dir, "execution-timeline.json"), job.Timeline, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(dir, "execution-report.md"), _reportService.ExportMarkdown(job).Content, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(dir, "execution-items.csv"), _reportService.ExportCsv(job).Content, cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(dir, "execution-errors.csv"), Encoding.UTF8.GetBytes(Csv(job.Errors.Select(e => new[] { e.Severity, e.Message, e.WaveExecutionId, e.ItemExecutionId, e.RecommendedAction }))), cancellationToken);
        await File.WriteAllBytesAsync(Path.Combine(dir, "wave-summary.csv"), Encoding.UTF8.GetBytes(Csv(job.Waves.Select(w => new[] { w.WaveName, w.Status, w.ProgressPercent.ToString(), w.TotalItems.ToString(), w.CompletedItems.ToString(), w.FailedItems.ToString(), w.SkippedItems.ToString() }))), cancellationToken);
    }

    public Task<MigrationExecutionJob?> GetAsync(string jobId, CancellationToken cancellationToken) =>
        Guid.TryParse(jobId, out _) ? ReadJsonAsync<MigrationExecutionJob>(Path.Combine(_root, jobId, "execution-job.json"), cancellationToken) : Task.FromResult<MigrationExecutionJob?>(null);

    public async Task<MigrationExecutionJob?> GetLatestAsync(CancellationToken cancellationToken)
    {
        var all = await GetAllAsync(cancellationToken);
        return all.OrderByDescending(j => j.CreatedAt).FirstOrDefault();
    }

    public async Task<IReadOnlyCollection<MigrationExecutionJob>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_root)) return [];
        var jobs = new List<MigrationExecutionJob>();
        foreach (var path in Directory.EnumerateFiles(_root, "execution-job.json", SearchOption.AllDirectories))
        {
            var job = await ReadJsonAsync<MigrationExecutionJob>(path, cancellationToken);
            if (job is not null) jobs.Add(job);
        }
        return jobs.OrderByDescending(j => j.CreatedAt).ToList();
    }

    internal static string Csv(IEnumerable<string[]> rows)
    {
        static string Escape(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        return string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(Escape))));
    }

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
}

public sealed class MigrationExecutionReportService : IMigrationExecutionReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public MigrationExecutionExportResult ExportJson(MigrationExecutionJob job) => new() { FileName = $"migration-execution-{job.JobId}.json", ContentType = "application/json", Content = JsonSerializer.SerializeToUtf8Bytes(job, JsonOptions) };
    public MigrationExecutionExportResult ExportCsv(MigrationExecutionJob job) => new()
    {
        FileName = $"migration-execution-items-{job.JobId}.csv",
        ContentType = "text/csv",
        Content = Encoding.UTF8.GetBytes(MigrationExecutionStorageService.Csv(job.Waves.SelectMany(w => w.Items.Select(i => new[] { w.WaveName, i.Library, i.Path, i.Action, i.Status, i.ProgressPercent.ToString(), string.Join("; ", i.Warnings), string.Join("; ", i.Errors) }))))
    };
    public MigrationExecutionExportResult ExportMarkdown(MigrationExecutionJob job) => new() { FileName = $"migration-execution-report-{job.JobId}.md", ContentType = "text/markdown", Content = Encoding.UTF8.GetBytes(BuildMarkdown(job)) };

    public string BuildMarkdown(MigrationExecutionJob job)
    {
        var md = new StringBuilder("# Migration Execution Simulation Report\n\n")
            .AppendLine("> Simulation Mode - No tenant changes performed.")
            .AppendLine($"Job: `{job.JobId}`")
            .AppendLine($"Plan: `{job.PlanId}`")
            .AppendLine($"Status: **{job.Status}**")
            .AppendLine($"Progress: {job.Summary.ProgressPercent}%")
            .AppendLine($"Completed items: {job.Summary.CompletedItems}/{job.Summary.TotalItems}")
            .AppendLine($"Failed items: {job.Summary.FailedItems}")
            .AppendLine($"Skipped items: {job.Summary.SkippedItems}")
            .AppendLine("\n## Waves");
        foreach (var wave in job.Waves.OrderBy(w => w.Order))
        {
            md.AppendLine($"- {wave.WaveName}: {wave.Status}, {wave.ProgressPercent}% complete, completed {wave.CompletedItems}, failed {wave.FailedItems}, skipped {wave.SkippedItems}");
        }
        md.AppendLine("\n## Timeline");
        foreach (var entry in job.Timeline.OrderBy(e => e.CreatedAt).TakeLast(50))
        {
            md.AppendLine($"- {entry.CreatedAt:u} [{entry.Severity}] {entry.EventType}: {entry.Message}");
        }
        return md.ToString();
    }
}
