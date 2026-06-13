using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using ZMS.API.Middleware;
using ZMS.API.Security;
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
const long DefaultMaxRequestBodySize = 50_000_000;
var maxRequestBodySize = builder.Configuration.GetValue<long?>("RequestLimits:MaxBodyBytes") ?? DefaultMaxRequestBodySize;
var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? Environment.GetEnvironmentVariable("SENTRY_DSN");

if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.SendDefaultPii = false;
        options.TracesSampleRate = builder.Configuration.GetValue<double?>("Sentry:TracesSampleRate") ?? 0.0;
    });
}

builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxRequestBodySize);

builder.Services.AddProblemDetails();
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = maxRequestBodySize;
    options.ValueLengthLimit = (int)Math.Min(maxRequestBodySize, int.MaxValue);
});
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

builder.Services.AddAuthorization(options =>
{
    var viewerPolicy = BuildZmsRolePolicy(builder.Configuration, ZmsRoles.Viewer, ZmsRoles.Operator, ZmsRoles.Admin);

    options.DefaultPolicy = viewerPolicy;
    options.FallbackPolicy = viewerPolicy;
    options.AddPolicy(ZmsAuthorizationPolicies.Viewer, viewerPolicy);
    options.AddPolicy(ZmsAuthorizationPolicies.Operator, BuildZmsRolePolicy(builder.Configuration, ZmsRoles.Operator, ZmsRoles.Admin));
    options.AddPolicy(ZmsAuthorizationPolicies.Admin, BuildZmsRolePolicy(builder.Configuration, ZmsRoles.Admin));
});

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
var dataProtectionKeyStorage = builder.Configuration["DataProtection:KeyStorage"];
var dataProtectionKeyRingPath = builder.Configuration["DataProtection:KeyRingPath"];
if (string.Equals(dataProtectionKeyStorage, "Database", StringComparison.OrdinalIgnoreCase))
{
    dataProtectionBuilder.PersistKeysToDbContext<ZmsDbContext>();
}
else if (!string.IsNullOrWhiteSpace(dataProtectionKeyRingPath))
{
    var keyRingDirectory = new DirectoryInfo(dataProtectionKeyRingPath);
    keyRingDirectory.Create();
    dataProtectionBuilder.PersistKeysToFileSystem(keyRingDirectory);
}

builder.Services
    .AddZmsApplication(builder.Configuration)
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
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors("ZmsCors");
app.UseAuthentication();
app.UseMiddleware<AuditLoggingMiddleware>();
app.UseAuthorization();
app.MapGet("/", () => Results.Ok(new
{
    Status = "Healthy",
    HealthEndpoint = "/api/health"
})).AllowAnonymous();
app.MapControllers();

await EnsureDatabaseCreatedAsync(app.Services, app.Logger);

app.Run();

static async Task EnsureDatabaseCreatedAsync(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ZmsDbContext>();

    if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await EnsurePostgresSchemaAsync(dbContext);
        await EnsureEnterpriseTablesAsync(dbContext);
        await EnsureAuditLogsTableAsync(dbContext);
        await ApplyMigrationsIfSafeAsync(dbContext, logger);
        await EnablePostgresRowLevelSecurityAsync(dbContext);
        return;
    }

    await dbContext.Database.EnsureCreatedAsync();
    await EnsureMigrationJobColumnsAsync(dbContext);
    await EnsureEnterpriseTablesAsync(dbContext);
    await EnsureAuditLogsTableAsync(dbContext);
    await ApplyMigrationsIfSafeAsync(dbContext, logger);
}

static async Task ApplyMigrationsIfSafeAsync(ZmsDbContext dbContext, ILogger logger)
{
    try
    {
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToArray();
        if (pendingMigrations.Length == 0)
        {
            logger.LogInformation("Database schema validation completed. No pending EF migrations.");
            return;
        }

        logger.LogWarning(
            "Database has {PendingMigrationCount} pending EF migration(s): {PendingMigrations}. Applying additive hardening migrations.",
            pendingMigrations.Length,
            string.Join(", ", pendingMigrations));

        await dbContext.Database.MigrateAsync();
        logger.LogInformation("Database EF migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Database migration validation could not complete. Startup schema safeguards remain active.");
    }
}

