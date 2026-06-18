using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Connectors.FileShare.Connectors;

public class FileShareSourceConnector : ISourceConnector
{
    private const int SharePointPathReviewThreshold = 350;
    private static readonly char[] InvalidSharePointNameChars = ['"', '*', ':', '<', '>', '?', '|', '#', '%'];

    public ConnectionType SupportedConnectionType => ConnectionType.FileShare;

    public Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken)
    {
        var path = ResolveRootPath(connection, connection.RootPath ?? connection.Url);
        var exists = Directory.Exists(ToIoPath(path));

        return Task.FromResult(new ConnectionTestResult
        {
            IsSuccess = exists,
            Message = exists
                ? $"File share '{path}' is reachable."
                : $"File share '{path}' could not be found."
        });
    }

    public Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(ConnectionProfile connection, CancellationToken cancellationToken)
    {
        var path = ResolveRootPath(connection, connection.RootPath ?? connection.Url);
        var name = Directory.Exists(ToIoPath(path)) ? new DirectoryInfo(ToIoPath(path)).Name : "File Share Root";

        IReadOnlyCollection<SiteInfo> sites =
        [
            new SiteInfo
            {
                Id = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(path)),
                Name = name,
                Url = path
            }
        ];

        return Task.FromResult(sites);
    }

    public async Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        CancellationToken cancellationToken)
    {
        var rootPath = ResolveRootPath(connection, sourceLocation);
        var rootIoPath = ToIoPath(rootPath);

        return await Task.Run<IReadOnlyCollection<LibraryInfo>>(() =>
        {
            if (!Directory.Exists(rootIoPath))
            {
                return
                [
                    new LibraryInfo { Id = "default", Name = "Shared Files", ItemCount = 0 }
                ];
            }

            var libraries = SafeEnumerateDirectories(rootIoPath)
                .Select(directory =>
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    var itemCount = SafeEnumerateFiles(directory).Count();

                    return new LibraryInfo
                    {
                        Id = directoryInfo.Name,
                        Name = directoryInfo.Name,
                        ItemCount = itemCount
                    };
                })
                .OrderBy(library => library.Name)
                .ToList();

            if (libraries.Count == 0)
            {
                libraries.Add(new LibraryInfo { Id = "root", Name = "Root Files", ItemCount = SafeEnumerateFiles(rootIoPath).Count() });
            }

            return (IReadOnlyCollection<LibraryInfo>)libraries;
        }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken)
    {
        var rootPath = ResolveRootPath(connection, sourceLocation);
        var libraryPath = string.IsNullOrWhiteSpace(libraryName) ? rootPath : Path.Combine(rootPath, libraryName.Trim());
        var libraryIoPath = ToIoPath(libraryPath);

        return await Task.Run<IReadOnlyCollection<FileItem>>(() =>
        {
            if (!Directory.Exists(libraryIoPath))
            {
                return
                [
                    new FileItem
                    {
                        Name = "Welcome.txt",
                        SourcePath = Path.Combine(libraryPath, "Welcome.txt"),
                        SizeInBytes = 1024,
                        ModifiedUtc = DateTimeOffset.UtcNow.AddDays(-1),
                        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["RelativePath"] = "Welcome.txt",
                            ["Source"] = "Sample File Share",
                            ["Note"] = "Replace this with a real file share path."
                        }
                    }
                ];
            }

            return SafeEnumerateFiles(libraryIoPath)
                .Select(path =>
                {
                    var fileInfo = new FileInfo(path);
                    var displayPath = StripExtendedPathPrefix(path);
                    var displayLibraryPath = StripExtendedPathPrefix(libraryIoPath);
                    var relativePath = Path.GetRelativePath(displayLibraryPath, displayPath).Replace('\\', '/');
                    var invalidCharacters = GetInvalidSharePointCharacters(fileInfo.Name);
                    return new FileItem
                    {
                        Name = fileInfo.Name,
                        SourcePath = path,
                        SizeInBytes = fileInfo.Length,
                        ModifiedUtc = fileInfo.LastWriteTimeUtc,
                        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            [MigrationItemMetadataKeys.RelativePath] = relativePath,
                            [MigrationItemMetadataKeys.ItemType] = MigrationItemMetadataKeys.ItemTypeFile,
                            ["Extension"] = fileInfo.Extension,
                            ["Folder"] = StripExtendedPathPrefix(fileInfo.DirectoryName ?? string.Empty),
                            ["PathLength"] = displayPath.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["PathLengthRisk"] = (displayPath.Length > SharePointPathReviewThreshold).ToString(),
                            ["InvalidSharePointCharacters"] = invalidCharacters,
                            ["InvalidSharePointCharacterRisk"] = (!string.IsNullOrWhiteSpace(invalidCharacters)).ToString(),
                            ["CreatedUtc"] = fileInfo.CreationTimeUtc.ToString("o"),
                            ["ModifiedUtc"] = fileInfo.LastWriteTimeUtc.ToString("o")
                        }
                    };
                })
                .OrderBy(file => file.SourcePath)
                .ToList();
        }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<FolderItem>> GetFoldersAsync(
        ConnectionProfile connection,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken)
    {
        var rootPath = ResolveRootPath(connection, sourceLocation);
        var libraryPath = string.IsNullOrWhiteSpace(libraryName) ? rootPath : Path.Combine(rootPath, libraryName.Trim());
        var libraryIoPath = ToIoPath(libraryPath);

        return await Task.Run<IReadOnlyCollection<FolderItem>>(() =>
        {
            if (!Directory.Exists(libraryIoPath))
            {
                return Array.Empty<FolderItem>();
            }

            return SafeEnumerateDirectoriesRecursive(libraryIoPath)
                .Select(path =>
                {
                    var directoryInfo = new DirectoryInfo(path);
                    var displayPath = StripExtendedPathPrefix(path);
                    var displayLibraryPath = StripExtendedPathPrefix(libraryIoPath);
                    var relativePath = Path.GetRelativePath(displayLibraryPath, displayPath).Replace('\\', '/');
                    var invalidCharacters = GetInvalidSharePointCharacters(directoryInfo.Name);

                    return new FolderItem
                    {
                        Name = directoryInfo.Name,
                        SourcePath = path,
                        RelativePath = relativePath,
                        ModifiedUtc = directoryInfo.LastWriteTimeUtc,
                        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            [MigrationItemMetadataKeys.RelativePath] = relativePath,
                            [MigrationItemMetadataKeys.ItemType] = MigrationItemMetadataKeys.ItemTypeFolder,
                            ["Folder"] = displayPath,
                            ["PathLength"] = displayPath.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["PathLengthRisk"] = (displayPath.Length > SharePointPathReviewThreshold).ToString(),
                            ["InvalidSharePointCharacters"] = invalidCharacters,
                            ["InvalidSharePointCharacterRisk"] = (!string.IsNullOrWhiteSpace(invalidCharacters)).ToString(),
                            ["CreatedUtc"] = directoryInfo.CreationTimeUtc.ToString("o"),
                            ["ModifiedUtc"] = directoryInfo.LastWriteTimeUtc.ToString("o")
                        }
                    };
                })
                .OrderBy(folder => folder.RelativePath)
                .ToArray();
        }, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(
        ConnectionProfile connection,
        MigrationItem item,
        CancellationToken cancellationToken)
    {
        var ioPath = ToIoPath(item.SourcePath);
        if (File.Exists(ioPath))
        {
            Stream stream = new FileStream(
                ioPath,
                FileMode.Open,
                FileAccess.Read,
                System.IO.FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            return Task.FromResult(stream);
        }

        var sampleContent = $"Sample content for '{item.FileName}' from file share '{connection.Name}'.";
        Stream fallbackStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sampleContent));
        return Task.FromResult(fallbackStream);
    }

    private static string ResolveRootPath(ConnectionProfile connection, string sourceLocation)
    {
        return string.IsNullOrWhiteSpace(connection.RootPath)
            ? sourceLocation
            : connection.RootPath;
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string rootPath)
    {
        try
        {
            return Directory.EnumerateDirectories(rootPath).ToArray();
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException)
        {
            return [];
        }
    }

    private static IReadOnlyCollection<string> SafeEnumerateFiles(string rootPath)
    {
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            try
            {
                foreach (var file in Directory.EnumerateFiles(current))
                {
                    files.Add(file);
                }

                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    pending.Push(directory);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException)
            {
                continue;
            }
        }

        return files;
    }

    private static IReadOnlyCollection<string> SafeEnumerateDirectoriesRecursive(string rootPath)
    {
        var directories = new List<string>();
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            try
            {
                foreach (var directory in Directory.EnumerateDirectories(current))
                {
                    directories.Add(directory);
                    pending.Push(directory);
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PathTooLongException)
            {
                continue;
            }
        }

        return directories;
    }

    private static string GetInvalidSharePointCharacters(string name)
    {
        var invalid = name
            .Where(character => InvalidSharePointNameChars.Contains(character))
            .Distinct()
            .OrderBy(character => character)
            .ToArray();

        return invalid.Length == 0 ? string.Empty : string.Join(string.Empty, invalid);
    }

    private static string ToIoPath(string path)
    {
        if (!OperatingSystem.IsWindows() || string.IsNullOrWhiteSpace(path) || path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return path;
        }

        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(@"\\", StringComparison.Ordinal)
            ? $@"\\?\UNC\{fullPath.TrimStart('\\')}"
            : $@"\\?\{fullPath}";
    }

    private static string StripExtendedPathPrefix(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            return path;
        }

        if (path.StartsWith(@"\\?\UNC\", StringComparison.Ordinal))
        {
            return $@"\\{path[8..]}";
        }

        return path.StartsWith(@"\\?\", StringComparison.Ordinal)
            ? path[4..]
            : path;
    }
}
