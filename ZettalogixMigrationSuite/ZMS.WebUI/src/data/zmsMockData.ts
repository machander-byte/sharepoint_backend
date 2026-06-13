import {
  AIRecommendation,
  Connection,
  DashboardStat,
  MetadataMapping,
  MigrationJob,
  ModernizationItem,
  PermissionRisk,
  ReportItem,
  RiskItem,
  SiteCollection
} from "../types/zms";

const metadataFields = [
  { id: "department", name: "Department", type: "Choice" as const, usedIn: "All libraries", required: true },
  { id: "region", name: "Region", type: "Choice" as const, usedIn: "3 libraries", required: false },
  { id: "sensitivity", name: "Sensitivity", type: "Choice" as const, usedIn: "5 libraries", required: true },
  { id: "owner", name: "Business Owner", type: "Person" as const, usedIn: "4 libraries", required: true },
  { id: "retention", name: "Retention Date", type: "Date" as const, usedIn: "2 libraries", required: false },
  { id: "document-id", name: "Document ID", type: "Text" as const, usedIn: "All libraries", required: true },
  { id: "cost-center", name: "Cost Center", type: "Number" as const, usedIn: "2 libraries", required: false },
  { id: "term", name: "Business Term", type: "Managed Metadata" as const, usedIn: "3 libraries", required: false }
];

function buildSite(
  id: string,
  name: string,
  department: string,
  subsites: string[],
  libraries: Array<[string, string, number, number, string, "Low" | "Medium" | "High" | "Critical"]>,
  edgeCases: Array<[string, string, "Low" | "Medium" | "High" | "Critical"]>,
  lists: string[],
  groups: string[]
): SiteCollection {
  return {
    id,
    name,
    department,
    description: `${name} test site collection with representative SharePoint content, permissions, metadata, and migration edge cases.`,
    owner: `${department} Operations`,
    subsites: subsites.map((subsite) => ({
      id: `${id}-${subsite.toLowerCase().replace(/\s+/g, "-")}`,
      name: subsite,
      description: `${subsite} workspace for ${department.toLowerCase()} content.`
    })),
    libraries: libraries.map(([libraryName, type, metadataCount, files, permissionStatus, riskLevel], index) => ({
      id: `${id}-library-${index + 1}`,
      name: libraryName,
      type,
      metadataCount,
      files,
      storageGb: Math.max(1, Math.round(files / 45) / 10),
      permissionStatus,
      riskLevel
    })),
    lists: lists.map((listName, index) => ({
      id: `${id}-list-${index + 1}`,
      name: listName,
      itemCount: 40 + index * 18,
      purpose: `${listName} records for migration readiness testing.`
    })),
    metadataFields,
    permissionGroups: groups.map((group, index) => ({
      id: `${id}-group-${index + 1}`,
      name: group,
      role: index === 0 ? "Full Control" : index === 1 ? "Contribute" : "Read",
      users: index === 0 ? 4 : index === 1 ? 18 : 42
    })),
    edgeCases: edgeCases.map(([title, description, riskLevel], index) => ({
      id: `${id}-edge-${index + 1}`,
      title,
      description,
      riskLevel
    }))
  };
}

