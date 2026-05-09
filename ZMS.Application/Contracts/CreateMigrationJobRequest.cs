namespace ZMS.Application.Contracts;

public class CreateMigrationJobRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid SourceConnectionId { get; set; }
    public Guid TargetConnectionId { get; set; }
    public string? SourceLocation { get; set; }
    public string? SourceLibraryName { get; set; }
    public string TargetSiteUrl { get; set; } = string.Empty;
    public string TargetLibraryName { get; set; } = string.Empty;
    public string? TargetLibraryUrlSegment { get; set; }
    public string? TargetRootPath { get; set; }
    public bool PreserveMetadata { get; set; } = true;
    public int BatchSize { get; set; } = 20;
    public int MaxRetryCount { get; set; } = 3;
}
