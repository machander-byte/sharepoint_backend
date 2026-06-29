import {
  aiRecommendations,
  connections,
  dashboardStats,
  metadataMappings,
  permissionsRisks,
  reports,
  siteCollections
} from "../data/zmsMockData";
import {
  BuilderOptions,
  Connection,
  ConfigValidationResponse,
  DemoStatus,
  DiscoveryImportResponse,
  DiscoveryScanRequest,
  DiscoveryScanResult,
  DiscoveryScanStatusResponse,
  EnvironmentConfig,
  ExecutionSimulationResponse,
  ExecutionSimulationResult,
  GeneratedPackageResult,
  CreateMigrationPlanResponse,
  CreateMigrationExecutionJobResponse,
  MigrationPlan,
  MigrationPlanValidationResult,
  MigrationExecutionJob,
  MigrationExecutionRequest,
  MigrationTransferPreview,
  MigrationRunbook,
  MigrationReadinessAssessment,
  MetadataFinding,
  MigrationRiskFinding,
  MigrationWaveSuggestion,
  PackageManifest,
  PermissionRiskFinding,
  PreMigrationCheck,
  PreMigrationValidationResponse,
  PreMigrationValidationResult,
  ReadinessAnalyzeResponse,
  RemediationAction,
  SaveConfigResponse,
  SharePointMigrationCapabilityResult,
  LivePilotMigrationRequest,
  LivePilotMigrationResult,
  SiteCollection,
  StartDiscoveryScanResponse,
  TenantValues,
  ValidationSummary,
  WorkflowValidationRequest,
  WorkflowValidationResponse,
  WorkflowValidationRun
} from "../types/zms";
import { apiGet, apiGetBlob, apiPost, apiPostForm, apiPut, hasBackendBaseUrl } from "./httpClient";
import { downloadCsv } from "../utils/downloadCsv";
import { downloadBlob } from "../utils/downloadFile";
import { downloadJson } from "../utils/downloadJson";
import { generateDiscoveryResults } from "../utils/generateDiscoveryResults";
import { generateEnvironmentConfig as buildEnvironmentConfig } from "../utils/generateEnvironmentConfig";

function delay(ms = 400): Promise<void> {
  return new Promise((resolve) => window.setTimeout(resolve, ms));
}

const mockDiscoveryScans = new Map<string, DiscoveryScanResult>();
const mockReadinessAssessments = new Map<string, MigrationReadinessAssessment>();
const mockMigrationPlans = new Map<string, MigrationPlan>();
const mockPreMigrationValidations = new Map<string, PreMigrationValidationResult>();
const mockExecutionSimulations = new Map<string, ExecutionSimulationResult>();
const mockMigrationExecutionJobs = new Map<string, MigrationExecutionJob>();
const mockTransferPreviews = new Map<string, MigrationTransferPreview>();
const mockPilotRuns = new Map<string, LivePilotMigrationResult>();
const mockWorkflowRuns = new Map<string, WorkflowValidationRun>();
let mockDemoStatus: DemoStatus = {
  demoMode: import.meta.env.VITE_DEMO_MODE === "true",
  seeded: false,
  latestScanId: "",
  latestAssessmentId: "",
  latestPlanId: "",
  latestExecutionJobId: "",
  latestPreviewId: "",
  latestWorkflowRunId: "",
  lastDemoChainResult: "",
  warnings: []
};

function getLatestMockDiscoveryScan(): DiscoveryScanResult | null {
  const values = [...mockDiscoveryScans.values()];
  return values.length > 0 ? values[values.length - 1] : null;
}

function getLatestMockReadinessAssessment(): MigrationReadinessAssessment | null {
  const values = [...mockReadinessAssessments.values()];
  return values.length > 0 ? values[values.length - 1] : null;
}

function riskLevelForScore(score: number): string {
  if (score >= 90) return "Low";
  if (score >= 75) return "Moderate";
  if (score >= 60) return "Medium";
  if (score >= 40) return "High";
  return "Critical";
}

function buildMockReadinessAssessment(scan: DiscoveryScanResult): MigrationReadinessAssessment {
  const riskFindings = [
    ...scan.permissionRisks.map((risk) => ({
      id: `perm-${risk.id}`,
      category: "Permissions",
      severity: risk.riskLevel,
      title: "Review unique permissions before migration",
      description: `Permission inheritance is ${risk.inheritanceStatus}.`,
      affectedLocation: risk.libraryOrFolder,
      affectedSite: risk.site,
      affectedLibrary: risk.libraryOrFolder,
      affectedPath: risk.libraryOrFolder,
      evidence: risk.groups.join(", "),
      impact: "Can cause incorrect access after cutover.",
      recommendedAction: risk.recommendedAction,
      canAutoRemediate: false,
      migrationBlocker: risk.riskLevel === "High" && /confidential|payroll|security|audit|contract/i.test(risk.libraryOrFolder)
    })),
    ...scan.metadataFindings.map((finding) => ({
      id: `meta-${finding.id}`,
      category: "Metadata",
      severity: finding.mappingRisk,
      title: "Standardize metadata fields",
      description: `${finding.fieldName} has ${finding.missingValueCount} missing values.`,
      affectedLocation: finding.library,
      affectedSite: finding.site,
      affectedLibrary: finding.library,
      affectedPath: finding.library,
      evidence: finding.fieldName,
      impact: "Can reduce validation quality and target search/filtering.",
      recommendedAction: "Standardize metadata mappings and fill required values.",
      canAutoRemediate: true,
      migrationBlocker: finding.required && finding.missingValueCount > 0
    })),
    ...scan.migrationRisks.map((risk) => ({
      id: `risk-${risk.id}`,
      category: risk.riskType.toLowerCase().includes("path") ? "Path Length" : risk.riskType.toLowerCase().includes("archive") ? "Archived Content" : risk.riskType.toLowerCase().includes("large") ? "Large Files" : "Governance",
      severity: risk.riskLevel,
      title: risk.riskType,
      description: risk.description,
      affectedLocation: risk.path || risk.libraryOrPath,
      affectedSite: risk.site,
      affectedLibrary: risk.libraryOrPath,
      affectedPath: risk.path,
      evidence: risk.description,
      impact: "Can increase migration risk, validation effort, or cutover time.",
      recommendedAction: risk.recommendedAction,
      canAutoRemediate: risk.riskType.toLowerCase().includes("path"),
      migrationBlocker: risk.riskLevel === "Critical" || (risk.riskLevel === "High" && risk.riskType.toLowerCase().includes("path"))
    }))
  ];
  const blockers = riskFindings.filter((risk) => risk.migrationBlocker).length;
  const highRisks = riskFindings.filter((risk) => risk.severity === "High" || risk.severity === "Critical").length;
  const mediumRisks = riskFindings.filter((risk) => risk.severity === "Medium").length;
  const lowRisks = riskFindings.filter((risk) => risk.severity === "Low").length;
  const readinessScore = Math.max(0, 100 - highRisks * 5 - mediumRisks * 3 - lowRisks - blockers * 8);
  const remediationActions: RemediationAction[] = Array.from(new Set(riskFindings.map((risk) => risk.category))).map((category) => ({
    id: `rem-${category.toLowerCase().replace(/\s+/g, "-")}`,
    priority: riskFindings.some((risk) => risk.category === category && risk.migrationBlocker) ? "High" : "Medium",
    category,
    actionTitle: category === "Metadata" ? "Standardize metadata fields" : category === "Path Length" ? "Shorten deep folder paths" : category === "Archived Content" ? "Decide archive vs migrate strategy" : "Review unique permissions before migration",
    actionDescription: riskFindings.find((risk) => risk.category === category)?.recommendedAction ?? "Review before migration.",
    affectedLocations: riskFindings.filter((risk) => risk.category === category).map((risk) => risk.affectedLocation).slice(0, 8),
    estimatedEffort: category === "Path Length" ? "High" : "Medium",
    ownerRole: category === "Metadata" ? "Information Architect" : category === "Permissions" ? "SharePoint Admin / Security Owner" : "Business Owner",
    status: "Open",
    dependsOn: [],
    expectedBenefit: "Improves migration predictability and reduces cutover risk."
  }));
  const libraries = scan.inventoryItems.filter((item) => item.itemType === "Library");
  const wave = (waveId: string, waveName: string, recommendedOrder: number, includedLibraries: string[], riskLevel: string): MigrationWaveSuggestion => ({
    waveId,
    waveName,
    description: waveName.includes("Pilot") ? "Low-risk pilot validation wave." : "Suggested analysis-only migration wave.",
    recommendedOrder,
    includedSites: Array.from(new Set(libraries.filter((item) => includedLibraries.includes(item.library)).map((item) => item.siteCollection))),
    includedLibraries,
    excludedRisks: [],
    estimatedFiles: libraries.filter((item) => includedLibraries.includes(item.library)).reduce((sum, item) => sum + item.fileCount, 0),
    estimatedStorage: libraries.filter((item) => includedLibraries.includes(item.library)).reduce((sum, item) => sum + item.sizeBytes, 0),
    readinessScore: riskLevel === "Low" ? 92 : riskLevel === "Medium" ? 72 : 58,
    riskLevel,
    prerequisites: riskLevel === "Low" ? [] : remediationActions.map((action) => action.actionTitle).slice(0, 3)
  });
  const libraryNames = libraries.map((item) => item.library);
  return {
    assessmentId: `mock-assessment-${Date.now()}`,
    scanId: scan.scanId,
    generatedAt: new Date().toISOString(),
    status: "completed",
    readinessScore,
    riskLevel: riskLevelForScore(readinessScore),
    summary: { blockers, highRisks, mediumRisks, lowRisks, remediationActions: remediationActions.length, suggestedWaves: 4 },
    riskFindings,
    remediationActions,
    migrationWaves: [
      wave("wave-1", "Wave 1 - Low Risk Pilot", 1, libraryNames.filter((name) => !/payroll|audit|security|contract|archive/i.test(name)).slice(0, 6), "Low"),
      wave("wave-2", "Wave 2 - Business Content", 2, libraryNames.filter((name) => /document|report|vendor|project/i.test(name)).slice(0, 6), "Medium"),
      wave("wave-3", "Wave 3 - Restricted Content", 3, libraryNames.filter((name) => /payroll|audit|security|contract/i.test(name)), "High"),
      wave("wave-4", "Wave 4 - Archive and Cleanup", 4, libraryNames.filter((name) => /archive|tax|compliance/i.test(name)), "High")
    ],
    modernizationOpportunities: libraries.filter((item) => /workflow|approval|request|tracker|form|report|excel|policy/i.test(item.library)).map((item) => ({
      id: `mod-${item.id}`,
      type: /report|excel/i.test(item.library) ? "Reporting Modernization" : /form/i.test(item.library) ? "Forms Modernization" : "Workflow Modernization",
      sourceName: item.library,
      location: item.siteCollection,
      potentialTarget: /report|excel/i.test(item.library) ? "Power BI" : "Power Automate / Power Apps",
      rationale: "Name indicates a modernization candidate.",
      estimatedEffort: "Medium"
    })),
    warnings: scan.warnings,
    errors: scan.errors
  };
}

