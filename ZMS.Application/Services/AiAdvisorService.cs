using Microsoft.Extensions.Logging;
using ZMS.Application.Contracts;
using ZMS.Application.Discovery;
using ZMS.Core.Enums;
using ZMS.Core.Security;

namespace ZMS.Application.Services;

public class AiAdvisorService : IAiAdvisorService
{
    private const string SystemPrompt = """
        You are the zettalogixmigrationsuite local migration advisor.
        Answer only from the provided platform context.
        Do not ask for, expose, or infer secrets, tokens, client secrets, refresh tokens, passwords, or private keys.
        If the context is insufficient, say what discovery, migration, or validation data is required.
        Keep recommendations specific, operational, and safe for Microsoft 365 migrations.
        """;

    private readonly IDiscoveryService _discoveryService;
    private readonly IMigrationService _migrationService;
    private readonly IValidationService _validationService;
    private readonly IOllamaClient _ollamaClient;
    private readonly ILogger<AiAdvisorService> _logger;

    public AiAdvisorService(
        IDiscoveryService discoveryService,
        IMigrationService migrationService,
        IValidationService validationService,
        IOllamaClient ollamaClient,
        ILogger<AiAdvisorService> logger)
    {
        _discoveryService = discoveryService;
        _migrationService = migrationService;
        _validationService = validationService;
        _ollamaClient = ollamaClient;
        _logger = logger;
    }

    public async Task<AiAdvisorResponse> AskAsync(AiAdvisorRequest request, string userId, CancellationToken cancellationToken)
    {
        var context = await BuildContextAsync(request, userId, cancellationToken);
        if (!context.HasAnyContext)
        {
            return new AiAdvisorResponse
            {
                Answer = "Discovery, migration, or validation data is required before the advisor can answer this question.",
                UsedOllama = false,
                Model = _ollamaClient.Model,
                Warning = "No platform context was available.",
                ContextSummary = context.PublicSummary
            };
        }

        var fallback = BuildFallbackAnswer(request.Question, context);
        var ollama = await _ollamaClient.GenerateAsync(SystemPrompt, SecretRedactor.Redact(request.Question), context.PublicSummary, cancellationToken);
        if (!ollama.IsAvailable)
        {
            _logger.LogWarning("AI advisor returned deterministic fallback. Reason: {Warning}", SecretRedactor.Redact(ollama.Warning));
        }
        return new AiAdvisorResponse
        {
            Answer = ollama.IsAvailable ? ollama.Answer ?? fallback : fallback,
            UsedOllama = ollama.IsAvailable,
            Model = _ollamaClient.Model,
            Warning = ollama.Warning,
            ContextSummary = context.PublicSummary
        };
    }

    public async Task<IReadOnlyCollection<RemediationItem>> GetDiscoveryRemediationAsync(string discoveryRunId, CancellationToken cancellationToken)
    {
        var result = await ResolveDiscoveryAsync(discoveryRunId, cancellationToken);
        return result?.MigrationRisks.Select(ToRemediation).ToArray() ?? [];
    }

    public async Task<IReadOnlyCollection<RemediationItem>> GetMigrationRemediationAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await _migrationService.GetJobAsync(jobId, userId, cancellationToken);
        if (job is null)
        {
            return [];
        }