static async Task EnsurePostgresSchemaAsync(ZmsDbContext dbContext)
{
    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "Connections"
        (
            "Id" uuid NOT NULL PRIMARY KEY,
            "UserId" character varying(200) NOT NULL,
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
            "UserId" character varying(200) NOT NULL,
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

    await dbContext.Database.ExecuteSqlRawAsync("""
        CREATE TABLE IF NOT EXISTS "DataProtectionKeys"
        (
            "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
            "FriendlyName" text NULL,
            "Xml" text NULL
        );
        """);

    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"Connections\" ADD COLUMN IF NOT EXISTS \"UserId\" character varying(200) NOT NULL DEFAULT '';");
    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"UserId\" character varying(200) NOT NULL DEFAULT '';");
    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetLibraryUrlSegment\" character varying(200) NULL;");
    await dbContext.Database.ExecuteSqlRawAsync(
        "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetRootPath\" character varying(500) NULL;");

    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_Connections_UserId_IsEnabled\" ON \"Connections\"(\"UserId\", \"IsEnabled\");");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_MigrationJobs_Status\" ON \"MigrationJobs\"(\"Status\");");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_MigrationJobs_CreatedUtc\" ON \"MigrationJobs\"(\"CreatedUtc\" DESC);");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_MigrationItems_JobId_Status\" ON \"MigrationItems\"(\"JobId\", \"Status\");");
    await dbContext.Database.ExecuteSqlRawAsync(
        "CREATE INDEX IF NOT EXISTS \"IX_Logs_JobId_CreatedUtc\" ON \"Logs\"(\"JobId\", \"CreatedUtc\" DESC);");

}