export const siteCollections: SiteCollection[] = [
  buildSite(
    "hr-portal",
    "HR Portal",
    "HR",
    ["Recruitment", "Payroll", "Policies", "Employee Records"],
    [
      ["Employee Documents", "Document Library", 8, 260, "Inherited", "Low"],
      ["HR Reports", "Report Library", 6, 95, "Inherited", "Low"],
      ["Policies Archive", "Document Library", 7, 180, "Inherited with archived folders", "Medium"],
      ["Recruitment Files", "Document Library", 8, 310, "Restricted", "Medium"],
      ["Payroll Documents", "Secure Library", 10, 405, "Broken inheritance", "Critical"]
    ],
    [
      ["Payroll Documents / Confidential has broken inheritance", "Validate target group mapping before migration.", "Critical"],
      ["Employee Records restricted to HR Admins and HR Staff", "Confirm restricted access maps to Microsoft 365 groups.", "High"],
      ["Policies Archive contains archived folders", "Archive markers should be preserved or excluded by rule.", "Medium"]
    ],
    ["Employees", "Leave Requests", "Recruitment Pipeline", "Policy Review Tracker"],
    ["HR Admins", "HR Staff", "Employees"]
  ),
  buildSite(
    "finance-hub",
    "Finance Hub",
    "Finance",
    ["Invoices", "Budgeting", "Audit", "Taxation"],
    [
      ["Financial Reports", "Report Library", 7, 210, "Inherited", "Low"],
      ["Vendor Bills", "Document Library", 8, 185, "Inherited", "Medium"],
      ["Audit Evidence", "Secure Library", 9, 140, "Restricted", "High"],
      ["Tax Filings", "Records Library", 8, 120, "Inherited", "Medium"],
      ["Budget Files", "Document Library", 6, 155, "Inherited", "Low"]
    ],
    [
      ["Audit Evidence has restricted groups", "Review target access before pilot migration.", "High"],
      ["Tax Documents restricted", "Preserve restricted access for tax records.", "High"],
      ["Vendor Bills / High Value Vendors has unique permissions", "Preserve unique permissions for high-value vendors.", "Medium"]
    ],
    ["Vendors", "Expense Requests", "Audit Tracking", "Budget Approvals"],
    ["Finance Admins", "Finance Team", "Executives"]
  ),
  buildSite(
    "it-operations",
    "IT Operations",
    "IT",
    ["Infrastructure", "Security", "DevOps", "Helpdesk"],
    [
      ["Architecture Docs", "Document Library", 8, 175, "Inherited", "Low"],
      ["Deployment Scripts", "Document Library", 7, 220, "Restricted", "High"],
      ["Security Policies", "Secure Library", 9, 110, "Restricted", "High"],
      ["Incident Evidence", "Records Library", 8, 145, "Broken inheritance", "Critical"],
      ["Helpdesk Attachments", "Document Library", 6, 255, "Inherited", "Low"]
    ],
    [
      ["Security Policies IT Admins only", "Map least-privilege groups in target.", "High"],
      ["Incident Evidence / Critical Incidents has broken inheritance", "Preserve unique permissions for critical incident records.", "Critical"],
      ["Deployment Scripts restricted to Engineers and IT Admins", "Restrict script access to operational owners.", "High"]
    ],
    ["Support Tickets", "Assets", "Server Inventory", "Change Requests"],
    ["IT Admins", "Engineers", "Employees"]
  ),
  buildSite(
    "project-management-office",
    "Project Management Office",
    "PMO",
    ["Client A", "Client B", "Internal Projects", "Archive"],
    [
      ["Project Documents", "Document Library", 8, 260, "Inherited", "Low"],
      ["Deliverables", "Document Library", 7, 210, "Inherited", "Medium"],
      ["Contracts", "Document Library", 6, 115, "Broken inheritance", "High"],
      ["Meeting Notes", "Document Library", 5, 85, "Inherited", "Low"],
      ["UAT Documents", "Records Library", 8, 220, "Inherited", "Medium"]
    ],
    [
      ["Contracts has broken inheritance", "Validate contract access before migration.", "High"],
      ["Client A folders simulate restricted client access", "Validate external user handling.", "High"],
      ["Archive / 2021 / Old Deliverables has long paths", "Apply path remediation rules.", "Medium"]
    ],
    ["Tasks", "Risks", "Milestones", "Client Contacts"],
    ["PMO Admins", "Project Managers", "Clients"]
  ),
  buildSite(
    "operations-center",
    "Operations Center",
    "Operations",
    ["Logistics", "Procurement", "Vendors", "Reports"],
    [
      ["Procurement Docs", "Document Library", 7, 165, "Inherited", "Low"],
      ["Vendor Agreements", "Secure Library", 9, 205, "Restricted", "High"],
      ["Operations Reports", "Report Library", 6, 130, "Inherited", "Low"],
      ["Compliance Records", "Records Library", 8, 180, "Restricted", "Medium"],
      ["Inspection Reports", "Report Library", 8, 230, "Inherited", "Low"]
    ],
    [
      ["Vendor Agreements has unique permissions", "Review guest access before cutover.", "High"],
      ["Compliance Records / Expired restricted", "Apply restricted access to expired compliance records.", "Medium"],
      ["Procurement Docs has duplicate folder structures", "Review duplicate folder cleanup.", "Medium"]
    ],
    ["Shipments", "Purchase Orders", "Vendor Tracking", "Operations Tasks"],
    ["Operations Admins", "Ops Team", "Management"]
  )
];

