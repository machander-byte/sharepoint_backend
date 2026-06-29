using Microsoft.Extensions.DependencyInjection;
using ZMS.TestDataGenerator.Models;
using ZMS.TestDataGenerator.Services;

namespace ZMS.TestDataGenerator.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTestDataGeneratorServices(this IServiceCollection services)
    {
        services.AddSingleton<IFolderStructureService, FolderStructureService>();
        services.AddSingleton<IMetadataGenerator, MetadataGenerator>();
        services.AddSingleton<IFileContentGenerator, FileContentGenerator>();
        services.AddSingleton<IProgressReporter, ConsoleProgressReporter>();
        services.AddSingleton<ISummaryReportService, SummaryReportService>();
        services.AddSingleton<IDataGeneratorService, DataGeneratorService>();

        return services;
    }

    public static IServiceCollection ConfigureGenerationOptions(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<GenerationOptions>(configuration.GetSection(GenerationOptions.SectionName));
        return services;
    }
}
