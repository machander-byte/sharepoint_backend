import {
  DiscoveredInventoryItem,
  DiscoveryScanResult,
  EnvironmentConfig,
  MetadataFinding,
  MigrationRiskFinding,
  PermissionRiskFinding,
  RiskLevel
} from "../types/zms";

function riskFromRules(ruleCount: number, edgeCount: number): RiskLevel {
  if (edgeCount > 1 || ruleCount > 1) {
    return "High";
  }
  if (edgeCount === 1 || ruleCount === 1) {
    return "Medium";
  }
  return "Low";
}

function readinessForRisk(risk: RiskLevel): string {
  if (risk === "High" || risk === "Critical") {
    return "Needs remediation";
  }
  if (risk === "Medium") {
    return "Review required";
  }
  return "Ready";
}

function riskTypeFromText(value: string): string {
  const text = value.toLowerCase();
  if (text.includes("broken") || text.includes("unique permission")) return "Broken Permissions";
  if (text.includes("long path")) return "Long Paths";
  if (text.includes("large file")) return "Large Files";
  if (text.includes("duplicate")) return "Duplicate Content";
  if (text.includes("metadata")) return "Missing Metadata";
  if (text.includes("archive")) return "Archived Content";
  return "Restricted Content";
}

function slug(value: string): string {
  return value.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
}

