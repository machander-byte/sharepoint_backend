namespace ZMS.TestDataGenerator.Services;

public interface IFolderStructureService
{
    IReadOnlyList<string> Departments { get; }
    string BuildFolderPath(string department, int depth, Random random);
}
