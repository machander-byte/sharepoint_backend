import {
  BuilderOptions,
  EnvironmentConfig,
  FolderStructureConfig,
  MetadataFieldConfig,
  PermissionGroupConfig,
  PermissionRuleConfig,
  SiteCollection,
  SiteCollectionConfig,
  TenantValues
} from "../types/zms";

interface SiteBlueprint {
  id: string;
  title: string;
  defaultUrl: string;
  department: string;
  description: string;
  subsites: string[];
  libraries: string[];
  lists: string[];
  groups: string[];
  edgeCases: Array<{
    title: string;
    description: string;
    riskLevel: "Low" | "Medium" | "High" | "Critical";
    affectedPath: string;
  }>;
}

const siteBlueprints: Record<string, SiteBlueprint> = {
  "hr-portal": {
    id: "hr-portal",
    title: "HR Portal",
    defaultUrl: "https://zettalogix.sharepoint.com/sites/ZMS-HR-Portal",
    department: "Human Resources",
    description: "Enterprise HR portal for employee records, payroll, policies, and recruiting.",
    subsites: ["Recruitment", "Payroll", "Policies", "Employee Records"],
    libraries: ["Employee Documents", "HR Reports", "Policies Archive", "Recruitment Files", "Payroll Documents"],
    lists: ["Employees", "Leave Requests", "Recruitment Pipeline", "Policy Review Tracker"],
    groups: ["HR Admins", "HR Staff", "Employees"],
    edgeCases: [
      {
        title: "Payroll Documents / Confidential has broken inheritance",
        description: "Confidential payroll folder uses unique permissions that must map to target HR groups.",
        riskLevel: "Critical",
        affectedPath: "Payroll Documents/Confidential"
      },
      {
        title: "Employee Records restricted to HR Admins and HR Staff",
        description: "Employee Records should not inherit broad employee access.",
        riskLevel: "High",
        affectedPath: "Employee Records"
      },
      {
        title: "Policies Archive contains archived folders",
        description: "Archived policy folders should be included or skipped according to migration options.",
        riskLevel: "Medium",
        affectedPath: "Policies Archive/Archived"
      }
    ]
  },
  "finance-hub": {
    id: "finance-hub",
    title: "Finance Hub",
    defaultUrl: "https://zettalogix.sharepoint.com/sites/ZMS-Finance-Hub",
    department: "Finance",
    description: "Finance workspace for reporting, vendor bills, tax documents, audit evidence, and budgeting.",
    subsites: ["Invoices", "Budgeting", "Audit", "Taxation"],
    libraries: ["Financial Reports", "Vendor Bills", "Tax Documents", "Audit Evidence", "Budget Files"],
    lists: ["Vendors", "Expense Requests", "Audit Tracking", "Budget Approvals"],
    groups: ["Finance Admins", "Finance Team", "Executives"],
    edgeCases: [
      {
        title: "Audit Evidence has broken inheritance",
        description: "Audit evidence library contains unique permissions for audit stakeholders.",
        riskLevel: "High",
        affectedPath: "Audit Evidence"
      },
      {
        title: "Tax Documents restricted",
        description: "Tax Documents requires restricted access during and after migration.",
        riskLevel: "High",
        affectedPath: "Tax Documents"
      },
      {
        title: "Vendor Bills / High Value Vendors has unique permissions",
        description: "High value vendor folder should remain restricted to Finance Admins and Executives.",
        riskLevel: "Medium",
        affectedPath: "Vendor Bills/High Value Vendors"
      }
    ]
  },
  "it-operations": {
    id: "it-operations",
    title: "IT Operations",
    defaultUrl: "https://zettalogix.sharepoint.com/sites/ZMS-IT-Operations",
    department: "IT",
    description: "IT operations workspace for infrastructure, security, deployment, incidents, and helpdesk content.",
    subsites: ["Infrastructure", "Security", "DevOps", "Helpdesk"],
    libraries: ["Architecture Docs", "Deployment Scripts", "Security Policies", "Incident Evidence", "Helpdesk Attachments"],
    lists: ["Support Tickets", "Assets", "Server Inventory", "Change Requests"],
    groups: ["IT Admins", "Engineers", "Employees"],
    edgeCases: [
      {
        title: "Security Policies IT Admins only",
        description: "Security policy library should be limited to IT Admins.",
        riskLevel: "High",
        affectedPath: "Security Policies"
      },
      {
        title: "Incident Evidence / Critical Incidents has broken inheritance",
        description: "Critical incident evidence should preserve unique permissions.",
        riskLevel: "Critical",
        affectedPath: "Incident Evidence/Critical Incidents"
      },
      {
        title: "Deployment Scripts restricted to Engineers and IT Admins",
        description: "Deployment scripts should not be readable by all employees.",
        riskLevel: "High",
        affectedPath: "Deployment Scripts"
      }
    ]
  },
  "project-management-office": {
    id: "project-management-office",
    title: "Project Management Office",
    defaultUrl: "https://zettalogix.sharepoint.com/sites/ZMS-PMO",
    department: "PMO",
    description: "PMO workspace for client projects, deliverables, contracts, meeting notes, and UAT documents.",
    subsites: ["Client A", "Client B", "Internal Projects", "Archive"],
    libraries: ["Project Documents", "Deliverables", "Contracts", "Meeting Notes", "UAT Documents"],
    lists: ["Tasks", "Risks", "Milestones", "Client Contacts"],
    groups: ["PMO Admins", "Project Managers", "Clients"],
    edgeCases: [
      {
        title: "Contracts has broken inheritance",
        description: "Contracts are restricted by project and client relationship.",
        riskLevel: "High",
        affectedPath: "Contracts"
      },
      {
        title: "Client A folders simulate restricted client access",
        description: "Client A deliverables include client-specific group access.",
        riskLevel: "High",
        affectedPath: "Deliverables/Client A"
      },
      {
        title: "Archive / 2021 / Old Deliverables has long paths",
        description: "Archive folder contains a long-path example for remediation testing.",
        riskLevel: "Medium",
        affectedPath: "Archive/2021/Old Deliverables"
      }
    ]
  },
  "operations-center": {
    id: "operations-center",
    title: "Operations Center",
    defaultUrl: "https://zettalogix.sharepoint.com/sites/ZMS-Operations",
    department: "Operations",
    description: "Operations workspace for procurement, vendor agreements, operational reporting, and compliance.",
    subsites: ["Logistics", "Procurement", "Vendors", "Reports"],
    libraries: ["Procurement Docs", "Vendor Agreements", "Operational Reports", "Compliance Records", "Inspection Reports"],
    lists: ["Shipments", "Purchase Orders", "Vendor Tracking", "Operations Tasks"],
    groups: ["Operations Admins", "Ops Team", "Management"],
    edgeCases: [
      {
        title: "Vendor Agreements has unique permissions",
        description: "Vendor Agreements should preserve vendor-specific restrictions.",
        riskLevel: "High",
        affectedPath: "Vendor Agreements"
      },
      {
        title: "Compliance Records / Expired restricted",
        description: "Expired compliance records should remain restricted to Management.",
        riskLevel: "Medium",
        affectedPath: "Compliance Records/Expired"
      },
      {
        title: "Procurement Docs has duplicate folder structures",
        description: "Duplicate folder paths test cleanup and merge recommendations.",
        riskLevel: "Medium",
        affectedPath: "Procurement Docs"
      }
    ]
  }
};

