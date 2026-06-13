using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace ZMS.Application.EnvironmentBridge;

public sealed class EnvironmentPackageGenerator : IEnvironmentPackageGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IEnvironmentConfigValidator _validator;
    private readonly IPowerShellScriptGenerator _scriptGenerator;
    private readonly IDocumentationGenerator _documentationGenerator;
    private readonly IReportTemplateGenerator _reportTemplateGenerator;
    private readonly IZipPackageService _zipPackageService;
    private readonly string _packagesDirectory;

    public EnvironmentPackageGenerator(
        IEnvironmentConfigValidator validator,
        IPowerShellScriptGenerator scriptGenerator,
        IDocumentationGenerator documentationGenerator,
        IReportTemplateGenerator reportTemplateGenerator,
        IZipPackageService zipPackageService,
        IHostEnvironment hostEnvironment)
    {
        _validator = validator;
        _scriptGenerator = scriptGenerator;
        _documentationGenerator = documentationGenerator;
        _reportTemplateGenerator = reportTemplateGenerator;
        _zipPackageService = zipPackageService;
        _packagesDirectory = Path.Combine(hostEnvironment.ContentRootPath, "App_Data", "generated-packages");
    }

    public async Task<GeneratedPackageResult> GenerateAsync(EnvironmentConfig config, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_packagesDirectory);

        var packageId = Guid.NewGuid().ToString("N");
        var packageDirectory = Path.Combine(_packagesDirectory, packageId);
        Directory.CreateDirectory(packageDirectory);

        var files = new List<string>();
        await WriteJsonAsync(Path.Combine(packageDirectory, "config", "zms-spo-environment.json"), config, cancellationToken);
        files.Add("config/zms-spo-environment.json");

        await WriteTextAsync(packageDirectory, "README.md", _documentationGenerator.GenerateReadme(config), cancellationToken);
        files.Add("README.md");

        foreach (var file in _scriptGenerator.GenerateScripts(config))
        {
            await WriteTextAsync(packageDirectory, file.Key, file.Value, cancellationToken);
            files.Add(NormalizePath(file.Key));
        }

        foreach (var file in _documentationGenerator.GenerateDocumentation(config))
        {
            await WriteTextAsync(packageDirectory, file.Key, file.Value, cancellationToken);
            files.Add(NormalizePath(file.Key));
        }

        foreach (var file in _reportTemplateGenerator.GenerateReportTemplates(config))
        {
            await WriteTextAsync(packageDirectory, file.Key, file.Value, cancellationToken);
            files.Add(NormalizePath(file.Key));
        }

        await WriteTextAsync(packageDirectory, "logs/.gitkeep", string.Empty, cancellationToken);
        files.Add("logs/.gitkeep");

        await WriteTextAsync(packageDirectory, "discovery-output/.gitkeep", string.Empty, cancellationToken);
        files.Add("discovery-output/.gitkeep");

        await WriteJsonAsync(Path.Combine(packageDirectory, "execution", "execution-plan.json"), GenerateExecutionPlan(config), cancellationToken);
        files.Add("execution/execution-plan.json");

        await WriteJsonAsync(Path.Combine(packageDirectory, "execution", "execution-status.json"), GenerateInitialExecutionStatus(), cancellationToken);
        files.Add("execution/execution-status.json");

        await WriteTextAsync(packageDirectory, "execution/preflight-report.md", GenerateInitialPreflightReport(config), cancellationToken);
        files.Add("execution/preflight-report.md");

        await WriteTextAsync(packageDirectory, "execution/dry-run-report.md", GenerateInitialDryRunReport(config), cancellationToken);
        files.Add("execution/dry-run-report.md");

        await WriteTextAsync(packageDirectory, "execution/runbook.md", GenerateRunbook(config), cancellationToken);
        files.Add("execution/runbook.md");

        var manifest = new PackageManifest
        {
            PackageId = packageId,
            GeneratedAt = DateTimeOffset.UtcNow,
            Files = [.. files],
            Summary = _validator.GetSummary(config)
        };
        await WriteJsonAsync(Path.Combine(packageDirectory, "manifest.json"), manifest, cancellationToken);

        var zipPath = Path.Combine(_packagesDirectory, $"{packageId}.zip");
        await _zipPackageService.CreateZipAsync(packageDirectory, zipPath, cancellationToken);

        return new GeneratedPackageResult
        {
            PackageId = packageId,
            Message = "Environment automation package generated successfully",
            Files = files,
            DownloadUrl = $"/api/environment-package/{packageId}/download"
        };
    }

    public async Task<PackageManifest?> GetManifestAsync(string packageId, CancellationToken cancellationToken = default)
    {
        if (!IsSafeId(packageId))
        {
            return null;
        }

        var manifestPath = Path.Combine(_packagesDirectory, packageId, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<PackageManifest>(stream, JsonOptions, cancellationToken);
    }

    public string? GetPackageZipPath(string packageId)
    {
        if (!IsSafeId(packageId))
        {
            return null;
        }

        var path = Path.Combine(_packagesDirectory, $"{packageId}.zip");
        return File.Exists(path) ? path : null;
    }

    private static async Task WriteTextAsync(string rootDirectory, string relativePath, string contents, CancellationToken cancellationToken)
    {
        var fullPath = GetSafePath(rootDirectory, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, contents, cancellationToken);
    }

    private static async Task WriteJsonAsync(string fullPath, object value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await using var stream = File.Create(fullPath);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
    }

    private static string GetSafePath(string rootDirectory, string relativePath)
    {
        var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(rootDirectory, normalizedRelativePath));
        var rootPath = Path.GetFullPath(rootDirectory);
        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid package file path.");
        }

        return fullPath;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static object GenerateExecutionPlan(EnvironmentConfig config)
    {
        return new
        {
            generatedAt = DateTimeOffset.UtcNow,
            safety = new
            {
                dryRunFirst = true,
                destructiveActionsIncluded = false,
                microsoftGraphDirectCallsIncluded = false,
                requiresExplicitRealExecutionConfirmation = true
            },
            steps = new[]
            {
                new
                {
                    order = 1,
                    name = "Check prerequisites",
                    script = "scripts/00-Check-Prerequisites.ps1",
                    description = "Validate local PowerShell, optional PnP module presence, config schema, URLs, permissions readiness, subsite warnings, and path risks.",
                    modifiesTenant = false,
                    supportsDryRun = false,
                    estimatedObjects = 0
                },
                new
                {
                    order = 2,
                    name = "Create Site Collections",
                    script = "scripts/01-Create-SiteCollections.ps1",
                    description = "Create missing SharePoint Online site collections and skip any that already exist.",
                    modifiesTenant = true,
                    supportsDryRun = true,
                    estimatedObjects = config.SiteCollections.Count
                },
                new
                {
                    order = 3,
                    name = "Create Subsites",
                    script = "scripts/02-Create-Subsites.ps1",
                    description = "Create missing subsites where tenant settings allow subsite creation.",
                    modifiesTenant = true,
                    supportsDryRun = true,
                    estimatedObjects = config.SiteCollections.Sum(site => site.Subsites.Count)
                },
                new
                {
                    order = 4,
                    name = "Create Libraries Lists Metadata",
                    script = "scripts/03-Create-Libraries-Lists-Metadata.ps1",
                    description = "Create missing libraries, lists, and metadata fields only.",
                    modifiesTenant = true,
                    supportsDryRun = true,
                    estimatedObjects = config.SiteCollections.Sum(site => site.Libraries.Count + site.Lists.Count + site.MetadataFields.Count)
                },
                new
                {
                    order = 5,
                    name = "Create Groups Permissions",
                    script = "scripts/04-Create-Groups-Permissions.ps1",
                    description = "Create missing SharePoint groups, assign configured permission levels, and apply only configured broken inheritance rules.",
                    modifiesTenant = true,
                    supportsDryRun = true,
                    estimatedObjects = config.SiteCollections.Sum(site => site.PermissionGroups.Count + site.PermissionRules.Count)
                },
                new
                {
                    order = 6,
                    name = "Create Folders And Sample Files",
                    script = "scripts/05-Create-Folders-And-SampleFiles.ps1",
                    description = "Ensure configured folders exist and upload small placeholder sample files only when not in dry-run mode.",
                    modifiesTenant = true,
                    supportsDryRun = true,
                    estimatedObjects = config.SiteCollections.Sum(site => site.Libraries.Sum(library => library.Folders.Count + (library.SampleFileCount > 0 ? 1 : 0)))
                },
                new
                {
                    order = 7,
                    name = "Apply Migration Edge Cases",
                    script = "scripts/06-Apply-Migration-EdgeCases.ps1",
                    description = "Document and apply safe migration edge-case structures without forcing invalid filename upload failures.",
                    modifiesTenant = true,
                    supportsDryRun = true,
                    estimatedObjects = config.SiteCollections.Sum(site => site.EdgeCases.Count + site.FolderStructures.Count)
                },
                new
                {
                    order = 8,
                    name = "Generate Inventory Report",
                    script = "scripts/07-Generate-InventoryReport.ps1",
                    description = "Generate expected inventory reports from config and optionally collect live tenant counts.",
                    modifiesTenant = false,
                    supportsDryRun = false,
                    estimatedObjects = config.SiteCollections.Count
                },
                new
                {
                    order = 9,
                    name = "Run Read-Only Discovery",
                    script = "scripts/11-Run-Discovery-ReadOnly.ps1",
                    description = "Collect live SharePoint discovery JSON/CSV output without creating, updating, deleting, uploading, or changing permissions.",
                    modifiesTenant = false,
                    supportsDryRun = false,
                    estimatedObjects = config.SiteCollections.Count
                }
            }
        };
    }

    private static object GenerateInitialExecutionStatus()
    {
        string[] stepNames =
        [
            "Check prerequisites",
            "Create Site Collections",
            "Create Subsites",
            "Create Libraries Lists Metadata",
            "Create Groups Permissions",
            "Create Folders And Sample Files",
            "Apply Migration Edge Cases",
            "Generate Inventory Report",
            "Run Read-Only Discovery"
        ];

        return new
        {
            status = "not_started",
            lastRunAt = (DateTimeOffset?)null,
            steps = stepNames.Select(name => new
            {
                name,
                status = "pending",
                created = 0,
                skipped = 0,
                failed = 0,
                message = string.Empty,
                lastRunAt = (DateTimeOffset?)null
            })
        };
    }

    private static string GenerateInitialPreflightReport(EnvironmentConfig config)
    {
        return $"""
        # zettalogixmigrationsuite Preflight Report

        Preflight has not been run yet.

        Run:

        ```powershell
        pwsh ./scripts/08-Run-Preflight.ps1
        ```

        Tenant: {config.TenantName}
        """;
    }

    private static string GenerateInitialDryRunReport(EnvironmentConfig config)
    {
        return $"""
        # zettalogixmigrationsuite Dry-Run Report

        Dry-run has not been run yet.

        Run:

        ```powershell
        pwsh ./scripts/09-Run-DryRun.ps1
        ```

        Tenant: {config.TenantName}
        """;
    }

    private static string GenerateRunbook(EnvironmentConfig config)
    {
        return $"""
        # zettalogixmigrationsuite Safe SharePoint Automation Runbook

        ## 1. What This Package Does

        This package prepares a repeatable SharePoint Online enterprise test environment for zettalogixmigrationsuite migration validation. It can create configured site collections, subsites, libraries, lists, metadata fields, SharePoint groups, permission rules, folders, sample files, and migration edge-case examples.

        ## 2. What This Package Does NOT Do

        - It does not execute automatically from the browser.
        - It does not call Microsoft Graph directly from the app.
        - It does not include client secrets.
        - It does not delete existing SharePoint content.
        - It does not remove site collection administrators or owners.
        - It does not create large files unless an admin explicitly passes `-CreateLargeFiles`.

        ## 3. Safety Rules

        - Run preflight before any dry-run or real execution.
        - Run dry-run before any real tenant change.
        - Review `execution/dry-run-report.md` and `execution/execution-status.json`.
        - Existing objects are skipped instead of overwritten or deleted.
        - Real execution requires manual script launch plus exact confirmation text.

        ## 4. Required Permissions

        - SharePoint Administrator or Global Administrator role for tenant-level site collection creation.
        - Approved PnP/Entra application client ID.
        - Delegated or app permissions appropriate for creating SharePoint sites, lists, libraries, groups, folders, and files.

        ## 5. How To Run Preflight

        ```powershell
        pwsh ./scripts/08-Run-Preflight.ps1
        ```

        This creates `execution/preflight-report.md` and does not create SharePoint objects.

        ## 6. How To Run Dry-Run

        ```powershell
        pwsh ./scripts/09-Run-DryRun.ps1
        ```

        This runs scripts `01` through `06` with `-DryRun` and does not create SharePoint objects.

        ## 7. How To Review Dry-Run Report

        Review:

        - `execution/dry-run-report.md`
        - `execution/execution-status.json`
        - `logs/zms-execution-YYYY-MM-DD.log`

        Confirm planned site collections, subsites, lists, libraries, fields, groups, folders, files, and edge cases match the approved tenant design.

        ## 8. How To Run Real Creation

        Only run real execution in a test tenant or approved SharePoint environment.

        ```powershell
        pwsh ./scripts/10-Run-All-Safe.ps1 -ClientId "YOUR-PNP-APP-CLIENT-ID"
        ```

        You must type this exact confirmation when prompted:

        ```text
        CREATE ZMS TEST ENVIRONMENT
        ```

        ## 9. How To Stop Safely

        Press `Ctrl+C`. Re-run the same script later; scripts are idempotent and skip existing objects. Review `execution/execution-status.json` to see which step last ran.

        ## 10. Read-Only Discovery After Environment Creation

        The discovery script is read-only. It connects to the configured SharePoint sites, reads webs, libraries, lists, metadata, files, permissions, and risk indicators, then writes importable output under `discovery-output/`.

        Recommended sequence:

        1. Run preflight.
        2. Run dry-run.
        3. Run real environment creation only after approval.
        4. Run read-only discovery.
        5. Import `discovery-output/scan-result.json` into ZMS.

        ```powershell
        pwsh ./scripts/11-Run-Discovery-ReadOnly.ps1 `
          -ConfigPath "./config/zms-spo-environment.json" `
          -ClientId "YOUR-PNP-APP-CLIENT-ID" `
          -OutputPath "./discovery-output" `
          -IncludeFiles `
          -IncludePermissions `
          -IncludeMetadata `
          -IncludeSubsites `
          -VerboseLogging
        ```

        ## 11. Troubleshooting Common Errors

        - PnP.PowerShell missing: install with `Install-Module PnP.PowerShell -Scope CurrentUser`.
        - Authentication failure: verify the approved app client ID and admin consent.
        - Subsite creation fails: subsite creation may be disabled in this tenant. Enable custom script/subsite capability or convert subsites into separate modern sites.
        - Permission errors: verify the runner is a SharePoint admin and site owner.
        - File upload errors: verify libraries and folders were created before uploading samples.

        ## Environment

        - Tenant: {config.TenantName}
        - Admin URL: {config.AdminUrl}
        - Root URL: {config.RootUrl}
        - Owner: {config.OwnerEmail}
        - Site collections: {config.SiteCollections.Count}
        """;
    }

    private static bool IsSafeId(string value)
    {
        return value.Length is >= 16 and <= 64 && value.All(char.IsLetterOrDigit);
    }
}