export const connections: Connection[] = [
  {
    id: "spo-source",
    name: "SharePoint Online Source",
    kind: "Source",
    provider: "SharePoint Online",
    status: "Connected",
    tenant: "zettalogix.sharepoint.com",
    authMethod: "App-Only Certificate",
    lastSync: "10 mins ago",
    actions: ["Test", "Configure"]
  },
  {
    id: "spo-target",
    name: "SharePoint Online Target",
    kind: "Target",
    provider: "SharePoint Online",
    status: "Warning",
    tenant: "zettalogix.sharepoint.com",
    warning: "Microsoft Graph permission missing: Files.ReadWrite.All",
    actions: ["Fix Permissions", "Configure"]
  },
  {
    id: "sharepoint-on-prem",
    name: "SharePoint On-Prem",
    kind: "Source",
    provider: "SharePoint Server",
    status: "Config Required",
    message: "Agent installation required on target farm servers before connection can be established.",
    actions: ["Download Agent", "Configure"]
  },
  {
    id: "box",
    name: "Box",
    kind: "Source",
    provider: "Box Enterprise",
    status: "Disconnected",
    message: "OAuth 2.0 authorization required to connect to Box Enterprise.",
    actions: ["Configure"]
  },
  {
    id: "google-drive",
    name: "Google Drive",
    kind: "Source",
    provider: "Google Workspace",
    status: "Disconnected",
    message: "Google Workspace connection required.",
    actions: ["Configure"]
  },
  {
    id: "file-share",
    name: "File Share",
    kind: "Source",
    provider: "SMB",
    status: "Disconnected",
    message: "Network path and credentials required for SMB file share.",
    actions: ["Configure"]
  }
];

export const dashboardStats: DashboardStat[] = [
  { id: "site-collections", label: "Site Collections", value: 5, tone: "primary" },
  { id: "subsites", label: "Subsites", value: 20 },
  { id: "libraries", label: "Libraries", value: 25 },
  { id: "files", label: "Files", value: "1,250" },
  { id: "storage", label: "Total Storage", value: "42 GB" },
  { id: "permission-risks", label: "Permission Risks", value: 12, tone: "error" },
  { id: "metadata-issues", label: "Metadata Issues", value: 84, tone: "warning" },
  { id: "active-jobs", label: "Active Jobs", value: 3, tone: "success" }
];

export const riskOverview: RiskItem[] = [
  { id: "broken-permissions", riskType: "Broken Permissions", count: 12, severity: "High", affectedArea: "HR, PMO, Operations", recommendedAction: "Validate group mapping" },
  { id: "long-paths", riskType: "Long Paths", count: 18, severity: "Medium", affectedArea: "Finance and Operations archives", recommendedAction: "Apply path remediation" },
  { id: "large-files", riskType: "Large Files", count: 6, severity: "Medium", affectedArea: "Reports libraries", recommendedAction: "Review exceptions" },
  { id: "duplicates", riskType: "Duplicate Content", count: 34, severity: "Low", affectedArea: "Templates and exports", recommendedAction: "Run duplicate cleanup" },
  { id: "missing-metadata", riskType: "Missing Metadata", count: 84, severity: "Low", affectedArea: "All departments", recommendedAction: "Apply AI metadata rules" }
];

export const recentActivity = [
  { id: "activity-1", title: "Environment template generated", time: "10 mins ago", detail: "HR Portal test environment preview was generated." },
  { id: "activity-2", title: "Finance metadata rules updated", time: "35 mins ago", detail: "Department and fiscal year normalization rules were revised." },
  { id: "activity-3", title: "SharePoint target connection warning detected", time: "1 hr ago", detail: "Files.ReadWrite.All permission is missing on the target connection." },
  { id: "activity-4", title: "Discovery scan completed", time: "2 hrs ago", detail: "Operations Center scan completed with 4 medium-risk findings." }
];

