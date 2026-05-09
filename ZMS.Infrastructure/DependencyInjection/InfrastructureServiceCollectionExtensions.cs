using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using ZMS.Core.Interfaces;
using ZMS.Infrastructure.Persistence;
using ZMS.Infrastructure.Repositories;

namespace ZMS.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddZmsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration["Database:Provider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("ZmsDatabase");

        // Validate that connection string is provided for non-Sqlite providers
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                // Default to in-memory SQLite for development
                connectionString = "Data Source=:memory:";
            }
            else if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(databaseProvider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(databaseProvider, "Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Postgres database provider requires 'ConnectionStrings:ZmsDatabase' to be configured. " +
                    "Please set the environment variable 'ConnectionStrings__ZmsDatabase' with a valid Postgres connection string.");
            }
            else
            {
                throw new InvalidOperationException(
                    "SQL Server database provider requires 'ConnectionStrings:ZmsDatabase' to be configured. " +
                    "Please set the environment variable 'ConnectionStrings__ZmsDatabase' with a valid SQL Server connection string.");
            }
        }

        var looksLikePostgresConnection = connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase) && connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase);
        if (looksLikePostgresConnection
            && !IsPostgresProvider(databaseProvider)
            && !string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:ZmsDatabase looks like a Postgres connection string, but Database:Provider is not Postgres. " +
                "Set the environment variable 'Database__Provider' to 'Postgres'.");
        }

        services.AddDbContext<ZmsDbContext>(options =>
        {
            if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                EnsureSqliteDatabaseDirectory(connectionString);
                options.UseSqlite(connectionString);
                return;
            }

            if (IsPostgresProvider(databaseProvider))
            {
                options.UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure());
                return;
            }

            options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure());
        });

        services.AddScoped<IConnectionRepository, ConnectionRepository>();
        services.AddScoped<IMigrationJobRepository, MigrationJobRepository>();
        services.AddScoped<IMigrationItemRepository, MigrationItemRepository>();
        services.AddScoped<ILogRepository, LogRepository>();

        return services;
    }

    private static bool IsPostgresProvider(string databaseProvider)
    {
        return string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase)
            || string.Equals(databaseProvider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
            || string.Equals(databaseProvider, "Npgsql", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureSqliteDatabaseDirectory(string connectionString)
    {
        var connectionStringBuilder = new DbConnectionStringBuilder
        {
            ConnectionString = connectionString
        };

        if (!connectionStringBuilder.TryGetValue("Data Source", out var dataSourceValue)
            && !connectionStringBuilder.TryGetValue("DataSource", out dataSourceValue))
        {
            return;
        }

        var dataSource = dataSourceValue?.ToString();
        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }
    }
}
