using System.Text;

namespace ZMS.TestDataGenerator.Services;

public sealed class ConsoleProgressReporter : IProgressReporter
{
    private readonly object _sync = new();
    private DateTime _lastRenderUtc = DateTime.MinValue;

    public void ReportStart(int totalFiles)
    {
        Console.WriteLine();
        Console.WriteLine($"Starting dataset generation for {totalFiles:N0} files...");
        Console.WriteLine(new string('-', 90));
    }

    public void ReportProgress(string currentFile, int filesCreated, int totalFiles, TimeSpan elapsed)
    {
        lock (_sync)
        {
            if ((DateTime.UtcNow - _lastRenderUtc).TotalMilliseconds < 250 && filesCreated < totalFiles)
                return;

            _lastRenderUtc = DateTime.UtcNow;

            var percent = totalFiles == 0 ? 0 : (double)filesCreated / totalFiles * 100;
            var eta = EstimateRemaining(elapsed, filesCreated, totalFiles);
            var line = new StringBuilder()
                .Append($"[{filesCreated:N0}/{totalFiles:N0}] ")
                .Append($"{percent,5:F1}% | ")
                .Append($"ETA: {FormatDuration(eta)} | ")
                .Append($"Current: {Truncate(currentFile, 50)}")
                .ToString();

            Console.Write($"\r{line.PadRight(90)}");
        }
    }

    public void ReportComplete(int totalFiles, TimeSpan elapsed)
    {
        lock (_sync)
        {
            Console.WriteLine();
            Console.WriteLine(new string('-', 90));
            Console.WriteLine($"Generation complete: {totalFiles:N0} files in {FormatDuration(elapsed)}.");
            Console.WriteLine();
        }
    }

    private static TimeSpan EstimateRemaining(TimeSpan elapsed, int completed, int total)
    {
        if (completed <= 0 || completed >= total)
            return TimeSpan.Zero;

        var msPerFile = elapsed.TotalMilliseconds / completed;
        return TimeSpan.FromMilliseconds(msPerFile * (total - completed));
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";

        return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
}
