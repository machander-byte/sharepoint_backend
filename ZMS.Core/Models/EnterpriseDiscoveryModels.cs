using ZMS.Core.Enums;

namespace ZMS.Core.Models;

public class DiscoveryRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? ProjectId { get; set; }
    public Guid? ConnectionId { get; set; }
    public string SourceType { get; set; } = "SharePointOnline";
    public string Status { get; set; } = "completed";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public int TotalSites { get; set; }
    public int TotalWebs { get; set; }
    public int TotalLibraries { get; set; }
    public int TotalLists { get; set; }
    public int TotalFolders { get; set; }
    public int TotalFiles { get; set; }
    public int TotalPermissions { get; set; }
    public int TotalSharingLinks { get; set; }
    public int TotalRiskFindings { get; set; }
    public int ReadinessScore { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class DiscoveredSite
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int FolderCount { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public class DiscoveredWeb
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public Guid? SiteId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class DiscoveredLibrary
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? WebId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = "Document Library";
    public string Url { get; set; } = string.Empty;
    public int FileCount { get; set; }
    public int FolderCount { get; set; }
    public long SizeBytes { get; set; }
    public bool BrokenInheritance { get; set; }
}

public class DiscoveredListEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public Guid? SiteId { get; set; }
    public Guid? WebId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ItemCount { get; set; }
}

public class DiscoveredFolderEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public Guid? LibraryId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int Depth { get; set; }
    public int FileCount { get; set; }
    public long SizeBytes { get; set; }
    public bool Archived { get; set; }
    public bool LongPathRisk { get; set; }
    public bool DuplicateIndicator { get; set; }
}

public class DiscoveredFileEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public Guid? LibraryId { get; set; }
    public Guid? FolderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public bool LargeFileRisk { get; set; }
    public bool LongPathRisk { get; set; }
    public bool DuplicateIndicator { get; set; }
}

public class DiscoveredPermission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public string Site { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string Principal { get; set; } = string.Empty;
    public string PrincipalType { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool HasBrokenInheritance { get; set; }
    public bool IsExternal { get; set; }
    public bool IsBroadAccess { get; set; }
}

public class DiscoveredSharingLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string LinkType { get; set; } = string.Empty;
    public bool AllowsAnonymousAccess { get; set; }
    public bool AllowsExternalAccess { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}

public class DiscoveredMetadataFieldEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public Guid? LibraryId { get; set; }
    public string Site { get; set; } = string.Empty;
    public string Library { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FieldType { get; set; } = "Text";
    public bool Required { get; set; }
    public int MissingValueCount { get; set; }
    public string MappingRisk { get; set; } = "Low";
}

public class DiscoveredContentType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public Guid? LibraryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}

public class RiskFinding
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscoveryRunId { get; set; }
    public string SourceFindingId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public EnterpriseSeverity Severity { get; set; } = EnterpriseSeverity.Low;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
