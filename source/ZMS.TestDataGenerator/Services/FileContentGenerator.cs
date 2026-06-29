using System.IO.Compression;
using System.Text;
using System.Text.Json;
using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public sealed class FileContentGenerator : IFileContentGenerator
{
    private static readonly byte[] PaddingPattern = Encoding.UTF8.GetBytes("ZMS-TEST-DATA-PADDING-SEQUENCE-0123456789-ABCDEF-");

    public async Task WriteFileAsync(
        string filePath,
        string extension,
        long targetSizeBytes,
        int bufferSize,
        CancellationToken cancellationToken,
        FileContentMode contentMode = FileContentMode.Valid)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var stream = new FileStream(
            filePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        if (contentMode != FileContentMode.Valid)
        {
            await WriteEdgeCaseContentAsync(stream, contentMode, targetSizeBytes, bufferSize, cancellationToken);
            return;
        }

        switch (extension.ToLowerInvariant())
        {
            case "txt":
            case "csv":
                await WriteTextContentAsync(stream, extension, targetSizeBytes, bufferSize, cancellationToken);
                break;
            case "json":
                await WriteJsonContentAsync(stream, targetSizeBytes, bufferSize, cancellationToken);
                break;
            case "xml":
                await WriteXmlContentAsync(stream, targetSizeBytes, bufferSize, cancellationToken);
                break;
            case "pdf":
                await WritePdfContentAsync(stream, targetSizeBytes, bufferSize, cancellationToken);
                break;
            case "docx":
            case "xlsx":
            case "pptx":
                await WriteOfficeZipContentAsync(stream, extension, targetSizeBytes, bufferSize, cancellationToken);
                break;
            case "zip":
                await WriteZipContentAsync(stream, targetSizeBytes, bufferSize, cancellationToken);
                break;
            default:
                await WriteBinaryPaddingAsync(stream, targetSizeBytes, bufferSize, cancellationToken);
                break;
        }

        if (stream.Length < targetSizeBytes)
            await WriteBinaryPaddingAsync(stream, targetSizeBytes - stream.Length, bufferSize, cancellationToken);
        else if (stream.Length > targetSizeBytes)
            stream.SetLength(targetSizeBytes);
    }

    private static async Task WriteEdgeCaseContentAsync(
        Stream stream,
        FileContentMode contentMode,
        long targetSizeBytes,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        switch (contentMode)
        {
            case FileContentMode.BrokenZip:
                await stream.WriteAsync(Encoding.UTF8.GetBytes("BROKEN-ZIP: this file intentionally has a .zip extension but no central directory."), cancellationToken);
                await PadToSizeAsync(stream, targetSizeBytes, bufferSize, cancellationToken);
                break;
            case FileContentMode.InvalidPdf:
                await stream.WriteAsync(Encoding.UTF8.GetBytes("%PDF-BROKEN\nThis intentionally invalid PDF has no xref table or EOF marker."), cancellationToken);
                await PadToSizeAsync(stream, targetSizeBytes, bufferSize, cancellationToken);
                break;
            case FileContentMode.EmptyOfficeDocument:
            case FileContentMode.EmptyFile:
                stream.SetLength(0);
                break;
        }
    }

    private static async Task WriteTextContentAsync(
        Stream stream,
        string extension,
        long targetSize,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        if (extension.Equals("csv", StringComparison.OrdinalIgnoreCase))
            await stream.WriteAsync(Encoding.UTF8.GetBytes("Id,Department,Owner,Classification,Amount,Date\n"), cancellationToken);

        var lineNumber = 1;
        var lineBuffer = new byte[bufferSize];
        while (stream.Length < targetSize)
        {
            var line = extension.Equals("csv", StringComparison.OrdinalIgnoreCase)
                ? $"{lineNumber},Finance,jsmith,Internal,{lineNumber * 12.34:F2},{DateTime.UtcNow:yyyy-MM-dd}\n"
                : $"Enterprise migration test document line {lineNumber} generated for ZMS validation workflows.\n";

            var bytes = Encoding.UTF8.GetBytes(line);
            if (bytes.Length > lineBuffer.Length)
                lineBuffer = new byte[bytes.Length];

            Array.Copy(bytes, lineBuffer, bytes.Length);
            await stream.WriteAsync(lineBuffer.AsMemory(0, bytes.Length), cancellationToken);
            lineNumber++;
        }
    }

    private static async Task WriteJsonContentAsync(Stream stream, long targetSize, int bufferSize, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes("{\"records\":["), cancellationToken);

        var index = 0;
        while (stream.Length < targetSize - 2)
        {
            if (index > 0)
                await stream.WriteAsync(Encoding.UTF8.GetBytes(","), cancellationToken);

            var record = JsonSerializer.Serialize(new
            {
                id = index,
                department = "IT",
                owner = "jsmith",
                classification = "Internal",
                timestamp = DateTime.UtcNow
            });

            await stream.WriteAsync(Encoding.UTF8.GetBytes(record), cancellationToken);
            index++;
        }

        await stream.WriteAsync(Encoding.UTF8.GetBytes("]}"), cancellationToken);
        await PadToSizeAsync(stream, targetSize, bufferSize, cancellationToken);
    }

    private static async Task WriteXmlContentAsync(Stream stream, long targetSize, int bufferSize, CancellationToken cancellationToken)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Records>"), cancellationToken);

        var index = 0;
        while (stream.Length < targetSize - 10)
        {
            var entry = $"<Record id=\"{index}\" department=\"Finance\" owner=\"alee\" classification=\"Confidential\" />";
            await stream.WriteAsync(Encoding.UTF8.GetBytes(entry), cancellationToken);
            index++;
        }

        await stream.WriteAsync(Encoding.UTF8.GetBytes("</Records>"), cancellationToken);
        await PadToSizeAsync(stream, targetSize, bufferSize, cancellationToken);
    }

    private static async Task WritePdfContentAsync(Stream stream, long targetSize, int bufferSize, CancellationToken cancellationToken)
    {
        var header = "%PDF-1.4\n1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n" +
                     "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n" +
                     "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>endobj\n" +
                     "xref\n0 4\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n0000000115 00000 n \n" +
                     "trailer<< /Size 4 /Root 1 0 R >>\nstartxref\n190\n%%EOF\n";

        await stream.WriteAsync(Encoding.ASCII.GetBytes(header), cancellationToken);
        await PadToSizeAsync(stream, targetSize, bufferSize, cancellationToken);
    }

    private static async Task WriteOfficeZipContentAsync(
        Stream stream,
        string extension,
        long targetSize,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"zms-{Guid.NewGuid():N}.zip");

        try
        {
            using (var zip = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                var contentType = extension switch
                {
                    "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                    "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    _ => "application/vnd.openxmlformats-officedocument.presentationml.presentation"
                };

                var entry = zip.CreateEntry("[Content_Types].xml");
                await using (var entryStream = entry.Open())
                {
                    var xml = $"<?xml version=\"1.0\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" ContentType=\"{contentType}\"/></Types>";
                    await entryStream.WriteAsync(Encoding.UTF8.GetBytes(xml), cancellationToken);
                }

                var dataEntry = zip.CreateEntry("document/data.xml");
                await using (var entryStream = dataEntry.Open())
                {
                    await entryStream.WriteAsync(Encoding.UTF8.GetBytes("<Document>ZMS Test Data</Document>"), cancellationToken);
                }
            }

            await using var zipStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous);
            await zipStream.CopyToAsync(stream, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }

        await PadToSizeAsync(stream, targetSize, bufferSize, cancellationToken);
    }

    private static async Task WriteZipContentAsync(Stream stream, long targetSize, int bufferSize, CancellationToken cancellationToken)
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"zms-{Guid.NewGuid():N}.zip");

        try
        {
            using (var zip = ZipFile.Open(tempZipPath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("payload/data.txt");
                await using var entryStream = entry.Open();
                var payload = Encoding.UTF8.GetBytes("ZMS migration test archive payload.");
                await entryStream.WriteAsync(payload, cancellationToken);
            }

            await using var zipStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous);
            await zipStream.CopyToAsync(stream, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempZipPath))
                File.Delete(tempZipPath);
        }

        await PadToSizeAsync(stream, targetSize, bufferSize, cancellationToken);
    }

    private static async Task PadToSizeAsync(Stream stream, long targetSize, int bufferSize, CancellationToken cancellationToken)
    {
        if (stream.Length < targetSize)
            await WriteBinaryPaddingAsync(stream, targetSize - stream.Length, bufferSize, cancellationToken);
    }

    private static async Task WriteBinaryPaddingAsync(
        Stream stream,
        long bytesToWrite,
        int bufferSize,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[Math.Min(bufferSize, PaddingPattern.Length)];
        Array.Copy(PaddingPattern, buffer, buffer.Length);

        long remaining = bytesToWrite;
        while (remaining > 0)
        {
            var chunk = (int)Math.Min(remaining, buffer.Length);
            await stream.WriteAsync(buffer.AsMemory(0, chunk), cancellationToken);
            remaining -= chunk;
        }
    }
}
