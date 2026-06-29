using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public sealed class DataGeneratorService : IDataGeneratorService
{
    private static readonly JsonSerializerOptions ManifestJsonOptions = new() { WriteIndented = false };
    private const string EdgeCaseDepartment = "IT";

    private readonly GenerationOptions _options;
    private readonly IFolderStructureService _folderStructure;
    private readonly IMetadataGenerator _metadataGenerator;
    private readonly IFileContentGenerator _fileContentGenerator;
    private readonly IProgressReporter _progressReporter;
    private readonly ISummaryReportService _summaryReportService;
    private readonly ILogger<DataGeneratorService> _logger;

    public DataGeneratorService(
        IOptions<GenerationOptions> options,
        IFolderStructureService folderStructure,
        IMetadataGenerator metadataGenerator,
        IFileContentGenerator fileContentGenerator,
        IProgressReporter progressReporter,
        ISummaryReportService summaryReportService,
        ILogger<DataGeneratorService> logger)
    {
        _options = options.Value;
        _folderStructure = folderStructure;
        _metadataGenerator = metadataGenerator;
        _fileContentGenerator = fileContentGenerator;
        _progressReporter = progressReporter;
        _summaryReportService = summaryReportService;
        _logger = logger;
    }

    public async Task<GenerationSummary> GenerateAsync(CancellationToken cancellationToken)
    {
        _options.Validate();

        var outputPath = Path.GetFullPath(_options.OutputPath);
        Directory.CreateDirectory(outputPath);

        var metadataDir = Path.Combine(outputPath, "_metadata");
        Directory.CreateDirectory(metadataDir);

        var manifestPath = Path.Combine(metadataDir, "file-manifest.jsonl");
        var permissionsPath = Path.Combine(metadataDir, "permissions-simulation.json");

        var folders = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        var records = new ConcurrentBag<FileRecord>();
        var errors = new ConcurrentBag<string>();
        var edgeCasePlans = BuildEdgeCasePlans();
        var filesCreated = 0;
        var startTime = DateTime.UtcNow;

        await using var manifestStream = new FileStream(
            manifestPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            8192,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using var manifestWriter = new StreamWriter(manifestStream);
        var manifestLock = new SemaphoreSlim(1, 1);
        var manifestBatch = new List<string>(_options.ManifestBatchSize);

        _progressReporter.ReportStart(_options.FileCount);
        _logger.LogInformation(
            "Generating {FileCount} files at max depth {MaxDepth} into {OutputPath}",
            _options.FileCount,
            _options.MaxDepth,
            outputPath);

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.Parallelism,
            CancellationToken = cancellationToken
        };

        var writeBufferSize = _options.WriteBufferSizeKb * 1024;

        await Parallel.ForEachAsync(
            Enumerable.Range(0, _options.FileCount),
            parallelOptions,
            async (index, token) =>
            {
                var random = new Random(HashCode.Combine(Environment.TickCount, index, outputPath.GetHashCode()));

                try
                {
                    var hasEdgeCase = edgeCasePlans.TryGetValue(index, out var edgeCasePlan);
                    var department = edgeCasePlan?.Department
                        ?? _folderStructure.Departments[random.Next(_folderStructure.Departments.Count)];
                    var depth = edgeCasePlan?.FolderDepth ?? random.Next(1, _options.MaxDepth + 1);
                    var folderPath = edgeCasePlan?.FolderPath ?? _folderStructure.BuildFolderPath(department, depth, random);
                    var extension = edgeCasePlan?.Extension ?? MetadataGenerator.PickExtension(random);
                    var (selectedSizeBytes, _) = FileSizeSelector.SelectSize(_options.MaxFileSizeBytes, random);
                    var sizeBytes = edgeCasePlan?.SizeBytes ?? selectedSizeBytes;

                    var record = _metadataGenerator.CreateFileRecord(
                        folderPath,
                        depth,
                        department,
                        extension,
                        sizeBytes,
                        random,
                        edgeCasePlan?.Overrides);

                    folders.TryAdd(folderPath, 0);

                    var fullPath = Path.Combine(outputPath, record.RelativePath);
                    await _fileContentGenerator.WriteFileAsync(
                        fullPath,
                        extension,
                        sizeBytes,
                        writeBufferSize,
                        token,
                        edgeCasePlan?.ContentMode ?? FileContentMode.Valid);

                    File.SetCreationTimeUtc(fullPath, record.CreatedDateUtc);
                    File.SetLastWriteTimeUtc(fullPath, record.ModifiedDateUtc);
                    File.SetLastAccessTimeUtc(fullPath, record.ModifiedDateUtc);

                    records.Add(record);
                    await AppendManifestRecordAsync(manifestWriter, manifestLock, manifestBatch, record, cancellationToken);

                    var created = Interlocked.Increment(ref filesCreated);
                    if (created % 10 == 0 || created == _options.FileCount)
                    {
                        _progressReporter.ReportProgress(
                            hasEdgeCase ? $"{record.RelativePath} [{record.EdgeCase}]" : record.RelativePath,
                            created,
                            _options.FileCount,
                            DateTime.UtcNow - startTime);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"File index {index}: {ex.Message}");
                    _logger.LogError(ex, "Failed to generate file at index {FileIndex}", index);
                }
            });

        await FlushManifestBatchAsync(manifestWriter, manifestLock, manifestBatch, cancellationToken);
        await manifestWriter.FlushAsync(cancellationToken);

        await WritePermissionSimulationAsync(permissionsPath, records, cancellationToken);

        var elapsed = DateTime.UtcNow - startTime;
        var recordList = records.ToList();
        var summary = BuildSummary(recordList, folders.Count, elapsed);

        _progressReporter.ReportComplete(summary.TotalFiles, elapsed);
        _summaryReportService.PrintSummary(summary);
        await _summaryReportService.WriteSummaryAsync(outputPath, summary, cancellationToken);

        if (!errors.IsEmpty)
        {
            var errorPath = Path.Combine(outputPath, "_reports", "generation-errors.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(errorPath)!);
            await File.WriteAllLinesAsync(errorPath, errors, cancellationToken);
            _logger.LogWarning("{ErrorCount} files failed during generation. See {ErrorPath}", errors.Count, errorPath);
        }

        _logger.LogInformation("Dataset generation completed successfully at {OutputPath}", outputPath);
        return summary;
    }

    private async Task AppendManifestRecordAsync(
        StreamWriter writer,
        SemaphoreSlim writerLock,
        List<string> batch,
        FileRecord record,
        CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(record, ManifestJsonOptions);

        await writerLock.WaitAsync(cancellationToken);
        try
        {
            batch.Add(line);

            if (batch.Count >= _options.ManifestBatchSize)
            {
                foreach (var item in batch)
                    await writer.WriteLineAsync(item.AsMemory(), cancellationToken);

                batch.Clear();
            }
        }
        finally
        {
            writerLock.Release();
        }
    }

    private async Task FlushManifestBatchAsync(
        StreamWriter writer,
        SemaphoreSlim writerLock,
        List<string> batch,
        CancellationToken cancellationToken)
    {
        await writerLock.WaitAsync(cancellationToken);
        try
        {
            foreach (var line in batch)
                await writer.WriteLineAsync(line.AsMemory(), cancellationToken);

            batch.Clear();
        }
        finally
        {
            writerLock.Release();
        }
    }

    private static async Task WritePermissionSimulationAsync(
        string permissionsPath,
        ConcurrentBag<FileRecord> records,
        CancellationToken cancellationToken)
    {
        var permissionGroups = records
            .GroupBy(r => r.PermissionLevel, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    accessLevel = g.Key,
                    fileCount = g.Count(),
                    sampleOwners = g.Select(r => r.Owner).Distinct().Take(5).ToArray(),
                    samplePaths = g.Select(r => r.RelativePath).Take(10).ToArray()
                });

        var permissionIssues = records
            .Where(r => !string.IsNullOrWhiteSpace(r.PermissionIssue))
            .GroupBy(r => r.PermissionIssue!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    issue = g.Key,
                    fileCount = g.Count(),
                    sampleOwners = g.Select(r => r.Owner).Distinct().Take(5).ToArray(),
                    samplePaths = g.Select(r => r.RelativePath).Take(10).ToArray()
                });

        var duplicateGroups = records
            .Where(r => !string.IsNullOrWhiteSpace(r.DuplicateGroup))
            .GroupBy(r => r.DuplicateGroup!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => new
                {
                    group = g.Key,
                    fileCount = g.Count(),
                    names = g.Select(r => r.FileName).ToArray(),
                    paths = g.Select(r => r.RelativePath).ToArray()
                });

        var payload = new
        {
            generatedAtUtc = DateTime.UtcNow,
            description = "Simulated permission groups for ZMS migration validation",
            levels = new[] { "Public", "Internal", "Confidential", "Restricted" },
            groups = permissionGroups,
            permissionIssues,
            duplicateGroups
        };

        await using var stream = new FileStream(permissionsPath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
        await JsonSerializer.SerializeAsync(stream, payload, new JsonSerializerOptions { WriteIndented = true }, cancellationToken);
    }

    private static GenerationSummary BuildSummary(IReadOnlyList<FileRecord> records, int folderCount, TimeSpan elapsed)
    {
        if (records.Count == 0)
        {
            return new GenerationSummary
            {
                TotalFiles = 0,
                TotalSizeBytes = 0,
                FolderCount = folderCount,
                AverageSizeBytes = 0,
                DeepestFolderDepth = 0,
                ElapsedTime = elapsed
            };
        }

        var totalSize = records.Sum(r => r.SizeBytes);
        var largest = records.MaxBy(r => r.SizeBytes);
        var deepest = records.MaxBy(r => r.FolderDepth)!;

        return new GenerationSummary
        {
            TotalFiles = records.Count,
            TotalSizeBytes = totalSize,
            FolderCount = folderCount,
            AverageSizeBytes = (double)totalSize / records.Count,
            LargestFile = largest,
            DeepestFolderDepth = deepest.FolderDepth,
            DeepestFolderPath = Path.GetDirectoryName(deepest.RelativePath) ?? string.Empty,
            ElapsedTime = elapsed,
            FilesByDepartment = records.GroupBy(r => r.Department).ToDictionary(g => g.Key, g => g.Count()),
            FilesByExtension = records.GroupBy(r => r.Extension).ToDictionary(g => g.Key, g => g.Count()),
            FilesByPermission = records.GroupBy(r => r.PermissionLevel).ToDictionary(g => g.Key, g => g.Count()),
            FilesByEdgeCase = records
                .Where(r => !string.IsNullOrWhiteSpace(r.EdgeCase))
                .GroupBy(r => r.EdgeCase!)
                .ToDictionary(g => g.Key, g => g.Count()),
            FilesByPermissionIssue = records
                .Where(r => !string.IsNullOrWhiteSpace(r.PermissionIssue))
                .GroupBy(r => r.PermissionIssue!)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private IReadOnlyDictionary<int, EdgeCaseFilePlan> BuildEdgeCasePlans()
    {
        var plans = new Dictionary<int, EdgeCaseFilePlan>();
        if (!_options.IncludeEdgeCases)
            return plans;

        var nextIndex = 0;

        void AddPlan(EdgeCaseFilePlan plan)
        {
            if (nextIndex >= _options.FileCount)
                return;

            plans[nextIndex] = plan;
            nextIndex++;
        }

        for (var i = 0; i < _options.LongPathFileCount; i++)
        {
            var fileName = $"LongPathEvidence_{i + 1:D5}.docx";
            var folderPath = BuildLongPathFolder(fileName);
            AddPlan(new EdgeCaseFilePlan(
                folderPath,
                CountPathSegments(folderPath),
                EdgeCaseDepartment,
                "docx",
                null,
                FileContentMode.Valid,
                new FileRecordOverrides
                {
                    FileName = fileName,
                    EdgeCase = "LongPath",
                    PermissionLevel = PermissionLevel.Internal.ToString(),
                    Classification = DataClassification.Internal.ToString()
                }));
        }

        for (var set = 0; set < _options.DuplicateNameSetCount; set++)
        {
            var group = $"case-collision-{set + 1:D4}";
            var variants = new[] { "Report.docx", "report.docx", "REPORT.docx" };
            for (var variant = 0; variant < variants.Length; variant++)
            {
                AddPlan(new EdgeCaseFilePlan(
                    Path.Combine("Finance", "EdgeCases", "DuplicateNames", group, $"Source{variant + 1:D2}"),
                    5,
                    "Finance",
                    "docx",
                    null,
                    FileContentMode.Valid,
                    new FileRecordOverrides
                    {
                        FileName = variants[variant],
                        EdgeCase = "DuplicateNameCaseCollision",
                        DuplicateGroup = group,
                        PermissionLevel = PermissionLevel.Internal.ToString(),
                        Classification = DataClassification.Internal.ToString()
                    }));
            }
        }

        var corruptModes = new[]
        {
            (Extension: "zip", ContentMode: FileContentMode.BrokenZip, EdgeCase: "BrokenZip", FileNamePrefix: "BrokenArchive"),
            (Extension: "pdf", ContentMode: FileContentMode.InvalidPdf, EdgeCase: "InvalidPdf", FileNamePrefix: "InvalidPdf"),
            (Extension: "docx", ContentMode: FileContentMode.EmptyOfficeDocument, EdgeCase: "EmptyDocx", FileNamePrefix: "EmptyDocument")
        };
        for (var i = 0; i < _options.CorruptedFileCount; i++)
        {
            var mode = corruptModes[i % corruptModes.Length];
            var sizeBytes = mode.ContentMode == FileContentMode.EmptyOfficeDocument
                ? 0
                : Math.Min(_options.MaxFileSizeBytes, 4096);
            AddPlan(new EdgeCaseFilePlan(
                Path.Combine("Compliance", "EdgeCases", "CorruptedFiles"),
                3,
                "Compliance",
                mode.Extension,
                sizeBytes,
                mode.ContentMode,
                new FileRecordOverrides
                {
                    FileName = $"{mode.FileNamePrefix}_{i + 1:D5}.{mode.Extension}",
                    EdgeCase = mode.EdgeCase,
                    PermissionLevel = PermissionLevel.Restricted.ToString(),
                    Classification = DataClassification.Regulated.ToString()
                }));
        }

        for (var i = 0; i < _options.SpecialCharacterFileCount; i++)
        {
            AddPlan(new EdgeCaseFilePlan(
                Path.Combine("Legal", "EdgeCases", "SpecialCharacters"),
                3,
                "Legal",
                "docx",
                null,
                FileContentMode.Valid,
                new FileRecordOverrides
                {
                    FileName = $"Contract_₹_&_#_@_(_)_percent_%__-_{i + 1:D5}.docx",
                    EdgeCase = "SpecialCharacters",
                    PermissionLevel = PermissionLevel.Confidential.ToString(),
                    Classification = DataClassification.Confidential.ToString()
                }));
        }

        for (var i = 0; i < _options.HugeSingleFolderFileCount; i++)
        {
            AddPlan(new EdgeCaseFilePlan(
                Path.Combine("HR", "HugeSingleFolder"),
                2,
                "HR",
                "xlsx",
                null,
                FileContentMode.Valid,
                new FileRecordOverrides
                {
                    FileName = $"HugeFolder_File_{i + 1:D6}.xlsx",
                    EdgeCase = "HugeSingleFolder",
                    PermissionLevel = PermissionLevel.Internal.ToString(),
                    Classification = DataClassification.Internal.ToString()
                }));
        }

        var permissionIssues = new[]
        {
            (Issue: "MissingUser", Owner: "missing.user@contoso.invalid", Permission: "MissingUser"),
            (Issue: "BrokenGroup", Owner: "SharePoint Group: Broken Finance Approvers", Permission: "BrokenGroup"),
            (Issue: "OrphanPermission", Owner: "orphaned-principal-sid-s-1-5-21-000000", Permission: "OrphanPermission")
        };
        for (var i = 0; i < _options.PermissionEdgeCaseCount; i++)
        {
            var issue = permissionIssues[i % permissionIssues.Length];
            AddPlan(new EdgeCaseFilePlan(
                Path.Combine("Compliance", "EdgeCases", "PermissionAnomalies", issue.Issue),
                4,
                "Compliance",
                "pdf",
                null,
                FileContentMode.Valid,
                new FileRecordOverrides
                {
                    FileName = $"{issue.Issue}_Permission_{i + 1:D5}.pdf",
                    Owner = issue.Owner,
                    PermissionLevel = issue.Permission,
                    PermissionIssue = issue.Issue,
                    EdgeCase = "PermissionEdgeCase",
                    Classification = DataClassification.Regulated.ToString(),
                    RetentionLabel = "Legal Hold"
                }));
        }

        return plans;
    }

    private string BuildLongPathFolder(string fileName)
    {
        var segments = new List<string>
        {
            EdgeCaseDepartment,
            "EdgeCases",
            "LongPaths"
        };

        while (Path.Combine([.. segments, fileName]).Length < _options.LongPathTargetCharacters
            && segments.Count < _options.MaxDepth)
        {
            segments.Add($"VeryLongMigrationFolderSegment{segments.Count:D2}ForPathLimitValidation");
        }

        return Path.Combine(segments.ToArray());
    }

    private static int CountPathSegments(string path)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Count(segment => !string.IsNullOrWhiteSpace(segment));
    }

    private sealed record EdgeCaseFilePlan(
        string FolderPath,
        int FolderDepth,
        string Department,
        string Extension,
        long? SizeBytes,
        FileContentMode ContentMode,
        FileRecordOverrides Overrides);
}
