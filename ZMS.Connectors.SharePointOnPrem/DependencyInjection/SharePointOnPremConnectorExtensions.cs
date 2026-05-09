using Microsoft.Extensions.DependencyInjection;
using ZMS.Connectors.SharePointOnPrem.Connectors;
using ZMS.Core.Interfaces;

namespace ZMS.Connectors.SharePointOnPrem.DependencyInjection;

public static class SharePointOnPremConnectorExtensions
{
    public static IServiceCollection AddSharePointOnPremConnector(this IServiceCollection services)
    {
        services.AddScoped<ISourceConnector, SharePointOnPremSourceConnector>();
        return services;
    }
}
