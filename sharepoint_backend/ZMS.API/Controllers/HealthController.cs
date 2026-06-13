using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

    public HealthController(
        IQueueDiagnostics queueDiagnostics,
        ZmsDbContext dbContext,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _queueDiagnostics = queueDiagnostics;
        _dbContext = dbContext;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            UtcNow = DateTimeOffset.UtcNow,
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
        return Ok(new
        {
            Service = "ZMS.API",
            Version = assembly.Version?.ToString() ?? "unknown",
            Environment = _environment.EnvironmentName,
            Commit = _configuration["Build:Commit"] ?? _configuration["RENDER_GIT_COMMIT"] ?? "unknown",
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

        var healthy = database.Healthy;
        var status = healthy ? "Healthy" : "Unhealthy";

        return StatusCode(healthy ? StatusCodes.Status200OK : StatusCodes.Status503ServiceUnavailable, new
        {
            Status = status,
            UtcNow = DateTimeOffset.UtcNow,
            StartedUtc,
            UptimeSeconds = (long)(DateTimeOffset.UtcNow - StartedUtc).TotalSeconds,
            Database = database,
            Queue = queue
        });
    }

    private async Task<DependencyStatus> GetDatabaseStatusAsync(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return new DependencyStatus(canConnect, _dbContext.Database.ProviderName ?? "unknown", canConnect ? "Connected" : "Connection failed");
        }
        catch (Exception ex)
        {
            return new DependencyStatus(false, _dbContext.Database.ProviderName ?? "unknown", ex.GetType().Name);
        }
    }

    private sealed record DependencyStatus(bool Healthy, string Provider, string Message);
}
