using ZMS.Core.Enums;
using ZMS.Core.Interfaces;
using ZMS.Core.Models;

namespace ZMS.Connectors.SharePointOnPrem.Connectors;

public class SharePointOnPremSourceConnector : ISourceConnector
{
    public ConnectionType SupportedConnectionType => ConnectionType.SharePointOnPrem;

    public Task<ConnectionTestResult> TestConnectionAsync(ConnectionProfile connection, CancellationToken cancellationToken)
    {
        var isSuccess = Uri.TryCreate(connection.Url, UriKind.Absolute, out _);

        return Task.FromResult(new ConnectionTestResult
        {
            IsSuccess = isSuccess,
            Message = isSuccess
                ? "SharePoint On-Prem connection stub validated successfully."
                : "Provide a valid SharePoint On-Prem URL."
        });
    }

    public Task<IReadOnlyCollection<SiteInfo>> GetSitesAsync(ConnectionProfile connection, CancellationToken cancellationToken)
    {
        var baseUrl = connection.Url.TrimEnd('/');
        IReadOnlyCollection<SiteInfo> sites =
        [
            new SiteInfo { Id = "hr", Name = "HR Portal", Url = $"{baseUrl}/sites/hr" },
            new SiteInfo { Id = "ops", Name = "Operations", Url = $"{baseUrl}/sites/operations" },
            new SiteInfo { Id = "eng", Name = "Engineering", Url = $"{baseUrl}/sites/engineering" }
        ];

        return Task.FromResult(sites);
    }

    public Task<IReadOnlyCollection<LibraryInfo>> GetLibrariesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<LibraryInfo> libraries =
        [
            new LibraryInfo { Id = "documents", Name = "Documents", ItemCount = 42 },
            new LibraryInfo { Id = "contracts", Name = "Contracts", ItemCount = 17 },
            new LibraryInfo { Id = "engineering", Name = "Engineering Specs", ItemCount = 11 }
        ];

        return Task.FromResult(libraries);
    }

    public Task<IReadOnlyCollection<FileItem>> GetFilesAsync(
        ConnectionProfile connection,
        string sourceLocation,
        string? libraryName,
        CancellationToken cancellationToken)
    {
        var logicalLibrary = string.IsNullOrWhiteSpace(libraryName) ? "Documents" : libraryName.Trim();

        IReadOnlyCollection<FileItem> files =
        [
            new FileItem
            {
                Name = "Migration-Checklist.xlsx",
                SourcePath = $"{sourceLocation}/{logicalLibrary}/Migration-Checklist.xlsx",
                SizeInBytes = 87_420,
                ModifiedUtc = DateTimeOffset.UtcNow.AddDays(-4),
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RelativePath"] = "Migration-Checklist.xlsx",
                    ["Author"] = "SharePoint Admin",
                    ["Department"] = "PMO"
                }
            },
            new FileItem
            {
                Name = "Project-Summary.docx",
                SourcePath = $"{sourceLocation}/{logicalLibrary}/Project-Summary.docx",
                SizeInBytes = 42_100,
                ModifiedUtc = DateTimeOffset.UtcNow.AddDays(-9),
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RelativePath"] = "Project-Summary.docx",
                    ["Author"] = "Operations Team",
                    ["Classification"] = "Internal"
                }
            },
            new FileItem
            {
                Name = "Engineering-Spec.pdf",
                SourcePath = $"{sourceLocation}/{logicalLibrary}/Engineering-Spec.pdf",
                SizeInBytes = 128_512,
                ModifiedUtc = DateTimeOffset.UtcNow.AddDays(-12),
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["RelativePath"] = "Engineering-Spec.pdf",
                    ["Author"] = "Design Office",
                    ["Area"] = "Engineering"
                }
            }
        ];

        return Task.FromResult(files);
    }

    public Task<Stream> OpenReadAsync(
        ConnectionProfile connection,
        MigrationItem item,
        CancellationToken cancellationToken)
    {
        var sampleContent = $"""
            Sample SharePoint On-Prem content
            Connection: {connection.Name}
            File: {item.FileName}
            Source: {item.SourcePath}
            """;

        Stream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(sampleContent));
        return Task.FromResult(stream);
    }
}