static async Task EnablePostgresRowLevelSecurityAsync(ZmsDbContext dbContext)
{
    var enableRlsStatements = new[]
    {
        "ALTER TABLE \"Connections\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"MigrationJobs\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"MigrationItems\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"Logs\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DataProtectionKeys\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveryRuns\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredSites\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredWebs\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredLibraries\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredLists\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredFolders\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredFiles\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredPermissions\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredSharingLinks\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredMetadataFields\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"DiscoveredContentTypes\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"RiskFindings\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"MigrationJobEvents\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"ValidationRuns\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"ValidationFindings\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"ValidationItemResults\" ENABLE ROW LEVEL SECURITY;",
        "ALTER TABLE \"AuditLogs\" ENABLE ROW LEVEL SECURITY;"
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
        var existingMigrationJobColumns = await GetSqliteColumnsAsync(dbContext, "MigrationJobs");
        if (!existingMigrationJobColumns.Contains("UserId"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"UserId\" TEXT NOT NULL DEFAULT '';" );
        }

        if (!existingMigrationJobColumns.Contains("TargetLibraryUrlSegment"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"TargetLibraryUrlSegment\" TEXT NULL;");
        }

        if (!existingMigrationJobColumns.Contains("TargetRootPath"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"TargetRootPath\" TEXT NULL;");
        }

        if (!existingMigrationJobColumns.Contains("EnterpriseState"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"EnterpriseState\" TEXT NOT NULL DEFAULT 'CREATED';");
        }

        if (!existingMigrationJobColumns.Contains("FailureReason"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"FailureReason\" TEXT NULL;");
        }

        if (!existingMigrationJobColumns.Contains("RetryCount"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"RetryCount\" INTEGER NOT NULL DEFAULT 0;");
        }

        if (!existingMigrationJobColumns.Contains("CorrelationId"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"CorrelationId\" TEXT NULL;");
        }

        if (!existingMigrationJobColumns.Contains("LeaseId"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"LeaseId\" TEXT NULL;");
        }

        if (!existingMigrationJobColumns.Contains("LeaseExpiresUtc"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationJobs\" ADD COLUMN \"LeaseExpiresUtc\" TEXT NULL;");
        }

        var existingMigrationItemColumns = await GetSqliteColumnsAsync(dbContext, "MigrationItems");
        if (!existingMigrationItemColumns.Contains("EnterpriseState"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"MigrationItems\" ADD COLUMN \"EnterpriseState\" TEXT NOT NULL DEFAULT 'PENDING';");
        }

        var existingConnectionColumns = await GetSqliteColumnsAsync(dbContext, "Connections");
        if (!existingConnectionColumns.Contains("UserId"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                "ALTER TABLE \"Connections\" ADD COLUMN \"UserId\" TEXT NOT NULL DEFAULT '';" );
        }

        return;
    }

    if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'UserId') IS NULL ALTER TABLE [MigrationJobs] ADD [UserId] nvarchar(200) NOT NULL DEFAULT '';" );
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'TargetLibraryUrlSegment') IS NULL ALTER TABLE [MigrationJobs] ADD [TargetLibraryUrlSegment] nvarchar(200) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'TargetRootPath') IS NULL ALTER TABLE [MigrationJobs] ADD [TargetRootPath] nvarchar(500) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('Connections', 'UserId') IS NULL ALTER TABLE [Connections] ADD [UserId] nvarchar(200) NOT NULL DEFAULT '';" );
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'EnterpriseState') IS NULL ALTER TABLE [MigrationJobs] ADD [EnterpriseState] nvarchar(50) NOT NULL DEFAULT 'CREATED';");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'FailureReason') IS NULL ALTER TABLE [MigrationJobs] ADD [FailureReason] nvarchar(2000) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'RetryCount') IS NULL ALTER TABLE [MigrationJobs] ADD [RetryCount] int NOT NULL DEFAULT 0;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'CorrelationId') IS NULL ALTER TABLE [MigrationJobs] ADD [CorrelationId] nvarchar(100) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'LeaseId') IS NULL ALTER TABLE [MigrationJobs] ADD [LeaseId] nvarchar(100) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationJobs', 'LeaseExpiresUtc') IS NULL ALTER TABLE [MigrationJobs] ADD [LeaseExpiresUtc] datetimeoffset NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "IF COL_LENGTH('MigrationItems', 'EnterpriseState') IS NULL ALTER TABLE [MigrationItems] ADD [EnterpriseState] nvarchar(50) NOT NULL DEFAULT 'PENDING';");
    }

    if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"UserId\" character varying(200) NOT NULL DEFAULT '';" );
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"Connections\" ADD COLUMN IF NOT EXISTS \"UserId\" character varying(200) NOT NULL DEFAULT '';" );
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetLibraryUrlSegment\" character varying(200) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"TargetRootPath\" character varying(500) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"EnterpriseState\" character varying(50) NOT NULL DEFAULT 'CREATED';");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"FailureReason\" character varying(2000) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"RetryCount\" integer NOT NULL DEFAULT 0;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"CorrelationId\" character varying(100) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"LeaseId\" character varying(100) NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationJobs\" ADD COLUMN IF NOT EXISTS \"LeaseExpiresUtc\" timestamp with time zone NULL;");
        await dbContext.Database.ExecuteSqlRawAsync(
            "ALTER TABLE \"MigrationItems\" ADD COLUMN IF NOT EXISTS \"EnterpriseState\" character varying(50) NOT NULL DEFAULT 'PENDING';");
    }
}

