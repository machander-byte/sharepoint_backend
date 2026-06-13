using ZMS.Application.Contracts;
using ZMS.Application.Discovery;
using ZMS.Application.EnvironmentBridge;
using ZMS.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ZMS.Core.Interfaces;
using ZMS.Core.Options;

namespace ZMS.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddZmsApplication(this IServiceCollection services, IConfiguration? configuration = null)
    {
        if (configuration is not null)
        {
            services.Configure<DiscoveryOptions>(configuration.GetSection(DiscoveryOptions.SectionName));
        }

        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddScoped<ConnectorResolver>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IDiscoveryService, DiscoveryService>();
        services.AddScoped<IMigrationService, MigrationService>();
        services.AddScoped<IEnterpriseJobStateMachine, EnterpriseJobStateMachine>();
        services.AddScoped<IValidationService, ValidationService>();
        services.AddHttpClient<IOllamaClient, OllamaClient>();
        services.AddScoped<IAiAdvisorService, AiAdvisorService>();
        services.AddScoped<ICopilotReadinessService, CopilotReadinessService>();
        services.AddScoped<IEnterprisePlanningService, EnterprisePlanningService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReadinessAnalysisService, ReadinessAnalysisService>();
        services.AddScoped<IReadinessStorageService, ReadinessStorageService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<IRemediationPlanner, RemediationPlanner>();
        services.AddScoped<IMigrationWavePlanner, MigrationWavePlanner>();
        services.AddScoped<IModernizationOpportunityDetector, ModernizationOpportunityDetector>();
        services.AddScoped<IReadinessExportService, ReadinessExportService>();
        services.AddScoped<IMigrationPlanService, MigrationPlanService>();
        services.AddScoped<IMigrationPlanStorageService, MigrationPlanStorageService>();
        services.AddScoped<IMigrationPlanGenerator, MigrationPlanGenerator>();
        services.AddScoped<IMigrationPlanValidator, MigrationPlanValidator>();
        services.AddScoped<IMigrationRunbookGenerator, MigrationRunbookGenerator>();
        services.AddScoped<IMigrationPlanExportService, MigrationPlanExportService>();
        services.AddScoped<IPreMigrationValidationService, PreMigrationValidationService>();
        services.AddScoped<IPreMigrationStorageService, PreMigrationStorageService>();
        services.AddScoped<IPreMigrationCheckEngine, PreMigrationCheckEngine>();
        services.AddScoped<IExecutionSimulationService, ExecutionSimulationService>();
        services.AddScoped<IExecutionEstimator, ExecutionEstimator>();
        services.AddScoped<IGoNoGoDecisionService, GoNoGoDecisionService>();
        services.AddScoped<IPreMigrationExportService, PreMigrationExportService>();
        services.AddScoped<IMigrationExecutionService, MigrationExecutionService>();
        services.AddScoped<IMigrationExecutionStorageService, MigrationExecutionStorageService>();
        services.AddScoped<IMigrationExecutionJobFactory, MigrationExecutionJobFactory>();
        services.AddScoped<IMigrationExecutionOrchestrator, MigrationExecutionOrchestrator>();
        services.AddScoped<IMigrationExecutionAdapter, MigrationSimulationAdapter>();
        services.AddScoped<IMigrationExecutionTimelineService, MigrationExecutionTimelineService>();
        services.AddScoped<IMigrationExecutionReportService, MigrationExecutionReportService>();
        services.AddScoped<ISharePointMigrationCapabilityService, SharePointMigrationCapabilityService>();
        services.AddScoped<IMigrationTransferPreviewService, MigrationTransferPreviewService>();
        services.AddScoped<ILivePilotMigrationService, LivePilotMigrationService>();
        services.AddScoped<ILivePilotSafetyGate, LivePilotSafetyGate>();
        services.AddScoped<ISharePointMigrationAdapter, SharePointMigrationAdapter>();
        services.AddScoped<ISharePointMigrationReportService, SharePointMigrationReportService>();
        services.AddScoped<SharePointMigrationStorage>();
        services.AddScoped<IWorkflowValidationService, WorkflowValidationService>();
        services.AddScoped<IWorkflowValidationStorageService, WorkflowValidationStorageService>();
        services.AddScoped<IWorkflowValidationReportService, WorkflowValidationReportService>();
        services.AddScoped<IDemoService, DemoService>();
        services.AddScoped<IDiscoveryStorageService, DiscoveryStorageService>();
        services.AddScoped<IConfigModeDiscoveryScanner, ConfigModeDiscoveryScanner>();
        services.AddHttpClient<ILiveSharePointDiscoveryScanner, LiveSharePointDiscoveryScanner>();
        services.AddScoped<IPermissionRiskAnalyzer, PermissionRiskAnalyzer>();
        services.AddScoped<IMetadataAnalyzer, MetadataAnalyzer>();
        services.AddScoped<IMigrationRiskAnalyzer, MigrationRiskAnalyzer>();
        services.AddScoped<IDiscoveryExportService, DiscoveryExportService>();
        services.AddScoped<IEnvironmentConfigValidator, EnvironmentConfigValidator>();
        services.AddScoped<IEnvironmentConfigStorageService, EnvironmentConfigStorageService>();
        services.AddScoped<IEnvironmentPackageGenerator, EnvironmentPackageGenerator>();
        services.AddScoped<IPowerShellScriptGenerator, PowerShellScriptGenerator>();
        services.AddScoped<IDocumentationGenerator, DocumentationGenerator>();
        services.AddScoped<IReportTemplateGenerator, ReportTemplateGenerator>();
        services.AddScoped<IZipPackageService, ZipPackageService>();

        return services;
    }
}
