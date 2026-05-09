using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ZMS.Core.Enums;
using ZMS.Core.Models;

namespace ZMS.Infrastructure.Persistence;

public class ZmsDbContext : DbContext
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dictionaryConverter = new ValueConverter<Dictionary<string, string>, string>(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => string.IsNullOrWhiteSpace(value)
                ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<Dictionary<string, string>>(value, JsonOptions)
                    ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

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
            builder.Property(connection => connection.AdditionalSettings).HasConversion(dictionaryConverter);
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
            builder.Property(job => job.LastError).HasMaxLength(2000);
        });

        modelBuilder.Entity<MigrationItem>(builder =>
        {
            builder.ToTable("MigrationItems");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.FileName).HasMaxLength(260).IsRequired();
            builder.Property(item => item.SourcePath).HasMaxLength(1000).IsRequired();
            builder.Property(item => item.TargetPath).HasMaxLength(1000);
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
            builder.Property(item => item.ErrorMessage).HasMaxLength(2000);
            builder.Property(item => item.Metadata).HasConversion(dictionaryConverter);
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
    }
}
