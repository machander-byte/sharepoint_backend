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
        var databaseProvider = configuration["Database:Provider"];
        var connectionString = configuration.GetConnectionString("ZmsDatabase")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ZMS;Trusted_Connection=True;TrustServerCertificate=True;";

        services.AddDbContext<ZmsDbContext>(options =>
        {
            if (string.Equals(databaseProvider, "Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                EnsureSqliteDatabaseDirectory(connectionString);
                options.UseSqlite(connectionString);
                return;
            }

            if (string.Equals(databaseProvider, "Postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(databaseProvider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
                || string.Equals(databaseProvider, "Npgsql", StringComparison.OrdinalIgnoreCase))
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