export const permissionsRisks: PermissionRisk[] = [
  { id: "payroll-confidential", site: "HR Portal", location: "Payroll Documents / Confidential", inheritanceStatus: "Broken", groups: "HR Admins, Executives", users: 2, riskLevel: "Critical", recommendedAction: "Validate Mapping" },
  { id: "employee-records", site: "HR Portal", location: "Employee Records", inheritanceStatus: "Broken", groups: "HR Admins, HR Staff", users: 18, riskLevel: "High", recommendedAction: "Map restricted groups" },
  { id: "audit-evidence", site: "Finance Hub", location: "Audit Evidence", inheritanceStatus: "Broken", groups: "Finance Controllers", users: 6, riskLevel: "High", recommendedAction: "Confirm target owners" },
  { id: "client-deliverables", site: "Project Management Office", location: "Client Deliverables", inheritanceStatus: "Broken", groups: "Project Team, Client Guests", users: 15, riskLevel: "High", recommendedAction: "Review external users" },
  { id: "vendor-contracts", site: "Operations Center", location: "Vendor Contracts", inheritanceStatus: "Broken", groups: "Procurement, Legal", users: 12, riskLevel: "Medium", recommendedAction: "Remove stale users" }
];

export const metadataMappings: MetadataMapping[] = [
  { id: "document-id", sourceField: "Document_ID", fieldType: "Text", usedIn: "25 libraries", targetField: "Document ID", mappingStatus: "Mapped" },
  { id: "dept", sourceField: "Dept", fieldType: "Choice", usedIn: "12 libraries", targetField: "Department", mappingStatus: "Conflict", issue: "Choice values do not fully match target values." },
  { id: "legacy-author", sourceField: "Legacy_Author", fieldType: "Person", usedIn: "18 libraries", targetField: "Created By", mappingStatus: "Suggested" },
  { id: "review-date", sourceField: "ReviewDate", fieldType: "Date", usedIn: "9 libraries", targetField: "Review Date", mappingStatus: "Mapped" },
  { id: "sensitivity", sourceField: "Security Classification", fieldType: "Choice", usedIn: "16 libraries", targetField: "Sensitivity", mappingStatus: "Unmapped", issue: "Target field exists but term values need cleanup." }
];

export const modernizationItems: ModernizationItem[] = [
  { id: "hr-onboarding", legacyAsset: "HR Employee Onboarding Leave Request", sourceType: "SharePoint Designer", department: "HR", complexity: "High", recommendedTarget: "Power Automate", confidence: 92 },
  { id: "expense-claims", legacyAsset: "Q3 Expense Claim Processing", sourceType: "InfoPath 2013", department: "Finance", complexity: "Medium", recommendedTarget: "Power Apps Canvas", confidence: 85 },
  { id: "sales-summary", legacyAsset: "Monthly Regional Sales Summary", sourceType: "SSRS", department: "Operations", complexity: "Low", recommendedTarget: "Power BI", confidence: 98 },
  { id: "vendor-approval", legacyAsset: "Custom Vendor Approval Gateway", sourceType: "K2 BlackPearl", department: "Operations", complexity: "High", recommendedTarget: "Azure Logic Apps", confidence: 72 },
  { id: "ticket-intake", legacyAsset: "IT Helpdesk Ticket Intake Form", sourceType: "InfoPath 2010", department: "IT", complexity: "Medium", recommendedTarget: "Power Apps Model", confidence: 78 }
];

