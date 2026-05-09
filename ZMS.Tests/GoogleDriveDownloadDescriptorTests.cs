using ZMS.Connectors.GoogleDrive.Services;

namespace ZMS.Tests;

public class GoogleDriveDownloadDescriptorTests
{
    [Theory]
    [InlineData(
        "Migration Plan",
        "application/vnd.google-apps.document",
        "Migration Plan.docx",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData(
        "Inventory",
        "application/vnd.google-apps.spreadsheet",
        "Inventory.xlsx",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData(
        "Executive Review",
        "application/vnd.google-apps.presentation",
        "Executive Review.pptx",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public void ResolveDownloadDescriptor_MapsGoogleNativeFilesToOfficeFormats(
        string fileName,
        string mimeType,
        string expectedFileName,
        string expectedExportMimeType)
    {
        var descriptor = GoogleDriveApiClient.ResolveDownloadDescriptor(fileName, mimeType);

        Assert.Equal(expectedFileName, descriptor.FileName);
        Assert.Equal(expectedExportMimeType, descriptor.ExportMimeType);
    }

    [Fact]
    public void ResolveDownloadDescriptor_DoesNotChangeBinaryFiles()
    {
        var descriptor = GoogleDriveApiClient.ResolveDownloadDescriptor(
            "archive.zip",
            "application/zip");

        Assert.Equal("archive.zip", descriptor.FileName);
        Assert.Null(descriptor.ExportMimeType);
    }
}
