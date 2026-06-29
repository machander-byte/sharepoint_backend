namespace ZMS.TestDataGenerator.Models;

public sealed class GenerationSummary
{
    public int TotalFiles { get; init; }
    public long TotalSizeBytes { get; init; }
    public int FolderCount { get; init; }
    public double AverageSizeBytes { get; init; }
    public FileRecord? LargestFile { get; init; }
    public int DeepestFolderDepth { get; init; }
    public string DeepestFolderPath { get; init; } = string.Empty;
    public TimeSpan ElapsedTime { get; init; }
    public Dictionary<string, int> FilesByDepartment { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> FilesByExtension { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> FilesByPermission { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> FilesByEdgeCase { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> FilesByPermissionIssue { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public string TotalSizeFormatted => FormatBytes(TotalSizeBytes);
    public string AverageSizeFormatted => FormatBytes((long)AverageSizeBytes);
    public string LargestFileSizeFormatted => LargestFile is null ? "N/A" : FormatBytes(LargestFile.SizeBytes);

    public static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unitIndex = 0;

        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.##} {units[unitIndex]}";
    }
}
