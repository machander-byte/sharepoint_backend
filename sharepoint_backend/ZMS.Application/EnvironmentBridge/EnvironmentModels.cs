namespace ZMS.Application.EnvironmentBridge;

public sealed class EnvironmentConfig
{
    public string TenantName { get; set; } = string.Empty;
    public string AdminUrl { get; set; } = string.Empty;
    public string RootUrl { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public string ClientIdPlaceholder { get; set; } = "PASTE-PNP-ENTRA-APP-CLIENT-ID-HERE";
    public DateTimeOffset GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public EnvironmentGlobalOptions GlobalOptions { get; set; } = new();
    public List<SiteCollectionConfig> SiteCollections { get; set; } = [];
}

public sealed class EnvironmentGlobalOptions
{
    public bool IncludeDefaultSubsites { get; set; }
    public bool GenerateSampleDocuments { get; set; }
    public bool IncludeMetadataColumns { get; set; }
    public bool CreatePermissionGroups { get; set; }
    public bool AddMigrationEdgeCases { get; set; }
    public bool IncludeArchivedFolders { get; set; }
    public bool IncludeLongPathExamples { get; set; }
    public bool IncludeLargeFilePlaceholders { get; set; }
}

public sealed class SiteCollectionConfig
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SubsiteConfig> Subsites { get; set; } = [];
    public List<LibraryConfig> Libraries { get; set; } = [];
    public List<CustomListConfig> Lists { get; set; } = [];
    public List<MetadataFieldConfig> MetadataFields { get; set; } = [];
    public List<PermissionGroupConfig> PermissionGroups { get; set; } = [];
    public List<PermissionRuleConfig> PermissionRules { get; set; } = [];
    public List<FolderStructureConfig> FolderStructures { get; set; } = [];
    public List<MigrationEdgeCaseConfig> EdgeCases { get; set; } = [];
}

public sealed class SubsiteConfig
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class LibraryConfig
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "Document Library";
    public string Description { get; set; } = string.Empty;
    public List<string> MetadataFieldIds { get; set; } = [];
    public List<FolderStructureConfig> Folders { get; set; } = [];
    public int SampleFileCount { get; set; }
    public bool IncludeVersioning { get; set; }
}

public sealed class CustomListConfig
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<MetadataFieldConfig> Columns { get; set; } = [];
    public int SampleItemCount { get; set; }
}

public sealed class MetadataFieldConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Text";
    public bool Required { get; set; }
    public List<string> Choices { get; set; } = [];
    public string? DefaultValue { get; set; }
}

public sealed class PermissionGroupConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = "Read";
    public List<string> Users { get; set; } = [];
}

public sealed class PermissionRuleConfig
{
    public string Id { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Inheritance { get; set; } = "Inherited";
    public List<string> Groups { get; set; } = [];
    public string Notes { get; set; } = string.Empty;
}

public sealed class FolderStructureConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public bool LongPathExample { get; set; }
    public bool LargeFilePlaceholder { get; set; }
    public List<FolderStructureConfig> Children { get; set; } = [];
}

public sealed class MigrationEdgeCaseConfig
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low";
    public string AffectedPath { get; set; } = string.Empty;
}

public sealed class EnvironmentSummary
{
    public int SiteCollections { get; set; }
    public int Subsites { get; set; }
    public int Libraries { get; set; }
    public int Lists { get; set; }
    public int MetadataFields { get; set; }
    public int PermissionGroups { get; set; }
    public int EdgeCases { get; set; }
}

public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public EnvironmentSummary Summary { get; set; } = new();
}

public sealed class SaveConfigResponse
{
    public string ConfigId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset SavedAt { get; set; }
}

public sealed class GeneratedPackageResult
{
    public string PackageId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string> Files { get; set; } = [];
    public string DownloadUrl { get; set; } = string.Empty;
}

public sealed class PackageManifest
{
    public string PackageId { get; set; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; set; }
    public List<string> Files { get; set; } = [];
    public EnvironmentSummary Summary { get; set; } = new();
}
