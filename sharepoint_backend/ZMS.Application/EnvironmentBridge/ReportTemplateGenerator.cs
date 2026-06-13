using System.Text;

namespace ZMS.Application.EnvironmentBridge;

public sealed class ReportTemplateGenerator : IReportTemplateGenerator
{
    public IReadOnlyDictionary<string, string> GenerateReportTemplates(EnvironmentConfig config)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["reports/environment-inventory-template.csv"] = GenerateInventoryCsv(config),
            ["reports/migration-complexity-matrix.md"] = GenerateComplexityMatrix(config),
            ["reports/environment-summary.md"] = GenerateEnvironmentSummary(config),
            ["reports/execution-summary-template.json"] = GenerateExecutionSummaryJsonTemplate(),
            ["reports/execution-summary-template.md"] = GenerateExecutionSummaryMarkdownTemplate()
        };
    }

    private static string GenerateInventoryCsv(EnvironmentConfig config)
    {
        var builder = new StringBuilder();
        builder.AppendLine("SiteCollection,Url,Department,Subsites,Libraries,Lists,MetadataFields,PermissionGroups,EdgeCases");
        foreach (var site in config.SiteCollections)
        {
            builder.AppendLine($"{Escape(site.Title)},{Escape(site.Url)},{Escape(site.Department)},{site.Subsites.Count},{site.Libraries.Count},{site.Lists.Count},{site.MetadataFields.Count},{site.PermissionGroups.Count},{site.EdgeCases.Count}");
        }

        return builder.ToString();
    }

    private static string GenerateComplexityMatrix(EnvironmentConfig config)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Migration Complexity Matrix");
        builder.AppendLine();
        builder.AppendLine("| Site Collection | Libraries | Permission Rules | Edge Cases | Complexity |");
        builder.AppendLine("| --- | ---: | ---: | ---: | --- |");
        foreach (var site in config.SiteCollections)
        {
            var complexity = site.EdgeCases.Count >= 3 || site.PermissionRules.Count >= 3 ? "High" : "Medium";
            builder.AppendLine($"| {site.Title} | {site.Libraries.Count} | {site.PermissionRules.Count} | {site.EdgeCases.Count} | {complexity} |");
        }

        return builder.ToString();
    }

    private static string GenerateEnvironmentSummary(EnvironmentConfig config)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Environment Summary");
        builder.AppendLine();
        builder.AppendLine($"- Tenant: {config.TenantName}");
        builder.AppendLine($"- Site Collections: {config.SiteCollections.Count}");
        builder.AppendLine($"- Subsites: {config.SiteCollections.Sum(site => site.Subsites.Count)}");
        builder.AppendLine($"- Libraries: {config.SiteCollections.Sum(site => site.Libraries.Count)}");
        builder.AppendLine($"- Lists: {config.SiteCollections.Sum(site => site.Lists.Count)}");
        builder.AppendLine($"- Metadata Fields: {config.SiteCollections.Sum(site => site.MetadataFields.Count)}");
        builder.AppendLine($"- Permission Groups: {config.SiteCollections.Sum(site => site.PermissionGroups.Count)}");
        builder.AppendLine($"- Edge Cases: {config.SiteCollections.Sum(site => site.EdgeCases.Count)}");
        return builder.ToString();
    }

    private static string GenerateExecutionSummaryJsonTemplate()
    {
        return """
        {
          "status": "not_started",
          "startedAt": null,
          "completedAt": null,
          "created": 0,
          "skipped": 0,
          "failed": 0,
          "steps": []
        }
        """;
    }

    private static string GenerateExecutionSummaryMarkdownTemplate()
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Execution Summary");
        builder.AppendLine();
        builder.AppendLine("Execution has not been run yet.");
        builder.AppendLine();
        builder.AppendLine("| Step | Status | Created | Skipped | Failed |");
        builder.AppendLine("| --- | --- | ---: | ---: | ---: |");
        builder.AppendLine("| Check prerequisites | pending | 0 | 0 | 0 |");
        builder.AppendLine("| Create Site Collections | pending | 0 | 0 | 0 |");
        builder.AppendLine("| Create Subsites | pending | 0 | 0 | 0 |");
        builder.AppendLine("| Create Libraries Lists Metadata | pending | 0 | 0 | 0 |");
        builder.AppendLine("| Create Groups Permissions | pending | 0 | 0 | 0 |");
        builder.AppendLine("| Create Folders And Sample Files | pending | 0 | 0 | 0 |");
        builder.AppendLine("| Apply Migration Edge Cases | pending | 0 | 0 | 0 |");
        builder.AppendLine("| Generate Inventory Report | pending | 0 | 0 | 0 |");
        return builder.ToString();
    }

    private static string Escape(string value)
    {
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