function slug(value: string): string {
  return value.replace(/&/g, "and").replace(/[^A-Za-z0-9]+/g, "-").replace(/^-|-$/g, "");
}

function buildUrl(blueprint: SiteBlueprint, targetUrlPrefix: string): string {
  const normalizedPrefix = targetUrlPrefix.trim().replace(/\/+$/, "");
  if (!normalizedPrefix || normalizedPrefix === "https://zettalogix.sharepoint.com/sites") {
    return blueprint.defaultUrl;
  }
  return `${normalizedPrefix}/${slug(`ZMS ${blueprint.title}`)}`;
}

function buildMetadata(blueprint: SiteBlueprint, includeMetadataColumns: boolean): MetadataFieldConfig[] {
  if (!includeMetadataColumns) {
    return [];
  }

  return [
    { id: `${blueprint.id}-department`, name: "Department", type: "Choice", required: true, choices: [blueprint.department] },
    { id: `${blueprint.id}-region`, name: "Region", type: "Choice", required: false, choices: ["North America", "EMEA", "APAC"] },
    { id: `${blueprint.id}-sensitivity`, name: "Sensitivity", type: "Choice", required: true, choices: ["Public", "Internal", "Confidential", "Restricted"], defaultValue: "Internal" },
    { id: `${blueprint.id}-owner`, name: "Business Owner", type: "Person", required: true },
    { id: `${blueprint.id}-retention`, name: "Retention Date", type: "Date", required: false },
    { id: `${blueprint.id}-document-id`, name: "Document ID", type: "Text", required: true },
    { id: `${blueprint.id}-migration-status`, name: "Migration Status", type: "Choice", required: false, choices: ["Ready", "Blocked", "Migrated"] },
    { id: `${blueprint.id}-cost-center`, name: "Cost Center", type: "Number", required: false }
  ];
}