        var timeline = await _migrationService.GetTimelineAsync(jobId, userId, cancellationToken);
        return timeline
            .Where(item => item.Severity is EnterpriseSeverity.Warning or EnterpriseSeverity.High or EnterpriseSeverity.Critical or EnterpriseSeverity.Error)
            .Select(item => new RemediationItem
            {
                Issue = item.EventType,
                Impact = item.Message,
                RecommendedFix = "Review the job timeline, correct the source or target condition, then retry the job or affected item.",
                Priority = item.Severity.ToString(),
                AutomationEligible = item.EventType.Contains("Retry", StringComparison.OrdinalIgnoreCase),
                Confidence = 0.78,
                SourceFindingId = item.Id.ToString("N")
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<RemediationItem>> GetValidationRemediationAsync(Guid validationRunId, CancellationToken cancellationToken)
    {
        var findings = await _validationService.GetFindingsAsync(validationRunId, cancellationToken);
        return findings.Select(finding => new RemediationItem
        {
            Issue = finding.Category,
            Impact = finding.Message,
            RecommendedFix = finding.RecommendedAction,
            Priority = finding.Severity.ToString(),
            AutomationEligible = finding.Category.Contains("MissingTarget", StringComparison.OrdinalIgnoreCase)
                || finding.Category.Contains("FailedItem", StringComparison.OrdinalIgnoreCase),
            Confidence = 0.82,
            SourceFindingId = finding.Id.ToString("N")
        }).ToArray();
    }

    public async Task<EtaEstimate> GetMigrationEtaAsync(Guid jobId, string userId, CancellationToken cancellationToken)
    {
        var job = await _migrationService.GetJobAsync(jobId, userId, cancellationToken)
            ?? throw new KeyNotFoundException($"Migration job '{jobId}' was not found.");
        var items = await _migrationService.GetJobItemsAsync(jobId, userId, cancellationToken);
        return Estimate(items.Count, items.Sum(item => item.FileSizeInBytes), job.FailedItems, 0, items.Count(item => item.FileSizeInBytes > 100L * 1024L * 1024L), job.BatchSize);
    }

    public async Task<EtaEstimate> GetDiscoveryEtaAsync(string discoveryRunId, CancellationToken cancellationToken)
    {
        var result = await ResolveDiscoveryAsync(discoveryRunId, cancellationToken);
        return result is null
            ? Estimate(0, 0, 0, 0, 0, 4)
            : Estimate(result.Summary.Files, result.Summary.TotalStorageBytes, result.MigrationRisks.Count(item => item.RiskLevel == "High"), 0, result.Summary.LargeFileRisks, 4);
    }

    private async Task<AdvisorContext> BuildContextAsync(AiAdvisorRequest request, string userId, CancellationToken cancellationToken)
    {
        var discovery = await ResolveDiscoveryAsync(request.DiscoveryRunId, cancellationToken);
        var job = request.MigrationJobId.HasValue
            ? await _migrationService.GetJobAsync(request.MigrationJobId.Value, userId, cancellationToken)
            : null;
        var validation = request.ValidationRunId.HasValue
            ? await _validationService.GetRunAsync(request.ValidationRunId.Value, cancellationToken)
            : null;
        var validationFindings = request.ValidationRunId.HasValue
            ? await _validationService.GetFindingsAsync(request.ValidationRunId.Value, cancellationToken)
            : [];

        return new AdvisorContext
        {
            Discovery = discovery,
            Job = job,
            Validation = validation,
            ValidationFindings = validationFindings
        };
    }

    private async Task<DiscoveryScanResult?> ResolveDiscoveryAsync(string? discoveryRunId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(discoveryRunId))
        {
            return await _discoveryService.GetScanResultAsync(discoveryRunId, cancellationToken);
        }

        return await _discoveryService.GetLatestCompletedResultAsync(cancellationToken);
    }

    private static RemediationItem ToRemediation(MigrationRiskFinding finding)
    {
        return new RemediationItem
        {
            Issue = finding.RiskType,
            Impact = finding.Description,
            RecommendedFix = finding.RecommendedAction,
            Priority = finding.RiskLevel,
            AutomationEligible = finding.RiskType.Contains("Metadata", StringComparison.OrdinalIgnoreCase)
                || finding.RiskType.Contains("Path", StringComparison.OrdinalIgnoreCase),
            Confidence = finding.RiskLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase) ? 0.92 : 0.84,
            SourceFindingId = finding.Id
        };
    }

