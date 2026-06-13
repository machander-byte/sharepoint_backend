namespace ZMS.Application.Discovery;

public sealed class DiscoveryScanRequest
{
    public string ScanName { get; set; } = string.Empty;
    public string Mode { get; set; } = "config";
    public string TenantUrl { get; set; } = string.Empty;
    public string AdminUrl { get; set; } = string.Empty;
    public List<string> SiteUrls { get; set; } = [];
    public string ClientId { get; set; } = string.Empty;
    public bool IncludeFiles { get; set; } = true;
    public bool IncludePermissions { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
    public bool IncludeSubsites { get; set; } = true;
    public bool IncludeSharingLinks { get; set; } = true;
    public int? MaxDepth { get; set; }
    public int? MaxItems { get; set; }
    public string? EnvironmentConfigId { get; set; }
    public string? EnvironmentConfigPath { get; set; }
}

public sealed class StartDiscoveryScanResponse
{
    public string ScanId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public string Message { get; set; } = "Discovery scan started";
}

public sealed class DiscoveryImportResponse
{
    public string ScanId { get; set; } = string.Empty;
    public string Status { get; set; } = "completed";
    public string Message { get; set; } = "Discovery result imported successfully";
    public DiscoverySummary Summary { get; set; } = new();
}

public sealed class DiscoveryImportFolderRequest
{
    public string FolderPath { get; set; } = string.Empty;
}

public sealed class DiscoveryScanStatus
{
    public string ScanId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public int Progress { get; set; }
    public string CurrentStep { get; set; } = "Queued";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class DiscoveryScanResult
{
    public string ScanId { get; set; } = string.Empty;
    public string ScanName { get; set; } = string.Empty;
    public string Mode { get; set; } = "config";
    public string Status { get; set; } = "completed";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DiscoverySummary Summary { get; set; } = new();
    public List<DiscoveredSiteCollection> SiteCollections { get; set; } = [];
    public List<DiscoveredInventoryItem> InventoryItems { get; set; } = [];
    public List<PermissionRiskFinding> PermissionRisks { get; set; } = [];
    public List<MetadataFinding> MetadataFindings { get; set; } = [];
    public List<MigrationRiskFinding> MigrationRisks { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public int ThrottleCount { get; set; }
    public bool IsPartial { get; set; }
}

public sealed class DiscoverySummary
{
    public int SiteCollections { get; set; }
    public int Subsites { get; set; }
    public int Libraries { get; set; }
    public int Lists { get; set; }
    public int Files { get; set; }
    public int Folders { get; set; }
    public long TotalStorageBytes { get; set; }
    public int MetadataFields { get; set; }
    public int PermissionGroups { get; set; }
    public int BrokenInheritanceCount { get; set; }
    public int LongPathRisks { get; set; }
    public int LargeFileRisks { get; set; }
    public int MissingMetadataIssues { get; set; }
    public int ReadinessScore { get; set; }
}

public sealed class DiscoveredSiteCollection
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int FolderCount { get; set; }
    public long SizeBytes { get; set; }
    public List<DiscoveredSubsite> Subsites { get; set; } = [];
    public List<DiscoveredLibrary> Libraries { get; set; } = [];
    public List<DiscoveredList> Lists { get; set; } = [];
    public List<DiscoveredMetadataField> MetadataFields { get; set; } = [];
    public List<DiscoveredSharePointGroup> SharePointGroups { get; set; } = [];
    public List<DiscoveredPermissionEntry> Permissions { get; set; } = [];
    public List<MigrationRiskFinding> ConfiguredRisks { get; set; } = [];
}

public sealed class DiscoveredSubsite
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class DiscoveredLibrary
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "Document Library";
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int FolderCount { get; set; }
    public long SizeBytes { get; set; }
    public bool BrokenInheritance { get; set; }
    public bool HasArchivedFolders { get; set; }
    public List<string> ContentTypes { get; set; } = [];
    public List<DiscoveredMetadataField> MetadataFields { get; set; } = [];
    public List<DiscoveredPermissionEntry> Permissions { get; set; } = [];
    public List<DiscoveredFolder> Folders { get; set; } = [];
    public List<DiscoveredFile> Files { get; set; } = [];
}

public sealed class DiscoveredList
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public List<DiscoveredMetadataField> Fields { get; set; } = [];
}

public sealed class DiscoveredMetadataField
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = "Text";
    public bool Required { get; set; }
    public List<string> Choices { get; set; } = [];
    public string? DefaultValue { get; set; }
    public int MissingValueCount { get; set; }
    public string MappedTargetField { get; set; } = string.Empty;
    public string MappingRisk { get; set; } = "Low";
}

public sealed class DiscoveredPermissionEntry
{
    public string Site { get; set; } = string.Empty;
    public string LibraryOrFolder { get; set; } = string.Empty;
    public string InheritanceStatus { get; set; } = "Inherited";
    public List<string> Groups { get; set; } = [];
    public List<string> Users { get; set; } = [];
    public List<string> AccessLevels { get; set; } = [];
    public string RiskLevel { get; set; } = "Low";
    public string RecommendedAction { get; set; } = "No action required";
}

public sealed class DiscoveredSharePointGroup
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Users { get; set; } = [];
}

public sealed class DiscoveredFolder
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public bool LongPathRisk { get; set; }
    public bool DuplicateIndicator { get; set; }
    public int Depth { get; set; }
    public int FileCount { get; set; }
    public long SizeBytes { get; set; }
}

public sealed class DiscoveredFile
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string Url { get; set; } = string.Empty;
    public bool LargeFileRisk { get; set; }
    public bool LongPathRisk { get; set; }
    public bool DuplicateIndicator { get; set; }
}

public sealed class DiscoveredInventoryItem
{
    public string Id { get; set; } = string.Empty;
    public string SiteCollection { get; set; } = string.Empty;
    public string Subsite { get; set; } = string.Empty;
    public string Library { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public long SizeBytes { get; set; }
    public int MetadataCount { get; set; }
    public string PermissionStatus { get; set; } = "Inherited";
    public string RiskLevel { get; set; } = "Low";
    public string ReadinessStatus { get; set; } = "Ready";
}

public sealed class PermissionRiskFinding
{
    public string Id { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string LibraryOrFolder { get; set; } = string.Empty;
    public string InheritanceStatus { get; set; } = "Inherited";
    public List<string> Groups { get; set; } = [];
    public List<string> Users { get; set; } = [];
    public List<string> AccessLevels { get; set; } = [];
    public string RiskLevel { get; set; } = "Low";
    public string RecommendedAction { get; set; } = "No action required";
}

public sealed class MetadataFinding
{
    public string Id { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string Library { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string FieldType { get; set; } = "Text";
    public bool Required { get; set; }
    public int MissingValueCount { get; set; }
    public string MappedTargetField { get; set; } = string.Empty;
    public string MappingRisk { get; set; } = "Low";
}

public sealed class MigrationRiskFinding
{
    public string Id { get; set; } = string.Empty;
    public string RiskType { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string LibraryOrPath { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string RiskLevel { get; set; } = "Low";
    public string Description { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

public sealed class DiscoveryExportResult
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    public byte[] Content { get; set; } = [];
}
