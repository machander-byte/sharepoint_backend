using Microsoft.EntityFrameworkCore;
using ZMS.Infrastructure.Persistence;

namespace ZMS.API.Diagnostics;

public sealed class DatabaseSchemaReadinessChecker
{
    private static readonly string[] RequiredTables =
    [
        "Connections",
        "MigrationJobs",
        "MigrationItems",
        "Logs",
        "DataProtectionKeys",
        "DiscoveryRuns",
        "DiscoveredSites",
        "DiscoveredWebs",
        "DiscoveredLibraries",
        "DiscoveredLists",
        "DiscoveredFolders",
        "DiscoveredFiles",
        "DiscoveredPermissions",
        "DiscoveredSharingLinks",
        "DiscoveredMetadataFields",
        "DiscoveredContentTypes",
        "RiskFindings",
        "MigrationJobEvents",
        "ValidationRuns",
        "ValidationFindings",
        "ValidationItemResults",
        "AuditLogs"
    ];

    private readonly ZmsDbContext dbContext;
    private readonly IConfiguration configuration;

    public DatabaseSchemaReadinessChecker(ZmsDbContext dbContext, IConfiguration configuration)
    {
        this.dbContext = dbContext;
        this.configuration = configuration;
    }

    public async Task<DatabaseSchemaReadinessSnapshot> CheckAsync(CancellationToken cancellationToken)
    {
        var provider = dbContext.Database.ProviderName ?? "unknown";

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(configuration.GetValue<int?>("Database:SchemaReadinessTimeoutSeconds") ?? 5));

            var existingTables = await GetExistingTablesAsync(provider, timeoutCts.Token);
            var missingTables = RequiredTables
                .Where(table => !existingTables.Contains(table))
                .Order(StringComparer.Ordinal)
                .ToArray();

            if (missingTables.Length == 0)
            {
                return new DatabaseSchemaReadinessSnapshot(
                    Ready: true,
                    Status: "Ready",
                    Provider: provider,
                    Message: "Required database schema is present.",
                    MissingTables: [],
                    ErrorType: null,
                    LastCheckedUtc: DateTimeOffset.UtcNow);
            }

            return new DatabaseSchemaReadinessSnapshot(
                Ready: false,
                Status: "MissingRequiredTables",
                Provider: provider,
                Message: "Required database schema is incomplete.",
                MissingTables: missingTables,
                ErrorType: null,
                LastCheckedUtc: DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new DatabaseSchemaReadinessSnapshot(
                Ready: false,
                Status: "TimedOut",
                Provider: provider,
                Message: "Schema readiness check timed out.",
                MissingTables: [],
                ErrorType: "TimeoutException",
                LastCheckedUtc: DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            return new DatabaseSchemaReadinessSnapshot(
                Ready: false,
                Status: "Failed",
                Provider: provider,
                Message: "Schema readiness check failed.",
                MissingTables: [],
                ErrorType: ex.GetType().Name,
                LastCheckedUtc: DateTimeOffset.UtcNow);
        }
    }

    private async Task<HashSet<string>> GetExistingTablesAsync(string provider, CancellationToken cancellationToken)
    {
        if (provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return await QueryExistingTablesAsync(
                """
                SELECT table_name
                FROM (
                    VALUES
                        ('Connections', to_regclass('public."Connections"')),
                        ('MigrationJobs', to_regclass('public."MigrationJobs"')),
                        ('MigrationItems', to_regclass('public."MigrationItems"')),
                        ('Logs', to_regclass('public."Logs"')),
                        ('DataProtectionKeys', to_regclass('public."DataProtectionKeys"')),
                        ('DiscoveryRuns', to_regclass('public."DiscoveryRuns"')),
                        ('DiscoveredSites', to_regclass('public."DiscoveredSites"')),
                        ('DiscoveredWebs', to_regclass('public."DiscoveredWebs"')),
                        ('DiscoveredLibraries', to_regclass('public."DiscoveredLibraries"')),
                        ('DiscoveredLists', to_regclass('public."DiscoveredLists"')),
                        ('DiscoveredFolders', to_regclass('public."DiscoveredFolders"')),
                        ('DiscoveredFiles', to_regclass('public."DiscoveredFiles"')),
                        ('DiscoveredPermissions', to_regclass('public."DiscoveredPermissions"')),
                        ('DiscoveredSharingLinks', to_regclass('public."DiscoveredSharingLinks"')),
                        ('DiscoveredMetadataFields', to_regclass('public."DiscoveredMetadataFields"')),
                        ('DiscoveredContentTypes', to_regclass('public."DiscoveredContentTypes"')),
                        ('RiskFindings', to_regclass('public."RiskFindings"')),
                        ('MigrationJobEvents', to_regclass('public."MigrationJobEvents"')),
                        ('ValidationRuns', to_regclass('public."ValidationRuns"')),
                        ('ValidationFindings', to_regclass('public."ValidationFindings"')),
                        ('ValidationItemResults', to_regclass('public."ValidationItemResults"')),
                        ('AuditLogs', to_regclass('public."AuditLogs"'))
                ) AS required(table_name, object_id)
                WHERE object_id IS NOT NULL;
                """,
                cancellationToken);
        }

        if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            return await QueryExistingTablesAsync(
                """
                SELECT name
                FROM sqlite_master
                WHERE type = 'table'
                  AND name IN ('Connections','MigrationJobs','MigrationItems','Logs','DataProtectionKeys','DiscoveryRuns','DiscoveredSites','DiscoveredWebs','DiscoveredLibraries','DiscoveredLists','DiscoveredFolders','DiscoveredFiles','DiscoveredPermissions','DiscoveredSharingLinks','DiscoveredMetadataFields','DiscoveredContentTypes','RiskFindings','MigrationJobEvents','ValidationRuns','ValidationFindings','ValidationItemResults','AuditLogs');
                """,
                cancellationToken);
        }

        return await QueryExistingTablesAsync(
            """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME IN ('Connections','MigrationJobs','MigrationItems','Logs','DataProtectionKeys','DiscoveryRuns','DiscoveredSites','DiscoveredWebs','DiscoveredLibraries','DiscoveredLists','DiscoveredFolders','DiscoveredFiles','DiscoveredPermissions','DiscoveredSharingLinks','DiscoveredMetadataFields','DiscoveredContentTypes','RiskFindings','MigrationJobEvents','ValidationRuns','ValidationFindings','ValidationItemResults','AuditLogs');
            """,
            cancellationToken);
    }

    private async Task<HashSet<string>> QueryExistingTablesAsync(string sql, CancellationToken cancellationToken)
    {
        var existingTables = new HashSet<string>(StringComparer.Ordinal);
        var connection = dbContext.Database.GetDbConnection();
        var closeConnection = connection.State != System.Data.ConnectionState.Open;

        if (closeConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = configuration.GetValue<int?>("Database:SchemaReadinessTimeoutSeconds") ?? 5;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                existingTables.Add(reader.GetString(0));
            }
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }

        return existingTables;
    }
}

public sealed record DatabaseSchemaReadinessSnapshot(
    bool Ready,
    string Status,
    string Provider,
    string Message,
    string[] MissingTables,
    string? ErrorType,
    DateTimeOffset LastCheckedUtc);
