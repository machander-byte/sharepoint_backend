using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public static class FileSizeSelector
{
    private const long SmallMin = 10 * 1024;
    private const long SmallMax = 1 * 1024 * 1024;
    private const long MediumMin = 1 * 1024 * 1024;
    private const long MediumMax = 50L * 1024 * 1024;
    private const long LargeMin = 50L * 1024 * 1024;
    private const long LargeMax = 500L * 1024 * 1024;
    private const long HugeMin = 500L * 1024 * 1024;
    private const long HugeMax = 2L * 1024 * 1024 * 1024;

    public static (long SizeBytes, FileSizeCategory Category) SelectSize(long maxFileSizeBytes, Random random)
    {
        var available = GetAvailableCategories(maxFileSizeBytes);
        var category = available[random.Next(available.Count)];
        var (min, max) = GetRange(category);

        min = Math.Min(min, maxFileSizeBytes);
        max = Math.Min(max, maxFileSizeBytes);

        if (min > max)
            return (maxFileSizeBytes, category);

        return (random.NextInt64(min, max + 1), category);
    }

    private static List<FileSizeCategory> GetAvailableCategories(long maxFileSizeBytes)
    {
        var categories = new List<FileSizeCategory>();

        if (maxFileSizeBytes >= SmallMin)
            categories.Add(FileSizeCategory.Small);

        if (maxFileSizeBytes >= MediumMin)
            categories.Add(FileSizeCategory.Medium);

        if (maxFileSizeBytes >= LargeMin)
            categories.Add(FileSizeCategory.Large);

        if (maxFileSizeBytes >= HugeMin)
            categories.Add(FileSizeCategory.Huge);

        return categories.Count > 0 ? categories : [FileSizeCategory.Small];
    }

    private static (long Min, long Max) GetRange(FileSizeCategory category) =>
        category switch
        {
            FileSizeCategory.Small => (SmallMin, SmallMax),
            FileSizeCategory.Medium => (MediumMin, MediumMax),
            FileSizeCategory.Large => (LargeMin, LargeMax),
            FileSizeCategory.Huge => (HugeMin, HugeMax),
            _ => (SmallMin, SmallMax)
        };
}
