using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZMS.Infrastructure.Migrations;

[DbContext(typeof(Persistence.ZmsDbContext))]
[Migration("20260526160000_EnterpriseFoundationHardening")]
public partial class EnterpriseFoundationHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder.ActiveProvider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql(SqlServerEnterpriseSchema);
            return;
        }

        if (migrationBuilder.ActiveProvider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql(PostgresEnterpriseSchema);
            return;
        }

        if (migrationBuilder.ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            migrationBuilder.Sql(SqliteEnterpriseSchema);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive. Enterprise hardening migrations add tables/columns only.
    }

    private const string SqlServerEnterpriseSchema = """
        IF COL_LENGTH('MigrationJobs', 'EnterpriseState') IS NULL ALTER TABLE [MigrationJobs] ADD [EnterpriseState] nvarchar(50) NOT NULL CONSTRAINT [DF_MigrationJobs_EnterpriseState] DEFAULT 'CREATED';
        IF COL_LENGTH('MigrationJobs', 'FailureReason') IS NULL ALTER TABLE [MigrationJobs] ADD [FailureReason] nvarchar(2000) NULL;
        IF COL_LENGTH('MigrationJobs', 'RetryCount') IS NULL ALTER TABLE [MigrationJobs] ADD [RetryCount] int NOT NULL CONSTRAINT [DF_MigrationJobs_RetryCount] DEFAULT 0;
        IF COL_LENGTH('MigrationJobs', 'CorrelationId') IS NULL ALTER TABLE [MigrationJobs] ADD [CorrelationId] nvarchar(100) NULL;
        IF COL_LENGTH('MigrationJobs', 'LeaseId') IS NULL ALTER TABLE [MigrationJobs] ADD [LeaseId] nvarchar(100) NULL;
        IF COL_LENGTH('MigrationJobs', 'LeaseExpiresUtc') IS NULL ALTER TABLE [MigrationJobs] ADD [LeaseExpiresUtc] datetimeoffset NULL;
        IF COL_LENGTH('MigrationItems', 'EnterpriseState') IS NULL ALTER TABLE [MigrationItems] ADD [EnterpriseState] nvarchar(50) NOT NULL CONSTRAINT [DF_MigrationItems_EnterpriseState] DEFAULT 'PENDING';

        IF OBJECT_ID(N'[DiscoveryRuns]', N'U') IS NULL
        CREATE TABLE [DiscoveryRuns] (
            [Id] uniqueidentifier NOT NULL PRIMARY KEY,
            [Name] nvarchar(200) NOT NULL,
            [ProjectId] nvarchar(100) NULL,
            [ConnectionId] uniqueidentifier NULL,
            [SourceType] nvarchar(80) NOT NULL,
            [Status] nvarchar(50) NOT NULL,
            [StartedAt] datetimeoffset NOT NULL,
            [CompletedAt] datetimeoffset NULL,
            [TotalSites] int NOT NULL DEFAULT 0,
            [TotalWebs] int NOT NULL DEFAULT 0,
            [TotalLibraries] int NOT NULL DEFAULT 0,
            [TotalLists] int NOT NULL DEFAULT 0,
            [TotalFolders] int NOT NULL DEFAULT 0,
            [TotalFiles] int NOT NULL DEFAULT 0,
            [TotalPermissions] int NOT NULL DEFAULT 0,
            [TotalSharingLinks] int NOT NULL DEFAULT 0,
            [TotalRiskFindings] int NOT NULL DEFAULT 0,
            [ReadinessScore] int NOT NULL DEFAULT 0,
            [ErrorMessage] nvarchar(2000) NULL,
            [CreatedUtc] datetimeoffset NOT NULL DEFAULT SYSDATETIMEOFFSET()
        );

        IF OBJECT_ID(N'[DiscoveredSites]', N'U') IS NULL
        CREATE TABLE [DiscoveredSites] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [ExternalId] nvarchar(200) NOT NULL, [Title] nvarchar(300) NOT NULL, [Url] nvarchar(1000) NOT NULL, [Department] nvarchar(100) NOT NULL DEFAULT '', [Description] nvarchar(1000) NOT NULL DEFAULT '', [FileCount] int NOT NULL DEFAULT 0, [FolderCount] int NOT NULL DEFAULT 0, [SizeBytes] bigint NOT NULL DEFAULT 0, [CreatedAt] datetimeoffset NULL, [ModifiedAt] datetimeoffset NULL);
        IF OBJECT_ID(N'[DiscoveredWebs]', N'U') IS NULL
        CREATE TABLE [DiscoveredWebs] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [SiteId] uniqueidentifier NULL, [ExternalId] nvarchar(200) NOT NULL, [Title] nvarchar(300) NOT NULL, [Url] nvarchar(1000) NOT NULL, [Description] nvarchar(1000) NOT NULL DEFAULT '');
        IF OBJECT_ID(N'[DiscoveredLibraries]', N'U') IS NULL
        CREATE TABLE [DiscoveredLibraries] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [SiteId] uniqueidentifier NULL, [WebId] uniqueidentifier NULL, [ExternalId] nvarchar(200) NOT NULL, [Title] nvarchar(300) NOT NULL, [Type] nvarchar(100) NOT NULL DEFAULT '', [Url] nvarchar(1000) NOT NULL DEFAULT '', [FileCount] int NOT NULL DEFAULT 0, [FolderCount] int NOT NULL DEFAULT 0, [SizeBytes] bigint NOT NULL DEFAULT 0, [BrokenInheritance] bit NOT NULL DEFAULT 0);
        IF OBJECT_ID(N'[DiscoveredLists]', N'U') IS NULL
        CREATE TABLE [DiscoveredLists] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [SiteId] uniqueidentifier NULL, [WebId] uniqueidentifier NULL, [ExternalId] nvarchar(200) NOT NULL, [Title] nvarchar(300) NOT NULL, [Description] nvarchar(1000) NOT NULL DEFAULT '', [ItemCount] int NOT NULL DEFAULT 0);
        IF OBJECT_ID(N'[DiscoveredFolders]', N'U') IS NULL
        CREATE TABLE [DiscoveredFolders] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [LibraryId] uniqueidentifier NULL, [ExternalId] nvarchar(200) NOT NULL, [Name] nvarchar(300) NOT NULL, [Path] nvarchar(1500) NOT NULL, [Depth] int NOT NULL DEFAULT 0, [FileCount] int NOT NULL DEFAULT 0, [SizeBytes] bigint NOT NULL DEFAULT 0, [Archived] bit NOT NULL DEFAULT 0, [LongPathRisk] bit NOT NULL DEFAULT 0, [DuplicateIndicator] bit NOT NULL DEFAULT 0);
        IF OBJECT_ID(N'[DiscoveredFiles]', N'U') IS NULL
        CREATE TABLE [DiscoveredFiles] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [LibraryId] uniqueidentifier NULL, [FolderId] uniqueidentifier NULL, [Name] nvarchar(300) NOT NULL, [Path] nvarchar(1500) NOT NULL, [Url] nvarchar(1500) NOT NULL DEFAULT '', [SizeBytes] bigint NOT NULL DEFAULT 0, [CreatedAt] datetimeoffset NULL, [ModifiedAt] datetimeoffset NULL, [LargeFileRisk] bit NOT NULL DEFAULT 0, [LongPathRisk] bit NOT NULL DEFAULT 0, [DuplicateIndicator] bit NOT NULL DEFAULT 0);
        IF OBJECT_ID(N'[DiscoveredPermissions]', N'U') IS NULL
        CREATE TABLE [DiscoveredPermissions] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [Site] nvarchar(300) NOT NULL DEFAULT '', [Scope] nvarchar(1000) NOT NULL DEFAULT '', [Principal] nvarchar(300) NOT NULL DEFAULT '', [PrincipalType] nvarchar(80) NOT NULL DEFAULT '', [Role] nvarchar(120) NOT NULL DEFAULT '', [HasBrokenInheritance] bit NOT NULL DEFAULT 0, [IsExternal] bit NOT NULL DEFAULT 0, [IsBroadAccess] bit NOT NULL DEFAULT 0);
        IF OBJECT_ID(N'[DiscoveredSharingLinks]', N'U') IS NULL
        CREATE TABLE [DiscoveredSharingLinks] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [Scope] nvarchar(300) NOT NULL DEFAULT '', [Path] nvarchar(1500) NOT NULL DEFAULT '', [LinkType] nvarchar(80) NOT NULL DEFAULT '', [AllowsAnonymousAccess] bit NOT NULL DEFAULT 0, [AllowsExternalAccess] bit NOT NULL DEFAULT 0, [ExpiresAt] datetimeoffset NULL);
        IF OBJECT_ID(N'[DiscoveredMetadataFields]', N'U') IS NULL
        CREATE TABLE [DiscoveredMetadataFields] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [LibraryId] uniqueidentifier NULL, [Site] nvarchar(300) NOT NULL DEFAULT '', [Library] nvarchar(300) NOT NULL DEFAULT '', [Name] nvarchar(300) NOT NULL DEFAULT '', [FieldType] nvarchar(80) NOT NULL DEFAULT '', [Required] bit NOT NULL DEFAULT 0, [MissingValueCount] int NOT NULL DEFAULT 0, [MappingRisk] nvarchar(50) NOT NULL DEFAULT '');
        IF OBJECT_ID(N'[DiscoveredContentTypes]', N'U') IS NULL
        CREATE TABLE [DiscoveredContentTypes] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [LibraryId] uniqueidentifier NULL, [Name] nvarchar(300) NOT NULL DEFAULT '', [Scope] nvarchar(1000) NOT NULL DEFAULT '');
        IF OBJECT_ID(N'[RiskFindings]', N'U') IS NULL
        CREATE TABLE [RiskFindings] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [DiscoveryRunId] uniqueidentifier NOT NULL, [SourceFindingId] nvarchar(300) NOT NULL DEFAULT '', [Category] nvarchar(120) NOT NULL, [Severity] nvarchar(50) NOT NULL, [Title] nvarchar(300) NOT NULL, [Description] nvarchar(2000) NOT NULL DEFAULT '', [RecommendedAction] nvarchar(2000) NOT NULL DEFAULT '', [Site] nvarchar(300) NOT NULL DEFAULT '', [Location] nvarchar(1000) NOT NULL DEFAULT '', [Path] nvarchar(1500) NOT NULL DEFAULT '', [CreatedUtc] datetimeoffset NOT NULL DEFAULT SYSDATETIMEOFFSET());
        IF OBJECT_ID(N'[MigrationJobEvents]', N'U') IS NULL
        CREATE TABLE [MigrationJobEvents] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [JobId] uniqueidentifier NOT NULL, [EventType] nvarchar(120) NOT NULL, [PreviousState] nvarchar(50) NULL, [NewState] nvarchar(50) NOT NULL, [Message] nvarchar(2000) NOT NULL, [Severity] nvarchar(50) NOT NULL, [CreatedAt] datetimeoffset NOT NULL, [CorrelationId] nvarchar(100) NULL, [MetadataJson] nvarchar(max) NOT NULL DEFAULT '{}');
        IF OBJECT_ID(N'[ValidationRuns]', N'U') IS NULL
        CREATE TABLE [ValidationRuns] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [MigrationJobId] uniqueidentifier NOT NULL, [Status] nvarchar(50) NOT NULL, [StartedAt] datetimeoffset NOT NULL, [CompletedAt] datetimeoffset NULL, [SourceItemCount] int NOT NULL DEFAULT 0, [TargetItemCount] int NOT NULL DEFAULT 0, [PassedCount] int NOT NULL DEFAULT 0, [WarningCount] int NOT NULL DEFAULT 0, [FailedCount] int NOT NULL DEFAULT 0, [Summary] nvarchar(2000) NOT NULL DEFAULT '', [ErrorMessage] nvarchar(2000) NULL);
        IF OBJECT_ID(N'[ValidationFindings]', N'U') IS NULL
        CREATE TABLE [ValidationFindings] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [ValidationRunId] uniqueidentifier NOT NULL, [Severity] nvarchar(50) NOT NULL, [Category] nvarchar(120) NOT NULL DEFAULT '', [Message] nvarchar(2000) NOT NULL DEFAULT '', [SourcePath] nvarchar(1500) NOT NULL DEFAULT '', [TargetPath] nvarchar(1500) NOT NULL DEFAULT '', [RecommendedAction] nvarchar(2000) NOT NULL DEFAULT '');
        IF OBJECT_ID(N'[ValidationItemResults]', N'U') IS NULL
        CREATE TABLE [ValidationItemResults] ([Id] uniqueidentifier NOT NULL PRIMARY KEY, [ValidationRunId] uniqueidentifier NOT NULL, [MigrationItemId] uniqueidentifier NULL, [SourcePath] nvarchar(1500) NOT NULL DEFAULT '', [TargetPath] nvarchar(1500) NOT NULL DEFAULT '', [SourceSizeBytes] bigint NOT NULL DEFAULT 0, [TargetSizeBytes] bigint NOT NULL DEFAULT 0, [Status] nvarchar(50) NOT NULL DEFAULT '', [DifferenceType] nvarchar(120) NOT NULL DEFAULT '', [Message] nvarchar(2000) NOT NULL DEFAULT '');

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiscoveryRuns_Status' AND object_id = OBJECT_ID('DiscoveryRuns')) CREATE INDEX [IX_DiscoveryRuns_Status] ON [DiscoveryRuns]([Status]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiscoveryRuns_StartedAt' AND object_id = OBJECT_ID('DiscoveryRuns')) CREATE INDEX [IX_DiscoveryRuns_StartedAt] ON [DiscoveryRuns]([StartedAt]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiscoveryRuns_CompletedAt' AND object_id = OBJECT_ID('DiscoveryRuns')) CREATE INDEX [IX_DiscoveryRuns_CompletedAt] ON [DiscoveryRuns]([CompletedAt]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiscoveryRuns_ConnectionId' AND object_id = OBJECT_ID('DiscoveryRuns')) CREATE INDEX [IX_DiscoveryRuns_ConnectionId] ON [DiscoveryRuns]([ConnectionId]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiscoveredFiles_Path' AND object_id = OBJECT_ID('DiscoveredFiles')) CREATE INDEX [IX_DiscoveredFiles_Path] ON [DiscoveredFiles]([Path]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_DiscoveredFiles_Url' AND object_id = OBJECT_ID('DiscoveredFiles')) CREATE INDEX [IX_DiscoveredFiles_Url] ON [DiscoveredFiles]([Url]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RiskFindings_Category_Severity' AND object_id = OBJECT_ID('RiskFindings')) CREATE INDEX [IX_RiskFindings_Category_Severity] ON [RiskFindings]([Category], [Severity]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MigrationJobEvents_JobId_CreatedAt' AND object_id = OBJECT_ID('MigrationJobEvents')) CREATE INDEX [IX_MigrationJobEvents_JobId_CreatedAt] ON [MigrationJobEvents]([JobId], [CreatedAt]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ValidationRuns_MigrationJobId_StartedAt' AND object_id = OBJECT_ID('ValidationRuns')) CREATE INDEX [IX_ValidationRuns_MigrationJobId_StartedAt] ON [ValidationRuns]([MigrationJobId], [StartedAt]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ValidationFindings_ValidationRunId' AND object_id = OBJECT_ID('ValidationFindings')) CREATE INDEX [IX_ValidationFindings_ValidationRunId] ON [ValidationFindings]([ValidationRunId]);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ValidationItemResults_ValidationRunId' AND object_id = OBJECT_ID('ValidationItemResults')) CREATE INDEX [IX_ValidationItemResults_ValidationRunId] ON [ValidationItemResults]([ValidationRunId]);
        """;

    private const string PostgresEnterpriseSchema = """
        ALTER TABLE "MigrationJobs" ADD COLUMN IF NOT EXISTS "EnterpriseState" character varying(50) NOT NULL DEFAULT 'CREATED';
        ALTER TABLE "MigrationJobs" ADD COLUMN IF NOT EXISTS "FailureReason" character varying(2000) NULL;
        ALTER TABLE "MigrationJobs" ADD COLUMN IF NOT EXISTS "RetryCount" integer NOT NULL DEFAULT 0;
        ALTER TABLE "MigrationJobs" ADD COLUMN IF NOT EXISTS "CorrelationId" character varying(100) NULL;
        ALTER TABLE "MigrationJobs" ADD COLUMN IF NOT EXISTS "LeaseId" character varying(100) NULL;
        ALTER TABLE "MigrationJobs" ADD COLUMN IF NOT EXISTS "LeaseExpiresUtc" timestamp with time zone NULL;
        ALTER TABLE "MigrationItems" ADD COLUMN IF NOT EXISTS "EnterpriseState" character varying(50) NOT NULL DEFAULT 'PENDING';
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_Status" ON "DiscoveryRuns"("Status");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_StartedAt" ON "DiscoveryRuns"("StartedAt");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_CompletedAt" ON "DiscoveryRuns"("CompletedAt");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_ConnectionId" ON "DiscoveryRuns"("ConnectionId");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveredFiles_Path" ON "DiscoveredFiles"("Path");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveredFiles_Url" ON "DiscoveredFiles"("Url");
        CREATE INDEX IF NOT EXISTS "IX_RiskFindings_Category_Severity" ON "RiskFindings"("Category", "Severity");
        CREATE INDEX IF NOT EXISTS "IX_MigrationJobEvents_JobId_CreatedAt" ON "MigrationJobEvents"("JobId", "CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_ValidationRuns_MigrationJobId_StartedAt" ON "ValidationRuns"("MigrationJobId", "StartedAt");
        CREATE INDEX IF NOT EXISTS "IX_ValidationFindings_ValidationRunId" ON "ValidationFindings"("ValidationRunId");
        CREATE INDEX IF NOT EXISTS "IX_ValidationItemResults_ValidationRunId" ON "ValidationItemResults"("ValidationRunId");
        """;

    private const string SqliteEnterpriseSchema = """
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_Status" ON "DiscoveryRuns"("Status");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_StartedAt" ON "DiscoveryRuns"("StartedAt");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_CompletedAt" ON "DiscoveryRuns"("CompletedAt");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveryRuns_ConnectionId" ON "DiscoveryRuns"("ConnectionId");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveredFiles_Path" ON "DiscoveredFiles"("Path");
        CREATE INDEX IF NOT EXISTS "IX_DiscoveredFiles_Url" ON "DiscoveredFiles"("Url");
        CREATE INDEX IF NOT EXISTS "IX_RiskFindings_Category_Severity" ON "RiskFindings"("Category", "Severity");
        CREATE INDEX IF NOT EXISTS "IX_MigrationJobEvents_JobId_CreatedAt" ON "MigrationJobEvents"("JobId", "CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_ValidationRuns_MigrationJobId_StartedAt" ON "ValidationRuns"("MigrationJobId", "StartedAt");
        CREATE INDEX IF NOT EXISTS "IX_ValidationFindings_ValidationRunId" ON "ValidationFindings"("ValidationRunId");
        CREATE INDEX IF NOT EXISTS "IX_ValidationItemResults_ValidationRunId" ON "ValidationItemResults"("ValidationRunId");
        """;
}
