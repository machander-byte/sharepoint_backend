namespace ZMS.TestDataGenerator.Services;

public sealed class FolderStructureService : IFolderStructureService
{
    private static readonly IReadOnlyDictionary<string, string[]> DepartmentSubfolders =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["HR"] = ["Payroll", "Benefits", "Recruitment", "Training", "Policies", "Onboarding", "Performance"],
            ["Finance"] = ["Accounting", "Budget", "Tax", "Audit", "Invoices", "Forecasting", "Treasury"],
            ["IT"] = ["Infrastructure", "Security", "Development", "Support", "Assets", "Networking", "Cloud"],
            ["PMO"] = ["Governance", "Portfolio", "Reporting", "Templates", "Standards", "Roadmaps"],
            ["Operations"] = ["Logistics", "Facilities", "Procurement", "Quality", "Maintenance", "SupplyChain"],
            ["Legal"] = ["Contracts", "Litigation", "IntellectualProperty", "Compliance", "Corporate", "Regulatory"],
            ["Compliance"] = ["Audits", "Policies", "Risk", "Controls", "Reporting", "Certifications"],
            ["Vendors"] = ["Agreements", "Invoices", "SLA", "Onboarding", "Performance", "Contacts"],
            ["Projects"] = ["Alpha", "Beta", "Gamma", "Delta", "Initiatives", "Milestones", "Deliverables"],
            ["Archive"] = ["Legacy", "Retired", "Historical", "ColdStorage", "Depreciated", "Records"]
        };

    private static readonly string[] YearFolders = ["2019", "2020", "2021", "2022", "2023", "2024", "2025", "2026"];
    private static readonly string[] QuarterFolders = ["Q1", "Q2", "Q3", "Q4"];
    private static readonly string[] GenericFolders = ["Drafts", "Final", "Review", "Approved", "Working", "Shared", "Private", "Reports", "Documents", "Attachments"];

    public IReadOnlyList<string> Departments { get; } = DepartmentSubfolders.Keys.ToList();

    public string BuildFolderPath(string department, int depth, Random random)
    {
        if (depth < 1)
            throw new ArgumentOutOfRangeException(nameof(depth));

        var segments = new List<string>(depth) { department };

        if (DepartmentSubfolders.TryGetValue(department, out var subfolders))
            segments.Add(subfolders[random.Next(subfolders.Length)]);

        while (segments.Count < depth)
        {
            var pick = random.Next(4);
            segments.Add(pick switch
            {
                0 => YearFolders[random.Next(YearFolders.Length)],
                1 => QuarterFolders[random.Next(QuarterFolders.Length)],
                2 => GenericFolders[random.Next(GenericFolders.Length)],
                _ => $"Team{random.Next(1, 20):D2}"
            });
        }

        return Path.Combine(segments.ToArray());
    }
}
