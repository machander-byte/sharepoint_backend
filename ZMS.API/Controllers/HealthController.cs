using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ZMS.API.Diagnostics;
using ZMS.Core.Interfaces;
using ZMS.Infrastructure.Persistence;

namespace ZMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private static readonly DateTimeOffset StartedUtc = DateTimeOffset.UtcNow;

    private readonly IQueueDiagnostics _queueDiagnostics;
    private readonly ZmsDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly DatabaseStartupState _databaseStartupState;
    private readonly DatabaseSchemaReadinessChecker _schemaReadinessChecker;

    public HealthController(
        IQueueDiagnostics queueDiagnostics,
        ZmsDbContext dbContext,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        DatabaseStartupState databaseStartupState,
        DatabaseSchemaReadinessChecker schemaReadinessChecker)
    {
        _queueDiagnostics = queueDiagnostics;
        _dbContext = dbContext;
        _environment = environment;
        _configuration = configuration;
        _databaseStartupState = databaseStartupState;
        _schemaReadinessChecker = schemaReadinessChecker;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var database = await GetDatabaseStatusAsync(cancellationToken);
        var schema = database.Healthy
            ? await _schemaReadinessChecker.CheckAsync(cancellationToken)
            : DatabaseSchemaReadinessNotChecked(database.Provider);
        var status = database.Healthy && schema.Ready ? "Healthy" : "Degraded";

        return Ok(new
        {
            Status = status,
            UtcNow = DateTimeOffset.UtcNow,
            DatabaseStartup = _databaseStartupState.Snapshot,
            Database = database,
            Schema = schema,
            Queue = new
            {
                _queueDiagnostics.Provider,
                _queueDiagnostics.IsConfigured,
                _queueDiagnostics.PendingCount,
                _queueDiagnostics.ActiveLeaseCount,
                _queueDiagnostics.DeadLetterCount,
                _queueDiagnostics.StatusMessage
            }
        });
    }

    [HttpGet("/api/version")]
    public IActionResult Version()
    {
        var assembly = typeof(Program).Assembly.GetName();
        var deployment = GetDeploymentFingerprint();

        return Ok(new
        {
            AppName = "ZMS",
            Service = "ZMS.API",
            Version = assembly.Version?.ToString() ?? "unknown",
            Environment = _environment.EnvironmentName,
            deployment.Commit,
            deployment.BuildTime,
            DatabaseStartup = _databaseStartupState.Snapshot,
            StartedUtc,
            UtcNow = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("/api/status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var database = await GetDatabaseStatusAsync(cancellationToken);
        var queue = new
        {
            _queueDiagnostics.Provider,
            _queueDiagnostics.IsConfigured,
            _queueDiagnostics.PendingCount,
            _queueDiagnostics.ActiveLeaseCount,
            _queueDiagnostics.DeadLetterCount,
            _queueDiagnostics.StatusMessage
        };

        var startup = _databaseStartupState.Snapshot;
        var schema = database.Healthy
            ? await _schemaReadinessChecker.CheckAsync(cancellationToken)
            : DatabaseSchemaReadinessNotChecked(database.Provider);
        var queueHealthy = _queueDiagnostics.DeadLetterCount == 0;
        var healthy = database.Healthy && schema.Ready && queueHealthy;
        var status = healthy ? "Healthy" : "Degraded";

        return StatusCode(healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable, new
        {
            AppName = "ZMS",
            Status = status,
            UtcNow = DateTimeOffset.UtcNow,
            StartedUtc,
            UptimeSeconds = (long)(DateTimeOffset.UtcNow - StartedUtc).TotalSeconds,
            Deployment = GetDeploymentFingerprint(),
            DatabaseStartup = startup,
            Database = database,
            Schema = schema,
            Queue = queue
        });
    }

    private DeploymentFingerprint GetDeploymentFingerprint()
    {
        var commit = _configuration["RENDER_GIT_COMMIT"]
            ?? _configuration["ZMS_BUILD_COMMIT"]
            ?? _configuration["Build:Commit"]
            ?? "unknown";
        var buildTime = _configuration["RENDER_DEPLOY_ID"]
            ?? _configuration["ZMS_BUILD_TIME"]
            ?? _configuration["Build:Time"]
            ?? "unknown";

        return new DeploymentFingerprint("ZMS", "ZMS.API", _environment.EnvironmentName, commit, buildTime);
    }

    private async Task<DependencyStatus> GetDatabaseStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            var canConnect = await _dbContext.Database.CanConnectAsync(timeoutCts.Token);
            return new DependencyStatus(canConnect, _dbContext.Database.ProviderName ?? "unknown", canConnect ? "Connected" : "Connection failed");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DependencyStatus(false, _dbContext.Database.ProviderName ?? "unknown", "Timed out after 5 seconds");
        }
        catch (Exception ex)
        {
            return new DependencyStatus(false, _dbContext.Database.ProviderName ?? "unknown", ex.GetType().Name);
        }
    }

    private static DatabaseSchemaReadinessSnapshot DatabaseSchemaReadinessNotChecked(string provider)
    {
        return new DatabaseSchemaReadinessSnapshot(
            Ready: false,
            Status: "NotChecked",
            Provider: provider,
            Message: "Schema readiness was not checked because database connectivity is unavailable.",
            MissingTables: [],
            ErrorType: null,
            LastCheckedUtc: DateTimeOffset.UtcNow);
    }

    private sealed record DependencyStatus(bool Healthy, string Provider, string Message);
    private sealed record DeploymentFingerprint(string AppName, string Service, string Environment, string Commit, string BuildTime);
}
