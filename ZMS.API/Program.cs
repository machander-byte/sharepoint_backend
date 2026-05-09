using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;
using ZMS.Application.DependencyInjection;
using ZMS.Connectors.FileShare.DependencyInjection;
using ZMS.Connectors.GoogleDrive.DependencyInjection;
using ZMS.Connectors.SharePointOnPrem.DependencyInjection;
using ZMS.Connectors.SharePointOnline.DependencyInjection;
using ZMS.Core.Options;
using ZMS.Infrastructure.DependencyInjection;
using ZMS.Infrastructure.Persistence;
using ZMS.MigrationEngine.DependencyInjection;
using ZMS.Reporting.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers(options => options.Filters.Add(new AuthorizeFilter()))
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var supabaseAuthority = (builder.Configuration["Supabase:Auth:Authority"]
    ?? "https://hxptmbphcdyzhmwnimwh.supabase.co/auth/v1").TrimEnd('/');
var supabaseAudience = builder.Configuration["Supabase:Auth:Audience"] ?? "authenticated";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = supabaseAuthority;
        options.Audience = supabaseAudience;
        options.RequireHttpsMetadata = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = supabaseAuthority,
            ValidateAudience = true,
            ValidAudience = supabaseAudience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("ZmsCors", policy =>
    {
        policy.WithOrigins(GetCorsAllowedOrigins(builder.Configuration))
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<MigrationEngineOptions>(builder.Configuration.GetSection(MigrationEngineOptions.SectionName));
builder.Services.Configure<GoogleDriveOptions>(builder.Configuration.GetSection(GoogleDriveOptions.SectionName));

var dataProtectionBuilder = builder.Services
    .AddDataProtection()
    .SetApplicationName("ZettalogixMigrationSuite");
var dataProtectionKeyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeyRingPath))
{
    var keyRingDirectory = new DirectoryInfo(dataProtectionKeyRingPath);
    keyRingDirectory.Create();
    dataProtectionBuilder.PersistKeysToFileSystem(keyRingDirectory);
}

builder.Services
    .AddZmsApplication()
    .AddZmsInfrastructure(builder.Configuration)
    .AddSharePointOnPremConnector()
    .AddFileShareConnector()
    .AddGoogleDriveConnector()
    .AddSharePointOnlineConnector()
    .AddZmsReporting()
    .AddZmsMigrationEngine(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("ZmsCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Ok(new
{
    Status = "Healthy",
    HealthEndpoint = "/api/health"
})).AllowAnonymous();
app.MapControllers();

await EnsureDatabaseCreatedAsync(app.Services);

app.Run();

static async Task EnsureDatabaseCreatedAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ZmsDbContext>();

    if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await EnsurePostgresSchemaAsync(dbContext);
        return;
    }

    await dbContext.Database.EnsureCreatedAsync();
    await EnsureMigrationJobColumnsAsync(dbContext);
}