function defaultPlanOptions() {
  return [
    ["preservePermissions", "Preserve permissions", true],
    ["preserveMetadata", "Preserve metadata", true],
    ["includeVersionHistory", "Include version history", true],
    ["includeSubsites", "Include subsites", true],
    ["skipArchivedContent", "Skip archived content", false],
    ["renameInvalidFiles", "Rename invalid files", true],
    ["validateAfterMigration", "Validate after migration", true]
  ].map(([key, label, value]) => ({ key: key as string, label: label as string, value: value as boolean, description: "Planning option only." }));
}

function defaultChecklist() {
  return [
    "Confirm source SharePoint access",
    "Confirm target SharePoint access",
    "Confirm Microsoft Graph/PnP permissions",
    "Review broken permission areas",
    "Review metadata mapping",
    "Review long path risks",
    "Review large file risks",
    "Confirm archive strategy",
    "Confirm restricted content approvals",
    "Confirm migration wave owners",
    "Generate pre-migration report",
    "Confirm rollback/restore plan",
    "Confirm post-migration validation plan"
  ].map((title, index) => ({ id: `check-${index + 1}`, title, description: title, category: "Planning", required: true, status: "not_started", ownerRole: "Migration Lead" }));
}

function buildMockMigrationPlan(assessment: MigrationReadinessAssessment): MigrationPlan {
  const now = new Date().toISOString();
  return {
    planId: `mock-plan-${Date.now()}`,
    assessmentId: assessment.assessmentId,
    scanId: assessment.scanId,
    planName: `Migration plan from readiness ${new Date(assessment.generatedAt).toLocaleDateString()}`,
    description: "Planning-only draft generated from readiness assessment.",
    status: assessment.summary.blockers > 0 ? "blocked" : "draft",
    createdAt: now,
    updatedAt: now,
    createdBy: "ZMS Planner",
    sourceEnvironment: `Discovery scan ${assessment.scanId}`,
    targetEnvironment: "Target SharePoint Online",
    options: defaultPlanOptions(),
    checklist: defaultChecklist(),
    risks: assessment.riskFindings,
    remediationPrerequisites: assessment.remediationActions,
    approvals: [{ role: "Migration Lead", status: "not_started", approvedBy: "", approvedAt: null, notes: "" }],
    runbookPath: "",
    warnings: assessment.warnings,
    errors: assessment.errors,
    waves: assessment.migrationWaves.map((wave) => ({
      waveId: wave.waveId,
      waveName: wave.waveName,
      order: wave.recommendedOrder,
      description: wave.description,
      riskLevel: wave.riskLevel,
      readinessScore: wave.readinessScore,
      includedItems: wave.includedLibraries.map((library) => ({
        itemId: `${wave.waveId}-${library}`,
        siteCollection: wave.includedSites[0] ?? "",
        library,
        path: library,
        itemType: "Library",
        sourceUrl: library,
        targetUrl: library,
        fileCount: Math.floor(wave.estimatedFiles / Math.max(1, wave.includedLibraries.length)),
        storageBytes: Math.floor(wave.estimatedStorage / Math.max(1, wave.includedLibraries.length)),
        metadataCount: 0,
        permissionRisk: wave.riskLevel,
        migrationAction: wave.riskLevel === "High" ? "manual_review" : "migrate",
        includeInMigration: true,
        reason: wave.riskLevel === "High" ? "Requires review before execution planning." : "Included in draft plan."
      })),
      excludedItems: [],
      prerequisites: wave.prerequisites,
      estimatedFiles: wave.estimatedFiles,
      estimatedStorage: wave.estimatedStorage,
      estimatedDuration: `PT${Math.max(30, Math.floor(wave.estimatedFiles / 25))}M`,
      ownerRole: wave.riskLevel === "High" ? "Migration Lead / Security Owner" : "Migration Lead",
      approvalStatus: "not_started",
      notes: "Generated from readiness assessment."
    }))
  };
}

function buildMockPreMigrationValidation(plan: MigrationPlan): PreMigrationValidationResult {
  const checks: PreMigrationCheck[] = [
    { checkId: "source", category: "Source Access", title: "Source environment is defined", status: plan.sourceEnvironment ? "passed" : "failed", severity: plan.sourceEnvironment ? "Info" : "High", recommendedAction: "Define source environment." },
    { checkId: "target", category: "Target Access", title: "Target environment is defined", status: plan.targetEnvironment ? "passed" : "failed", severity: plan.targetEnvironment ? "Info" : "High", recommendedAction: "Define target environment." },
    ...plan.checklist.map((item) => ({ checkId: item.id, category: "Checklist", title: item.title, status: item.status === "completed" ? "passed" : "warning", severity: "Medium", recommendedAction: `Complete checklist item: ${item.title}.` })),
    ...plan.waves.flatMap((wave) => wave.includedItems.map((item) => ({ checkId: item.itemId, category: item.migrationAction === "manual_review" ? "Restricted Content" : "Governance", title: `${item.library} execution readiness`, status: item.migrationAction === "migrate" ? "passed" : "failed", severity: item.migrationAction === "migrate" ? "Info" : "High", recommendedAction: item.reason, affectedWave: wave.waveName, affectedItem: item.library })))
  ].map((item) => ({
    description: item.title,
    affectedWave: typeof (item as { affectedWave?: unknown }).affectedWave === "string" ? String((item as { affectedWave?: unknown }).affectedWave) : "",
    affectedItem: typeof (item as { affectedItem?: unknown }).affectedItem === "string" ? String((item as { affectedItem?: unknown }).affectedItem) : "",
    evidence: item.status === "passed" ? "Satisfied by migration plan." : "Not satisfied by current migration plan.",
    requiredForGoLive: true,
    ...item
  }));
  const waveResults = plan.waves.map((wave) => {
    const waveChecks = checks.filter((check) => check.affectedWave === wave.waveName);
    const errors = waveChecks.filter((check) => check.status === "failed").length;
    return { waveId: wave.waveId, waveName: wave.waveName, status: errors ? "blocked" : "ready", errors, warnings: waveChecks.filter((check) => check.status === "warning").length, passedChecks: waveChecks.filter((check) => check.status === "passed").length };
  });
  const errors = checks.filter((check) => check.status === "failed").length;
  const warnings = checks.filter((check) => check.status === "warning").length;
  const validation: PreMigrationValidationResult = {
    validationId: `mock-validation-${Date.now()}`,
    planId: plan.planId,
    generatedAt: new Date().toISOString(),
    status: "completed",
    decision: errors ? "no_go" : warnings ? "conditional_go" : "go",
    summary: {
      errors,
      warnings,
      passedChecks: checks.filter((check) => check.status === "passed").length,
      blockedWaves: waveResults.filter((wave) => wave.status === "blocked").length,
      readyWaves: waveResults.filter((wave) => wave.status === "ready").length
    },
    checks,
    waveResults,
    blockers: checks.filter((check) => check.status === "failed").map((check) => check.title),
    warnings: checks.filter((check) => check.status === "warning").map((check) => check.title),
    recommendations: checks.filter((check) => check.status !== "passed").map((check) => check.recommendedAction),
    exportPaths: {}
  };
  return validation;
}

function buildMockExecutionSimulation(plan: MigrationPlan): ExecutionSimulationResult {
  const waves = plan.waves.map((wave) => {
    const warnings = wave.includedItems.filter((item) => item.migrationAction !== "migrate").length + (wave.riskLevel === "High" ? 1 : 0);
    const failures = wave.excludedItems.length;
    return {
      waveId: wave.waveId,
      waveName: wave.waveName,
      order: wave.order,
      itemCount: wave.includedItems.length,
      estimatedFiles: wave.estimatedFiles,
      estimatedStorageBytes: wave.estimatedStorage,
      estimatedDurationMinutes: Math.max(10, Math.ceil(wave.estimatedFiles / 100 + wave.estimatedStorage / 1024 / 1024 / 1024 + 10 + warnings * 3)),
      riskLevel: wave.riskLevel,
      readinessScore: wave.readinessScore,
      expectedWarnings: warnings,
      expectedFailures: failures,
      steps: ["Pre-wave validation", "Source accessibility check", "Target accessibility check", "Metadata mapping check", "Permission mapping check", "Content copy simulation", "Post-wave validation simulation", "Report generation"].map((stepName, index) => ({
        stepId: `${wave.waveId}-${index + 1}`,
        stepName,
        order: index + 1,
        description: `${stepName} for ${wave.waveName}.`,
        estimatedDurationMinutes: index === 0 || index === 7 ? 5 : 10,
        status: "simulated",
        dependencies: index === 0 ? [] : [`${wave.waveId}-${index}`],
        expectedIssues: []
      }))
    };
  });
  const expectedIssues = waves.flatMap((wave) => Array.from({ length: wave.expectedWarnings }, (_, index) => ({
    issueId: `${wave.waveId}-issue-${index + 1}`,
    severity: "Warning",
    waveName: wave.waveName,
    item: "",
    description: "Simulation detected a planning warning.",
    recommendedAction: "Review wave prerequisites before execution design."
  })));
  return {
    simulationId: `mock-simulation-${Date.now()}`,
    planId: plan.planId,
    generatedAt: new Date().toISOString(),
    status: "completed",
    estimatedDurationMinutes: waves.reduce((sum, wave) => sum + wave.estimatedDurationMinutes, 0),
    estimatedFiles: waves.reduce((sum, wave) => sum + wave.estimatedFiles, 0),
    estimatedStorageBytes: waves.reduce((sum, wave) => sum + wave.estimatedStorageBytes, 0),
    waves,
    expectedIssues,
    checkpoints: ["Pre-wave validation", "Source accessibility check", "Target accessibility check", "Metadata mapping check", "Permission mapping check", "Content copy simulation", "Post-wave validation simulation", "Report generation"],
    assumptions: ["Simulation only. No files are copied."],
    recommendations: expectedIssues.map((issue) => issue.recommendedAction)
  };
}

