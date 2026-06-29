using ZMS.TestDataGenerator.Models;

namespace ZMS.TestDataGenerator.Services;

public sealed class MetadataGenerator : IMetadataGenerator
{
    private static readonly string[] Extensions = ["pdf", "docx", "xlsx", "pptx", "txt", "csv", "zip", "json", "xml"];

    private static readonly string[] Owners =
    [
        "jsmith", "mjohnson", "alee", "kpatel", "rwilson", "tchen", "dgarcia", "bthomas",
        "srodriguez", "lwhite", "hnguyen", "emartinez", "canderson", "jtaylor", "mclark"
    ];

    private static readonly string[] RetentionLabels =
    [
        "Retain 1 Year", "Retain 3 Years", "Retain 7 Years", "Retain 10 Years",
        "Permanent", "Legal Hold", "Review Annually", "Destroy After 90 Days"
    ];

    private static readonly string[] DocumentPrefixes =
    [
        "Policy", "Report", "Budget", "Contract", "Invoice", "Proposal", "Analysis",
        "Summary", "Plan", "Audit", "Review", "Statement", "Specification", "Minutes"
    ];

    public FileRecord CreateFileRecord(
        string folderPath,
        int folderDepth,
        string department,
        string extension,
        long sizeBytes,
        Random random,
        FileRecordOverrides? overrides = null)
    {
        var created = DateTime.UtcNow.AddDays(-random.Next(1, 3650));
        var modified = created.AddDays(random.Next(0, Math.Max(1, (int)(DateTime.UtcNow - created).TotalDays)));

        var permission = (PermissionLevel)random.Next(Enum.GetValues<PermissionLevel>().Length);
        var classification = MapClassification(permission, random);

        var prefix = DocumentPrefixes[random.Next(DocumentPrefixes.Length)];
        var year = random.Next(2018, 2027);
        var sequence = random.Next(1, 9999);
        var fileName = overrides?.FileName ?? $"{prefix}_{department}_{year}_{sequence:D4}.{extension}";

        return new FileRecord
        {
            RelativePath = Path.Combine(folderPath, fileName),
            FileName = fileName,
            Extension = extension,
            SizeBytes = sizeBytes,
            FolderDepth = folderDepth,
            Department = department,
            Owner = overrides?.Owner ?? Owners[random.Next(Owners.Length)],
            Classification = overrides?.Classification ?? classification.ToString(),
            RetentionLabel = overrides?.RetentionLabel ?? RetentionLabels[random.Next(RetentionLabels.Length)],
            PermissionLevel = overrides?.PermissionLevel ?? permission.ToString(),
            EdgeCase = overrides?.EdgeCase,
            PermissionIssue = overrides?.PermissionIssue,
            DuplicateGroup = overrides?.DuplicateGroup,
            CreatedDateUtc = created,
            ModifiedDateUtc = modified
        };
    }

    public static string PickExtension(Random random) => Extensions[random.Next(Extensions.Length)];

    private static DataClassification MapClassification(PermissionLevel permission, Random random) =>
        permission switch
        {
            PermissionLevel.Public => DataClassification.General,
            PermissionLevel.Internal => random.Next(2) == 0 ? DataClassification.General : DataClassification.Internal,
            PermissionLevel.Confidential => random.Next(2) == 0 ? DataClassification.Confidential : DataClassification.Regulated,
            PermissionLevel.Restricted => random.Next(2) == 0 ? DataClassification.HighlyConfidential : DataClassification.Regulated,
            _ => DataClassification.Internal
        };
}
