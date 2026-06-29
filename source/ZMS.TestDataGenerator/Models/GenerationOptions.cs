namespace ZMS.TestDataGenerator.Models;

public sealed class GenerationOptions
{
    public const string SectionName = "Generation";

    public int FileCount { get; set; } = 1000;
    public int MaxDepth { get; set; } = 10;
    public int MaxFileSizeMb { get; set; } = 50;
    public string OutputPath { get; set; } = "./TestTenant";
    public int Parallelism { get; set; } = 4;
    public int ManifestBatchSize { get; set; } = 100;
    public int WriteBufferSizeKb { get; set; } = 1024;
    public bool IncludeEdgeCases { get; set; } = true;
    public int LongPathFileCount { get; set; } = 10;
    public int LongPathTargetCharacters { get; set; } = 320;
    public int DuplicateNameSetCount { get; set; } = 3;
    public int CorruptedFileCount { get; set; } = 6;
    public int SpecialCharacterFileCount { get; set; } = 10;
    public int HugeSingleFolderFileCount { get; set; } = 100;
    public int PermissionEdgeCaseCount { get; set; } = 10;

    public long MaxFileSizeBytes => (long)MaxFileSizeMb * 1024 * 1024;

    public void Validate()
    {
        if (FileCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(FileCount), "File count must be greater than zero.");

        if (MaxDepth is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(MaxDepth), "Depth must be between 1 and 20.");

        if (MaxFileSizeMb <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxFileSizeMb), "Max file size must be greater than zero.");

        if (string.IsNullOrWhiteSpace(OutputPath))
            throw new ArgumentException("Output path is required.", nameof(OutputPath));

        if (Parallelism <= 0)
            throw new ArgumentOutOfRangeException(nameof(Parallelism), "Parallelism must be greater than zero.");

        if (ManifestBatchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(ManifestBatchSize), "Manifest batch size must be greater than zero.");

        if (WriteBufferSizeKb <= 0)
            throw new ArgumentOutOfRangeException(nameof(WriteBufferSizeKb), "Write buffer size must be greater than zero.");

        if (LongPathFileCount < 0)
            throw new ArgumentOutOfRangeException(nameof(LongPathFileCount), "Long path file count cannot be negative.");

        if (LongPathTargetCharacters is < 260 or > 800)
            throw new ArgumentOutOfRangeException(nameof(LongPathTargetCharacters), "Long path target characters must be between 260 and 800.");

        if (DuplicateNameSetCount < 0)
            throw new ArgumentOutOfRangeException(nameof(DuplicateNameSetCount), "Duplicate name set count cannot be negative.");

        if (CorruptedFileCount < 0)
            throw new ArgumentOutOfRangeException(nameof(CorruptedFileCount), "Corrupted file count cannot be negative.");

        if (SpecialCharacterFileCount < 0)
            throw new ArgumentOutOfRangeException(nameof(SpecialCharacterFileCount), "Special-character file count cannot be negative.");

        if (HugeSingleFolderFileCount < 0)
            throw new ArgumentOutOfRangeException(nameof(HugeSingleFolderFileCount), "Huge single-folder file count cannot be negative.");

        if (PermissionEdgeCaseCount < 0)
            throw new ArgumentOutOfRangeException(nameof(PermissionEdgeCaseCount), "Permission edge-case count cannot be negative.");
    }
}
