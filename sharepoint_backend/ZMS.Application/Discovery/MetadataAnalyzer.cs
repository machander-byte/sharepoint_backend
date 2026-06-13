namespace ZMS.Application.Discovery;

public sealed class MetadataAnalyzer : IMetadataAnalyzer
{
    public IReadOnlyCollection<MetadataFinding> Analyze(DiscoveryScanResult result)
    {
        var findings = new List<MetadataFinding>();

        foreach (var site in result.SiteCollections)
        {
            var requiredSiteFields = site.MetadataFields
                .Where(field => field.Required)
                .ToList();
            var missingRequiredFields = new Dictionary<string, MetadataFinding>(StringComparer.OrdinalIgnoreCase);

            foreach (var library in site.Libraries)
            {
                foreach (var field in library.MetadataFields)
                {
                    findings.Add(ToFinding(site.Title, library.Title, field, library.FileCount));
                }

                var libraryFieldIds = library.MetadataFields
                    .Select(field => field.Id)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var requiredField in requiredSiteFields.Where(field => !libraryFieldIds.Contains(field.Id)))
                {
                    if (!missingRequiredFields.TryGetValue(requiredField.Id, out var missingFinding))
                    {
                        missingFinding = new MetadataFinding
                        {
                            Id = StableId(site.Title, "Multiple libraries", requiredField.Name),
                            Site = site.Title,
                            Library = "Multiple libraries",
                            FieldName = requiredField.Name,
                            FieldType = requiredField.FieldType,
                            Required = true,
                            MappedTargetField = MapTargetField(requiredField.Name),
                            MappingRisk = "High"
                        };
                        missingRequiredFields[requiredField.Id] = missingFinding;
                    }

                    missingFinding.MissingValueCount += library.FileCount;
                }
            }

            findings.AddRange(missingRequiredFields.Values);

            foreach (var list in site.Lists)
            {
                foreach (var field in list.Fields)
                {
                    findings.Add(ToFinding(site.Title, list.Title, field, list.ItemCount));
                }
            }
        }

        return findings
            .GroupBy(item => $"{item.Site}|{item.Library}|{item.FieldName}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(item => RiskRank(item.MappingRisk)).First())
            .OrderByDescending(item => RiskRank(item.MappingRisk))
            .ThenBy(item => item.Site)
            .ThenBy(item => item.Library)
            .ThenBy(item => item.FieldName)
            .ToList();
    }

    private static MetadataFinding ToFinding(string site, string library, DiscoveredMetadataField field, int itemCount)
    {
        var missingCount = field.MissingValueCount;

        return new MetadataFinding
        {
            Id = StableId(site, library, field.Name),
            Site = site,
            Library = library,
            FieldName = field.Name,
            FieldType = field.FieldType,
            Required = field.Required,
            MissingValueCount = missingCount,
            MappedTargetField = string.IsNullOrWhiteSpace(field.MappedTargetField)
                ? MapTargetField(field.Name)
                : field.MappedTargetField,
            MappingRisk = NormalizeRisk(field, missingCount)
        };
    }

    private static string NormalizeRisk(DiscoveredMetadataField field, int missingCount)
    {
        if ((field.Required && missingCount > 0)
            || (field.FieldType.Equals("Person", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(field.MappedTargetField))
            || (IsSensitivityOrRetention(field.Name) && missingCount > 0)
            || field.MappingRisk.Equals("High", StringComparison.OrdinalIgnoreCase))
        {
            return "High";
        }

        if (field.FieldType.Equals("Choice", StringComparison.OrdinalIgnoreCase)
            || field.Name.Contains("department", StringComparison.OrdinalIgnoreCase)
            || field.MappingRisk.Equals("Medium", StringComparison.OrdinalIgnoreCase))
        {
            return "Medium";
        }

        return "Low";
    }

    private static bool IsSensitivityOrRetention(string fieldName)
    {
        return fieldName.Contains("sensitivity", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("retention", StringComparison.OrdinalIgnoreCase);
    }

    private static string MapTargetField(string fieldName)
    {
        return fieldName.Replace("_", " ", StringComparison.Ordinal).Trim();
    }

    private static int RiskRank(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }

    private static string StableId(string site, string library, string field)
    {
        return Slug($"{site}-{library}-{field}-metadata");
    }

    private static string Slug(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        return string.Join(string.Empty, chars).Replace("--", "-", StringComparison.Ordinal).Trim('-');
    }
}
