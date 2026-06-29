namespace ZMS.TestDataGenerator.Services;

public interface IProgressReporter
{
    void ReportStart(int totalFiles);
    void ReportProgress(string currentFile, int filesCreated, int totalFiles, TimeSpan elapsed);
    void ReportComplete(int totalFiles, TimeSpan elapsed);
}
