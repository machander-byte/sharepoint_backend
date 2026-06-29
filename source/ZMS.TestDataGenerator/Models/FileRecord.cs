namespace ZMS.TestDataGenerator.Models;

public sealed class FileRecord
{
    public required string RelativePath { get; init; }
    public required string FileName { get; init; }
    public required string Extension { get; init; }
    public required long SizeBytes { get; init; }
    public required int FolderDepth { get; init; }
    public required string Department { get; init; }
    public required string Owner { get; init; }
    public required string Classification { get; init; }
    public required string RetentionLabel { get; init; }
    public required string PermissionLevel { get; init; }
    public string? EdgeCase { get; init; }
    public string? PermissionIssue { get; init; }
    public string? DuplicateGroup { get; init; }
    public required DateTime CreatedDateUtc { get; init; }
    public required DateTime ModifiedDateUtc { get; init; }
}
