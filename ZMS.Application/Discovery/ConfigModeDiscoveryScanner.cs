using System.Text.Json;
using Microsoft.Extensions.Hosting;
using ZMS.Application.EnvironmentBridge;

namespace ZMS.Application.Discovery;

public sealed class ConfigModeDiscoveryScanner : IConfigModeDiscoveryScanner
{
    private const long EstimatedFileBytes = 3_500_000L;
    private const long EstimatedFolderBytes = 128_000L;
    private const long LargePlaceholderBytes = 650L * 1024L * 1024L;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IEnvironmentConfigStorageService _environmentConfigStorage;
    private readonly IHostEnvironment _hostEnvironment;

    public ConfigModeDiscoveryScanner(
        IEnvironmentConfigStorageService environmentConfigStorage,
        IHostEnvironment hostEnvironment)
    {
        _environmentConfigStorage = environmentConfigStorage;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<DiscoveryScanResult> ScanAsync(
        string scanId,
        DiscoveryScanRequest request,
        Func<int, string, Task> reportProgress,
        CancellationToken cancellationToken = default)
    {
        await reportProgress(15, "Loading environment config");
        var config = await LoadConfigAsync(request, cancellationToken);

        await reportProgress(30, "Reading site collections from config");
        var requestedUrls = request.SiteUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim().TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var sites = requestedUrls.Count == 0
            ? config.SiteCollections
            : config.SiteCollections
                .Where(site => requestedUrls.Contains(site.Url.TrimEnd('/')))
                .ToList();

        var result = new DiscoveryScanResult
        {
            ScanId = scanId,
            ScanName = string.IsNullOrWhiteSpace(request.ScanName) ? "Config Mode Discovery" : request.ScanName,
            Mode = "config",
            Status = "running",
            StartedAt = DateTimeOffset.UtcNow
        };

        if (requestedUrls.Count > 0 && sites.Count != requestedUrls.Count)
        {
            result.Warnings.Add("One or more requested site URLs were not present in the environment config.");
        }

        await reportProgress(45, "Building inventory");
        foreach (var site in sites)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result.SiteCollections.Add(BuildSite(site, request));
        }

        await reportProgress(65, "Flattening discovery inventory");
        result.InventoryItems.AddRange(BuildInventory(result.SiteCollections));
        result.Summary = BuildSummary(result);

        await reportProgress(75, "Config discovery complete");
        return result;
    }

