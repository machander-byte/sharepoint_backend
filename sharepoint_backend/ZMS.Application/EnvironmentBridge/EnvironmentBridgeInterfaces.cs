namespace ZMS.Application.EnvironmentBridge;

public interface IEnvironmentConfigValidator
{
    ValidationResult Validate(EnvironmentConfig config);
    EnvironmentSummary GetSummary(EnvironmentConfig config);
}

public interface IEnvironmentConfigStorageService
{
    Task<SaveConfigResponse> SaveAsync(EnvironmentConfig config, CancellationToken cancellationToken = default);
    Task<EnvironmentConfig?> GetAsync(string configId, CancellationToken cancellationToken = default);
}

public interface IEnvironmentPackageGenerator
{
    Task<GeneratedPackageResult> GenerateAsync(EnvironmentConfig config, CancellationToken cancellationToken = default);
    Task<PackageManifest?> GetManifestAsync(string packageId, CancellationToken cancellationToken = default);
    string? GetPackageZipPath(string packageId);
}

public interface IPowerShellScriptGenerator
{
    IReadOnlyDictionary<string, string> GenerateScripts(EnvironmentConfig config);
}

public interface IDocumentationGenerator
{
    IReadOnlyDictionary<string, string> GenerateDocumentation(EnvironmentConfig config);
    string GenerateReadme(EnvironmentConfig config);
}

public interface IReportTemplateGenerator
{
    IReadOnlyDictionary<string, string> GenerateReportTemplates(EnvironmentConfig config);
}

public interface IZipPackageService
{
    Task CreateZipAsync(string sourceDirectory, string zipPath, CancellationToken cancellationToken = default);
}
