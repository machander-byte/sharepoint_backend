namespace ZMS.TestDataGenerator.Services;

using ZMS.TestDataGenerator.Models;

public interface IFileContentGenerator
{
    Task WriteFileAsync(
        string filePath,
        string extension,
        long targetSizeBytes,
        int bufferSize,
        CancellationToken cancellationToken,
        FileContentMode contentMode = FileContentMode.Valid);
}