function buildGroups(blueprint: SiteBlueprint, createPermissionGroups: boolean): PermissionGroupConfig[] {
  if (!createPermissionGroups) {
    return [];
  }

  return blueprint.groups.map((group, index) => ({
    id: `${blueprint.id}-group-${slug(group).toLowerCase()}`,
    name: group,
    role: index === 0 ? "Full Control" : index === 1 ? "Contribute" : "Read",
    users: [
      `${slug(group).toLowerCase()}-owner@zettalogix.com`,
      `${slug(group).toLowerCase()}-member@zettalogix.com`
    ]
  }));
}

function buildFolder(library: string, options: BuilderOptions, edgePath?: string): FolderStructureConfig[] {
  const base: FolderStructureConfig[] = [
    { id: `${slug(library)}-active`, name: "Active", path: `${library}/Active` },
    { id: `${slug(library)}-review`, name: "Review", path: `${library}/Review` }
  ];

  if (options.includeArchivedFolders) {
    base.push({ id: `${slug(library)}-archive`, name: "Archive", path: `${library}/Archive`, archived: true });
  }

  if (options.includeLongPathExamples) {
    base.push({
      id: `${slug(library)}-long-path`,
      name: "Long Path Example",
      path: `${library}/Archive/2021/Old Deliverables/Regional Review/Final Client Approved Copies`,
      longPathExample: true
    });
  }

  if (options.includeLargeFilePlaceholders) {
    base.push({
      id: `${slug(library)}-large-file`,
      name: "Large File Placeholder",
      path: `${library}/Large Files`,
      largeFilePlaceholder: true
    });
  }

  if (edgePath?.startsWith(library)) {
    base.push({
      id: `${slug(edgePath)}-edge`,
      name: edgePath.split("/").slice(1).join(" / ") || "Restricted",
      path: edgePath
    });
  }

  return base;
}

