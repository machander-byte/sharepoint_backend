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
        services.AddSingleton<IJobQueue, InMemoryJobQueue>();
        services.AddSingleton<MigrationJobProcessor>();
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<MigrationJobProcessor>());

        return services;
    }
}
