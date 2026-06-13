using System.IO.Compression;

namespace ZMS.Application.EnvironmentBridge;

public sealed class ZipPackageService : IZipPackageService
{
    public Task CreateZipAsync(string sourceDirectory, string zipPath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(zipPath))
        {
            File.Delete(zipPath);
        }

        ZipFile.CreateFromDirectory(sourceDirectory, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        return Task.CompletedTask;
    }
}
