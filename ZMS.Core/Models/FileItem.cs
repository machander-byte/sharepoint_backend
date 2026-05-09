namespace ZMS.Core.Models;

public class FileItem
{
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public DateTimeOffset ModifiedUtc { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