export const jobs: MigrationJob[] = [
  { id: "job-hr", name: "HR_Portal_Pilot", source: "HR Portal", target: "SharePoint Online / HR", progress: 68, filesMigrated: 850, totalFiles: 1250, errors: 0, started: "10:24 AM Today", status: "Running" },
  { id: "job-finance", name: "Finance_Metadata_DryRun", source: "Finance Hub", target: "SharePoint Online / Finance", progress: 100, filesMigrated: 670, totalFiles: 670, errors: 0, started: "08:15 AM Today", status: "Completed" },
  { id: "job-ops", name: "Operations_LongPath_Test", source: "Operations Center", target: "SharePoint Online / Ops", progress: 32, filesMigrated: 210, totalFiles: 650, errors: 5, started: "07:45 AM Today", status: "Failed" },
  { id: "job-pmo", name: "PMO_Client_A_Wave", source: "Project Management Office", target: "SharePoint Online / PMO", progress: 0, filesMigrated: 0, totalFiles: 620, errors: 0, started: "Next: 23:00 UTC", status: "Scheduled" }
];

export const reports: ReportItem[] = [
  { id: "environment-inventory", title: "Environment Inventory Report", description: "Site, subsite, library, list, and storage inventory.", lastGenerated: "May 12, 2026", formats: ["CSV", "PDF", "JSON"] },
  { id: "permission-risk", title: "Permission Risk Report", description: "Broken inheritance, unique permissions, and external access.", lastGenerated: "May 12, 2026", formats: ["CSV", "PDF", "JSON"] },
  { id: "metadata-mapping", title: "Metadata Mapping Report", description: "Source field mappings, conflicts, and cleanup opportunities.", lastGenerated: "May 11, 2026", formats: ["CSV", "PDF", "JSON"] },
  { id: "large-file", title: "Large File Report", description: "Files requiring special migration handling.", lastGenerated: "May 10, 2026", formats: ["CSV", "PDF"] },
  { id: "long-path", title: "Long Path Report", description: "Paths requiring truncation, rename, or flattening rules.", lastGenerated: "May 10, 2026", formats: ["CSV", "JSON"] },
  { id: "readiness", title: "Migration Readiness Report", description: "Readiness score by site collection and workload.", lastGenerated: "May 12, 2026", formats: ["PDF", "JSON"] },
  { id: "validation", title: "Post-Migration Validation Report", description: "Target validation and reconciliation summary.", lastGenerated: "Not generated", formats: ["CSV", "PDF", "JSON"] }
];

export const aiRecommendations: AIRecommendation[] = [
  { id: "permission-cleanup", category: "Permission Cleanup", issue: "Unique permissions found in Payroll Documents / Confidential.", impact: "Target users may gain or lose access if groups are not mapped.", suggestedAction: "Validate HR Admins and Executives mapping before migration.", confidence: 94, affectedLocation: "HR Portal / Payroll Documents" },
  { id: "metadata-standardization", category: "Metadata Standardization", issue: "Department, Region, and Sensitivity values vary across departments.", impact: "Search, filtering, and retention rules may be inconsistent.", suggestedAction: "Apply standardized choice values before final migration.", confidence: 91, affectedLocation: "All site collections" },
  { id: "archive-strategy", category: "Archive Strategy", issue: "Archived folders are mixed with active content.", impact: "Migration waves may carry unnecessary historical content.", suggestedAction: "Separate archive libraries or skip archived folders by rule.", confidence: 86, affectedLocation: "Policies Archive, PMO Archive, Shipment Archive" },
  { id: "long-path-remediation", category: "Long Path Remediation", issue: "Nested folders exceed recommended path length.", impact: "Items may fail or need renaming in SharePoint Online.", suggestedAction: "Apply folder flattening and rename previews.", confidence: 82, affectedLocation: "Finance Hub / Budget Workbooks" },
  { id: "duplicate-content", category: "Duplicate Content Cleanup", issue: "Template and export duplicates detected.", impact: "Unnecessary storage and noisy search results.", suggestedAction: "Review duplicates before scheduling migration waves.", confidence: 79, affectedLocation: "IT Operations, PMO" },
  { id: "modernization", category: "Modernization Opportunities", issue: "Legacy workflows and InfoPath forms should be modernized.", impact: "Classic dependencies may not work after migration.", suggestedAction: "Plan Power Automate and Power Apps replacements.", confidence: 88, affectedLocation: "HR, Finance, IT" }
];
