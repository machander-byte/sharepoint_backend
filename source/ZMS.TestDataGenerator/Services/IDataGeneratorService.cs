using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public interface IDataGeneratorService
{
    Task<GenerationSummary> GenerateAsync(CancellationToken cancellationToken);
}
