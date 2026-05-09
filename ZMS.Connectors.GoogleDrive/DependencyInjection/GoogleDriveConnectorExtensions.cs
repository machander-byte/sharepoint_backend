using Microsoft.Extensions.DependencyInjection;
using ZMS.Connectors.GoogleDrive.Connectors;
using ZMS.Connectors.GoogleDrive.Services;
using ZMS.Core.Interfaces;

namespace ZMS.Connectors.GoogleDrive.DependencyInjection;

public static class GoogleDriveConnectorExtensions
{
    public static IServiceCollection AddGoogleDriveConnector(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<GoogleDriveApiClient>();
        services.AddScoped<ISourceConnector, GoogleDriveSourceConnector>();
        return services;
    }
}