function buildMockMigrationExecutionJob(plan: MigrationPlan, request?: Partial<MigrationExecutionRequest>): MigrationExecutionJob {
  const now = new Date().toISOString();
  const jobId = `mock-execution-${Date.now()}`;
  const waves = plan.waves.map((wave) => {
    const items = wave.includedItems.filter((item) => item.includeInMigration).map((item) => ({
      itemExecutionId: `${jobId}-${item.itemId}`,
      sourceItemId: item.itemId,
      siteCollection: item.siteCollection,
      library: item.library,
      path: item.path,
      itemType: item.itemType,
      action: item.migrationAction,
      status: item.migrationAction === "manual_review" || item.migrationAction === "remediate_first" ? "retry_pending" : "pending",
      progressPercent: 0,
      simulatedSourceUrl: item.sourceUrl,
      simulatedTargetUrl: item.targetUrl,
      warnings: item.migrationAction === "manual_review" ? ["Manual review item will be skipped in simulation."] : [],
      errors: [],
      startedAt: null,
      completedAt: null
    }));
    return {
      waveExecutionId: `${jobId}-${wave.waveId}`,
      sourceWaveId: wave.waveId,
      waveName: wave.waveName,
      order: wave.order,
      status: "created",
      progressPercent: 0,
      totalItems: items.length,
      completedItems: 0,
      failedItems: 0,
      skippedItems: 0,
      estimatedFiles: wave.estimatedFiles,
      estimatedStorageBytes: wave.estimatedStorage,
      startedAt: null,
      completedAt: null,
      items,
      checkpoints: ["Pre-wave validation", "Source accessibility simulation", "Target accessibility simulation", "Metadata mapping simulation", "Permission mapping simulation", "Content transfer simulation", "Post-wave validation simulation", "Wave report generated"].map((name) => ({
        checkpointId: `${jobId}-${wave.waveId}-${name}`,
        name,
        status: "pending",
        startedAt: null,
        completedAt: null,
        message: `${name} for ${wave.waveName}.`,
        severity: "Info"
      })),
      errors: []
    };
  });
  const totalItems = waves.reduce((sum, wave) => sum + wave.totalItems, 0);
  return {
    jobId,
    planId: plan.planId,
    validationId: "",
    simulationId: "",
    mode: request?.mode ?? "simulation",
    status: "created",
    createdAt: now,
    startedAt: null,
    completedAt: null,
    createdBy: request?.createdBy ?? "Migration Lead",
    summary: { progressPercent: 0, totalWaves: waves.length, completedWaves: 0, totalItems, completedItems: 0, failedItems: 0, skippedItems: 0, warningCount: waves.flatMap((wave) => wave.items).reduce((sum, item) => sum + item.warnings.length, 0), errorCount: 0 },
    waves,
    checkpoints: ["Plan loaded", "Go/No-Go validation checked", "Simulation mode confirmed", "Waves generated", "Execution report initialized"].map((name) => ({
      checkpointId: `${jobId}-${name}`,
      name,
      status: "pending",
      startedAt: null,
      completedAt: null,
      message: name,
      severity: "Info"
    })),
    timeline: [{ eventId: `${jobId}-created`, createdAt: now, eventType: "JobCreated", message: "Simulation Mode - No tenant changes performed. Job created.", severity: "Info", waveExecutionId: "", itemExecutionId: "" }],
    errors: [],
    warnings: ["Simulation Mode - No tenant changes performed."],
    reportPaths: {}
  };
}

function startMockMigrationExecutionJob(job: MigrationExecutionJob): MigrationExecutionJob {
  const now = new Date().toISOString();
  const next = { ...job, status: "running", startedAt: job.startedAt ?? now, timeline: [...job.timeline] };
  next.timeline.push({ eventId: `${job.jobId}-started-${Date.now()}`, createdAt: now, eventType: "JobStarted", message: "Simulated migration execution started.", severity: "Info", waveExecutionId: "", itemExecutionId: "" });
  next.checkpoints = next.checkpoints.map((checkpoint) => ({ ...checkpoint, status: "passed", startedAt: checkpoint.startedAt ?? now, completedAt: now, message: "Job checkpoint passed." }));
  next.waves = next.waves.map((wave) => {
    const updatedItems = wave.items.map((item) => {
      if (item.action === "remediate_first") return { ...item, status: "failed", progressPercent: 100, startedAt: now, completedAt: now, errors: ["Remediation prerequisite is unresolved."] };
      if (item.action === "manual_review" || item.action === "archive") return { ...item, status: "skipped", progressPercent: 100, startedAt: now, completedAt: now, warnings: [...item.warnings, "Skipped in simulation until approved."] };
      return { ...item, status: "completed", progressPercent: 100, startedAt: now, completedAt: now };
    });
    const completedItems = updatedItems.filter((item) => item.status === "completed").length;
    const failedItems = updatedItems.filter((item) => item.status === "failed").length;
    const skippedItems = updatedItems.filter((item) => item.status === "skipped").length;
    return {
      ...wave,
      status: failedItems ? "failed" : skippedItems || updatedItems.some((item) => item.warnings.length) ? "completed_with_warnings" : "completed",
      progressPercent: 100,
      completedItems,
      failedItems,
      skippedItems,
      startedAt: wave.startedAt ?? now,
      completedAt: now,
      items: updatedItems,
      checkpoints: wave.checkpoints.map((checkpoint) => ({ ...checkpoint, status: "passed", startedAt: checkpoint.startedAt ?? now, completedAt: now, message: "Wave checkpoint passed." }))
    };
  });
  const items = next.waves.flatMap((wave) => wave.items);
  next.summary = {
    progressPercent: items.length ? 100 : 0,
    totalWaves: next.waves.length,
    completedWaves: next.waves.filter((wave) => wave.status === "completed" || wave.status === "completed_with_warnings").length,
    totalItems: items.length,
    completedItems: items.filter((item) => item.status === "completed").length,
    failedItems: items.filter((item) => item.status === "failed").length,
    skippedItems: items.filter((item) => item.status === "skipped").length,
    warningCount: next.warnings.length + items.reduce((sum, item) => sum + item.warnings.length, 0),
    errorCount: next.errors.length + items.reduce((sum, item) => sum + item.errors.length, 0)
  };
  next.status = next.summary.failedItems ? "failed" : next.summary.warningCount || next.summary.skippedItems ? "completed_with_warnings" : "completed";
  next.completedAt = now;
  next.timeline.push({ eventId: `${job.jobId}-completed-${Date.now()}`, createdAt: now, eventType: "JobCompleted", message: `Simulated execution completed with status ${next.status}.`, severity: next.status === "completed" ? "Info" : "Warning", waveExecutionId: "", itemExecutionId: "" });
  return next;
}

function buildMockWorkflowRun(): WorkflowValidationRun {
  const now = new Date().toISOString();
  const latestScan = getLatestMockDiscoveryScan();
  const latestAssessment = getLatestMockReadinessAssessment();
  const plans = [...mockMigrationPlans.values()];
  const validations = [...mockPreMigrationValidations.values()];
  const simulations = [...mockExecutionSimulations.values()];
  const jobs = [...mockMigrationExecutionJobs.values()];
  const previews = [...mockTransferPreviews.values()];
  const latestPlan = plans.length ? plans[plans.length - 1] : undefined;
  const latestValidation = validations.length ? validations[validations.length - 1] : undefined;
  const latestSimulation = simulations.length ? simulations[simulations.length - 1] : undefined;
  const latestJob = jobs.length ? jobs[jobs.length - 1] : undefined;
  const latestPreview = previews.length ? previews[previews.length - 1] : undefined;
  const summary = {
    scanId: latestScan?.scanId ?? "mock-scan-required",
    assessmentId: latestAssessment?.assessmentId ?? "mock-assessment-required",
    planId: latestPlan?.planId ?? "mock-plan-required",
    validationId: latestValidation?.validationId ?? "mock-validation-required",
    simulationId: latestSimulation?.simulationId ?? "mock-simulation-required",
    executionJobId: latestJob?.jobId ?? "mock-execution-required",
    previewId: latestPreview?.previewId ?? "mock-preview-required"
  };
  const names = ["Discovery Scan", "Readiness Analysis", "Migration Plan", "Plan Validation", "Runbook", "Pre-Migration Validation", "Execution Simulation", "Execution Job", "Transfer Preview", "Export Verification"];
  const steps = names.map((name, index) => ({
    stepId: `mock-step-${index + 1}`,
    order: index + 1,
    name,
    description: `${name} validation step.`,
    status: index === 5 || index === 8 ? "warning" : "passed",
    startedAt: now,
    completedAt: now,
    durationMs: 120 + index * 10,
    relatedArtifactId: Object.values(summary)[Math.min(index, Object.values(summary).length - 1)] ?? "",
    warnings: index === 5 ? ["Pre-migration validation may be no_go until approvals are complete."] : index === 8 ? ["Transfer preview may contain blocked items."] : [],
    errors: [],
    notes: []
  }));
  const issues = steps.flatMap((step) => step.warnings.map((warning) => ({
    issueId: `${step.stepId}-warning`,
    severity: "Warning",
    stepName: step.name,
    message: warning,
    recommendedAction: "Review this step before real pilot migration."
  })));
  return {
    workflowRunId: `mock-workflow-${Date.now()}`,
    startedAt: now,
    completedAt: now,
    status: "completed",
    overallResult: issues.length ? "pass_with_warnings" : "pass",
    source: "latest_scan",
    createdBy: "Migration Lead",
    steps,
    artifacts: Object.entries(summary).map(([key, value]) => ({ artifactId: value, artifactType: key, displayName: key, status: "created", location: value })),
    issues,
    summary,
    reportPaths: {}
  };
}

export interface ConnectionInput {
  id?: string;
  name: string;
  provider: string;
  kind: "Source" | "Target";
  status?: Connection["status"];
  tenant?: string;
  authMethod?: string;
  message?: string;
  url?: string;
  rootPath?: string;
  username?: string;
  password?: string;
  tenantId?: string;
  clientId?: string;
  clientSecret?: string;
  documentLibraryName?: string;
  folderUrl?: string;
  folderId?: string;
}

type BackendConnectionType = "SharePointOnPrem" | "SharePointOnline" | "FileShare" | "GoogleDrive";

interface BackendConnectionResponse {
  id: string;
  name: string;
  type: BackendConnectionType;
  url: string;
  rootPath?: string | null;
  documentLibraryName?: string | null;
  connectionKind?: "Source" | "Target" | string | null;
  authenticationType?: string | null;
  hasClientSecret: boolean;
  hasRefreshToken: boolean;
  isEnabled: boolean;
  createdUtc: string;
  updatedUtc: string;
}

interface BackendConnectionCreateRequest {
  name: string;
  type: BackendConnectionType;
  url: string;
  username?: string | null;
  password?: string | null;
  clientId?: string | null;
  clientSecret?: string | null;
  tenantId?: string | null;
  rootPath?: string | null;
  additionalSettings: Record<string, string>;
}

interface BackendConnectionTestResponse {
  isSuccess: boolean;
  message: string;
  testedUtc: string;
}

function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function providerFromBackendType(type: BackendConnectionType): string {
  switch (type) {
    case "SharePointOnline":
      return "SharePoint Online";
    case "SharePointOnPrem":
      return "SharePoint Server";
    case "GoogleDrive":
      return "Google Workspace";
    case "FileShare":
      return "SMB";
    default:
      return "Migration Source";
  }
}

function backendTypeFromConnectionInput(input: ConnectionInput): BackendConnectionType {
  if (input.provider === "SharePoint Online") return "SharePointOnline";
  if (input.provider === "SharePoint Server") return "SharePointOnPrem";
  if (input.provider === "Google Workspace") return "GoogleDrive";
  if (input.provider === "SMB") return "FileShare";
  return "SharePointOnline";
}

function tenantLabelFromUrl(url?: string | null): string | undefined {
  const value = url?.trim();
  if (!value) return undefined;
  try {
    return new URL(value).host;
  } catch {
    return value;
  }
}

function mapBackendConnection(connection: BackendConnectionResponse): Connection {
  const provider = providerFromBackendType(connection.type);
  const kind = connection.connectionKind === "Target" ? "Target" : "Source";
  const authMethod = connection.authenticationType
    ?? (connection.hasClientSecret ? "App-only client secret" : connection.type === "GoogleDrive" ? "Backend OAuth" : undefined);

  return {
    id: connection.id,
    name: connection.name,
    kind,
    provider,
    status: connection.isEnabled ? "Disconnected" : "Config Required",
    tenant: tenantLabelFromUrl(connection.url || connection.rootPath),
    authMethod,
    lastSync: connection.updatedUtc ? new Date(connection.updatedUtc).toLocaleString() : undefined,
    message: connection.documentLibraryName
      ? `Document library: ${connection.documentLibraryName}`
      : connection.rootPath
        ? `Root path: ${connection.rootPath}`
        : undefined,
    actions: ["Test", "Configure"]
  };
}

