namespace ZMS.Core.Models;

public class FolderItem
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public DateTimeOffset ModifiedUtc { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
