using Microsoft.Extensions.DependencyInjection;
using ZMS.Connectors.FileShare.Connectors;
using ZMS.Core.Interfaces;

namespace ZMS.Connectors.FileShare.DependencyInjection;

public static class FileShareConnectorExtensions
{
    public static IServiceCollection AddFileShareConnector(this IServiceCollection services)
    {
        services.AddScoped<ISourceConnector, FileShareSourceConnector>();
        return services;
    }
}