function toBackendConnectionRequest(input: ConnectionInput): BackendConnectionCreateRequest {
  const type = backendTypeFromConnectionInput(input);
  const additionalSettings: Record<string, string> = {
    ConnectionKind: input.kind,
    AuthenticationType: input.authMethod ?? ""
  };

  if (input.documentLibraryName) {
    additionalSettings.DocumentLibraryName = input.documentLibraryName;
  }

  if (input.folderId) {
    additionalSettings.FolderId = input.folderId;
  }

  if (input.folderUrl) {
    additionalSettings.FolderUrl = input.folderUrl;
  }

  const url = type === "GoogleDrive"
    ? input.folderUrl ?? input.url ?? ""
    : type === "FileShare"
      ? input.rootPath ?? input.url ?? ""
      : input.url ?? "";

  return {
    name: input.name,
    type,
    url,
    username: input.username || null,
    password: input.password || null,
    clientId: input.clientId || null,
    clientSecret: input.clientSecret || null,
    tenantId: input.tenantId || null,
    rootPath: type === "FileShare" ? input.rootPath || url : input.rootPath || null,
    additionalSettings
  };
}

const packageFiles = [
  "README.md",
  "config/zms-spo-environment.json",
  "scripts/00-Check-Prerequisites.ps1",
  "scripts/01-Create-SiteCollections.ps1",
  "scripts/02-Create-Subsites.ps1",
  "scripts/03-Create-Libraries-Lists-Metadata.ps1",
  "scripts/04-Create-Groups-Permissions.ps1",
  "scripts/05-Create-Folders-And-SampleFiles.ps1",
  "scripts/06-Apply-Migration-EdgeCases.ps1",
  "scripts/07-Generate-InventoryReport.ps1",
  "scripts/08-Run-Preflight.ps1",
  "scripts/09-Run-DryRun.ps1",
  "scripts/10-Run-All-Safe.ps1",
  "scripts/11-Run-Discovery-ReadOnly.ps1",
  "scripts/lib/Zms.Logging.ps1",
  "scripts/lib/Zms.Config.ps1",
  "scripts/lib/Zms.SharePoint.ps1",
  "scripts/lib/Zms.Validation.ps1",
  "scripts/lib/Zms.Reporting.ps1",
  "docs/architecture-overview.md",
  "docs/permission-model.md",
  "docs/metadata-model.md",
  "docs/migration-test-scenarios.md",
  "reports/environment-inventory-template.csv",
  "reports/migration-complexity-matrix.md",
  "reports/environment-summary.md",
  "reports/execution-summary-template.json",
  "reports/execution-summary-template.md",
  "logs/.gitkeep",
  "discovery-output/.gitkeep",
  "execution/execution-plan.json",
  "execution/execution-status.json",
  "execution/runbook.md",
  "execution/preflight-report.md",
  "execution/dry-run-report.md"
];

function buildSummary(config: EnvironmentConfig): ValidationSummary {
  return {
    siteCollections: config.siteCollections.length,
    subsites: config.siteCollections.reduce((sum, site) => sum + site.subsites.length, 0),
    libraries: config.siteCollections.reduce((sum, site) => sum + site.libraries.length, 0),
    lists: config.siteCollections.reduce((sum, site) => sum + site.lists.length, 0),
    metadataFields: config.siteCollections.reduce((sum, site) => sum + site.metadataFields.length, 0),
    permissionGroups: config.siteCollections.reduce((sum, site) => sum + site.permissionGroups.length, 0),
    edgeCases: config.siteCollections.reduce((sum, site) => sum + site.edgeCases.length, 0)
  };
}

function validateLocally(config: EnvironmentConfig): ConfigValidationResponse {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (!config.tenantName.trim()) errors.push("Tenant name is required.");
  if (!config.adminUrl.trim()) errors.push("Admin URL is required.");
  if (!config.rootUrl.trim()) errors.push("Root URL is required.");
  if (!config.ownerEmail.trim()) errors.push("Owner email is required.");
  if (config.siteCollections.length === 0) errors.push("At least one site collection is required.");

  const urls = new Set<string>();
  config.siteCollections.forEach((site) => {
    if (!site.title.trim()) errors.push(`Site collection ${site.id} is missing a title.`);
    if (!site.url.trim()) errors.push(`Site collection ${site.title || site.id} is missing a URL.`);
    const normalizedUrl = site.url.toLowerCase();
    if (urls.has(normalizedUrl)) errors.push(`Duplicate site URL detected: ${site.url}`);
    urls.add(normalizedUrl);

    if (site.subsites.length === 0) errors.push(`${site.title} must include at least one subsite.`);
    if (site.libraries.length === 0) errors.push(`${site.title} must include at least one library.`);
    if (site.lists.length === 0) errors.push(`${site.title} must include at least one list.`);
    if (site.metadataFields.length === 0) warnings.push(`${site.title} has no metadata fields.`);
    if (site.permissionGroups.length === 0) warnings.push(`${site.title} has no permission groups.`);
    if (site.permissionRules.length === 0) warnings.push(`${site.title} has no permission rules.`);
    if (site.folderStructures.length === 0) warnings.push(`${site.title} has no folder structures.`);

    const libraryNames = new Set<string>();
    site.libraries.forEach((library) => {
      const name = library.title.toLowerCase();
      if (libraryNames.has(name)) warnings.push(`${site.title} has duplicate library name: ${library.title}`);
      libraryNames.add(name);
    });

    site.folderStructures.forEach((folder) => {
      if (folder.path.length > 180) warnings.push(`${site.title} has a long folder path: ${folder.path}`);
    });
  });

  if (!config.globalOptions.includeLargeFilePlaceholders) {
    warnings.push("Large file placeholders are disabled.");
  }

  return {
    isValid: errors.length === 0,
    errors,
    warnings,
    summary: buildSummary(config),
    source: "mock"
  };
}

function markBackend<T extends object>(value: T): T & { source: "backend" } {
  return { ...value, source: "backend" };
}