function buildSiteConfig(source: SiteCollection, options: BuilderOptions, tenantValues: TenantValues): SiteCollectionConfig {
  const blueprint = siteBlueprints[source.id] ?? {
    id: source.id,
    title: source.name,
    defaultUrl: `${tenantValues.targetUrlPrefix.replace(/\/+$/, "")}/${slug(source.name)}`,
    department: source.department,
    description: source.description,
    subsites: source.subsites.map((subsite) => subsite.name),
    libraries: source.libraries.map((library) => library.name),
    lists: source.lists.map((list) => list.name),
    groups: source.permissionGroups.map((group) => group.name),
    edgeCases: source.edgeCases.map((edgeCase) => ({
      title: edgeCase.title,
      description: edgeCase.description,
      riskLevel: edgeCase.riskLevel,
      affectedPath: edgeCase.title.split(" has ")[0]
    }))
  };
  const siteUrl = buildUrl(blueprint, tenantValues.targetUrlPrefix);
  const metadataFields = buildMetadata(blueprint, options.includeMetadataColumns);
  const permissionGroups = buildGroups(blueprint, options.createPermissionGroups);
  const enabledEdgeCases = options.addMigrationEdgeCases ? blueprint.edgeCases : [];

  return {
    id: blueprint.id,
    title: blueprint.title,
    url: siteUrl,
    department: blueprint.department,
    description: blueprint.description,
    subsites: options.includeDefaultSubsites
      ? blueprint.subsites.map((subsite) => ({
          id: `${blueprint.id}-subsite-${slug(subsite).toLowerCase()}`,
          title: subsite,
          url: `${siteUrl}/${slug(subsite)}`,
          description: `${subsite} subsite for ${blueprint.title}.`
        }))
      : [],
    libraries: blueprint.libraries.map((library) => ({
      id: `${blueprint.id}-library-${slug(library).toLowerCase()}`,
      title: library,
      type: library.includes("Reports") ? "Report Library" : library.includes("Evidence") || library.includes("Records") ? "Records Library" : "Document Library",
      description: `${library} library for ${blueprint.department} migration testing.`,
      metadataFieldIds: metadataFields.map((field) => field.id),
      folders: buildFolder(
        library,
        options,
        enabledEdgeCases.find((edgeCase) => edgeCase.affectedPath.startsWith(library))?.affectedPath
      ),
      sampleFileCount: options.generateSampleDocuments ? 50 : 0,
      includeVersioning: true
    })),
    lists: blueprint.lists.map((list) => ({
      id: `${blueprint.id}-list-${slug(list).toLowerCase()}`,
      title: list,
      description: `${list} list for ${blueprint.title}.`,
      columns: metadataFields.slice(0, 4),
      sampleItemCount: options.generateSampleDocuments ? 25 : 0
    })),
    metadataFields,
    permissionGroups,
    permissionRules: enabledEdgeCases.map<PermissionRuleConfig>((edgeCase) => ({
      id: `${blueprint.id}-permission-${slug(edgeCase.affectedPath).toLowerCase()}`,
      targetPath: edgeCase.affectedPath,
      inheritance: edgeCase.title.toLowerCase().includes("broken") || edgeCase.title.toLowerCase().includes("unique") ? "Broken" : "Inherited",
      groups: permissionGroups.slice(0, 2).map((group) => group.name),
      notes: edgeCase.description
    })),
    folderStructures: blueprint.libraries.flatMap((library) =>
      buildFolder(library, options, enabledEdgeCases.find((edgeCase) => edgeCase.affectedPath.startsWith(library))?.affectedPath)
    ),
    edgeCases: enabledEdgeCases.map((edgeCase) => ({
      id: `${blueprint.id}-edge-${slug(edgeCase.affectedPath).toLowerCase()}`,
      ...edgeCase
    }))
  };
}

export function generateEnvironmentConfig(
  selectedSiteCollections: SiteCollection[],
  builderOptions: BuilderOptions,
  tenantValues: TenantValues
): EnvironmentConfig {
  return {
    tenantName: tenantValues.tenantName,
    adminUrl: tenantValues.adminUrl,
    rootUrl: tenantValues.rootUrl,
    ownerEmail: tenantValues.ownerEmail,
    clientIdPlaceholder: tenantValues.clientIdPlaceholder,
    siteCollections: selectedSiteCollections.map((site) => buildSiteConfig(site, builderOptions, tenantValues)),
    globalOptions: builderOptions,
    generatedAt: new Date().toISOString(),
    generatedBy: tenantValues.generatedBy
  };
}