static async Task EnsureEnterpriseTablesAsync(ZmsDbContext dbContext)
{
    if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "DiscoveryRuns" ("Id" TEXT NOT NULL PRIMARY KEY, "Name" TEXT NOT NULL, "ProjectId" TEXT NULL, "ConnectionId" TEXT NULL, "SourceType" TEXT NOT NULL, "Status" TEXT NOT NULL, "StartedAt" TEXT NOT NULL, "CompletedAt" TEXT NULL, "TotalSites" INTEGER NOT NULL, "TotalWebs" INTEGER NOT NULL, "TotalLibraries" INTEGER NOT NULL, "TotalLists" INTEGER NOT NULL, "TotalFolders" INTEGER NOT NULL, "TotalFiles" INTEGER NOT NULL, "TotalPermissions" INTEGER NOT NULL, "TotalSharingLinks" INTEGER NOT NULL, "TotalRiskFindings" INTEGER NOT NULL, "ReadinessScore" INTEGER NOT NULL, "ErrorMessage" TEXT NULL, "CreatedUtc" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredSites" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "ExternalId" TEXT NOT NULL, "Title" TEXT NOT NULL, "Url" TEXT NOT NULL, "Department" TEXT NOT NULL, "Description" TEXT NOT NULL, "FileCount" INTEGER NOT NULL, "FolderCount" INTEGER NOT NULL, "SizeBytes" INTEGER NOT NULL, "CreatedAt" TEXT NULL, "ModifiedAt" TEXT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredWebs" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "SiteId" TEXT NULL, "ExternalId" TEXT NOT NULL, "Title" TEXT NOT NULL, "Url" TEXT NOT NULL, "Description" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredLibraries" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "SiteId" TEXT NULL, "WebId" TEXT NULL, "ExternalId" TEXT NOT NULL, "Title" TEXT NOT NULL, "Type" TEXT NOT NULL, "Url" TEXT NOT NULL, "FileCount" INTEGER NOT NULL, "FolderCount" INTEGER NOT NULL, "SizeBytes" INTEGER NOT NULL, "BrokenInheritance" INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredLists" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "SiteId" TEXT NULL, "WebId" TEXT NULL, "ExternalId" TEXT NOT NULL, "Title" TEXT NOT NULL, "Description" TEXT NOT NULL, "ItemCount" INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredFolders" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "LibraryId" TEXT NULL, "ExternalId" TEXT NOT NULL, "Name" TEXT NOT NULL, "Path" TEXT NOT NULL, "Depth" INTEGER NOT NULL, "FileCount" INTEGER NOT NULL, "SizeBytes" INTEGER NOT NULL, "Archived" INTEGER NOT NULL, "LongPathRisk" INTEGER NOT NULL, "DuplicateIndicator" INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredFiles" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "LibraryId" TEXT NULL, "FolderId" TEXT NULL, "Name" TEXT NOT NULL, "Path" TEXT NOT NULL, "Url" TEXT NOT NULL, "SizeBytes" INTEGER NOT NULL, "CreatedAt" TEXT NULL, "ModifiedAt" TEXT NULL, "LargeFileRisk" INTEGER NOT NULL, "LongPathRisk" INTEGER NOT NULL, "DuplicateIndicator" INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredPermissions" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "Site" TEXT NOT NULL, "Scope" TEXT NOT NULL, "Principal" TEXT NOT NULL, "PrincipalType" TEXT NOT NULL, "Role" TEXT NOT NULL, "HasBrokenInheritance" INTEGER NOT NULL, "IsExternal" INTEGER NOT NULL, "IsBroadAccess" INTEGER NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredSharingLinks" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "Scope" TEXT NOT NULL, "Path" TEXT NOT NULL, "LinkType" TEXT NOT NULL, "AllowsAnonymousAccess" INTEGER NOT NULL, "AllowsExternalAccess" INTEGER NOT NULL, "ExpiresAt" TEXT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredMetadataFields" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "LibraryId" TEXT NULL, "Site" TEXT NOT NULL, "Library" TEXT NOT NULL, "Name" TEXT NOT NULL, "FieldType" TEXT NOT NULL, "Required" INTEGER NOT NULL, "MissingValueCount" INTEGER NOT NULL, "MappingRisk" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredContentTypes" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "LibraryId" TEXT NULL, "Name" TEXT NOT NULL, "Scope" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "RiskFindings" ("Id" TEXT NOT NULL PRIMARY KEY, "DiscoveryRunId" TEXT NOT NULL, "SourceFindingId" TEXT NOT NULL, "Category" TEXT NOT NULL, "Severity" TEXT NOT NULL, "Title" TEXT NOT NULL, "Description" TEXT NOT NULL, "RecommendedAction" TEXT NOT NULL, "Site" TEXT NOT NULL, "Location" TEXT NOT NULL, "Path" TEXT NOT NULL, "CreatedUtc" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "MigrationJobEvents" ("Id" TEXT NOT NULL PRIMARY KEY, "JobId" TEXT NOT NULL, "EventType" TEXT NOT NULL, "PreviousState" TEXT NULL, "NewState" TEXT NOT NULL, "Message" TEXT NOT NULL, "Severity" TEXT NOT NULL, "CreatedAt" TEXT NOT NULL, "CorrelationId" TEXT NULL, "MetadataJson" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "ValidationRuns" ("Id" TEXT NOT NULL PRIMARY KEY, "MigrationJobId" TEXT NOT NULL, "Status" TEXT NOT NULL, "StartedAt" TEXT NOT NULL, "CompletedAt" TEXT NULL, "SourceItemCount" INTEGER NOT NULL, "TargetItemCount" INTEGER NOT NULL, "PassedCount" INTEGER NOT NULL, "WarningCount" INTEGER NOT NULL, "FailedCount" INTEGER NOT NULL, "Summary" TEXT NOT NULL, "ErrorMessage" TEXT NULL);
            CREATE TABLE IF NOT EXISTS "ValidationFindings" ("Id" TEXT NOT NULL PRIMARY KEY, "ValidationRunId" TEXT NOT NULL, "Severity" TEXT NOT NULL, "Category" TEXT NOT NULL, "Message" TEXT NOT NULL, "SourcePath" TEXT NOT NULL, "TargetPath" TEXT NOT NULL, "RecommendedAction" TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS "ValidationItemResults" ("Id" TEXT NOT NULL PRIMARY KEY, "ValidationRunId" TEXT NOT NULL, "MigrationItemId" TEXT NULL, "SourcePath" TEXT NOT NULL, "TargetPath" TEXT NOT NULL, "SourceSizeBytes" INTEGER NOT NULL, "TargetSizeBytes" INTEGER NOT NULL, "Status" TEXT NOT NULL, "DifferenceType" TEXT NOT NULL, "Message" TEXT NOT NULL);
            """);
        return;
    }

    if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "DiscoveryRuns" ("Id" uuid NOT NULL PRIMARY KEY, "Name" character varying(200) NOT NULL, "ProjectId" character varying(100) NULL, "ConnectionId" uuid NULL, "SourceType" character varying(80) NOT NULL, "Status" character varying(50) NOT NULL, "StartedAt" timestamp with time zone NOT NULL, "CompletedAt" timestamp with time zone NULL, "TotalSites" integer NOT NULL DEFAULT 0, "TotalWebs" integer NOT NULL DEFAULT 0, "TotalLibraries" integer NOT NULL DEFAULT 0, "TotalLists" integer NOT NULL DEFAULT 0, "TotalFolders" integer NOT NULL DEFAULT 0, "TotalFiles" integer NOT NULL DEFAULT 0, "TotalPermissions" integer NOT NULL DEFAULT 0, "TotalSharingLinks" integer NOT NULL DEFAULT 0, "TotalRiskFindings" integer NOT NULL DEFAULT 0, "ReadinessScore" integer NOT NULL DEFAULT 0, "ErrorMessage" character varying(2000) NULL, "CreatedUtc" timestamp with time zone NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS "DiscoveredSites" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "ExternalId" character varying(200) NOT NULL, "Title" character varying(300) NOT NULL, "Url" character varying(1000) NOT NULL, "Department" character varying(100) NOT NULL, "Description" character varying(1000) NOT NULL, "FileCount" integer NOT NULL DEFAULT 0, "FolderCount" integer NOT NULL DEFAULT 0, "SizeBytes" bigint NOT NULL DEFAULT 0, "CreatedAt" timestamp with time zone NULL, "ModifiedAt" timestamp with time zone NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredWebs" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "SiteId" uuid NULL, "ExternalId" character varying(200) NOT NULL, "Title" character varying(300) NOT NULL, "Url" character varying(1000) NOT NULL, "Description" character varying(1000) NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredLibraries" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "SiteId" uuid NULL, "WebId" uuid NULL, "ExternalId" character varying(200) NOT NULL, "Title" character varying(300) NOT NULL, "Type" character varying(100) NOT NULL, "Url" character varying(1000) NOT NULL, "FileCount" integer NOT NULL DEFAULT 0, "FolderCount" integer NOT NULL DEFAULT 0, "SizeBytes" bigint NOT NULL DEFAULT 0, "BrokenInheritance" boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS "DiscoveredLists" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "SiteId" uuid NULL, "WebId" uuid NULL, "ExternalId" character varying(200) NOT NULL, "Title" character varying(300) NOT NULL, "Description" character varying(1000) NOT NULL, "ItemCount" integer NOT NULL DEFAULT 0);
            CREATE TABLE IF NOT EXISTS "DiscoveredFolders" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "LibraryId" uuid NULL, "ExternalId" character varying(200) NOT NULL, "Name" character varying(300) NOT NULL, "Path" character varying(1500) NOT NULL, "Depth" integer NOT NULL DEFAULT 0, "FileCount" integer NOT NULL DEFAULT 0, "SizeBytes" bigint NOT NULL DEFAULT 0, "Archived" boolean NOT NULL DEFAULT false, "LongPathRisk" boolean NOT NULL DEFAULT false, "DuplicateIndicator" boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS "DiscoveredFiles" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "LibraryId" uuid NULL, "FolderId" uuid NULL, "Name" character varying(300) NOT NULL, "Path" character varying(1500) NOT NULL, "Url" character varying(1500) NOT NULL, "SizeBytes" bigint NOT NULL DEFAULT 0, "CreatedAt" timestamp with time zone NULL, "ModifiedAt" timestamp with time zone NULL, "LargeFileRisk" boolean NOT NULL DEFAULT false, "LongPathRisk" boolean NOT NULL DEFAULT false, "DuplicateIndicator" boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS "DiscoveredPermissions" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "Site" character varying(300) NOT NULL, "Scope" character varying(1000) NOT NULL, "Principal" character varying(300) NOT NULL, "PrincipalType" character varying(80) NOT NULL, "Role" character varying(120) NOT NULL, "HasBrokenInheritance" boolean NOT NULL DEFAULT false, "IsExternal" boolean NOT NULL DEFAULT false, "IsBroadAccess" boolean NOT NULL DEFAULT false);
            CREATE TABLE IF NOT EXISTS "DiscoveredSharingLinks" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "Scope" character varying(300) NOT NULL, "Path" character varying(1500) NOT NULL, "LinkType" character varying(80) NOT NULL, "AllowsAnonymousAccess" boolean NOT NULL DEFAULT false, "AllowsExternalAccess" boolean NOT NULL DEFAULT false, "ExpiresAt" timestamp with time zone NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredMetadataFields" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "LibraryId" uuid NULL, "Site" character varying(300) NOT NULL, "Library" character varying(300) NOT NULL, "Name" character varying(300) NOT NULL, "FieldType" character varying(80) NOT NULL, "Required" boolean NOT NULL DEFAULT false, "MissingValueCount" integer NOT NULL DEFAULT 0, "MappingRisk" character varying(50) NOT NULL);
            CREATE TABLE IF NOT EXISTS "DiscoveredContentTypes" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "LibraryId" uuid NULL, "Name" character varying(300) NOT NULL, "Scope" character varying(1000) NOT NULL);
            CREATE TABLE IF NOT EXISTS "RiskFindings" ("Id" uuid NOT NULL PRIMARY KEY, "DiscoveryRunId" uuid NOT NULL, "SourceFindingId" character varying(300) NOT NULL, "Category" character varying(120) NOT NULL, "Severity" character varying(50) NOT NULL, "Title" character varying(300) NOT NULL, "Description" character varying(2000) NOT NULL, "RecommendedAction" character varying(2000) NOT NULL, "Site" character varying(300) NOT NULL, "Location" character varying(1000) NOT NULL, "Path" character varying(1500) NOT NULL, "CreatedUtc" timestamp with time zone NOT NULL DEFAULT now());
            CREATE TABLE IF NOT EXISTS "MigrationJobEvents" ("Id" uuid NOT NULL PRIMARY KEY, "JobId" uuid NOT NULL, "EventType" character varying(120) NOT NULL, "PreviousState" character varying(50) NULL, "NewState" character varying(50) NOT NULL, "Message" character varying(2000) NOT NULL, "Severity" character varying(50) NOT NULL, "CreatedAt" timestamp with time zone NOT NULL, "CorrelationId" character varying(100) NULL, "MetadataJson" text NOT NULL DEFAULT '{{}}');
            CREATE TABLE IF NOT EXISTS "ValidationRuns" ("Id" uuid NOT NULL PRIMARY KEY, "MigrationJobId" uuid NOT NULL, "Status" character varying(50) NOT NULL, "StartedAt" timestamp with time zone NOT NULL, "CompletedAt" timestamp with time zone NULL, "SourceItemCount" integer NOT NULL DEFAULT 0, "TargetItemCount" integer NOT NULL DEFAULT 0, "PassedCount" integer NOT NULL DEFAULT 0, "WarningCount" integer NOT NULL DEFAULT 0, "FailedCount" integer NOT NULL DEFAULT 0, "Summary" character varying(2000) NOT NULL DEFAULT '', "ErrorMessage" character varying(2000) NULL);
            CREATE TABLE IF NOT EXISTS "ValidationFindings" ("Id" uuid NOT NULL PRIMARY KEY, "ValidationRunId" uuid NOT NULL, "Severity" character varying(50) NOT NULL, "Category" character varying(120) NOT NULL, "Message" character varying(2000) NOT NULL, "SourcePath" character varying(1500) NOT NULL, "TargetPath" character varying(1500) NOT NULL, "RecommendedAction" character varying(2000) NOT NULL);
            CREATE TABLE IF NOT EXISTS "ValidationItemResults" ("Id" uuid NOT NULL PRIMARY KEY, "ValidationRunId" uuid NOT NULL, "MigrationItemId" uuid NULL, "SourcePath" character varying(1500) NOT NULL, "TargetPath" character varying(1500) NOT NULL, "SourceSizeBytes" bigint NOT NULL DEFAULT 0, "TargetSizeBytes" bigint NOT NULL DEFAULT 0, "Status" character varying(50) NOT NULL, "DifferenceType" character varying(120) NOT NULL, "Message" character varying(2000) NOT NULL);
            """);
    }
}

static async Task EnsureAuditLogsTableAsync(ZmsDbContext dbContext)
{
    if (dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AuditLogs"
            (
                "Id" TEXT NOT NULL PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "Action" TEXT NOT NULL,
                "Method" TEXT NOT NULL,
                "Path" TEXT NOT NULL,
                "StatusCode" INTEGER NOT NULL,
                "IpAddress" TEXT NOT NULL,
                "CorrelationId" TEXT NOT NULL,
                "CreatedUtc" TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId_CreatedUtc" ON "AuditLogs"("UserId", "CreatedUtc");
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedUtc" ON "AuditLogs"("CreatedUtc");
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CorrelationId" ON "AuditLogs"("CorrelationId");
            """);
        return;
    }

    if (dbContext.Database.ProviderName?.Contains("SqlServer", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AuditLogs
                (
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    UserId NVARCHAR(200) NOT NULL,
                    Action NVARCHAR(200) NOT NULL,
                    Method NVARCHAR(12) NOT NULL,
                    Path NVARCHAR(1000) NOT NULL,
                    StatusCode INT NOT NULL,
                    IpAddress NVARCHAR(100) NOT NULL,
                    CorrelationId NVARCHAR(100) NOT NULL,
                    CreatedUtc DATETIMEOFFSET NOT NULL
                );
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_UserId_CreatedUtc' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
            BEGIN
                CREATE INDEX IX_AuditLogs_UserId_CreatedUtc ON dbo.AuditLogs(UserId, CreatedUtc);
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CreatedUtc' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
            BEGIN
                CREATE INDEX IX_AuditLogs_CreatedUtc ON dbo.AuditLogs(CreatedUtc);
            END;
            IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CorrelationId' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
            BEGIN
                CREATE INDEX IX_AuditLogs_CorrelationId ON dbo.AuditLogs(CorrelationId);
            END;
            """);
        return;
    }

    if (dbContext.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
    {
        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "AuditLogs"
            (
                "Id" uuid NOT NULL PRIMARY KEY,
                "UserId" character varying(200) NOT NULL,
                "Action" character varying(200) NOT NULL,
                "Method" character varying(12) NOT NULL,
                "Path" character varying(1000) NOT NULL,
                "StatusCode" integer NOT NULL,
                "IpAddress" character varying(100) NOT NULL,
                "CorrelationId" character varying(100) NOT NULL,
                "CreatedUtc" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId_CreatedUtc" ON "AuditLogs"("UserId", "CreatedUtc");
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedUtc" ON "AuditLogs"("CreatedUtc");
            CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CorrelationId" ON "AuditLogs"("CorrelationId");
            """);
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

static AuthorizationPolicy BuildZmsRolePolicy(IConfiguration configuration, params string[] allowedRoles)
{
    return new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .RequireAssertion(context => IsInZmsRole(context.User, configuration, allowedRoles))
        .Build();
}

static bool IsInZmsRole(ClaimsPrincipal user, IConfiguration configuration, params string[] allowedRoles)
{
    if (!configuration.GetValue<bool>("Authorization:EnforceRoles"))
    {
        return user.Identity?.IsAuthenticated == true;
    }

    var roles = GetZmsRoles(user);
    return roles.Any(role => allowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
}

static HashSet<string> GetZmsRoles(ClaimsPrincipal user)
{
    var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var claim in user.Claims)
    {
        if (IsRoleClaimType(claim.Type))
        {
            AddRoleValuesFromText(roles, claim.Value);
        }
    }

    return roles;
}

static bool IsRoleClaimType(string claimType)
{
    return claimType.Equals(ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("role", StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("roles", StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("app_metadata", StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("user_metadata", StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("app_metadata.role", StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("app_metadata.roles", StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("user_metadata.role", StringComparison.OrdinalIgnoreCase)
        || claimType.Equals("user_metadata.roles", StringComparison.OrdinalIgnoreCase)
        || claimType.EndsWith("/role", StringComparison.OrdinalIgnoreCase)
        || claimType.EndsWith("/roles", StringComparison.OrdinalIgnoreCase);
}

static void AddRoleValuesFromText(HashSet<string> roles, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    var trimmed = value.Trim();
    if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
    {
        try
        {
            using var document = JsonDocument.Parse(trimmed);
            AddRoleValuesFromJson(roles, document.RootElement);
            return;
        }
        catch (JsonException)
        {
            // Fall through to delimiter parsing.
        }
    }

    foreach (var role in trimmed.Split([',', ';', ' '], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
    {
        var normalized = NormalizeZmsRole(role);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            roles.Add(normalized);
        }
    }
}

static void AddRoleValuesFromJson(HashSet<string> roles, JsonElement element)
{
    switch (element.ValueKind)
    {
        case JsonValueKind.String:
            AddRoleValuesFromText(roles, element.GetString());
            break;
        case JsonValueKind.Array:
            foreach (var item in element.EnumerateArray())
            {
                AddRoleValuesFromJson(roles, item);
            }
            break;
        case JsonValueKind.Object:
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Contains("role", StringComparison.OrdinalIgnoreCase))
                {
                    AddRoleValuesFromJson(roles, property.Value);
                }
            }
            break;
    }
}

static string? NormalizeZmsRole(string role)
{
    return role.Trim().ToLowerInvariant() switch
    {
        "admin" or "administrator" or "zms_admin" => ZmsRoles.Admin,
        "operator" or "migration_operator" or "zms_operator" => ZmsRoles.Operator,
        "viewer" or "reader" or "readonly" or "read_only" or "zms_viewer" => ZmsRoles.Viewer,
        _ => role.Trim()
    };
}
