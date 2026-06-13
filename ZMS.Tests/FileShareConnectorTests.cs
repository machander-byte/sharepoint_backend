using ZMS.Connectors.FileShare.Connectors;
using ZMS.Core.Enums;
using ZMS.Core.Models;

namespace ZMS.Tests;

public class FileShareConnectorTests
{
    [Fact]
    public async Task GetFilesAsync_AnnotatesInvalidSharePointCharacters()
    {
        var root = Path.Combine(Path.GetTempPath(), $"zms-file-share-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var filePath = Path.Combine(root, "bad#name.txt");
        await File.WriteAllTextAsync(filePath, "content");

        try
        {
            var connector = new FileShareSourceConnector();
            var connection = new ConnectionProfile
            {
                Type = ConnectionType.FileShare,
                Name = "Local",
                RootPath = root,
                Url = root
            };

            var files = await connector.GetFilesAsync(connection, root, null, CancellationToken.None);
            var file = Assert.Single(files);

            Assert.Equal("True", file.Metadata["InvalidSharePointCharacterRisk"]);
            Assert.Contains("#", file.Metadata["InvalidSharePointCharacters"]);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task GetFilesAsync_EnumeratesLongPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), $"zms-file-share-long-{Guid.NewGuid():N}");
        var longFolder = root;
        while (Path.Combine(longFolder, "LongPathEvidence.txt").Length < 380)
        {
            longFolder = Path.Combine(longFolder, "VeryLongMigrationFolderSegmentForValidation");
        }

        var longFilePath = Path.Combine(longFolder, "LongPathEvidence.txt");
        Directory.CreateDirectory(ToIoPath(longFolder));
        await File.WriteAllTextAsync(ToIoPath(longFilePath), "long path content");

        try
        {
            var connector = new FileShareSourceConnector();
            var connection = new ConnectionProfile
            {
                Type = ConnectionType.FileShare,
                Name = "Local",
                RootPath = root,
                Url = root
            };

            var files = await connector.GetFilesAsync(connection, root, null, CancellationToken.None);
            var file = Assert.Single(files);

            Assert.Equal("LongPathEvidence.txt", file.Name);
            Assert.Equal("True", file.Metadata["PathLengthRisk"]);
            Assert.Contains("VeryLongMigrationFolderSegmentForValidation", file.Metadata["RelativePath"]);
        }
        finally
        {
            Directory.Delete(ToIoPath(root), recursive: true);
        }
    }

    private static string ToIoPath(string path)
    {
        if (!OperatingSystem.IsWindows() || path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return path;
        }

        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(@"\\", StringComparison.Ordinal)
            ? $@"\\?\UNC\{fullPath.TrimStart('\\')}"
            : $@"\\?\{fullPath}";
    }
}
