IF OBJECT_ID(N'dbo.Connections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Connections
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Type NVARCHAR(50) NOT NULL,
        Url NVARCHAR(500) NOT NULL,
        Username NVARCHAR(200) NULL,
        Password NVARCHAR(500) NULL,
        ClientId NVARCHAR(200) NULL,
        ClientSecret NVARCHAR(500) NULL,
        TenantId NVARCHAR(200) NULL,
        RootPath NVARCHAR(500) NULL,
        AdditionalSettings NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Connections_AdditionalSettings DEFAULT N'{}',
        IsEnabled BIT NOT NULL CONSTRAINT DF_Connections_IsEnabled DEFAULT 1,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_Connections_CreatedUtc DEFAULT SYSUTCDATETIME(),
        UpdatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_Connections_UpdatedUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO

IF OBJECT_ID(N'dbo.MigrationJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationJobs
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        SourceConnectionId UNIQUEIDENTIFIER NOT NULL,
        TargetConnectionId UNIQUEIDENTIFIER NOT NULL,
        SourceLocation NVARCHAR(500) NOT NULL,
        SourceLibraryName NVARCHAR(200) NULL,
        TargetSiteUrl NVARCHAR(500) NOT NULL,
        TargetLibraryName NVARCHAR(200) NOT NULL,
        PreserveMetadata BIT NOT NULL CONSTRAINT DF_MigrationJobs_PreserveMetadata DEFAULT 1,
        BatchSize INT NOT NULL CONSTRAINT DF_MigrationJobs_BatchSize DEFAULT 20,
        MaxRetryCount INT NOT NULL CONSTRAINT DF_MigrationJobs_MaxRetryCount DEFAULT 3,
        Status NVARCHAR(50) NOT NULL,
        TotalItems INT NOT NULL CONSTRAINT DF_MigrationJobs_TotalItems DEFAULT 0,
        CompletedItems INT NOT NULL CONSTRAINT DF_MigrationJobs_CompletedItems DEFAULT 0,
        FailedItems INT NOT NULL CONSTRAINT DF_MigrationJobs_FailedItems DEFAULT 0,
        LastError NVARCHAR(2000) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationJobs_CreatedUtc DEFAULT SYSUTCDATETIME(),
        StartedUtc DATETIMEOFFSET NULL,
        FinishedUtc DATETIMEOFFSET NULL,
        UpdatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationJobs_UpdatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_MigrationJobs_SourceConnection FOREIGN KEY (SourceConnectionId) REFERENCES dbo.Connections(Id),
        CONSTRAINT FK_MigrationJobs_TargetConnection FOREIGN KEY (TargetConnectionId) REFERENCES dbo.Connections(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.MigrationItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MigrationItems
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        JobId UNIQUEIDENTIFIER NOT NULL,
        FileName NVARCHAR(260) NOT NULL,
        SourcePath NVARCHAR(1000) NOT NULL,
        TargetPath NVARCHAR(1000) NULL,
        FileSizeInBytes BIGINT NOT NULL,
        Metadata NVARCHAR(MAX) NOT NULL CONSTRAINT DF_MigrationItems_Metadata DEFAULT N'{}',
        Status NVARCHAR(50) NOT NULL,
        RetryCount INT NOT NULL CONSTRAINT DF_MigrationItems_RetryCount DEFAULT 0,
        ErrorMessage NVARCHAR(2000) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_MigrationItems_CreatedUtc DEFAULT SYSUTCDATETIME(),
        StartedUtc DATETIMEOFFSET NULL,
        CompletedUtc DATETIMEOFFSET NULL,
        CONSTRAINT FK_MigrationItems_MigrationJobs FOREIGN KEY (JobId) REFERENCES dbo.MigrationJobs(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.Logs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Logs
    (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        JobId UNIQUEIDENTIFIER NULL,
        ItemId UNIQUEIDENTIFIER NULL,
        Severity NVARCHAR(50) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        Details NVARCHAR(4000) NULL,
        CreatedUtc DATETIMEOFFSET NOT NULL CONSTRAINT DF_Logs_CreatedUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Logs_MigrationJobs FOREIGN KEY (JobId) REFERENCES dbo.MigrationJobs(Id),
        CONSTRAINT FK_Logs_MigrationItems FOREIGN KEY (ItemId) REFERENCES dbo.MigrationItems(Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationJobs_Status' AND object_id = OBJECT_ID(N'dbo.MigrationJobs'))
BEGIN
    CREATE INDEX IX_MigrationJobs_Status ON dbo.MigrationJobs(Status);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationJobs_CreatedUtc' AND object_id = OBJECT_ID(N'dbo.MigrationJobs'))
BEGIN
    CREATE INDEX IX_MigrationJobs_CreatedUtc ON dbo.MigrationJobs(CreatedUtc DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MigrationItems_JobId_Status' AND object_id = OBJECT_ID(N'dbo.MigrationItems'))
BEGIN
    CREATE INDEX IX_MigrationItems_JobId_Status ON dbo.MigrationItems(JobId, Status);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Logs_JobId_CreatedUtc' AND object_id = OBJECT_ID(N'dbo.Logs'))
BEGIN
    CREATE INDEX IX_Logs_JobId_CreatedUtc ON dbo.Logs(JobId, CreatedUtc DESC);
END;
GO
