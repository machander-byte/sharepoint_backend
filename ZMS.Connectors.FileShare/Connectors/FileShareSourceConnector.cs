using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Connectors.FileShare.Connectors;

public class FileShareSourceConnector : ISourceConnector
{
    public ConnectionType SupportedConnectionType => ConnectionType.FileShare;

    public Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken)
    {
        var path = ResolveRootPath(connection, connection.RootPath ?? connection.Url);
        var exists = Directory.Exists(path);

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
        var name = Directory.Exists(path) ? new DirectoryInfo(path).Name : "File Share Root";

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

        return await Task.Run<IReadOnlyCollection<LibraryInfo>>(() =>
        {
            if (!Directory.Exists(rootPath))
            {
                return
                [
                    new LibraryInfo { Id = "default", Name = "Shared Files", ItemCount = 0 }
                ];
            }

            var libraries = Directory
                .GetDirectories(rootPath)
                .Select(directory =>
                {
                    var directoryInfo = new DirectoryInfo(directory);
                    var itemCount = Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Count();

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
                libraries.Add(new LibraryInfo { Id = "root", Name = "Root Files", ItemCount = Directory.EnumerateFiles(rootPath).Count() });
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

        return await Task.Run<IReadOnlyCollection<FileItem>>(() =>
        {
            if (!Directory.Exists(libraryPath))
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

            return Directory
                .EnumerateFiles(libraryPath, "*", SearchOption.AllDirectories)
                .Select(path =>
                {
                    var fileInfo = new FileInfo(path);
                    return new FileItem
                    {
                        Name = fileInfo.Name,
                        SourcePath = path,
                        SizeInBytes = fileInfo.Length,
                        ModifiedUtc = fileInfo.LastWriteTimeUtc,
                        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["RelativePath"] = Path.GetRelativePath(libraryPath, path).Replace('\\', '/'),
                            ["Extension"] = fileInfo.Extension,
                            ["Folder"] = fileInfo.DirectoryName ?? string.Empty
                        }
                    };
                })
                .OrderBy(file => file.SourcePath)
                .ToList();
        }, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(
        ConnectionProfile connection,
        MigrationItem item,
        CancellationToken cancellationToken)
    {
        if (File.Exists(item.SourcePath))
        {
            Stream stream = new FileStream(
                item.SourcePath,
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
}
