using ZMS.API.Extensions;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;

namespace ZMS.API.Middleware;

public sealed class AuditLoggingMiddleware
{
    private static readonly HashSet<string> AuditedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ZmsDbContext dbContext)
    {
        await _next(context);

        if (!ShouldAudit(context))
        {
            return;
        }

        try
        {
            dbContext.AuditLogs.Add(new AuditLog
            {
                UserId = context.User.TryGetUserId() ?? "anonymous",
                Action = Trim($"{context.Request.Method} {context.Request.Path.Value}", 200),
                Method = context.Request.Method,
                Path = Trim(context.Request.Path.Value ?? string.Empty, 1000),
                StatusCode = context.Response.StatusCode,
                IpAddress = Trim(GetIpAddress(context), 100),
                CorrelationId = Trim(context.TraceIdentifier, 100),
                CreatedUtc = DateTimeOffset.UtcNow
            });

            await dbContext.SaveChangesAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write audit log for {Method} {Path}", context.Request.Method, context.Request.Path.Value);
        }
    }

    private static bool ShouldAudit(HttpContext context)
    {
        return AuditedMethods.Contains(context.Request.Method)
            && context.Request.Path.StartsWithSegments("/api");
    }

    private static string GetIpAddress(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor)
            && !string.IsNullOrWhiteSpace(forwardedFor.FirstOrDefault()))
        {
            return forwardedFor.First()!.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
    }

    private static string Trim(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
