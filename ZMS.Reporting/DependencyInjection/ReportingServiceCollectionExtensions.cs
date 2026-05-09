using Microsoft.Extensions.DependencyInjection;
using ZMS.Core.Interfaces;
using ZMS.Reporting.Services;

namespace ZMS.Reporting.DependencyInjection;

public static class ReportingServiceCollectionExtensions
{
    public static IServiceCollection AddZmsReporting(this IServiceCollection services)
    {
        services.AddScoped<IReportingService, ReportingService>();
        return services;
    }
}
