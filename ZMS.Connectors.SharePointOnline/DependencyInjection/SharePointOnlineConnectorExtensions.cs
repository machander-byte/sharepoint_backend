using Microsoft.Extensions.DependencyInjection;
using ZMS.Connectors.SharePointOnline.Connectors;
using ZMS.Connectors.SharePointOnline.Services;
using ZMS.Core.Interfaces;

namespace ZMS.Connectors.SharePointOnline.DependencyInjection;

public static class SharePointOnlineConnectorExtensions
{
    public static IServiceCollection AddSharePointOnlineConnector(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<SharePointGraphClient>();
        services.AddScoped<IFileTransferService, SharePointOnlineFileTransferService>();
        services.AddScoped<ISourceConnector, SharePointOnlineSourceConnector>();
        services.AddScoped<ITargetConnector, SharePointOnlineTargetConnector>();
        return services;
    }
}
