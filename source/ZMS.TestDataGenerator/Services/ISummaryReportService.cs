using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public interface ISummaryReportService
{
    Task WriteSummaryAsync(string outputPath, GenerationSummary summary, CancellationToken cancellationToken);
    void PrintSummary(GenerationSummary summary);
}
