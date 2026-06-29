using System.Text.Json;
using Microsoft.Extensions.Logging;
using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public sealed class SummaryReportService(ILogger<SummaryReportService> logger) : ISummaryReportService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task WriteSummaryAsync(string outputPath, GenerationSummary summary, CancellationToken cancellationToken)
    {
        var reportDir = Path.Combine(outputPath, "_reports");
        Directory.CreateDirectory(reportDir);

        var jsonPath = Path.Combine(reportDir, "generation-summary.json");
        await using var stream = new FileStream(jsonPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, summary, JsonOptions, cancellationToken);

        var textPath = Path.Combine(reportDir, "generation-summary.txt");
        await File.WriteAllTextAsync(textPath, BuildTextReport(summary), cancellationToken);

        logger.LogInformation("Summary reports written to {ReportDirectory}", reportDir);
    }

    public void PrintSummary(GenerationSummary summary)
    {
        Console.WriteLine("=== Generation Summary ===");
        Console.WriteLine($"Total Files      : {summary.TotalFiles:N0}");
        Console.WriteLine($"Total Size       : {summary.TotalSizeFormatted}");
        Console.WriteLine($"Folder Count     : {summary.FolderCount:N0}");
        Console.WriteLine($"Average Size     : {summary.AverageSizeFormatted}");
        Console.WriteLine($"Largest File     : {summary.LargestFile?.RelativePath ?? "N/A"} ({summary.LargestFileSizeFormatted})");
        Console.WriteLine($"Deepest Folder   : Depth {summary.DeepestFolderDepth} - {summary.DeepestFolderPath}");
        Console.WriteLine($"Elapsed Time     : {summary.ElapsedTime:hh\\:mm\\:ss}");

        PrintDistribution("By Department", summary.FilesByDepartment);
        PrintDistribution("By Extension", summary.FilesByExtension);
        PrintDistribution("By Permission", summary.FilesByPermission);
        PrintDistribution("By Edge Case", summary.FilesByEdgeCase);
        PrintDistribution("By Permission Issue", summary.FilesByPermissionIssue);
        Console.WriteLine();
    }

    private static void PrintDistribution(string title, Dictionary<string, int> values)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        foreach (var pair in values.OrderByDescending(p => p.Value))
            Console.WriteLine($"  {pair.Key,-20} {pair.Value,10:N0}");
    }

    private static string BuildTextReport(GenerationSummary summary)
    {
        var lines = new List<string>
        {
            "ZMS Test Data Generator - Summary Report",
            $"Generated At: {DateTime.UtcNow:O}",
            "",
            $"Total Files: {summary.TotalFiles}",
            $"Total Size: {summary.TotalSizeFormatted} ({summary.TotalSizeBytes} bytes)",
            $"Folder Count: {summary.FolderCount}",
            $"Average Size: {summary.AverageSizeFormatted}",
            $"Largest File: {summary.LargestFile?.RelativePath} ({summary.LargestFileSizeFormatted})",
            $"Deepest Folder: Depth {summary.DeepestFolderDepth} - {summary.DeepestFolderPath}",
            $"Elapsed Time: {summary.ElapsedTime}"
        };

        AppendDistribution(lines, "Files By Department", summary.FilesByDepartment);
        AppendDistribution(lines, "Files By Extension", summary.FilesByExtension);
        AppendDistribution(lines, "Files By Permission", summary.FilesByPermission);
        AppendDistribution(lines, "Files By Edge Case", summary.FilesByEdgeCase);
        AppendDistribution(lines, "Files By Permission Issue", summary.FilesByPermissionIssue);

        return string.Join(Environment.NewLine, lines);
    }

    private static void AppendDistribution(List<string> lines, string title, Dictionary<string, int> values)
    {
        lines.Add("");
        lines.Add(title);
        foreach (var pair in values.OrderByDescending(p => p.Value))
            lines.Add($"  {pair.Key}: {pair.Value}");
    }
}
