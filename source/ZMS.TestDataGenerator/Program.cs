using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZMS.TestDataGenerator.Extensions;
using ZMS.TestDataGenerator.Services;

var commandLineMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    ["--files"] = "Generation:FileCount",
    ["--depth"] = "Generation:MaxDepth",
    ["--max-size"] = "Generation:MaxFileSizeMb",
    ["--output"] = "Generation:OutputPath",
    ["--parallelism"] = "Generation:Parallelism",
    ["--edge-cases"] = "Generation:IncludeEdgeCases",
    ["--long-path-files"] = "Generation:LongPathFileCount",
    ["--long-path-chars"] = "Generation:LongPathTargetCharacters",
    ["--duplicate-name-sets"] = "Generation:DuplicateNameSetCount",
    ["--corrupt-files"] = "Generation:CorruptedFileCount",
    ["--special-char-files"] = "Generation:SpecialCharacterFileCount",
    ["--huge-folder-files"] = "Generation:HugeSingleFolderFileCount",
    ["--permission-edge-files"] = "Generation:PermissionEdgeCaseCount"
};

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
        .AddCommandLine(args, commandLineMap);

    builder.Logging.ClearProviders();
    builder.Logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });

    builder.Services
        .ConfigureGenerationOptions(builder.Configuration)
        .AddTestDataGeneratorServices();

    using var host = builder.Build();

    if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) ||
        args.Contains("-h", StringComparer.OrdinalIgnoreCase))
    {
        PrintHelp();
        return 0;
    }

    var generator = host.Services.GetRequiredService<IDataGeneratorService>();
    await generator.GenerateAsync(CancellationToken.None);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal error: {ex.Message}");
    Console.Error.WriteLine(ex);
    return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
        ZMS.TestDataGenerator - Enterprise migration test dataset generator

        Usage:
          ZMS.TestDataGenerator [options]

        Options:
          --files <count>       Number of files to generate (presets: 100, 1000, 10000, 50000, 100000)
          --depth <levels>      Maximum folder nesting depth (1-20)
          --max-size <mb>       Maximum file size in megabytes (supports up to 2048 MB / 2 GB per file)
          --output <path>       Output directory for generated dataset
          --parallelism <n>     Degree of parallel file generation (default: 4)
          --edge-cases <bool>   Include migration edge cases (default: true)
          --long-path-files <n> Number of 300+ character path files
          --long-path-chars <n> Target relative path length for long-path files
          --duplicate-name-sets <n>
                               Number of Report.docx / report.docx / REPORT.docx case-collision sets
          --corrupt-files <n>   Broken ZIP, invalid PDF, and empty DOCX file count
          --special-char-files <n>
                               Filenames containing ₹, &, #, @, parentheses, %, _, and -
          --huge-folder-files <n>
                               Number of files under HR/HugeSingleFolder
          --permission-edge-files <n>
                               Missing-user, broken-group, and orphan-permission files
          --help, -h            Show this help message

        Examples:
          ZMS.TestDataGenerator --files 1000 --depth 10 --max-size 50 --output ./SmallDataset
          ZMS.TestDataGenerator --files 100000 --depth 20 --max-size 500 --huge-folder-files 10000 --output ./EnterpriseBenchmark

        Output:
          - Realistic department folder structures (HR, Finance, IT, PMO, Operations, Legal, Compliance, Vendors, Projects, Archive)
          - File types: PDF, DOCX, XLSX, PPTX, TXT, CSV, ZIP, JSON, XML
          - Edge cases: long paths, duplicate case-collision names, corrupt files, special characters, huge single folder, permission anomalies
          - Metadata manifest: _metadata/file-manifest.jsonl
          - Permission simulation: _metadata/permissions-simulation.json
          - Summary reports: _reports/generation-summary.json / .txt
        """);
}
