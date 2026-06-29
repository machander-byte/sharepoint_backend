using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public interface IMetadataGenerator
{
    FileRecord CreateFileRecord(
        string folderPath,
        int folderDepth,
        string department,
        string extension,
        long sizeBytes,
        Random random,
        FileRecordOverrides? overrides = null);
}
