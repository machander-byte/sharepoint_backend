namespace ZMS.Application.EnvironmentBridge;

public sealed class EnvironmentConfigValidator : IEnvironmentConfigValidator
{
    private const int LongPathWarningThreshold = 180;

    public ValidationResult Validate(EnvironmentConfig config)
    {
        var result = new ValidationResult
        {
            Summary = GetSummary(config)
        };

        if (string.IsNullOrWhiteSpace(config.TenantName))
        {
            result.Errors.Add("tenantName is required.");
        }

        if (string.IsNullOrWhiteSpace(config.AdminUrl))
        {
            result.Errors.Add("adminUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(config.RootUrl))
        {
            result.Errors.Add("rootUrl is required.");
        }

        if (string.IsNullOrWhiteSpace(config.OwnerEmail))
        {
            result.Errors.Add("ownerEmail is required.");
        }

        if (config.SiteCollections.Count == 0)
        {
            result.Errors.Add("At least one site collection is required.");
        }

        var siteUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var site in config.SiteCollections)
        {
            ValidateSite(site, result);

            if (!string.IsNullOrWhiteSpace(site.Url) && !siteUrls.Add(site.Url.Trim()))
            {
                result.Errors.Add($"Duplicate site URL detected: {site.Url}");
            }
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public EnvironmentSummary GetSummary(EnvironmentConfig config)
    {
        return new EnvironmentSummary
        {
            SiteCollections = config.SiteCollections.Count,
            Subsites = config.SiteCollections.Sum(site => site.Subsites.Count),
            Libraries = config.SiteCollections.Sum(site => site.Libraries.Count),
            Lists = config.SiteCollections.Sum(site => site.Lists.Count),
            MetadataFields = config.SiteCollections.Sum(site => site.MetadataFields.Count),
            PermissionGroups = config.SiteCollections.Sum(site => site.PermissionGroups.Count),
            EdgeCases = config.SiteCollections.Sum(site => site.EdgeCases.Count)
        };
    }

    private static void ValidateSite(SiteCollectionConfig site, ValidationResult result)
    {
        var siteLabel = string.IsNullOrWhiteSpace(site.Title) ? site.Id : site.Title;

        if (string.IsNullOrWhiteSpace(site.Title))
        {
            result.Errors.Add($"Site collection '{site.Id}' is missing title.");
        }

        if (string.IsNullOrWhiteSpace(site.Url))
        {
            result.Errors.Add($"Site collection '{siteLabel}' is missing URL.");
        }

        if (site.Subsites.Count == 0)
        {
            result.Errors.Add($"Site collection '{siteLabel}' must include at least one subsite.");
        }

        if (site.Libraries.Count == 0)
        {
            result.Errors.Add($"Site collection '{siteLabel}' must include at least one library.");
        }

        if (site.Lists.Count == 0)
        {
            result.Errors.Add($"Site collection '{siteLabel}' must include at least one list.");
        }

        if (site.MetadataFields.Count == 0)
        {
            result.Errors.Add($"Site collection '{siteLabel}' must include metadata fields.");
        }

        if (site.PermissionGroups.Count == 0)
        {
            result.Errors.Add($"Site collection '{siteLabel}' must include permission groups.");
        }

        var duplicateLibraryNames = site.Libraries
            .GroupBy(library => library.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => group.Key);
        foreach (var duplicateLibraryName in duplicateLibraryNames)
        {
            result.Warnings.Add($"Site collection '{siteLabel}' contains duplicate library name '{duplicateLibraryName}'.");
        }

        if (site.PermissionRules.Count == 0)
        {
            result.Warnings.Add($"Site collection '{siteLabel}' has no permission rules.");
        }

        if (site.FolderStructures.Count == 0 && site.Libraries.All(library => library.Folders.Count == 0))
        {
            result.Warnings.Add($"Site collection '{siteLabel}' has no folder structures.");
        }

        foreach (var folderPath in EnumerateFolderPaths(site))
        {
            if (folderPath.Length > LongPathWarningThreshold)
            {
                result.Warnings.Add($"Folder path may exceed recommended migration length in '{siteLabel}': {folderPath}");
            }
        }
    }

    private static IEnumerable<string> EnumerateFolderPaths(SiteCollectionConfig site)
    {
        foreach (var folder in site.FolderStructures)
        {
            foreach (var path in EnumerateFolderPaths(folder))
            {
                yield return path;
            }
        }

        foreach (var libraryFolder in site.Libraries.SelectMany(library => library.Folders))
        {
            foreach (var path in EnumerateFolderPaths(libraryFolder))
            {
                yield return path;
            }
        }
    }

    private static IEnumerable<string> EnumerateFolderPaths(FolderStructureConfig folder)
    {
        if (!string.IsNullOrWhiteSpace(folder.Path))
        {
            yield return folder.Path;
        }

        foreach (var child in folder.Children)
        {
            foreach (var childPath in EnumerateFolderPaths(child))
            {
                yield return childPath;
            }
        }
    }
}