export function generateDiscoveryResults(config: EnvironmentConfig, scanId = `mock-scan-${Date.now()}`): DiscoveryScanResult {
  const inventoryItems: DiscoveredInventoryItem[] = [];
  const permissionRisks: PermissionRiskFinding[] = [];
  const metadataFindings: MetadataFinding[] = [];
  const migrationRisks: MigrationRiskFinding[] = [];

  config.siteCollections.forEach((site) => {
    const siteFileCount = site.libraries.reduce((sum, library) => sum + library.sampleFileCount, 0);
    const siteStorage = siteFileCount * 3_500_000;

    inventoryItems.push({
      id: `${site.id}-site`,
      siteCollection: site.title,
      subsite: "Root",
      library: "",
      itemType: "Site Collection",
      path: site.url,
      fileCount: siteFileCount,
      sizeBytes: siteStorage,
      metadataCount: site.metadataFields.length,
      permissionStatus: site.permissionRules.some((rule) => rule.inheritance === "Broken") ? "Broken" : "Inherited",
      riskLevel: site.edgeCases.some((edgeCase) => edgeCase.riskLevel === "Critical" || edgeCase.riskLevel === "High") ? "High" : "Medium",
      readinessStatus: site.edgeCases.length > 0 ? "Review required" : "Ready"
    });

    site.subsites.forEach((subsite) => {
      inventoryItems.push({
        id: `${site.id}-${subsite.id}`,
        siteCollection: site.title,
        subsite: subsite.title,
        library: "",
        itemType: "Subsite",
        path: subsite.url,
        fileCount: 0,
        sizeBytes: 0,
        metadataCount: 0,
        permissionStatus: "Inherited",
        riskLevel: "Low",
        readinessStatus: "Ready"
      });
    });

    site.libraries.forEach((library, index) => {
      const libraryRules = site.permissionRules.filter((rule) => rule.targetPath.startsWith(library.title));
      const libraryEdges = site.edgeCases.filter((edgeCase) => edgeCase.affectedPath.startsWith(library.title));
      const risk = riskFromRules(libraryRules.length, libraryEdges.length);
      const files = library.sampleFileCount || 20 + index * 8;

      inventoryItems.push({
        id: `${site.id}-${library.id}`,
        siteCollection: site.title,
        subsite: "Root",
        library: library.title,
        itemType: "Library",
        path: `${site.url}/${library.title}`,
        fileCount: files,
        sizeBytes: files * 3_500_000,
        metadataCount: library.metadataFieldIds.length,
        permissionStatus: libraryRules.some((rule) => rule.inheritance === "Broken") ? "Broken" : libraryRules.length > 0 ? "Restricted" : "Inherited",
        riskLevel: risk,
        readinessStatus: readinessForRisk(risk)
      });

      library.folders.forEach((folder) => {
        const folderRisk: RiskLevel = folder.longPathExample || folder.archived || folder.path.toLowerCase().includes("duplicate") ? "Medium" : risk;
        inventoryItems.push({
          id: `${site.id}-${library.id}-${folder.id}`,
          siteCollection: site.title,
          subsite: "Root",
          library: library.title,
          itemType: "Folder",
          path: folder.longPathExample
            ? `${folder.path}/Regional Governance Review/Final Client Approved Copies/Legacy Migration Batch/Archive Evidence/Extended Nested Folder`
            : folder.path,
          fileCount: Math.max(1, Math.round(files / 10)),
          sizeBytes: Math.max(1, Math.round(files / 10)) * 3_500_000,
          metadataCount: library.metadataFieldIds.length,
          permissionStatus: libraryRules.some((rule) => rule.inheritance === "Broken") ? "Broken" : "Inherited",
          riskLevel: folderRisk,
          readinessStatus: folder.archived ? "Archive review" : readinessForRisk(folderRisk)
        });
      });

      const libraryFieldIds = new Set(library.metadataFieldIds);
      site.metadataFields.forEach((field) => {
        const applied = libraryFieldIds.has(field.id);
        if (applied || field.required) {
          const missingValueCount = applied ? 0 : files;
          metadataFindings.push({
            id: `${site.id}-${library.id}-${field.id}`,
            site: site.title,
            library: library.title,
            fieldName: field.name,
            fieldType: field.type,
            required: field.required,
            missingValueCount,
            mappedTargetField: field.name,
            mappingRisk: field.required && !applied ? "High" : field.type === "Choice" ? "Medium" : "Low"
          });
        }
      });
    });

    site.lists.forEach((list) => {
      inventoryItems.push({
        id: `${site.id}-${list.id}`,
        siteCollection: site.title,
        subsite: "Root",
        library: list.title,
        itemType: "List",
        path: `${site.url}/${list.title}`,
        fileCount: 0,
        sizeBytes: 0,
        metadataCount: list.columns.length,
        permissionStatus: "Inherited",
        riskLevel: "Low",
        readinessStatus: "Ready"
      });
    });

    site.permissionRules.forEach((rule) => {
      const groups = rule.groups.length > 0 ? rule.groups : site.permissionGroups.slice(0, 1).map((group) => group.name);
      const assignedGroups = site.permissionGroups.filter((group) => groups.includes(group.name));
      permissionRisks.push({
        id: `${site.id}-${rule.id}`,
        site: site.title,
        libraryOrFolder: rule.targetPath,
        inheritanceStatus: rule.inheritance,
        groups,
        users: assignedGroups.flatMap((group) => group.users),
        accessLevels: assignedGroups.map((group) => group.role),
        riskLevel: rule.inheritance === "Broken" ? "High" : "Medium",
        recommendedAction: rule.notes || "Validate target group mapping before migration."
      });
    });

    site.edgeCases.forEach((edgeCase) => {
      migrationRisks.push({
        id: edgeCase.id,
        riskType: riskTypeFromText(`${edgeCase.title} ${edgeCase.description} ${edgeCase.affectedPath}`),
        site: site.title,
        libraryOrPath: edgeCase.affectedPath,
        path: edgeCase.affectedPath,
        riskLevel: edgeCase.riskLevel === "Critical" ? "High" : edgeCase.riskLevel,
        description: edgeCase.description,
        recommendedAction: "Review and remediate before migration."
      });
    });
  });

  metadataFindings
    .filter((finding) => finding.missingValueCount > 0 || finding.mappingRisk === "High" || finding.mappingRisk === "Critical")
    .forEach((finding) => {
      migrationRisks.push({
        id: slug(`metadata-${finding.site}-${finding.library}-${finding.fieldName}`),
        riskType: "Missing Metadata",
        site: finding.site,
        libraryOrPath: finding.library,
        path: finding.fieldName,
        riskLevel: finding.mappingRisk,
        description: `${finding.fieldName} has ${finding.missingValueCount} missing or risky values.`,
        recommendedAction: "Clean or map metadata before migration."
      });
    });

  const highPermissionIssues = permissionRisks.filter((risk) => risk.riskLevel === "High" || risk.riskLevel === "Critical").length;
  const longPathRisks = migrationRisks.filter((risk) => risk.riskType === "Long Paths").length;
  const largeFileRisks = migrationRisks.filter((risk) => risk.riskType === "Large Files").length;
  const metadataIssues = metadataFindings.filter((finding) => finding.missingValueCount > 0 || finding.mappingRisk === "High" || finding.mappingRisk === "Critical").length;
  const readinessScore = Math.max(0, 100 - highPermissionIssues * 3 - longPathRisks * 2 - metadataIssues - largeFileRisks * 2);

  return {
    scanId,
    scanName: "Mock Config Discovery",
    mode: "config",
    status: "completed",
    startedAt: new Date().toISOString(),
    completedAt: new Date().toISOString(),
    summary: {
      siteCollections: config.siteCollections.length,
      subsites: config.siteCollections.reduce((sum, site) => sum + site.subsites.length, 0),
      libraries: config.siteCollections.reduce((sum, site) => sum + site.libraries.length, 0),
      lists: config.siteCollections.reduce((sum, site) => sum + site.lists.length, 0),
      files: config.siteCollections.reduce((sum, site) => sum + site.libraries.reduce((librarySum, library) => librarySum + library.sampleFileCount, 0), 0),
      folders: inventoryItems.filter((item) => item.itemType === "Folder").length,
      totalStorageBytes: inventoryItems.reduce((sum, item) => sum + item.sizeBytes, 0),
      metadataFields: config.siteCollections.reduce((sum, site) => sum + site.metadataFields.length, 0),
      permissionGroups: config.siteCollections.reduce((sum, site) => sum + site.permissionGroups.length, 0),
      brokenInheritanceCount: permissionRisks.filter((risk) => risk.inheritanceStatus === "Broken").length,
      longPathRisks,
      largeFileRisks,
      missingMetadataIssues: metadataIssues,
      readinessScore
    },
    siteCollections: [],
    inventoryItems,
    permissionRisks,
    metadataFindings,
    migrationRisks,
    warnings: ["Backend unavailable. Showing mock discovery results from the generated environment config."],
    errors: []
  };
}
