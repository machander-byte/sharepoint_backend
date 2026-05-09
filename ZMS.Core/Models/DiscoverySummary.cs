namespace ZMS.Core.Models;

public class DiscoverySummary
{
    public int SiteCount { get; set; }
    public int LibraryCount { get; set; }
    public int FileCount { get; set; }
    public long TotalBytes { get; set; }
}
