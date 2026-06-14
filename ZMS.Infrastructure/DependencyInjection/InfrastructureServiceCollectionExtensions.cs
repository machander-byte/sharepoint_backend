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
        var databaseProvider = configuration["Database:Provider"] ?? "Postgres";
        var connectionString = configuration.GetConnectionString("ZmsDatabase");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                connectionString = "Data Source=:memory:";
            }
            else if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(databaseProvider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(databaseProvider, "Npgsql", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Supabase Postgres requires 'ConnectionStrings:ZmsDatabase' to be configured. " +
                    "Set 'ConnectionStrings__ZmsDatabase' to the Supabase pooler connection string.");
            }
            else
            {
                throw new InvalidOperationException(
                    $"Unsupported database provider '{databaseProvider}'. ZMS runtime is configured for Supabase Postgres.");
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

        if (!IsPostgresProvider(databaseProvider)
            && !string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported database provider '{databaseProvider}'. Use 'Postgres' for Supabase.");
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
                var commandTimeoutSeconds = configuration.GetValue<int?>("Database:CommandTimeoutSeconds") ?? 120;
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure();
                    npgsqlOptions.CommandTimeout(commandTimeoutSeconds);
                });
                return;
            }

            throw new InvalidOperationException(
                $"Unsupported database provider '{databaseProvider}'. Use 'Postgres' for Supabase.");
        });

        services.AddScoped<IConnectionRepository, ConnectionRepository>();
        services.AddScoped<IMigrationJobRepository, MigrationJobRepository>();
        services.AddScoped<IMigrationItemRepository, MigrationItemRepository>();
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IDiscoveryGraphRepository, DiscoveryGraphRepository>();
        services.AddScoped<IMigrationJobEventRepository, MigrationJobEventRepository>();
        services.AddScoped<IValidationRepository, ValidationRepository>();

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
