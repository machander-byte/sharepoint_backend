using System.Text.Json;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZMS.Core.Enums;
using ZMS.Core.Models;

namespace ZMS.Infrastructure.Persistence;

public class ZmsDbContext : DbContext, IDataProtectionKeyContext
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ZmsDbContext(DbContextOptions<ZmsDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConnectionProfile> Connections => Set<ConnectionProfile>();
    public DbSet<MigrationJob> MigrationJobs => Set<MigrationJob>();
    public DbSet<MigrationItem> MigrationItems => Set<MigrationItem>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<DiscoveryRun> DiscoveryRuns => Set<DiscoveryRun>();
    public DbSet<DiscoveredSite> DiscoveredSites => Set<DiscoveredSite>();
    public DbSet<DiscoveredWeb> DiscoveredWebs => Set<DiscoveredWeb>();
    public DbSet<DiscoveredLibrary> DiscoveredLibraries => Set<DiscoveredLibrary>();
    public DbSet<DiscoveredListEntity> DiscoveredLists => Set<DiscoveredListEntity>();
    public DbSet<DiscoveredFolderEntity> DiscoveredFolders => Set<DiscoveredFolderEntity>();
    public DbSet<DiscoveredFileEntity> DiscoveredFiles => Set<DiscoveredFileEntity>();
    public DbSet<DiscoveredPermission> DiscoveredPermissions => Set<DiscoveredPermission>();
    public DbSet<DiscoveredSharingLink> DiscoveredSharingLinks => Set<DiscoveredSharingLink>();
    public DbSet<DiscoveredMetadataFieldEntity> DiscoveredMetadataFields => Set<DiscoveredMetadataFieldEntity>();
    public DbSet<DiscoveredContentType> DiscoveredContentTypes => Set<DiscoveredContentType>();
    public DbSet<RiskFinding> RiskFindings => Set<RiskFinding>();
    public DbSet<MigrationJobEvent> MigrationJobEvents => Set<MigrationJobEvent>();
    public DbSet<ValidationRun> ValidationRuns => Set<ValidationRun>();
    public DbSet<ValidationFinding> ValidationFindings => Set<ValidationFinding>();
    public DbSet<ValidationItemResult> ValidationItemResults => Set<ValidationItemResult>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dictionaryConverter = new ValueConverter<Dictionary<string, string>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => string.IsNullOrWhiteSpace(value)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<Dictionary<string, string>>(value, JsonOptions)
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var dictionaryComparer = new ValueComparer<Dictionary<string, string>>(
            (left, right) => DictionariesEqual(left, right),
            value => GetDictionaryHashCode(value),
            value => CloneDictionary(value));

        modelBuilder.Entity<ConnectionProfile>(builder =>
        {
            builder.ToTable("Connections");
            builder.HasKey(connection => connection.Id);
            builder.Property(connection => connection.UserId).HasMaxLength(200).IsRequired();
            builder.Property(connection => connection.Name).HasMaxLength(200).IsRequired();
            builder.Property(connection => connection.Type).HasConversion<string>().HasMaxLength(50);
            builder.Property(connection => connection.Url).HasMaxLength(500);
            builder.Property(connection => connection.Username).HasMaxLength(200);
            builder.Property(connection => connection.ClientId).HasMaxLength(200);
            builder.Property(connection => connection.TenantId).HasMaxLength(200);
            builder.Property(connection => connection.RootPath).HasMaxLength(500);
            builder.Property(connection => connection.AdditionalSettings)
                .HasConversion(dictionaryConverter)
                .Metadata.SetValueComparer(dictionaryComparer);
        });

        modelBuilder.Entity<MigrationJob>(builder =>
        {
            builder.ToTable("MigrationJobs");
            builder.HasKey(job => job.Id);
            builder.Property(job => job.UserId).HasMaxLength(200).IsRequired();
            builder.Property(job => job.Name).HasMaxLength(200).IsRequired();
            builder.Property(job => job.SourceLocation).HasMaxLength(500).IsRequired();
            builder.Property(job => job.SourceLibraryName).HasMaxLength(200);
            builder.Property(job => job.TargetSiteUrl).HasMaxLength(500).IsRequired();
            builder.Property(job => job.TargetLibraryName).HasMaxLength(200).IsRequired();
            builder.Property(job => job.TargetLibraryUrlSegment).HasMaxLength(200);
            builder.Property(job => job.TargetRootPath).HasMaxLength(500);
            builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(job => job.EnterpriseState).HasConversion<string>().HasMaxLength(50);
            builder.Property(job => job.LastError).HasMaxLength(2000);
            builder.Property(job => job.FailureReason).HasMaxLength(2000);
            builder.Property(job => job.CorrelationId).HasMaxLength(100);
            builder.Property(job => job.LeaseId).HasMaxLength(100);
        });

        modelBuilder.Entity<MigrationItem>(builder =>
        {
            builder.ToTable("MigrationItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.FileName).HasMaxLength(260).IsRequired();
            builder.Property(item => item.SourcePath).HasMaxLength(1000).IsRequired();
            builder.Property(item => item.TargetPath).HasMaxLength(1000);
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(item => item.EnterpriseState).HasConversion<string>().HasMaxLength(50);
            builder.Property(item => item.ErrorMessage).HasMaxLength(2000);
            builder.Property(item => item.Metadata)
                .HasConversion(dictionaryConverter)
                .Metadata.SetValueComparer(dictionaryComparer);
            builder.HasIndex(item => new { item.JobId, item.Status });
        });

        modelBuilder.Entity<LogEntry>(builder =>
        {
            builder.ToTable("Logs");
            builder.HasKey(log => log.Id);
            builder.Property(log => log.Severity).HasConversion<string>().HasMaxLength(50);
            builder.Property(log => log.Message).HasMaxLength(1000).IsRequired();
            builder.Property(log => log.Details).HasMaxLength(4000);
            builder.HasIndex(log => log.JobId);
        });

        modelBuilder.Entity<DataProtectionKey>(builder =>
        {
            builder.ToTable("DataProtectionKeys");
        });

        ConfigureEnterpriseDiscovery(modelBuilder);
        ConfigureEnterpriseMigration(modelBuilder);
        ConfigureAuditLogs(modelBuilder);
    }

    private static bool DictionariesEqual(
        Dictionary<string, string>? left,
        Dictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        return left.All(pair =>
            right.TryGetValue(pair.Key, out var rightValue)
            && string.Equals(pair.Value, rightValue, StringComparison.Ordinal));
    }

    private static int GetDictionaryHashCode(Dictionary<string, string>? value)
    {
        if (value is null)
        {
            return 0;
        }

        var hash = new HashCode();
        foreach (var pair in value.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(pair.Key, StringComparer.OrdinalIgnoreCase);
            hash.Add(pair.Value, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }

    private static Dictionary<string, string> CloneDictionary(Dictionary<string, string>? value) =>
        value is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(value, StringComparer.OrdinalIgnoreCase);

    private static void ConfigureAuditLogs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("AuditLogs");
            builder.HasKey(log => log.Id);
            builder.Property(log => log.UserId).HasMaxLength(200).IsRequired();
            builder.Property(log => log.Action).HasMaxLength(200).IsRequired();
            builder.Property(log => log.Method).HasMaxLength(12).IsRequired();
            builder.Property(log => log.Path).HasMaxLength(1000).IsRequired();
            builder.Property(log => log.IpAddress).HasMaxLength(100);
            builder.Property(log => log.CorrelationId).HasMaxLength(100);
            builder.HasIndex(log => new { log.UserId, log.CreatedUtc });
            builder.HasIndex(log => log.CreatedUtc);
            builder.HasIndex(log => log.CorrelationId);
        });
    }

    private static void ConfigureEnterpriseDiscovery(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscoveryRun>(builder =>
        {
            builder.ToTable("DiscoveryRuns");
            builder.HasKey(run => run.Id);
            builder.Property(run => run.Name).HasMaxLength(200).IsRequired();
            builder.Property(run => run.ProjectId).HasMaxLength(100);
            builder.Property(run => run.SourceType).HasMaxLength(80).IsRequired();
            builder.Property(run => run.Status).HasMaxLength(50).IsRequired();
            builder.Property(run => run.ErrorMessage).HasMaxLength(2000);
            builder.HasIndex(run => run.ConnectionId);
            builder.HasIndex(run => run.Status);
            builder.HasIndex(run => run.StartedAt);
            builder.HasIndex(run => run.CompletedAt);
        });

        modelBuilder.Entity<DiscoveredSite>(builder =>
        {
            builder.ToTable("DiscoveredSites");
            builder.HasKey(site => site.Id);
            builder.Property(site => site.ExternalId).HasMaxLength(200);
            builder.Property(site => site.Title).HasMaxLength(300).IsRequired();
            builder.Property(site => site.Url).HasMaxLength(1000).IsRequired();
            builder.Property(site => site.Department).HasMaxLength(100);
            builder.Property(site => site.Description).HasMaxLength(1000);
            builder.HasIndex(site => site.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredWeb>(builder =>
        {
            builder.ToTable("DiscoveredWebs");
            builder.HasKey(web => web.Id);
            builder.Property(web => web.ExternalId).HasMaxLength(200);
            builder.Property(web => web.Title).HasMaxLength(300).IsRequired();
            builder.Property(web => web.Url).HasMaxLength(1000).IsRequired();
            builder.Property(web => web.Description).HasMaxLength(1000);
            builder.HasIndex(web => web.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredLibrary>(builder =>
        {
            builder.ToTable("DiscoveredLibraries");
            builder.HasKey(library => library.Id);
            builder.Property(library => library.ExternalId).HasMaxLength(200);
            builder.Property(library => library.Title).HasMaxLength(300).IsRequired();
            builder.Property(library => library.Type).HasMaxLength(100);
            builder.Property(library => library.Url).HasMaxLength(1000);
            builder.HasIndex(library => library.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredListEntity>(builder =>
        {
            builder.ToTable("DiscoveredLists");
            builder.HasKey(list => list.Id);
            builder.Property(list => list.ExternalId).HasMaxLength(200);
            builder.Property(list => list.Title).HasMaxLength(300).IsRequired();
            builder.Property(list => list.Description).HasMaxLength(1000);
            builder.HasIndex(list => list.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredFolderEntity>(builder =>
        {
            builder.ToTable("DiscoveredFolders");
            builder.HasKey(folder => folder.Id);
            builder.Property(folder => folder.ExternalId).HasMaxLength(200);
            builder.Property(folder => folder.Name).HasMaxLength(300).IsRequired();
            builder.Property(folder => folder.Path).HasMaxLength(1500).IsRequired();
            builder.HasIndex(folder => folder.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredFileEntity>(builder =>
        {
            builder.ToTable("DiscoveredFiles");
            builder.HasKey(file => file.Id);
            builder.Property(file => file.Name).HasMaxLength(300).IsRequired();
            builder.Property(file => file.Path).HasMaxLength(1500).IsRequired();
            builder.Property(file => file.Url).HasMaxLength(1500);
            builder.HasIndex(file => file.DiscoveryRunId);
            builder.HasIndex(file => file.Path);
            builder.HasIndex(file => file.Url);
        });

        modelBuilder.Entity<DiscoveredPermission>(builder =>
        {
            builder.ToTable("DiscoveredPermissions");
            builder.HasKey(permission => permission.Id);
            builder.Property(permission => permission.Site).HasMaxLength(300);
            builder.Property(permission => permission.Scope).HasMaxLength(1000);
            builder.Property(permission => permission.Principal).HasMaxLength(300);
            builder.Property(permission => permission.PrincipalType).HasMaxLength(80);
            builder.Property(permission => permission.Role).HasMaxLength(120);
            builder.HasIndex(permission => permission.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredSharingLink>(builder =>
        {
            builder.ToTable("DiscoveredSharingLinks");
            builder.HasKey(link => link.Id);
            builder.Property(link => link.Scope).HasMaxLength(300);
            builder.Property(link => link.Path).HasMaxLength(1500);
            builder.Property(link => link.LinkType).HasMaxLength(80);
            builder.HasIndex(link => link.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredMetadataFieldEntity>(builder =>
        {
            builder.ToTable("DiscoveredMetadataFields");
            builder.HasKey(field => field.Id);
            builder.Property(field => field.Site).HasMaxLength(300);
            builder.Property(field => field.Library).HasMaxLength(300);
            builder.Property(field => field.Name).HasMaxLength(300);
            builder.Property(field => field.FieldType).HasMaxLength(80);
            builder.Property(field => field.MappingRisk).HasMaxLength(50);
            builder.HasIndex(field => field.DiscoveryRunId);
        });

        modelBuilder.Entity<DiscoveredContentType>(builder =>
        {
            builder.ToTable("DiscoveredContentTypes");
            builder.HasKey(contentType => contentType.Id);
            builder.Property(contentType => contentType.Name).HasMaxLength(300);
            builder.Property(contentType => contentType.Scope).HasMaxLength(1000);
            builder.HasIndex(contentType => contentType.DiscoveryRunId);
        });

        modelBuilder.Entity<RiskFinding>(builder =>
        {
            builder.ToTable("RiskFindings");
            builder.HasKey(finding => finding.Id);
            builder.Property(finding => finding.SourceFindingId).HasMaxLength(300);
            builder.Property(finding => finding.Category).HasMaxLength(120).IsRequired();
            builder.Property(finding => finding.Severity).HasConversion<string>().HasMaxLength(50);
            builder.Property(finding => finding.Title).HasMaxLength(300).IsRequired();
            builder.Property(finding => finding.Description).HasMaxLength(2000);
            builder.Property(finding => finding.RecommendedAction).HasMaxLength(2000);
            builder.Property(finding => finding.Site).HasMaxLength(300);
            builder.Property(finding => finding.Location).HasMaxLength(1000);
            builder.Property(finding => finding.Path).HasMaxLength(1500);
            builder.HasIndex(finding => finding.DiscoveryRunId);
            builder.HasIndex(finding => finding.Category);
            builder.HasIndex(finding => finding.Severity);
            builder.HasIndex(finding => new { finding.Category, finding.Severity });
        });
    }

    private static void ConfigureEnterpriseMigration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MigrationJobEvent>(builder =>
        {
            builder.ToTable("MigrationJobEvents");
            builder.HasKey(jobEvent => jobEvent.Id);
            builder.Property(jobEvent => jobEvent.EventType).HasMaxLength(120).IsRequired();
            builder.Property(jobEvent => jobEvent.PreviousState).HasMaxLength(50);
            builder.Property(jobEvent => jobEvent.NewState).HasMaxLength(50).IsRequired();
            builder.Property(jobEvent => jobEvent.Message).HasMaxLength(2000).IsRequired();
            builder.Property(jobEvent => jobEvent.Severity).HasConversion<string>().HasMaxLength(50);
            builder.Property(jobEvent => jobEvent.CorrelationId).HasMaxLength(100);
            builder.Property(jobEvent => jobEvent.MetadataJson).HasColumnType("text");
            builder.HasIndex(jobEvent => new { jobEvent.JobId, jobEvent.CreatedAt });
        });

        modelBuilder.Entity<ValidationRun>(builder =>
        {
            builder.ToTable("ValidationRuns");
            builder.HasKey(run => run.Id);
            builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(run => run.Summary).HasMaxLength(2000);
            builder.Property(run => run.ErrorMessage).HasMaxLength(2000);
            builder.HasIndex(run => new { run.MigrationJobId, run.StartedAt });
        });

        modelBuilder.Entity<ValidationFinding>(builder =>
        {
            builder.ToTable("ValidationFindings");
            builder.HasKey(finding => finding.Id);
            builder.Property(finding => finding.Severity).HasConversion<string>().HasMaxLength(50);
            builder.Property(finding => finding.Category).HasMaxLength(120);
            builder.Property(finding => finding.Message).HasMaxLength(2000);
            builder.Property(finding => finding.SourcePath).HasMaxLength(1500);
            builder.Property(finding => finding.TargetPath).HasMaxLength(1500);
            builder.Property(finding => finding.RecommendedAction).HasMaxLength(2000);
            builder.HasIndex(finding => finding.ValidationRunId);
        });

        modelBuilder.Entity<ValidationItemResult>(builder =>
        {
            builder.ToTable("ValidationItemResults");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.SourcePath).HasMaxLength(1500);
            builder.Property(item => item.TargetPath).HasMaxLength(1500);
            builder.Property(item => item.Status).HasMaxLength(50);
            builder.Property(item => item.DifferenceType).HasMaxLength(120);
            builder.Property(item => item.Message).HasMaxLength(2000);
            builder.HasIndex(item => item.ValidationRunId);
        });
    }
}
