namespace ZMS.TestDataGenerator.Models;

public enum FileSizeCategory
{
    Small,
    Medium,
    Large,
    Huge
}

public enum PermissionLevel
{
    Public,
    Internal,
    Confidential,
    Restricted
}

public enum DataClassification
{
    General,
    Internal,
    Confidential,
    HighlyConfidential,
    Regulated
}

public enum FileContentMode
{
    Valid,
    BrokenZip,
    InvalidPdf,
    EmptyOfficeDocument,
    EmptyFile
}