    private static string BuildFallbackAnswer(string question, AdvisorContext context)
    {
        var topRisk = context.Discovery?.MigrationRisks
            .OrderByDescending(item => SeverityRank(item.RiskLevel))
            .FirstOrDefault();

        if (topRisk is not null)
        {
            return $"Based on the available zettalogixmigrationsuite context, the highest priority risk is {topRisk.RiskType} at {topRisk.Site}. Impact: {topRisk.Description} Recommended fix: {topRisk.RecommendedAction}";
        }

        if (context.Job is not null)
        {
            return $"Migration job '{context.Job.Name}' is currently {context.Job.EnterpriseState}. Completed items: {context.Job.CompletedItems}/{context.Job.TotalItems}; failed items: {context.Job.FailedItems}.";
        }

        if (context.Validation is not null)
        {
            return $"Latest validation status is {context.Validation.Status}: {context.Validation.PassedCount} passed, {context.Validation.WarningCount} warnings, {context.Validation.FailedCount} failed.";
        }

        return "No actionable risk, migration, or validation context is available yet.";
    }

    private static EtaEstimate Estimate(int totalFiles, long totalSizeBytes, int retryCount, int throttlingCount, int largeFileCount, int concurrency)
    {
        var effectiveConcurrency = Math.Max(1, concurrency);
        var throughputBytesPerMinute = 60L * 1024L * 1024L * effectiveConcurrency;
        var sizeMinutes = totalSizeBytes <= 0 ? 5 : Math.Ceiling(totalSizeBytes / (double)throughputBytesPerMinute);
        var operationMinutes = Math.Ceiling(totalFiles / (double)(40 * effectiveConcurrency));
        var penaltyMinutes = retryCount * 3 + throttlingCount * 5 + largeFileCount * 2;
        var minutes = Math.Max(5, sizeMinutes + operationMinutes + penaltyMinutes);

        return new EtaEstimate
        {
            EstimatedDuration = TimeSpan.FromMinutes(minutes),
            Confidence = totalFiles == 0 ? 0.35 : Math.Max(0.45, 0.82 - retryCount * 0.02 - throttlingCount * 0.03),
            BottleneckExplanation = largeFileCount > 0
                ? "Large files and metadata operations are the expected bottleneck."
                : "Throughput is primarily driven by item count and target upload concurrency.",
            Assumptions =
            [
                $"Concurrency setting: {effectiveConcurrency}",
                "Throughput estimate uses recorded platform inventory and retry counts.",
                "Hash-level validation is not assumed."
            ],
            OptimizationRecommendations =
            [
                "Run discovery and validation before cutover.",
                "Move large files in separate waves.",
                "Reduce retry pressure by fixing permission and path risks first."
            ]
        };
    }

    private static int SeverityRank(string severity)
    {
        return severity.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }

    private sealed class AdvisorContext
    {
        public DiscoveryScanResult? Discovery { get; set; }
        public ZMS.Core.Models.MigrationJob? Job { get; set; }
        public ZMS.Core.Models.ValidationRun? Validation { get; set; }
        public IReadOnlyCollection<ZMS.Core.Models.ValidationFinding> ValidationFindings { get; set; } = [];

        public bool HasAnyContext => Discovery is not null || Job is not null || Validation is not null;

        public object PublicSummary => new
        {
            Discovery = Discovery is null ? null : new
            {
                Discovery.ScanId,
                Discovery.ScanName,
                Discovery.Mode,
                Discovery.Status,
                Discovery.Summary,
                TopRisks = Discovery.MigrationRisks.OrderByDescending(item => SeverityRank(item.RiskLevel)).Take(10)
            },
            MigrationJob = Job is null ? null : new
            {
                Job.Id,
                Job.Name,
                LegacyStatus = Job.Status.ToString(),
                State = Job.EnterpriseState.ToString(),
                Job.TotalItems,
                Job.CompletedItems,
                Job.FailedItems,
                Job.RetryCount,
                LastError = SecretRedactor.Redact(Job.LastError)
            },
            Validation = Validation is null ? null : new
            {
                Validation.Id,
                Validation.Status,
                Validation.PassedCount,
                Validation.WarningCount,
                Validation.FailedCount,
                Findings = ValidationFindings.Take(20)
            }
        };
    }
}
