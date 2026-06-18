using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ZMS.Application.Services;
using ZMS.Core.Enums;
using ZMS.Core.Models;
using ZMS.Infrastructure.Persistence;
using ZMS.Infrastructure.Repositories;

namespace ZMS.Tests;

public class ValidationServiceTests
{
    [Fact]
    public async Task StartAsync_ClassifiesMissingTargetPathAsFailedFinding()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<ZmsDbContext>().UseSqlite(database).Options;
        await using var context = new ZmsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var userId = "user-1";
        var job = new MigrationJob
        {
            UserId = userId,
            Name = "Validation test",
            SourceLocation = "source",
            TargetSiteUrl = "https://contoso.sharepoint.com/sites/target",
            TargetLibraryName = "Documents",
            Status = JobStatus.CompletedWithErrors
        };
        context.MigrationJobs.Add(job);
        context.MigrationItems.Add(new MigrationItem
        {
            JobId = job.Id,
            FileName = "a.docx",
            SourcePath = "source/a.docx",
            FileSizeInBytes = 1024,
            Status = MigrationItemStatus.Completed
        });
        await context.SaveChangesAsync();

        var service = new ValidationService(
            new MigrationJobRepository(context),
            new MigrationItemRepository(context),
            new ValidationRepository(context),
            NullLogger<ValidationService>.Instance);

        var run = await service.StartAsync(job.Id, userId, CancellationToken.None);
        var findings = await service.GetFindingsAsync(run.Id, CancellationToken.None);

        Assert.Equal(ValidationRunStatus.FAILED, run.Status);
        Assert.Contains(findings, finding => finding.Category == "MissingTargetPath");
    }

    [Fact]
    public async Task StartAsync_PassesCompletedFolderItems()
    {
        await using var database = new SqliteConnection("Data Source=:memory:");
        await database.OpenAsync();

        var options = new DbContextOptionsBuilder<ZmsDbContext>().UseSqlite(database).Options;
        await using var context = new ZmsDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var userId = "user-1";
        var job = new MigrationJob
        {
            UserId = userId,
            Name = "Folder validation test",
            SourceLocation = "source",
            TargetSiteUrl = "https://contoso.sharepoint.com/sites/target",
            TargetLibraryName = "Documents",
            Status = JobStatus.Completed
        };

        context.MigrationJobs.Add(job);
        context.MigrationItems.Add(new MigrationItem
        {
            JobId = job.Id,
            FileName = "EmptyArchive",
            SourcePath = "source/Finance/EmptyArchive",
            TargetPath = "https://contoso.sharepoint.com/sites/target/Documents/Finance/EmptyArchive",
            FileSizeInBytes = 0,
            Status = MigrationItemStatus.Completed,
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [MigrationItemMetadataKeys.ItemType] = MigrationItemMetadataKeys.ItemTypeFolder,
                [MigrationItemMetadataKeys.RelativePath] = "Finance/EmptyArchive"
            }
        });
        await context.SaveChangesAsync();

        var service = new ValidationService(
            new MigrationJobRepository(context),
            new MigrationItemRepository(context),
            new ValidationRepository(context),
            NullLogger<ValidationService>.Instance);

        var run = await service.StartAsync(job.Id, userId, CancellationToken.None);
        var items = await service.GetItemsAsync(run.Id, CancellationToken.None);
        var result = Assert.Single(items);

        Assert.Equal(ValidationRunStatus.PASSED, run.Status);
        Assert.Equal("PASSED", result.Status);
        Assert.Equal("Folder path was preserved on the target.", result.Message);
    }
}
