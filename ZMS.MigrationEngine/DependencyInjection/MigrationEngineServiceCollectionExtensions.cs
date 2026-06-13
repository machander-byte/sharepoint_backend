using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZMS.Core.Interfaces;
using ZMS.Core.Options;
using ZMS.MigrationEngine.Processing;

namespace ZMS.MigrationEngine.DependencyInjection;

public static class MigrationEngineServiceCollectionExtensions
{
    public static IServiceCollection AddZmsMigrationEngine(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MigrationEngineOptions>(configuration.GetSection(MigrationEngineOptions.SectionName));
        services.Configure<QueueOptions>(configuration.GetSection(QueueOptions.SectionName));
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();
        services.AddSingleton<IJobLeaseService, JobLeaseService>();
        services.AddSingleton<IMigrationJobQueue>(serviceProvider =>
        {
            var leaseService = serviceProvider.GetRequiredService<IJobLeaseService>();
            var provider = configuration["QueueProvider"]
                ?? configuration[$"{QueueOptions.SectionName}:Provider"]
                ?? "Local";

            if (provider.Equals("Local", StringComparison.OrdinalIgnoreCase)
                || provider.Equals("Database", StringComparison.OrdinalIgnoreCase))
            {
                return new InMemoryEnterpriseJobQueue(leaseService);
            }

            if (provider.Equals("AzureServiceBus", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration[$"{QueueOptions.SectionName}:ConnectionString"];
                var message = string.IsNullOrWhiteSpace(connectionString)
                    ? "Azure Service Bus queue provider is selected but Queue:ConnectionString is not configured."
                    : "Azure Service Bus queue provider is configuration-ready, but the adapter package is not enabled in this build.";
                return new NotConfiguredMigrationJobQueue("AzureServiceBus", message);
            }

            if (provider.Equals("RabbitMQ", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration[$"{QueueOptions.SectionName}:ConnectionString"];
                var message = string.IsNullOrWhiteSpace(connectionString)
                    ? "RabbitMQ queue provider is selected but Queue:ConnectionString is not configured."
                    : "RabbitMQ queue provider is configuration-ready, but the adapter package is not enabled in this build.";
                return new NotConfiguredMigrationJobQueue("RabbitMQ", message);
            }

            throw new InvalidOperationException($"Unsupported QueueProvider '{provider}'. Use Local, Database, AzureServiceBus, or RabbitMQ.");
        });
        services.AddSingleton<IQueueDiagnostics>(serviceProvider =>
            (IQueueDiagnostics)serviceProvider.GetRequiredService<IMigrationJobQueue>());
        services.AddSingleton<IJobCheckpointService, InMemoryJobCheckpointService>();
        services.AddSingleton<MigrationJobProcessor>();
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<MigrationJobProcessor>());

        return services;
    }
}