static async Task EnsurePostgresSchemaAsync(ZmsDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Connections"
        (
            "Id" uuid NOT NULL PRIMARY KEY,
            "Name" character varying(200) NOT NULL,
            "Type" character varying(50) NOT NULL,
            "Url" character varying(500) NOT NULL,
            "Username" character varying(200) NULL,
            "Password" character varying(500) NULL,
            "ClientId" character varying(200) NULL,
            "ClientSecret" character varying(500) NULL,
            "TenantId" character varying(200) NULL,
            "RootPath" character varying(500) NULL,
            "AdditionalSettings" text NOT NULL DEFAULT '{{}}',
            "IsEnabled" boolean NOT NULL DEFAULT true,
            "CreatedUtc" timestamp with time zone NOT NULL DEFAULT now(),
            "UpdatedUtc" timestamp with time zone NOT NULL DEFAULT now()
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "MigrationJobs"
        (
            "Id" uuid NOT NULL PRIMARY KEY,
            "Name" character varying(200) NOT NULL,
            "SourceConnectionId" uuid NOT NULL,
            "TargetConnectionId" uuid NOT NULL,
            "SourceLocation" character varying(500) NOT NULL,
            "SourceLibraryName" character varying(200) NULL,
            "TargetSiteUrl" character varying(500) NOT NULL,
            "TargetLibraryName" character varying(200) NOT NULL,
            "TargetLibraryUrlSegment" character varying(200) NULL,
            "TargetRootPath" character varying(500) NULL,
            "PreserveMetadata" boolean NOT NULL DEFAULT true,
            "BatchSize" integer NOT NULL DEFAULT 20,
            "MaxRetryCount" integer NOT NULL DEFAULT 3,
            "Status" character varying(50) NOT NULL,
            "TotalItems" integer NOT NULL DEFAULT 0,
            "CompletedItems" integer NOT NULL DEFAULT 0,
            "FailedItems" integer NOT NULL DEFAULT 0,
            "LastError" character varying(2000) NULL,
            "CreatedUtc" timestamp with time zone NOT NULL DEFAULT now(),
            "StartedUtc" timestamp with time zone NULL,
            "FinishedUtc" timestamp with time zone NULL,
            "UpdatedUtc" timestamp with time zone NOT NULL DEFAULT now(),
            CONSTRAINT "FK_MigrationJobs_SourceConnection" FOREIGN KEY ("SourceConnectionId") REFERENCES "Connections"("Id"),
            CONSTRAINT "FK_MigrationJobs_TargetConnection" FOREIGN KEY ("TargetConnectionId") REFERENCES "Connections"("Id")
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "MigrationItems"
        (
            "Id" uuid NOT NULL PRIMARY KEY,
            "JobId" uuid NOT NULL,
            "FileName" character varying(260) NOT NULL,
            "SourcePath" character varying(1000) NOT NULL,
            "TargetPath" character varying(1000) NULL,
            "FileSizeInBytes" bigint NOT NULL,
            "Metadata" text NOT NULL DEFAULT '{{}}',
            "Status" character varying(50) NOT NULL,
            "RetryCount" integer NOT NULL DEFAULT 0,
            "ErrorMessage" character varying(2000) NULL,
            "CreatedUtc" timestamp with time zone NOT NULL DEFAULT now(),
            "StartedUtc" timestamp with time zone NULL,
            "CompletedUtc" timestamp with time zone NULL,
            CONSTRAINT "FK_MigrationItems_MigrationJobs" FOREIGN KEY ("JobId") REFERENCES "MigrationJobs"("Id")
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Logs"
        (
            "Id" uuid NOT NULL PRIMARY KEY,
            "JobId" uuid NULL,
            "ItemId" uuid NULL,
            "Severity" character varying(50) NOT NULL,
            "Message" character varying(1000) NOT NULL,
            "Details" character varying(4000) NULL,
            "CreatedUtc" timestamp with time zone NOT NULL DEFAULT now(),
            CONSTRAINT "FK_Logs_MigrationJobs" FOREIGN KEY ("JobId") REFERENCES "MigrationJobs"("Id"),
            CONSTRAINT "FK_Logs_MigrationItems" FOREIGN KEY ("ItemId") REFERENCES "MigrationItems"("Id")
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetLibraryUrlSegment\" character varying(200) NULL;");
    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetRootPath\" character varying(500) NULL;");

    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_MigrationJobs_Status\" ON \"MigrationJobs\"(\"Status\");");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_MigrationJobs_CreatedUtc\" ON \"MigrationJobs\"(\"CreatedUtc\" DESC);");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_MigrationItems_JobId_Status\" ON \"MigrationItems\"(\"JobId\", \"Status\");");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_Logs_JobId_CreatedUtc\" ON \"Logs\"(\"JobId\", \"CreatedUtc\" DESC);");

    await EnablePostgresRowLevelSecurityAsync(dbContext);
}

static async Task EnablePostgresRowLevelSecurityAsync(ZmsDbContext dbContext)
{
    var enableRlsStatements = new[]
    {
        "ALTER TABLE \"Connections\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"MigrationJobs\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"MigrationItems\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"Logs\" ENABLE ROW LEVEL SECURITY;"
    };

    foreach (var statement in enableRlsStatements)
    {
        await dbContext.Database.ExecuteSqlRawAsync(statement);
    }
}

static async Task EnsureMigrationJobColumnsAsync(ZmsDbContext dbContext)
{
    if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        var existingColumns = await GetSqliteColumnsAsync(dbContext, "MigrationJobs");
        if (!existingColumns.Contains("TargetLibraryUrlSegment"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"TargetLibraryUrlSegment\" TEXT NULL;");
        }

        if (!existingColumns.Contains("TargetRootPath"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"TargetRootPath\" TEXT NULL;");
        }

        return;
    }

    if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'TargetLibraryUrlSegment') IS NULL ALTER TABLE [MigrationJobs] ADD [TargetLibraryUrlSegment] nvarchar(200) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'TargetRootPath') IS NULL ALTER TABLE [MigrationJobs] ADD [TargetRootPath] nvarchar(500) NULL;");
    }

    if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetLibraryUrlSegment\" character varying(200) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetRootPath\" character varying(500) NULL;");
    }
}

static async Task<HashSet<string>> GetSqliteColumnsAsync(ZmsDbContext dbContext, string tableName)
{
    var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var connection = dbContext.Database.GetDbConnection();
    var closeConnection = connection.State != System.Data.ConnectionState.Open;

    if (closeConnection)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName.Replace("\"", "\"\"")}\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columns.Add(reader.GetString(1));
        }
    }
    finally
    {
        if (closeConnection)
        {
            await connection.CloseAsync();
        }
    }

    return columns;
}

static string[] GetCorsAllowedOrigins(IConfiguration configuration)
{
    var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
    var commaSeparatedOrigins = configuration["Cors:AllowedOrigins"];

    if (!string.IsNullOrWhiteSpace(commaSeparatedOrigins))
    {
        configuredOrigins = [.. configuredOrigins, .. commaSeparatedOrigins.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];
    }

    var origins = configuredOrigins
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    return origins.Length > 0 ? origins : ["http://localhost:5173", "http://127.0.0.1:5173"];
}