    private async Task<EnvironmentConfig> LoadConfigAsync(DiscoveryScanRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.EnvironmentConfigId))
        {
            var storedConfig = await _environmentConfigStorage.GetAsync(request.EnvironmentConfigId, cancellationToken);
            if (storedConfig is not null)
            {
                return storedConfig;
            }
        }

        var configPath = ResolveConfigPath(request.EnvironmentConfigPath);
        if (configPath is null)
        {
            throw new FileNotFoundException("EnvironmentConfig JSON was not found for config mode discovery.");
        }

        await using var stream = File.OpenRead(configPath);
        return await JsonSerializer.DeserializeAsync<EnvironmentConfig>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("EnvironmentConfig JSON could not be parsed.");
    }

    private string? ResolveConfigPath(string? requestedPath)
    {
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(requestedPath))
        {
            candidates.Add(requestedPath);
            if (!Path.IsPathRooted(requestedPath))
            {
                candidates.Add(Path.Combine(_hostEnvironment.ContentRootPath, requestedPath));
                candidates.Add(Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, "..", requestedPath)));
            }
        }

        candidates.Add(Path.Combine(_hostEnvironment.ContentRootPath, "App_Data", "zms-spo-environment.json"));
        candidates.Add(Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, "..", "samples", "zms-spo-environment.json")));
        candidates.Add(Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, "..", "samples", "zms-spo-environment-config.sample.json")));

        var generatedPackageRoot = Path.Combine(_hostEnvironment.ContentRootPath, "App_Data", "generated-packages");
        if (Directory.Exists(generatedPackageRoot))
        {
            candidates.AddRange(Directory
                .EnumerateFiles(generatedPackageRoot, "zms-spo-environment.json", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc));
        }

        return candidates.FirstOrDefault(File.Exists);
    }

    private static DiscoveredSiteCollection BuildSite(SiteCollectionConfig site, DiscoveryScanRequest request)
    {
        var metadataById = site.MetadataFields.ToDictionary(field => field.Id, StringComparer.OrdinalIgnoreCase);
        var discoveredSite = new DiscoveredSiteCollection
        {
            Id = site.Id,
            Title = site.Title,
            Url = site.Url,
            Department = site.Department,
            Description = site.Description
        };

        if (request.IncludeSubsites)
        {
            discoveredSite.Subsites.AddRange(site.Subsites.Select(subsite => new DiscoveredSubsite
            {
                Id = subsite.Id,
                Title = subsite.Title,
                Url = subsite.Url,
                Description = subsite.Description
            }));
        }

        if (request.IncludeMetadata)
        {
            discoveredSite.MetadataFields.AddRange(site.MetadataFields.Select(field => ToDiscoveredField(field, 0)));
        }

        if (request.IncludePermissions)
        {
            discoveredSite.SharePointGroups.AddRange(site.PermissionGroups.Select(group => new DiscoveredSharePointGroup
            {
                Id = group.Id,
                Name = group.Name,
                Role = group.Role,
                Users = [.. group.Users]
            }));

            discoveredSite.Permissions.AddRange(site.PermissionRules.Select(rule => ToPermissionEntry(site, rule)));
        }

        discoveredSite.Lists.AddRange(site.Lists.Select(list => new DiscoveredList
        {
            Id = list.Id,
            Title = list.Title,
            Description = list.Description,
            ItemCount = list.SampleItemCount,
            Fields = request.IncludeMetadata
                ? list.Columns.Select(field => ToDiscoveredField(field, Math.Max(1, list.SampleItemCount / 10))).ToList()
                : []
        }));

        foreach (var library in site.Libraries)
        {
            var folders = FlattenFolders(library.Folders)
                .Concat(FlattenFolders(site.FolderStructures).Where(folder => PathMatchesLibrary(folder.Path, library.Title)))
                .GroupBy(folder => folder.Path, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            var fileCount = request.IncludeFiles ? library.SampleFileCount : 0;
            var libraryFields = request.IncludeMetadata
                ? library.MetadataFieldIds
                    .Where(metadataById.ContainsKey)
                    .Select(fieldId => ToDiscoveredField(metadataById[fieldId], 0))
                    .ToList()
                : [];

            var brokenInheritance = site.PermissionRules.Any(rule =>
                rule.Inheritance.Equals("Broken", StringComparison.OrdinalIgnoreCase)
                && PathMatchesLibrary(rule.TargetPath, library.Title));

            var discoveredLibrary = new DiscoveredLibrary
            {
                Id = library.Id,
                Title = library.Title,
                Type = library.Type,
                Description = library.Description,
                Url = CombineUrl(site.Url, library.Title),
                FileCount = fileCount,
                FolderCount = folders.Count,
                BrokenInheritance = brokenInheritance,
                HasArchivedFolders = folders.Any(folder => folder.Archived),
                ContentTypes = BuildContentTypes(library.Type),
                MetadataFields = libraryFields,
                Folders = folders.Select(folder => ToDiscoveredFolder(folder, fileCount)).ToList()
            };

            if (request.IncludePermissions)
            {
                discoveredLibrary.Permissions.AddRange(site.PermissionRules
                    .Where(rule => PathMatchesLibrary(rule.TargetPath, library.Title))
                    .Select(rule => ToPermissionEntry(site, rule)));
            }

            if (request.IncludeFiles)
            {
                discoveredLibrary.Files.AddRange(BuildRiskFiles(site, library, discoveredLibrary.Folders));
            }

            discoveredLibrary.SizeBytes =
                (fileCount * EstimatedFileBytes)
                + (discoveredLibrary.FolderCount * EstimatedFolderBytes)
                + discoveredLibrary.Files.Sum(file => file.SizeBytes);

            discoveredSite.Libraries.Add(discoveredLibrary);
        }

        discoveredSite.ConfiguredRisks.AddRange(site.EdgeCases.Select(edgeCase => ToMigrationRisk(site, edgeCase)));
        discoveredSite.FileCount = discoveredSite.Libraries.Sum(library => library.FileCount);
        discoveredSite.FolderCount = discoveredSite.Libraries.Sum(library => library.FolderCount);
        discoveredSite.SizeBytes = discoveredSite.Libraries.Sum(library => library.SizeBytes);

        return discoveredSite;
    }

    private static List<DiscoveredInventoryItem> BuildInventory(IReadOnlyCollection<DiscoveredSiteCollection> sites)
    {
        var inventory = new List<DiscoveredInventoryItem>();

        foreach (var site in sites)
        {
            inventory.Add(new DiscoveredInventoryItem
            {
                Id = StableId(site.Title, "site", site.Url),
                SiteCollection = site.Title,
                Subsite = "Root",
                Library = "",
                ItemType = "Site Collection",
                Path = site.Url,
                FileCount = site.FileCount,
                SizeBytes = site.SizeBytes,
                MetadataCount = site.MetadataFields.Count,
                PermissionStatus = site.Permissions.Any(permission => IsBrokenOrRestricted(permission.InheritanceStatus)) ? "Broken" : "Inherited",
                RiskLevel = site.ConfiguredRisks.Any(risk => IsHigh(risk.RiskLevel)) ? "High" : site.ConfiguredRisks.Count > 0 ? "Medium" : "Low",
                ReadinessStatus = site.ConfiguredRisks.Count > 0 ? "Review required" : "Ready"
            });

            foreach (var subsite in site.Subsites)
            {
                inventory.Add(new DiscoveredInventoryItem
                {
                    Id = StableId(site.Title, "subsite", subsite.Url),
                    SiteCollection = site.Title,
                    Subsite = subsite.Title,
                    Library = "",
                    ItemType = "Subsite",
                    Path = subsite.Url,
                    PermissionStatus = "Inherited",
                    RiskLevel = "Low",
                    ReadinessStatus = "Ready"
                });
            }

            foreach (var library in site.Libraries)
            {
                var libraryRisk = RiskForLibrary(library);
                inventory.Add(new DiscoveredInventoryItem
                {
                    Id = StableId(site.Title, library.Title, "library"),
                    SiteCollection = site.Title,
                    Subsite = "Root",
                    Library = library.Title,
                    ItemType = "Library",
                    Path = library.Url,
                    FileCount = library.FileCount,
                    SizeBytes = library.SizeBytes,
                    MetadataCount = library.MetadataFields.Count,
                    PermissionStatus = library.BrokenInheritance ? "Broken" : library.Permissions.Count > 0 ? "Restricted" : "Inherited",
                    RiskLevel = libraryRisk,
                    ReadinessStatus = ReadinessForRisk(libraryRisk)
                });

                foreach (var folder in library.Folders)
                {
                    var folderRisk = folder.LongPathRisk || folder.Archived || folder.DuplicateIndicator ? "Medium" : libraryRisk;
                    inventory.Add(new DiscoveredInventoryItem
                    {
                        Id = StableId(site.Title, library.Title, folder.Path),
                        SiteCollection = site.Title,
                        Subsite = "Root",
                        Library = library.Title,
                        ItemType = "Folder",
                        Path = folder.Path,
                        FileCount = folder.FileCount,
                        SizeBytes = folder.SizeBytes,
                        MetadataCount = library.MetadataFields.Count,
                        PermissionStatus = library.BrokenInheritance ? "Broken" : library.Permissions.Count > 0 ? "Restricted" : "Inherited",
                        RiskLevel = folderRisk,
                        ReadinessStatus = folder.Archived ? "Archive review" : ReadinessForRisk(folderRisk)
                    });
                }

                foreach (var file in library.Files)
                {
                    var risk = file.LargeFileRisk || file.LongPathRisk ? "High" : file.DuplicateIndicator ? "Medium" : "Low";
                    inventory.Add(new DiscoveredInventoryItem
                    {
                        Id = StableId(site.Title, library.Title, file.Path),
                        SiteCollection = site.Title,
                        Subsite = "Root",
                        Library = library.Title,
                        ItemType = "File",
                        Path = file.Path,
                        FileCount = 1,
                        SizeBytes = file.SizeBytes,
                        MetadataCount = library.MetadataFields.Count,
                        PermissionStatus = library.BrokenInheritance ? "Broken" : "Inherited",
                        RiskLevel = risk,
                        ReadinessStatus = ReadinessForRisk(risk)
                    });
                }
            }

            foreach (var list in site.Lists)
            {
                inventory.Add(new DiscoveredInventoryItem
                {
                    Id = StableId(site.Title, list.Title, "list"),
                    SiteCollection = site.Title,
                    Subsite = "Root",
                    Library = list.Title,
                    ItemType = "List",
                    Path = CombineUrl(site.Url, list.Title),
                    FileCount = 0,
                    SizeBytes = 0,
                    MetadataCount = list.Fields.Count,
                    PermissionStatus = "Inherited",
                    RiskLevel = "Low",
                    ReadinessStatus = "Ready"
                });
            }
        }

        return inventory;
    }

    private static DiscoverySummary BuildSummary(DiscoveryScanResult result)
    {
        return new DiscoverySummary
        {
            SiteCollections = result.SiteCollections.Count,
            Subsites = result.SiteCollections.Sum(site => site.Subsites.Count),
            Libraries = result.SiteCollections.Sum(site => site.Libraries.Count),
            Lists = result.SiteCollections.Sum(site => site.Lists.Count),
            Files = result.SiteCollections.Sum(site => site.Libraries.Sum(library => library.FileCount)),
            Folders = result.SiteCollections.Sum(site => site.Libraries.Sum(library => library.FolderCount)),
            TotalStorageBytes = result.SiteCollections.Sum(site => site.SizeBytes),
            MetadataFields = result.SiteCollections.Sum(site => site.MetadataFields.Count),
            PermissionGroups = result.SiteCollections.Sum(site => site.SharePointGroups.Count),
            BrokenInheritanceCount = result.SiteCollections.Sum(site =>
                site.Permissions.Count(permission => IsBrokenOrRestricted(permission.InheritanceStatus))
                + site.Libraries.Count(library => library.BrokenInheritance)),
            LongPathRisks = result.SiteCollections.Sum(site => site.Libraries.Sum(library => library.Folders.Count(folder => folder.LongPathRisk) + library.Files.Count(file => file.LongPathRisk))),
            LargeFileRisks = result.SiteCollections.Sum(site => site.Libraries.Sum(library => library.Files.Count(file => file.LargeFileRisk))),
            MissingMetadataIssues = 0,
            ReadinessScore = 100
        };
    }

    private static DiscoveredMetadataField ToDiscoveredField(MetadataFieldConfig field, int missingValueCount)
    {
        return new DiscoveredMetadataField
        {
            Id = field.Id,
            Name = field.Name,
            FieldType = field.Type,
            Required = field.Required,
            Choices = field.Choices,
            DefaultValue = field.DefaultValue,
            MissingValueCount = field.Required ? missingValueCount : 0,
            MappedTargetField = field.Name,
            MappingRisk = field.Required && missingValueCount > 0
                ? "High"
                : field.Type.Equals("Choice", StringComparison.OrdinalIgnoreCase)
                    ? "Medium"
                    : "Low"
        };
    }

    private static DiscoveredPermissionEntry ToPermissionEntry(SiteCollectionConfig site, PermissionRuleConfig rule)
    {
        var groups = rule.Groups.Count > 0 ? rule.Groups : site.PermissionGroups.Select(group => group.Name).Take(1).ToList();
        var accessLevels = site.PermissionGroups
            .Where(group => groups.Contains(group.Name, StringComparer.OrdinalIgnoreCase))
            .Select(group => group.Role)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var users = site.PermissionGroups
            .Where(group => groups.Contains(group.Name, StringComparer.OrdinalIgnoreCase))
            .SelectMany(group => group.Users)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DiscoveredPermissionEntry
        {
            Site = site.Title,
            LibraryOrFolder = rule.TargetPath,
            InheritanceStatus = rule.Inheritance,
            Groups = groups,
            Users = users,
            AccessLevels = accessLevels,
            RiskLevel = rule.Inheritance.Equals("Broken", StringComparison.OrdinalIgnoreCase) ? "High" : "Medium",
            RecommendedAction = string.IsNullOrWhiteSpace(rule.Notes)
                ? "Validate target group mapping before migration."
                : rule.Notes
        };
    }

    private static MigrationRiskFinding ToMigrationRisk(SiteCollectionConfig site, MigrationEdgeCaseConfig edgeCase)
    {
        var combined = $"{edgeCase.Title} {edgeCase.Description} {edgeCase.AffectedPath}";
        return new MigrationRiskFinding
        {
            Id = edgeCase.Id,
            RiskType = RiskTypeFromText(combined),
            Site = site.Title,
            LibraryOrPath = edgeCase.AffectedPath,
            Path = edgeCase.AffectedPath,
            RiskLevel = edgeCase.RiskLevel,
            Description = edgeCase.Description,
            RecommendedAction = RecommendedActionFromRisk(RiskTypeFromText(combined))
        };
    }

    private static IEnumerable<FolderStructureConfig> FlattenFolders(IEnumerable<FolderStructureConfig> folders)
    {
        foreach (var folder in folders)
        {
            yield return folder;

            foreach (var child in FlattenFolders(folder.Children ?? []))
            {
                yield return child;
            }
        }
    }

    private static DiscoveredFolder ToDiscoveredFolder(FolderStructureConfig folder, int libraryFileCount)
    {
        var path = BuildFolderPath(folder);
        var fileCount = Math.Max(1, libraryFileCount / 10);
        return new DiscoveredFolder
        {
            Id = folder.Id,
            Name = folder.Name,
            Path = path,
            Archived = folder.Archived,
            LongPathRisk = folder.LongPathExample || path.Length > 250,
            DuplicateIndicator = folder.Name.Contains("duplicate", StringComparison.OrdinalIgnoreCase)
                || path.Contains("duplicate", StringComparison.OrdinalIgnoreCase),
            Depth = path.Split('/', StringSplitOptions.RemoveEmptyEntries).Length,
            FileCount = fileCount,
            SizeBytes = fileCount * EstimatedFileBytes
        };
    }

    private static List<DiscoveredFile> BuildRiskFiles(
        SiteCollectionConfig site,
        LibraryConfig library,
        IReadOnlyCollection<DiscoveredFolder> folders)
    {
        var files = new List<DiscoveredFile>();

        foreach (var folder in folders)
        {
            if (folder.LongPathRisk)
            {
                files.Add(new DiscoveredFile
                {
                    Name = "long-path-sample.docx",
                    Path = $"{folder.Path}/long-path-sample.docx",
                    SizeBytes = EstimatedFileBytes,
                    LongPathRisk = true
                });
            }

            if (folder.DuplicateIndicator)
            {
                files.Add(new DiscoveredFile
                {
                    Name = "duplicate-candidate.docx",
                    Path = $"{folder.Path}/duplicate-candidate.docx",
                    SizeBytes = EstimatedFileBytes,
                    DuplicateIndicator = true
                });
            }
        }

        if (library.Folders.Any(folder => folder.LargeFilePlaceholder)
            || site.EdgeCases.Any(edgeCase => edgeCase.AffectedPath.StartsWith(library.Title, StringComparison.OrdinalIgnoreCase)
                && edgeCase.Title.Contains("large", StringComparison.OrdinalIgnoreCase)))
        {
            files.Add(new DiscoveredFile
            {
                Name = "large-file-placeholder.bin",
                Path = $"{library.Title}/large-file-placeholder.bin",
                SizeBytes = LargePlaceholderBytes,
                LargeFileRisk = true
            });
        }

        return files;
    }

    private static string BuildFolderPath(FolderStructureConfig folder)
    {
        if (!folder.LongPathExample || folder.Path.Length > 250)
        {
            return folder.Path;
        }

        return $"{folder.Path}/Regional Governance Review/Final Client Approved Copies/Legacy Migration Batch/Archive Evidence/Extended Nested Folder";
    }

    private static List<string> BuildContentTypes(string libraryType)
    {
        var contentTypes = new List<string> { "Document" };
        if (libraryType.Contains("record", StringComparison.OrdinalIgnoreCase))
        {
            contentTypes.Add("Record");
        }
        else if (libraryType.Contains("report", StringComparison.OrdinalIgnoreCase))
        {
            contentTypes.Add("Report");
        }
        else
        {
            contentTypes.Add("Folder");
        }

        return contentTypes;
    }

    private static string RiskTypeFromText(string text)
    {
        if (text.Contains("broken", StringComparison.OrdinalIgnoreCase)
            || text.Contains("unique permission", StringComparison.OrdinalIgnoreCase))
        {
            return "Broken Permissions";
        }

        if (text.Contains("long path", StringComparison.OrdinalIgnoreCase))
        {
            return "Long Paths";
        }

        if (text.Contains("large file", StringComparison.OrdinalIgnoreCase))
        {
            return "Large Files";
        }

        if (text.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
        {
            return "Duplicate Content";
        }

        if (text.Contains("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return "Missing Metadata";
        }

        if (text.Contains("archive", StringComparison.OrdinalIgnoreCase)
            || text.Contains("archived", StringComparison.OrdinalIgnoreCase))
        {
            return "Archived Content";
        }

        return "Restricted Content";
    }

    private static string RecommendedActionFromRisk(string riskType)
    {
        return riskType switch
        {
            "Broken Permissions" => "Validate target groups and preserve unique permissions only where required.",
            "Long Paths" => "Shorten or flatten folder paths before migration.",
            "Large Files" => "Review large files for migration throttling and exceptions.",
            "Duplicate Content" => "Review duplicate candidates before migration.",
            "Missing Metadata" => "Clean required metadata before migration.",
            "Archived Content" => "Confirm archive migration or exclusion rule.",
            _ => "Review restricted content handling before migration."
        };
    }

    private static string RiskForLibrary(DiscoveredLibrary library)
    {
        if (library.BrokenInheritance
            || library.Title.Contains("confidential", StringComparison.OrdinalIgnoreCase)
            || library.Title.Contains("restricted", StringComparison.OrdinalIgnoreCase)
            || library.Type.Contains("secure", StringComparison.OrdinalIgnoreCase)
            || library.Folders.Any(folder => folder.Path.Contains("restricted", StringComparison.OrdinalIgnoreCase)
                || folder.Path.Contains("confidential", StringComparison.OrdinalIgnoreCase)))
        {
            return "High";
        }

        return library.HasArchivedFolders
            || library.Folders.Any(folder => folder.LongPathRisk || folder.DuplicateIndicator)
            || library.Permissions.Count > 0
                ? "Medium"
                : "Low";
    }

    private static string ReadinessForRisk(string riskLevel)
    {
        return riskLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || riskLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)
            ? "Needs remediation"
            : riskLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase)
                ? "Review required"
                : "Ready";
    }

    private static bool PathMatchesLibrary(string path, string libraryTitle)
    {
        return path.Equals(libraryTitle, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith($"{libraryTitle}/", StringComparison.OrdinalIgnoreCase);
    }

    private static string CombineUrl(string siteUrl, string path)
    {
        var encodedSegments = path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);
        return $"{siteUrl.TrimEnd('/')}/{string.Join("/", encodedSegments)}";
    }

    private static bool IsBrokenOrRestricted(string value)
    {
        return value.Contains("broken", StringComparison.OrdinalIgnoreCase)
            || value.Contains("restricted", StringComparison.OrdinalIgnoreCase)
            || value.Contains("unique", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHigh(string riskLevel)
    {
        return riskLevel.Equals("High", StringComparison.OrdinalIgnoreCase)
            || riskLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase);
    }

    private static string StableId(string site, string item, string suffix)
    {
        var value = $"{site}-{item}-{suffix}";
        var chars = value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join(string.Empty, chars).Replace("--", "-", StringComparison.Ordinal).Trim('-');
    }
}