export const zmsApi = {
  async getDashboardStats() {
    await delay();
    return dashboardStats;
  },

  async getSiteCollections(): Promise<SiteCollection[]> {
    await delay();
    return siteCollections;
  },

  async generateEnvironmentConfig(
    selectedSiteCollections: SiteCollection[],
    builderOptions: BuilderOptions,
    tenantValues: TenantValues
  ): Promise<EnvironmentConfig> {
    await delay(500);
    return buildEnvironmentConfig(selectedSiteCollections, builderOptions, tenantValues);
  },

  async saveEnvironmentConfig(config: EnvironmentConfig): Promise<EnvironmentConfig> {
    await delay(300);
    return config;
  },

  async validateEnvironmentConfig(config: EnvironmentConfig): Promise<ConfigValidationResponse> {
    if (hasBackendBaseUrl()) {
      try {
        const response = await apiPost<EnvironmentConfig, ConfigValidationResponse>("/api/environment-config/validate", config);
        return markBackend(response);
      } catch {
        return validateLocally(config);
      }
    }

    await delay(250);
    return validateLocally(config);
  },

  async saveEnvironmentConfigToBackend(config: EnvironmentConfig): Promise<SaveConfigResponse> {
    if (hasBackendBaseUrl()) {
      try {
        const response = await apiPost<EnvironmentConfig, SaveConfigResponse>("/api/environment-config/save", config);
        return markBackend(response);
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(300);
    return {
      configId: `mock-config-${Date.now()}`,
      message: "Environment config saved to local mock state.",
      savedAt: new Date().toISOString(),
      source: "mock"
    };
  },

  async generateEnvironmentPackage(config: EnvironmentConfig): Promise<GeneratedPackageResult> {
    const summary = buildSummary(config);
    if (hasBackendBaseUrl()) {
      try {
        const response = await apiPost<EnvironmentConfig, GeneratedPackageResult>("/api/environment-package/generate", config);
        return { ...markBackend(response), generatedAt: new Date().toISOString(), summary };
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(800);
    return {
      packageId: `mock-package-${Date.now()}`,
      message: "Backend unavailable. Using mock package generation.",
      files: packageFiles,
      downloadUrl: "mock://environment-package",
      generatedAt: new Date().toISOString(),
      summary,
      source: "mock"
    };
  },

  async getPackageManifest(packageId: string, fallbackSummary?: ValidationSummary): Promise<PackageManifest> {
    if (hasBackendBaseUrl() && !packageId.startsWith("mock-package-")) {
      try {
        const response = await apiGet<PackageManifest>(`/api/environment-package/${packageId}/manifest`);
        return markBackend(response);
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(200);
    return {
      packageId,
      generatedAt: new Date().toISOString(),
      files: packageFiles,
      summary: fallbackSummary ?? {
        siteCollections: 0,
        subsites: 0,
        libraries: 0,
        lists: 0,
        metadataFields: 0,
        permissionGroups: 0,
        edgeCases: 0
      },
      source: "mock"
    };
  },

  async downloadEnvironmentPackage(packageId: string, configFallback?: EnvironmentConfig): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !packageId.startsWith("mock-package-")) {
      try {
        const blob = await apiGetBlob(`/api/environment-package/${packageId}/download`);
        downloadBlob(blob, `zms-sharepoint-environment-package-${packageId}.zip`);
        return { source: "backend" };
      } catch {
        // fall through to JSON fallback below
      }
    }

    if (configFallback) {
      downloadJson("zms-spo-environment-config.json", configFallback);
    }
    return { source: "mock" };
  },

  async exportEnvironmentConfig(config: EnvironmentConfig): Promise<void> {
    await delay(150);
    downloadJson("zms-spo-environment-config.json", config);
  },

  async getConnections(): Promise<Connection[]> {
    if (hasBackendBaseUrl()) {
      const backendConnections = await apiGet<BackendConnectionResponse[]>("/api/connections");
      return backendConnections.map(mapBackendConnection);
    }

    await delay();
    return connections;
  },

  async createConnection(input: ConnectionInput): Promise<Connection> {
    if (hasBackendBaseUrl()) {
      const payload = toBackendConnectionRequest(input);
      const saved = input.id && isGuid(input.id)
        ? await apiPut<BackendConnectionCreateRequest, BackendConnectionResponse>(`/api/connections/${input.id}`, payload)
        : await apiPost<BackendConnectionCreateRequest, BackendConnectionResponse>("/api/connections", payload);
      return mapBackendConnection(saved);
    }

    await delay(500);
    return {
      id: input.id ?? `connection-${Date.now()}`,
      name: input.name,
      kind: input.kind,
      provider: input.provider,
      status: input.status ?? "Disconnected",
      tenant: input.tenant,
      authMethod: input.authMethod,
      message: input.message,
      actions: input.status === "Connected" ? ["Test", "Configure"] : ["Configure"]
    };
  },

  async testConnection(connectionId: string): Promise<{ status: Connection["status"]; message: string; connectionId: string }> {
    if (hasBackendBaseUrl() && isGuid(connectionId)) {
      const result = await apiPost<object, BackendConnectionTestResponse>(`/api/connections/${connectionId}/test`, {});
      return {
        connectionId,
        status: result.isSuccess ? "Connected" : "Warning",
        message: result.message
      };
    }

    await delay(700);
    return {
      connectionId,
      status: "Warning",
      message: "Microsoft Graph permission missing: Files.ReadWrite.All"
    };
  },

  async startDiscoveryScan(request: DiscoveryScanRequest, fallbackConfig?: EnvironmentConfig): Promise<StartDiscoveryScanResponse> {
    if (hasBackendBaseUrl()) {
      try {
        return await apiPost<DiscoveryScanRequest, StartDiscoveryScanResponse>("/api/discovery/start", request);
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(300);
    const scanId = `mock-scan-${Date.now()}`;
    if (fallbackConfig) {
      mockDiscoveryScans.set(scanId, generateDiscoveryResults(fallbackConfig, scanId));
    }

    return {
      scanId,
      status: "completed",
      message: "Backend unavailable. Mock discovery scan completed locally."
    };
  },

  async getDiscoveryStatus(scanId: string): Promise<DiscoveryScanStatusResponse> {
    if (hasBackendBaseUrl() && !scanId.startsWith("mock-scan-")) {
      try {
        return await apiGet<DiscoveryScanStatusResponse>(`/api/discovery/${scanId}/status`);
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(150);
    const result = mockDiscoveryScans.get(scanId);
    return {
      scanId,
      status: result?.status ?? "completed",
      progress: result ? 100 : 0,
      currentStep: result ? "Mock discovery scan completed" : "Mock discovery scan unavailable",
      startedAt: result?.startedAt ?? new Date().toISOString(),
      completedAt: result?.completedAt ?? new Date().toISOString(),
      errors: result?.errors ?? [],
      warnings: result?.warnings ?? []
    };
  },

  async getDiscoveryResults(scanId: string, fallbackConfig?: EnvironmentConfig): Promise<DiscoveryScanResult> {
    if (hasBackendBaseUrl() && !scanId.startsWith("mock-scan-")) {
      try {
        return await apiGet<DiscoveryScanResult>(`/api/discovery/${scanId}/results`);
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(150);
    const existing = mockDiscoveryScans.get(scanId);
    if (existing) {
      return existing;
    }

    const generated = fallbackConfig
      ? generateDiscoveryResults(fallbackConfig, scanId)
      : generateDiscoveryResults(buildEnvironmentConfig(siteCollections, {
        includeDefaultSubsites: true,
        generateSampleDocuments: true,
        includeMetadataColumns: true,
        createPermissionGroups: true,
        addMigrationEdgeCases: true,
        includeArchivedFolders: true,
        includeLongPathExamples: true,
        includeLargeFilePlaceholders: false
      }, {
        tenantName: "Zettalogix SharePoint Online",
        adminUrl: "https://zettalogix-admin.sharepoint.com",
        rootUrl: "https://zettalogix.sharepoint.com",
        ownerEmail: "migrationlead@zettalogix.com",
        clientIdPlaceholder: "PNP_CLIENT_ID_PLACEHOLDER",
        targetUrlPrefix: "https://zettalogix.sharepoint.com/sites/",
        generatedBy: "Mock Discovery"
      }), scanId);
    mockDiscoveryScans.set(scanId, generated);
    return generated;
  },

  async getLatestDiscoveryResults(): Promise<DiscoveryScanResult | null> {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<DiscoveryScanResult>("/api/discovery/latest/results");
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(150);
    return getLatestMockDiscoveryScan();
  },

  async getLatestDiscoveryPermissionRisks(): Promise<PermissionRiskFinding[] | null> {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<PermissionRiskFinding[]>("/api/discovery/latest/permissions");
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(150);
    return getLatestMockDiscoveryScan()?.permissionRisks ?? null;
  },

  async getLatestDiscoveryMetadataFindings(): Promise<MetadataFinding[] | null> {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<MetadataFinding[]>("/api/discovery/latest/metadata");
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(150);
    return getLatestMockDiscoveryScan()?.metadataFindings ?? null;
  },

  async getLatestDiscoveryMigrationRisks(): Promise<MigrationRiskFinding[] | null> {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<MigrationRiskFinding[]>("/api/discovery/latest/risks");
      } catch {
        // fall through to mock fallback below
      }
    }

    await delay(150);
    return getLatestMockDiscoveryScan()?.migrationRisks ?? null;
  },

  async importDiscoveryResult(file: File): Promise<DiscoveryImportResponse> {
    if (hasBackendBaseUrl()) {
      try {
        const formData = new FormData();
        formData.append("scanResult", file);
        return await apiPostForm<DiscoveryImportResponse>("/api/discovery/import", formData);
      } catch {
        // fall through to local import fallback below
      }
    }

    const text = await file.text();
    const parsed = JSON.parse(text) as DiscoveryScanResult;
    const scanId = `mock-import-${Date.now()}`;
    const imported: DiscoveryScanResult = {
      ...parsed,
      scanId,
      scanName: parsed.scanName || "Imported Live SharePoint Discovery",
      mode: "live-import",
      status: parsed.status === "partial" ? "partial" : "completed",
      startedAt: parsed.startedAt || new Date().toISOString(),
      completedAt: parsed.completedAt || new Date().toISOString(),
      summary: parsed.summary,
      siteCollections: parsed.siteCollections ?? [],
      inventoryItems: parsed.inventoryItems ?? [],
      permissionRisks: parsed.permissionRisks ?? [],
      metadataFindings: parsed.metadataFindings ?? [],
      migrationRisks: parsed.migrationRisks ?? [],
      warnings: parsed.warnings ?? [],
      errors: parsed.errors ?? []
    };
    mockDiscoveryScans.set(scanId, imported);

    return {
      scanId,
      status: "completed",
      message: "Discovery result imported locally.",
      summary: imported.summary
    };
  },

  async downloadDiscoveryExport(scanId: string, exportType: "csv" | "json" | "permissions.csv" | "metadata.csv" | "risks.csv"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !scanId.startsWith("mock-scan-")) {
      try {
        const blob = await apiGetBlob(`/api/discovery/${scanId}/export/${exportType}`);
        const extension = exportType === "json" ? "json" : "csv";
        const fileName = exportType === "csv"
          ? `discovery-inventory-${scanId}.csv`
          : exportType === "json"
            ? `discovery-results-${scanId}.json`
            : `discovery-${exportType.replace(".csv", "")}-${scanId}.${extension}`;
        downloadBlob(blob, fileName);
        return { source: "backend" };
      } catch {
        // fall through to mock fallback below
      }
    }

    const result = mockDiscoveryScans.get(scanId);
    if (result) {
      if (exportType === "json") {
        downloadJson(`discovery-results-${scanId}.json`, result);
      } else {
        const rows =
          exportType === "permissions.csv"
            ? result.permissionRisks
            : exportType === "metadata.csv"
              ? result.metadataFindings
              : exportType === "risks.csv"
                ? result.migrationRisks
                : result.inventoryItems;
        downloadCsv(`discovery-${exportType}`, rows as unknown as Array<Record<string, unknown>>);
      }
    }

    return { source: "mock" };
  },

  async analyzeReadiness(scanId: string): Promise<ReadinessAnalyzeResponse> {
    if (hasBackendBaseUrl() && !scanId.startsWith("mock-")) {
      try {
        return await apiPost<object, ReadinessAnalyzeResponse>(`/api/readiness/analyze/${scanId}`, {});
      } catch {
        // fall through to mock fallback below
      }
    }

    const scan = mockDiscoveryScans.get(scanId) ?? getLatestMockDiscoveryScan();
    if (!scan) {
      throw new Error("Run or import discovery before readiness analysis.");
    }
    const assessment = buildMockReadinessAssessment(scan);
    mockReadinessAssessments.set(assessment.assessmentId, assessment);
    return {
      assessmentId: assessment.assessmentId,
      scanId: assessment.scanId,
      status: "completed",
      readinessScore: assessment.readinessScore,
      riskLevel: assessment.riskLevel,
      summary: assessment.summary
    };
  },

  async getReadinessAssessment(assessmentId: string): Promise<MigrationReadinessAssessment | null> {
    if (hasBackendBaseUrl() && !assessmentId.startsWith("mock-")) {
      try {
        return await apiGet<MigrationReadinessAssessment>(`/api/readiness/${assessmentId}`);
      } catch {
        // fall through to mock fallback below
      }
    }

    return mockReadinessAssessments.get(assessmentId) ?? null;
  },

  async getLatestReadinessAssessment(): Promise<MigrationReadinessAssessment | null> {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<MigrationReadinessAssessment>("/api/readiness/latest");
      } catch {
        // fall through to mock fallback below
      }
    }

    return getLatestMockReadinessAssessment();
  },

  async getReadinessRemediationPlan(assessmentId: string): Promise<RemediationAction[]> {
    if (hasBackendBaseUrl() && !assessmentId.startsWith("mock-")) {
      try {
        return await apiGet<RemediationAction[]>(`/api/readiness/${assessmentId}/remediation-plan`);
      } catch {
        // fall through to mock fallback below
      }
    }

    return mockReadinessAssessments.get(assessmentId)?.remediationActions ?? [];
  },

  async getReadinessMigrationWaves(assessmentId: string): Promise<MigrationWaveSuggestion[]> {
    if (hasBackendBaseUrl() && !assessmentId.startsWith("mock-")) {
      try {
        return await apiGet<MigrationWaveSuggestion[]>(`/api/readiness/${assessmentId}/migration-waves`);
      } catch {
        // fall through to mock fallback below
      }
    }

    return mockReadinessAssessments.get(assessmentId)?.migrationWaves ?? [];
  },

  async downloadReadinessExport(assessmentId: string, exportType: "json" | "csv" | "markdown"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !assessmentId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/readiness/${assessmentId}/export/${exportType}`);
        const extension = exportType === "markdown" ? "md" : exportType;
        downloadBlob(blob, `readiness-${assessmentId}.${extension}`);
        return { source: "backend" };
      } catch {
        // fall through to mock fallback below
      }
    }

    const assessment = mockReadinessAssessments.get(assessmentId) ?? getLatestMockReadinessAssessment();
    if (assessment) {
      if (exportType === "json") {
        downloadJson(`readiness-assessment-${assessment.assessmentId}.json`, assessment);
      } else if (exportType === "csv") {
        downloadCsv(`readiness-risk-findings-${assessment.assessmentId}.csv`, assessment.riskFindings as unknown as Array<Record<string, unknown>>);
      } else {
        downloadJson(`executive-readiness-summary-${assessment.assessmentId}.json`, {
          readinessScore: assessment.readinessScore,
          riskLevel: assessment.riskLevel,
          summary: assessment.summary,
          remediationActions: assessment.remediationActions,
          migrationWaves: assessment.migrationWaves
        });
      }
    }

    return { source: "mock" };
  },

  async createMigrationPlanFromAssessment(assessmentId: string): Promise<CreateMigrationPlanResponse> {
    if (hasBackendBaseUrl() && !assessmentId.startsWith("mock-")) {
      try {
        return await apiPost<object, CreateMigrationPlanResponse>(`/api/migration-plans/from-assessment/${assessmentId}`, {});
      } catch {
        // fall through to mock fallback
      }
    }
    const assessment = mockReadinessAssessments.get(assessmentId) ?? getLatestMockReadinessAssessment();
    if (!assessment) throw new Error("Create readiness analysis before migration planning.");
    const plan = buildMockMigrationPlan(assessment);
    mockMigrationPlans.set(plan.planId, plan);
    return { planId: plan.planId, assessmentId: plan.assessmentId, status: plan.status, message: "Migration plan generated locally." };
  },

  async getMigrationPlan(planId: string): Promise<MigrationPlan | null> {
    if (hasBackendBaseUrl() && !planId.startsWith("mock-")) {
      try {
        return await apiGet<MigrationPlan>(`/api/migration-plans/${planId}`);
      } catch {
        // fall through
      }
    }
    return mockMigrationPlans.get(planId) ?? null;
  },

  async getLatestMigrationPlan(): Promise<MigrationPlan | null> {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<MigrationPlan>("/api/migration-plans/latest");
      } catch {
        // fall through
      }
    }
    const values = [...mockMigrationPlans.values()];
    return values.length > 0 ? values[values.length - 1] : null;
  },

  async updateMigrationPlan(plan: MigrationPlan): Promise<MigrationPlan> {
    if (hasBackendBaseUrl() && !plan.planId.startsWith("mock-")) {
      try {
        return await apiPut<MigrationPlan, MigrationPlan>(`/api/migration-plans/${plan.planId}`, plan);
      } catch {
        // fall through
      }
    }
    const updated = { ...plan, updatedAt: new Date().toISOString() };
    mockMigrationPlans.set(updated.planId, updated);
    return updated;
  },

  async validateMigrationPlan(planId: string): Promise<MigrationPlanValidationResult> {
    if (hasBackendBaseUrl() && !planId.startsWith("mock-")) {
      try {
        return await apiPost<object, MigrationPlanValidationResult>(`/api/migration-plans/${planId}/validate`, {});
      } catch {
        // fall through
      }
    }
    const plan = mockMigrationPlans.get(planId);
    const errors = plan?.waves.length ? [] : ["Plan has no waves."];
    const warnings = plan?.checklist.some((item) => item.status === "completed") ? [] : ["No validation checklist items are completed."];
    return { isValid: errors.length === 0, errors, warnings, checklist: plan?.checklist ?? [] };
  },

  async generateMigrationRunbook(planId: string): Promise<MigrationRunbook | null> {
    if (hasBackendBaseUrl() && !planId.startsWith("mock-")) {
      try {
        return await apiPost<object, MigrationRunbook>(`/api/migration-plans/${planId}/generate-runbook`, {});
      } catch {
        // fall through
      }
    }
    const plan = mockMigrationPlans.get(planId);
    if (!plan) return null;
    const runbook = {
      planId,
      fileName: "migration-runbook.md",
      generatedAt: new Date().toISOString(),
      markdown: `# Migration Planning Runbook\n\nThis is a planning runbook only.\n\n## Plan\n${plan.planName}\n\n## Waves\n${plan.waves.map((wave) => `- ${wave.waveName}`).join("\n")}`
    };
    mockMigrationPlans.set(planId, { ...plan, runbookPath: "migration-runbook.md" });
    return runbook;
  },

  async downloadMigrationPlanExport(planId: string, exportType: "json" | "csv" | "markdown"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !planId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/migration-plans/${planId}/export/${exportType}`);
        downloadBlob(blob, `migration-plan-${planId}.${exportType === "markdown" ? "md" : exportType}`);
        return { source: "backend" };
      } catch {
        // fall through
      }
    }
    const plan = mockMigrationPlans.get(planId);
    if (plan) {
      if (exportType === "json") downloadJson(`migration-plan-${planId}.json`, plan);
      else downloadCsv(`migration-plan-${planId}.csv`, plan.waves.flatMap((wave) => wave.includedItems) as unknown as Array<Record<string, unknown>>);
    }
    return { source: "mock" };
  },

  async runPreMigrationValidation(planId: string): Promise<PreMigrationValidationResponse> {
    if (hasBackendBaseUrl() && !planId.startsWith("mock-")) {
      try {
        return await apiPost<object, PreMigrationValidationResponse>(`/api/pre-migration/validate/${planId}`, {});
      } catch {
        // fall through
      }
    }
    const plan = mockMigrationPlans.get(planId);
    if (!plan) throw new Error("Create a migration plan first.");
    const result = buildMockPreMigrationValidation(plan);
    mockPreMigrationValidations.set(result.validationId, result);
    return { validationId: result.validationId, planId, status: result.status, decision: result.decision, summary: result.summary };
  },

  async getPreMigrationValidation(validationId: string): Promise<PreMigrationValidationResult | null> {
    if (hasBackendBaseUrl() && !validationId.startsWith("mock-")) {
      try { return await apiGet<PreMigrationValidationResult>(`/api/pre-migration/validations/${validationId}`); } catch { /* fallback */ }
    }
    return mockPreMigrationValidations.get(validationId) ?? null;
  },

  async getLatestPreMigrationValidation(): Promise<PreMigrationValidationResult | null> {
    if (hasBackendBaseUrl()) {
      try { return await apiGet<PreMigrationValidationResult>("/api/pre-migration/latest"); } catch { /* fallback */ }
    }
    const values = [...mockPreMigrationValidations.values()];
    return values.length ? values[values.length - 1] : null;
  },

  async runExecutionSimulation(planId: string): Promise<ExecutionSimulationResponse> {
    if (hasBackendBaseUrl() && !planId.startsWith("mock-")) {
      try {
        return await apiPost<object, ExecutionSimulationResponse>(`/api/pre-migration/simulate/${planId}`, {});
      } catch {
        // fall through
      }
    }
    const plan = mockMigrationPlans.get(planId);
    if (!plan) throw new Error("Create a migration plan first.");
    const result = buildMockExecutionSimulation(plan);
    mockExecutionSimulations.set(result.simulationId, result);
    return {
      simulationId: result.simulationId,
      planId,
      status: result.status,
      estimatedDuration: `${Math.floor(result.estimatedDurationMinutes / 60)}h ${result.estimatedDurationMinutes % 60}m`,
      estimatedFiles: result.estimatedFiles,
      estimatedStorage: `${(result.estimatedStorageBytes / 1024 / 1024 / 1024).toFixed(2)} GB`,
      simulatedWaves: result.waves.length,
      expectedFailures: result.expectedIssues.filter((issue) => issue.severity === "Failure").length,
      expectedWarnings: result.expectedIssues.filter((issue) => issue.severity !== "Failure").length
    };
  },

  async getExecutionSimulation(simulationId: string): Promise<ExecutionSimulationResult | null> {
    if (hasBackendBaseUrl() && !simulationId.startsWith("mock-")) {
      try { return await apiGet<ExecutionSimulationResult>(`/api/pre-migration/simulations/${simulationId}`); } catch { /* fallback */ }
    }
    return mockExecutionSimulations.get(simulationId) ?? null;
  },

  async getLatestExecutionSimulation(): Promise<ExecutionSimulationResult | null> {
    if (hasBackendBaseUrl()) {
      try { return await apiGet<ExecutionSimulationResult>("/api/pre-migration/simulations/latest"); } catch { /* fallback */ }
    }
    const values = [...mockExecutionSimulations.values()];
    return values.length ? values[values.length - 1] : null;
  },

  async downloadPreMigrationValidationExport(validationId: string, exportType: "json" | "csv" | "markdown"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !validationId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/pre-migration/${validationId}/export/${exportType}`);
        downloadBlob(blob, `pre-migration-validation-${validationId}.${exportType === "markdown" ? "md" : exportType}`);
        return { source: "backend" };
      } catch { /* fallback */ }
    }
    const result = mockPreMigrationValidations.get(validationId);
    if (result) {
      if (exportType === "json") {
        downloadJson(`pre-migration-validation-${validationId}.json`, result);
      } else {
        downloadCsv(`pre-migration-checks-${validationId}.csv`, result.checks as unknown as Array<Record<string, unknown>>);
      }
    }
    return { source: "mock" };
  },

  async downloadExecutionSimulationExport(simulationId: string, exportType: "json" | "markdown"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !simulationId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/pre-migration/simulations/${simulationId}/export/${exportType}`);
        downloadBlob(blob, `execution-simulation-${simulationId}.${exportType === "markdown" ? "md" : "json"}`);
        return { source: "backend" };
      } catch { /* fallback */ }
    }
    const result = mockExecutionSimulations.get(simulationId);
    if (result) downloadJson(`execution-simulation-${simulationId}.json`, result);
    return { source: "mock" };
  },

  async createMigrationExecutionJobFromPlan(planId: string, request: Partial<MigrationExecutionRequest> = {}): Promise<CreateMigrationExecutionJobResponse> {
    const payload: MigrationExecutionRequest = {
      mode: "simulation",
      requireGoDecision: false,
      selectedWaveIds: [],
      createdBy: "Migration Lead",
      ...request
    };
    if (hasBackendBaseUrl() && !planId.startsWith("mock-")) {
      try {
        return await apiPost<MigrationExecutionRequest, CreateMigrationExecutionJobResponse>(`/api/migration-execution/jobs/from-plan/${planId}`, payload);
      } catch {
        // fall through
      }
    }
    const plan = mockMigrationPlans.get(planId) ?? await this.getLatestMigrationPlan();
    if (!plan) throw new Error("Create a migration plan first.");
    const job = buildMockMigrationExecutionJob(plan, payload);
    mockMigrationExecutionJobs.set(job.jobId, job);
    return { jobId: job.jobId, planId: job.planId, status: job.status, mode: job.mode, message: "Migration execution job created in local simulation mode." };
  },

  async getMigrationExecutionJob(jobId: string): Promise<MigrationExecutionJob | null> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiGet<MigrationExecutionJob>(`/api/migration-execution/jobs/${jobId}`); } catch { /* fallback */ }
    }
    return mockMigrationExecutionJobs.get(jobId) ?? null;
  },

  async getLatestMigrationExecutionJob(): Promise<MigrationExecutionJob | null> {
    if (hasBackendBaseUrl()) {
      try { return await apiGet<MigrationExecutionJob>("/api/migration-execution/jobs/latest"); } catch { /* fallback */ }
    }
    const values = [...mockMigrationExecutionJobs.values()];
    return values.length ? values[values.length - 1] : null;
  },

  async listMigrationExecutionJobs(): Promise<MigrationExecutionJob[]> {
    if (hasBackendBaseUrl()) {
      try { return await apiGet<MigrationExecutionJob[]>("/api/migration-execution/jobs"); } catch { /* fallback */ }
    }
    return [...mockMigrationExecutionJobs.values()];
  },

  async startMigrationExecutionJob(jobId: string): Promise<MigrationExecutionJob | null> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiPost<object, MigrationExecutionJob>(`/api/migration-execution/jobs/${jobId}/start`, {}); } catch { /* fallback */ }
    }
    const job = mockMigrationExecutionJobs.get(jobId);
    if (!job) return null;
    const updated = startMockMigrationExecutionJob(job);
    mockMigrationExecutionJobs.set(jobId, updated);
    return updated;
  },

  async pauseMigrationExecutionJob(jobId: string): Promise<MigrationExecutionJob | null> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiPost<object, MigrationExecutionJob>(`/api/migration-execution/jobs/${jobId}/pause`, {}); } catch { /* fallback */ }
    }
    const job = mockMigrationExecutionJobs.get(jobId);
    if (!job) return null;
    const updated = { ...job, status: job.status === "running" ? "paused" : job.status };
    mockMigrationExecutionJobs.set(jobId, updated);
    return updated;
  },

  async resumeMigrationExecutionJob(jobId: string): Promise<MigrationExecutionJob | null> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiPost<object, MigrationExecutionJob>(`/api/migration-execution/jobs/${jobId}/resume`, {}); } catch { /* fallback */ }
    }
    const job = mockMigrationExecutionJobs.get(jobId);
    if (!job) return null;
    const updated = job.status === "paused" ? startMockMigrationExecutionJob({ ...job, status: "running" }) : job;
    mockMigrationExecutionJobs.set(jobId, updated);
    return updated;
  },

  async cancelMigrationExecutionJob(jobId: string): Promise<MigrationExecutionJob | null> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiPost<object, MigrationExecutionJob>(`/api/migration-execution/jobs/${jobId}/cancel`, {}); } catch { /* fallback */ }
    }
    const job = mockMigrationExecutionJobs.get(jobId);
    if (!job) return null;
    const updated = { ...job, status: "cancelled", completedAt: new Date().toISOString() };
    mockMigrationExecutionJobs.set(jobId, updated);
    return updated;
  },

  async retryFailedMigrationExecutionJob(jobId: string): Promise<MigrationExecutionJob | null> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiPost<object, MigrationExecutionJob>(`/api/migration-execution/jobs/${jobId}/retry-failed`, {}); } catch { /* fallback */ }
    }
    const job = mockMigrationExecutionJobs.get(jobId);
    if (!job) return null;
    const updated = { ...job, waves: job.waves.map((wave) => ({ ...wave, items: wave.items.map((item) => item.status === "failed" ? { ...item, status: "skipped", errors: [], warnings: [...item.warnings, "Retry remained risky and was skipped in simulation."] } : item) })) };
    mockMigrationExecutionJobs.set(jobId, updated);
    return startMockMigrationExecutionJob(updated);
  },

  async getMigrationExecutionTimeline(jobId: string) {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiGet<MigrationExecutionJob["timeline"]>(`/api/migration-execution/jobs/${jobId}/timeline`); } catch { /* fallback */ }
    }
    return mockMigrationExecutionJobs.get(jobId)?.timeline ?? [];
  },

  async downloadMigrationExecutionReport(jobId: string, exportType: "json" | "csv" | "markdown"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/migration-execution/jobs/${jobId}/report/${exportType}`);
        downloadBlob(blob, `migration-execution-${jobId}.${exportType === "markdown" ? "md" : exportType}`);
        return { source: "backend" };
      } catch { /* fallback */ }
    }
    const job = mockMigrationExecutionJobs.get(jobId);
    if (job) {
      if (exportType === "json") downloadJson(`migration-execution-${jobId}.json`, job);
      else downloadCsv(`migration-execution-${jobId}.csv`, job.waves.flatMap((wave) => wave.items) as unknown as Array<Record<string, unknown>>);
    }
    return { source: "mock" };
  },

  async validateSharePointMigrationCapabilities(): Promise<SharePointMigrationCapabilityResult> {
    const payload = {
      sourceSiteUrl: "https://tenant.sharepoint.com/sites/source",
      targetSiteUrl: "https://tenant.sharepoint.com/sites/target",
      clientId: "client-id-placeholder",
      mode: "validate_only",
      includePermissions: true,
      includeMetadata: true
    };
    if (hasBackendBaseUrl()) {
      try { return await apiPost<typeof payload, SharePointMigrationCapabilityResult>("/api/sharepoint-migration/capabilities/validate", payload); } catch { /* fallback */ }
    }
    return {
      isReady: false,
      mode: "validate_only",
      checks: [{ checkId: "live-flag", title: "Live migration flag", status: "failed", severity: "High", message: "Live migration disabled by default." }],
      errors: [],
      warnings: ["Local fallback. No tenant connectivity attempted."],
      capabilities: { canReadSource: true, canReadTarget: true, canWriteTarget: false, canUploadFiles: false, canCreateFolders: false, canApplyMetadata: false, canApplyPermissions: false }
    };
  },

  async generateSharePointTransferPreview(jobId: string): Promise<MigrationTransferPreview | null> {
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiPost<object, MigrationTransferPreview>(`/api/sharepoint-migration/preview/from-job/${jobId}`, {}); } catch { /* fallback */ }
    }
    const job = mockMigrationExecutionJobs.get(jobId);
    if (!job) return null;
    const transferPlan = job.waves.flatMap((wave) => wave.items.map((item) => {
      const blocked = item.status === "failed" || item.status === "skipped" || item.action === "manual_review" || item.action === "remediate_first" || !item.simulatedTargetUrl;
      return {
        itemId: item.itemExecutionId,
        sourcePath: item.simulatedSourceUrl || item.path,
        targetPath: item.simulatedTargetUrl,
        itemType: item.itemType,
        estimatedSizeBytes: 0,
        metadataMappingStatus: "previewed",
        permissionMappingStatus: "not_applied",
        eligibility: blocked ? "blocked" : item.warnings.length ? "warning" : "eligible",
        reason: blocked ? "Resolve blockers before live pilot." : "Eligible for future pilot planning."
      };
    }));
    const preview: MigrationTransferPreview = {
      previewId: `mock-preview-${Date.now()}`,
      jobId,
      mode: "preview_only",
      generatedAt: new Date().toISOString(),
      totalItems: transferPlan.length,
      eligibleItems: transferPlan.filter((item) => item.eligibility === "eligible").length,
      blockedItems: transferPlan.filter((item) => item.eligibility === "blocked").length,
      metadataMappings: [{ sourceField: "Title", targetField: "Title", mappingStatus: "mapped", issue: "" }],
      permissionMappings: [{ sourcePrincipal: "Source Owners", targetPrincipal: "Target Owners", permissionLevel: "Owner", mappingStatus: "not_applied", issue: "Permission writeback disabled for pilot." }],
      transferPlan,
      blocked: transferPlan.filter((item) => item.eligibility === "blocked").map((item) => ({ itemId: item.itemId, path: item.sourcePath, reason: item.reason, recommendedAction: "Resolve blocker before live pilot." })),
      warnings: ["Preview only. No SharePoint tenant changes performed."],
      errors: []
    };
    mockTransferPreviews.set(preview.previewId, preview);
    return preview;
  },

  async getLatestSharePointTransferPreview(): Promise<MigrationTransferPreview | null> {
    const values = [...mockTransferPreviews.values()];
    return values.length ? values[values.length - 1] : null;
  },

  async runLockedLivePilot(jobId: string, request: Partial<LivePilotMigrationRequest> = {}): Promise<LivePilotMigrationResult | null> {
    const payload: LivePilotMigrationRequest = {
      mode: "live_pilot",
      confirmationText: "",
      selectedWaveId: "",
      selectedLibrary: "",
      maxFiles: 10,
      sourceSiteUrl: "https://tenant.sharepoint.com/sites/source",
      targetSiteUrl: "https://tenant.sharepoint.com/sites/target",
      targetLibrary: "Migration Pilot",
      preserveMetadata: true,
      preservePermissions: false,
      overwriteExisting: false,
      ...request
    };
    if (hasBackendBaseUrl() && !jobId.startsWith("mock-")) {
      try { return await apiPost<LivePilotMigrationRequest, LivePilotMigrationResult>(`/api/sharepoint-migration/pilot/from-job/${jobId}`, payload); } catch { /* fallback */ }
    }
    const result: LivePilotMigrationResult = {
      pilotRunId: `mock-pilot-${Date.now()}`,
      jobId,
      status: "blocked",
      mode: "live_pilot",
      message: "Live migration is disabled. Set ZMS_ENABLE_LIVE_MIGRATION=true and pass all safety gates.",
      generatedAt: new Date().toISOString(),
      filesAttempted: 0,
      filesCopied: 0,
      filesSkipped: 0,
      safetyChecks: [{ checkId: "env-flag", title: "Live migration flag enabled", status: "failed", severity: "High", message: "Live migration disabled by default." }],
      items: [],
      warnings: ["No SharePoint tenant changes performed."],
      errors: []
    };
    mockPilotRuns.set(result.pilotRunId, result);
    return result;
  },

  async getLatestLivePilotResult(): Promise<LivePilotMigrationResult | null> {
    const values = [...mockPilotRuns.values()];
    return values.length ? values[values.length - 1] : null;
  },

  async downloadSharePointPreviewReport(previewId: string, exportType: "json" | "csv"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !previewId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/sharepoint-migration/preview/${previewId}/report/${exportType}`);
        downloadBlob(blob, `transfer-preview-${previewId}.${exportType}`);
        return { source: "backend" };
      } catch { /* fallback */ }
    }
    const preview = mockTransferPreviews.get(previewId);
    if (preview) {
      if (exportType === "json") {
        downloadJson(`transfer-preview-${previewId}.json`, preview);
      } else {
        downloadCsv(`transfer-plan-${previewId}.csv`, preview.transferPlan as unknown as Array<Record<string, unknown>>);
      }
    }
    return { source: "mock" };
  },

  async downloadLivePilotReport(pilotRunId: string, exportType: "json" | "csv" | "markdown"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !pilotRunId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/sharepoint-migration/pilot/${pilotRunId}/report/${exportType}`);
        downloadBlob(blob, `pilot-report-${pilotRunId}.${exportType === "markdown" ? "md" : exportType}`);
        return { source: "backend" };
      } catch { /* fallback */ }
    }
    const pilot = mockPilotRuns.get(pilotRunId);
    if (pilot) {
      if (exportType === "json") {
        downloadJson(`pilot-result-${pilotRunId}.json`, pilot);
      } else {
        downloadCsv(`pilot-items-${pilotRunId}.csv`, pilot.items as unknown as Array<Record<string, unknown>>);
      }
    }
    return { source: "mock" };
  },

  async runFullWorkflowValidation(request: Partial<WorkflowValidationRequest> = {}): Promise<WorkflowValidationResponse> {
    const payload: WorkflowValidationRequest = {
      source: "latest_scan",
      useSampleFallback: true,
      createdBy: "Migration Lead",
      includeExecutionSimulation: true,
      includeTransferPreview: true,
      ...request
    };
    if (hasBackendBaseUrl()) {
      try {
        return await apiPost<WorkflowValidationRequest, WorkflowValidationResponse>("/api/workflow-validation/run-full-chain", payload);
      } catch {
        // fall through
      }
    }
    const run = buildMockWorkflowRun();
    mockWorkflowRuns.set(run.workflowRunId, run);
    return {
      workflowRunId: run.workflowRunId,
      status: run.status,
      overallResult: run.overallResult,
      stepsPassed: run.steps.filter((step) => step.status === "passed").length,
      stepsFailed: run.steps.filter((step) => step.status === "failed").length,
      stepsWarning: run.steps.filter((step) => step.status === "warning").length,
      summary: run.summary
    };
  },

  async getWorkflowValidationRun(workflowRunId: string): Promise<WorkflowValidationRun | null> {
    if (hasBackendBaseUrl() && !workflowRunId.startsWith("mock-")) {
      try { return await apiGet<WorkflowValidationRun>(`/api/workflow-validation/${workflowRunId}`); } catch { /* fallback */ }
    }
    return mockWorkflowRuns.get(workflowRunId) ?? null;
  },

  async getLatestWorkflowValidation(): Promise<WorkflowValidationRun | null> {
    if (hasBackendBaseUrl()) {
      try { return await apiGet<WorkflowValidationRun>("/api/workflow-validation/latest"); } catch { /* fallback */ }
    }
    const values = [...mockWorkflowRuns.values()];
    return values.length ? values[values.length - 1] : null;
  },

  async downloadWorkflowValidationExport(workflowRunId: string, exportType: "json" | "markdown"): Promise<{ source: "backend" | "mock" }> {
    if (hasBackendBaseUrl() && !workflowRunId.startsWith("mock-")) {
      try {
        const blob = await apiGetBlob(`/api/workflow-validation/${workflowRunId}/export/${exportType}`);
        downloadBlob(blob, `workflow-validation-${workflowRunId}.${exportType === "markdown" ? "md" : "json"}`);
        return { source: "backend" };
      } catch {
        // fall through
      }
    }
    const run = mockWorkflowRuns.get(workflowRunId);
    if (run) {
      if (exportType === "json") downloadJson(`workflow-validation-${workflowRunId}.json`, run);
      else downloadJson(`workflow-validation-report-${workflowRunId}.json`, { summary: run.summary, issues: run.issues, steps: run.steps });
    }
    return { source: "mock" };
  },

  async getDemoStatus(): Promise<DemoStatus> {
    if (hasBackendBaseUrl()) {
      try { return await apiGet<DemoStatus>("/api/demo/status"); } catch { /* fallback */ }
    }
    return mockDemoStatus;
  },

  async resetDemoData(): Promise<DemoStatus> {
    if (hasBackendBaseUrl()) {
      try { return await apiPost<object, DemoStatus>("/api/demo/reset", {}); } catch { /* fallback */ }
    }
    mockDemoStatus = { ...mockDemoStatus, seeded: false, lastDemoChainResult: "reset", warnings: ["Demo status reset locally."] };
    return mockDemoStatus;
  },

  async seedDemoData(): Promise<DemoStatus> {
    if (hasBackendBaseUrl()) {
      try { return await apiPost<object, DemoStatus>("/api/demo/seed", {}); } catch { /* fallback */ }
    }
    if (!getLatestMockDiscoveryScan()) {
      const result = generateDiscoveryResults(buildEnvironmentConfig(siteCollections, {
        includeDefaultSubsites: true,
        generateSampleDocuments: true,
        includeMetadataColumns: true,
        createPermissionGroups: true,
        addMigrationEdgeCases: true,
        includeArchivedFolders: true,
        includeLongPathExamples: true,
        includeLargeFilePlaceholders: true
      }, {
        tenantName: "ZMS Demo Tenant",
        adminUrl: "https://tenant-admin.sharepoint.com",
        rootUrl: "https://tenant.sharepoint.com",
        ownerEmail: "migration.lead@contoso.com",
        clientIdPlaceholder: "client-id-placeholder",
        targetUrlPrefix: "https://tenant.sharepoint.com/sites",
        generatedBy: "ZMS Demo"
      }));
      mockDiscoveryScans.set(result.scanId, result);
    }
    mockDemoStatus = { ...mockDemoStatus, seeded: true, latestScanId: getLatestMockDiscoveryScan()?.scanId ?? "", lastDemoChainResult: "seeded" };
    return mockDemoStatus;
  },

  async runDemoScriptedChain(): Promise<DemoStatus> {
    if (hasBackendBaseUrl()) {
      try { return await apiPost<object, DemoStatus>("/api/demo/run-scripted-chain", {}); } catch { /* fallback */ }
    }
    const response = await this.runFullWorkflowValidation();
    mockDemoStatus = {
      ...mockDemoStatus,
      seeded: true,
      latestWorkflowRunId: response.workflowRunId,
      latestScanId: response.summary.scanId,
      latestAssessmentId: response.summary.assessmentId,
      latestPlanId: response.summary.planId,
      latestExecutionJobId: response.summary.executionJobId,
      latestPreviewId: response.summary.previewId,
      lastDemoChainResult: response.overallResult
    };
    return mockDemoStatus;
  },

  async getPermissionRisks() {
    await delay();
    return permissionsRisks;
  },

  async getMetadataMappings() {
    await delay();
    return metadataMappings;
  },

  async getReports() {
    await delay();
    return reports;
  },

  async getAIRecommendations() {
    await delay();
    return aiRecommendations;
  },

  async askAiAdvisor(question: string, discoveryRunId?: string, migrationJobId?: string, validationRunId?: string) {
    if (hasBackendBaseUrl()) {
      try {
        return await apiPost<{ question: string; discoveryRunId?: string; migrationJobId?: string; validationRunId?: string }, {
          answer: string;
          usedOllama: boolean;
          model: string;
          warning?: string;
          contextSummary: unknown;
        }>("/api/ai/advisor/ask", { question, discoveryRunId, migrationJobId, validationRunId });
      } catch {
        // fall through to deterministic local fallback
      }
    }

    const latest = getLatestMockDiscoveryScan();
    const topRisk = latest?.migrationRisks[0];
    return {
      answer: topRisk
        ? `The highest current risk is ${topRisk.riskType} at ${topRisk.site}. ${topRisk.recommendedAction}`
        : "Run or import discovery before asking the advisor for migration-specific guidance.",
      usedOllama: false,
      model: "fallback",
      warning: "Backend or Ollama unavailable. Showing deterministic fallback guidance.",
      contextSummary: latest?.summary ?? null
    };
  },

  async getDiscoveryRemediation(scanId: string) {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<Array<{
          issue: string;
          impact: string;
          recommendedFix: string;
          priority: string;
          automationEligible: boolean;
          confidence: number;
          sourceFindingId: string;
        }>>(`/api/ai/remediation/discovery/${scanId}`);
      } catch {
        // fall through to mock fallback
      }
    }

    const latest = getLatestMockDiscoveryScan();
    return (latest?.migrationRisks ?? []).slice(0, 6).map((risk) => ({
      issue: risk.riskType,
      impact: risk.description,
      recommendedFix: risk.recommendedAction,
      priority: risk.riskLevel,
      automationEligible: risk.riskType.includes("Metadata") || risk.riskType.includes("Path"),
      confidence: 0.82,
      sourceFindingId: risk.id
    }));
  },

  async getDiscoveryEtaEstimate(scanId: string) {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<{
          estimatedDuration: string;
          confidence: number;
          bottleneckExplanation: string;
          assumptions: string[];
          optimizationRecommendations: string[];
        }>(`/api/discovery/${scanId}/eta-estimate`);
      } catch {
        // fall through to local fallback
      }
    }

    const latest = getLatestMockDiscoveryScan();
    const files = latest?.summary.files ?? 0;
    return {
      estimatedDuration: `PT${Math.max(10, Math.ceil(files / 40))}M`,
      confidence: latest ? 0.7 : 0.35,
      bottleneckExplanation: "Fallback ETA uses discovered file count only.",
      assumptions: ["Backend ETA endpoint unavailable."],
      optimizationRecommendations: ["Run backend discovery for a stronger ETA estimate."]
    };
  },

  async getCopilotReadinessLatest() {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<{
          overallScore: number;
          riskTier: string;
          summary: string;
          categoryScores: Record<string, number>;
          topFindings: Array<{ category: string; severity: string; location: string; description: string; recommendation: string }>;
          recommendedActions: string[];
        }>("/api/copilot-readiness/latest");
      } catch {
        // fall through to local fallback
      }
    }

    return null;
  },

  async getCopilotReadiness(discoveryRunId: string) {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<{
          overallScore: number;
          riskTier: string;
          summary: string;
          categoryScores: Record<string, number>;
          topFindings: Array<{ category: string; severity: string; location: string; description: string; recommendation: string }>;
          recommendedActions: string[];
        }>(`/api/copilot-readiness/${discoveryRunId}`);
      } catch {
        // fall through to latest fallback
      }
    }

    return this.getCopilotReadinessLatest();
  },

  async importOnPremDemo() {
    if (hasBackendBaseUrl()) {
      try {
        return await apiPost<object, {
          runId: string;
          summary: Record<string, number>;
          assets: Array<{ id: string; name: string; assetType: string; location: string }>;
          findings: Array<{ id: string; assetId: string; assetType: string; complexity: string; description: string; recommendation: string; requiresHumanReview: boolean }>;
          recommendations: Array<{ assetId: string; modernizationTarget: string; feasibility: string; estimatedEffort: string; automationEligible: boolean }>;
        }>("/api/onprem/discovery/import", {
          farmUrl: "https://legacy.contoso.local",
          version: "SharePoint2016",
          scanMethod: "ManifestImport",
          assets: []
        });
      } catch {
        // fall through to local fallback
      }
    }

    return null;
  },

  async getModernizationAssets(runId: string) {
    return hasBackendBaseUrl()
      ? apiGet<Array<{ id: string; name: string; assetType: string; location: string }>>(`/api/modernization/${runId}/assets`)
      : [];
  },

  async getModernizationRecommendations(runId: string) {
    return hasBackendBaseUrl()
      ? apiGet<Array<{ assetId: string; modernizationTarget: string; feasibility: string; estimatedEffort: string; automationEligible: boolean }>>(`/api/modernization/${runId}/recommendations`)
      : [];
  },

  async createModernizationDraftSpec(assetId: string) {
    if (!hasBackendBaseUrl()) return null;
    try {
      return await apiPost<object, {
        id: string;
        assetId: string;
        title: string;
        targetPlatform: string;
        sections: Record<string, unknown>;
        requiresHumanReview: boolean;
      }>(`/api/modernization/${assetId}/draft-spec`, {});
    } catch {
      return null;
    }
  },

  async explainModernization(runId: string) {
    if (!hasBackendBaseUrl()) return null;
    try {
      return await apiPost<object, { explanation: string }>(`/api/modernization/${runId}/explain`, {});
    } catch {
      return null;
    }
  },

  async startTeamsDiscovery() {
    if (hasBackendBaseUrl()) {
      try {
        return await apiPost<object, {
          runId: string;
          summary: Record<string, number>;
          topology: Array<Record<string, unknown>>;
          risks: Array<{ id: string; category: string; severity: string; teamName: string; description: string; recommendation: string }>;
          teams: Array<Record<string, unknown>>;
        }>("/api/teams/discovery/start", { mode: "Demo" });
      } catch {
        // fall through
      }
    }

    return null;
  },

  async getLatestTeamsDiscovery() {
    if (hasBackendBaseUrl()) {
      try {
        return await apiGet<{
          runId: string;
          summary: Record<string, number>;
          topology: Array<Record<string, unknown>>;
          risks: Array<{ id: string; category: string; severity: string; teamName: string; description: string; recommendation: string }>;
          teams: Array<Record<string, unknown>>;
        }>("/api/teams/discovery/latest");
      } catch {
        // fall through
      }
    }

    return null;
  }
};
