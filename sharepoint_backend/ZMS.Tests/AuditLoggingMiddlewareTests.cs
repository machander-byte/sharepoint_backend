using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ZMS.API.Middleware;
using ZMS.Infrastructure.Persistence;

namespace ZMS.Tests;

public sealed class AuditLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WritesAuditLog_ForMutatingApiRequest()
    {
        await using var dbContext = await CreateDbContextAsync();
        var middleware = new AuditLoggingMiddleware(
            next: context =>
            {
                context.Response.StatusCode = StatusCodes.Status201Created;
                return Task.CompletedTask;
            },
            logger: CreateLogger());

        var context = new DefaultHttpContext
        {
            TraceIdentifier = "corr-1",
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "Test"))
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/connections";
        context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        await middleware.InvokeAsync(context, dbContext);

        var auditLog = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal("user-1", auditLog.UserId);
        Assert.Equal("POST", auditLog.Method);
        Assert.Equal("/api/connections", auditLog.Path);
        Assert.Equal(StatusCodes.Status201Created, auditLog.StatusCode);
        Assert.Equal("corr-1", auditLog.CorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_DoesNotWriteAuditLog_ForReadRequest()
    {
        await using var dbContext = await CreateDbContextAsync();
        var middleware = new AuditLoggingMiddleware(_ => Task.CompletedTask, CreateLogger());
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/jobs";

        await middleware.InvokeAsync(context, dbContext);

        Assert.Empty(await dbContext.AuditLogs.ToListAsync());
    }

    private static async Task<ZmsDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<ZmsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var dbContext = new ZmsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private static ILogger<AuditLoggingMiddleware> CreateLogger()
    {
        return LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning))
            .CreateLogger<AuditLoggingMiddleware>();
    }
}
