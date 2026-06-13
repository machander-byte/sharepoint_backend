export type RiskLevel = "Low" | "Medium" | "High" | "Critical";
export type MappingStatus = "Mapped" | "Unmapped" | "Conflict" | "Suggested";
export type ConnectionStatus = "Connected" | "Warning" | "Config Required" | "Disconnected";
export type JobStatus = "Running" | "Completed" | "Failed" | "Scheduled";
export type ReportFormat = "CSV" | "PDF" | "JSON";
export type ToastTone = "success" | "warning" | "error" | "info";
export type UiDiscoveryStatus = "idle" | "running" | "completed" | "failed";
export type BackendDiscoveryStatus = "queued" | "running" | "completed" | "partial" | "failed" | "cancelled";
export type PackageGenerationStatus = "idle" | "running" | "success" | "warning" | "error";

export interface Subsite {
  id: string;
  name: string;
  description: string;
}

export interface Library {
  id: string;
  name: string;
  type: string;
  metadataCount: number;
  files: number;
  storageGb: number;
  permissionStatus: string;
  riskLevel: RiskLevel;
}

export interface CustomList {
  id: string;
  name: string;
  itemCount: number;
  purpose: string;
}

export interface MetadataField {
  id: string;
  name: string;
  type: "Text" | "Choice" | "Person" | "Date" | "Number" | "Managed Metadata";
  usedIn: string;
  required: boolean;
}

export interface PermissionGroup {
  id: string;
  name: string;
  role: string;
  users: number;
}

export interface MigrationEdgeCase {
  id: string;
  title: string;
  description: string;
  riskLevel: RiskLevel;
}

export interface SiteCollection {
  id: string;
  name: string;
  department: string;
  description: string;
  owner: string;
  subsites: Subsite[];
  libraries: Library[];
  lists: CustomList[];
  metadataFields: MetadataField[];
  permissionGroups: PermissionGroup[];
  edgeCases: MigrationEdgeCase[];
}

export interface Connection {
  id: string;
  name: string;
  kind: "Source" | "Target";
  provider: string;
  status: ConnectionStatus;
  tenant?: string;
  authMethod?: string;
  lastSync?: string;
  message?: string;
  warning?: string;
  actions: string[];
}

export interface BuilderOptions {
  includeDefaultSubsites: boolean;
  generateSampleDocuments: boolean;
  includeMetadataColumns: boolean;
  createPermissionGroups: boolean;
  addMigrationEdgeCases: boolean;
  includeArchivedFolders: boolean;
  includeLongPathExamples: boolean;
  includeLargeFilePlaceholders: boolean;
}

export interface TenantValues {
  tenantName: string;
  adminUrl: string;
  rootUrl: string;
  ownerEmail: string;
  clientIdPlaceholder: string;
  targetUrlPrefix: string;
  generatedBy: string;
}

export interface EnvironmentConfig {
  tenantName: string;
  adminUrl: string;
  rootUrl: string;
  ownerEmail: string;
  clientIdPlaceholder: string;
  siteCollections: SiteCollectionConfig[];
  globalOptions: BuilderOptions;
  generatedAt: string;
  generatedBy: string;
}

export interface SiteCollectionConfig {
  id: string;
  title: string;
  url: string;
  department: string;
  description: string;
  subsites: SubsiteConfig[];
  libraries: LibraryConfig[];
  lists: ListConfig[];
  metadataFields: MetadataFieldConfig[];
  permissionGroups: PermissionGroupConfig[];
  permissionRules: PermissionRuleConfig[];
  folderStructures: FolderStructureConfig[];
  edgeCases: MigrationEdgeCaseConfig[];
}

export interface SubsiteConfig {
  id: string;
  title: string;
  url: string;
  description: string;
}

export interface LibraryConfig {
  id: string;
  title: string;
  type: string;
  description: string;
  metadataFieldIds: string[];
  folders: FolderStructureConfig[];
  sampleFileCount: number;
  includeVersioning: boolean;
}

export interface ListConfig {
  id: string;
  title: string;
  description: string;
  columns: MetadataFieldConfig[];
  sampleItemCount: number;
}

export interface MetadataFieldConfig {
  id: string;
  name: string;
  type: MetadataField["type"];
  required: boolean;
  choices?: string[];
  defaultValue?: string;
}

export interface PermissionGroupConfig {
  id: string;
  name: string;
  role: string;
  users: string[];
}

export interface PermissionRuleConfig {
  id: string;
  targetPath: string;
  inheritance: "Inherited" | "Broken";
  groups: string[];
  notes: string;
}

export interface FolderStructureConfig {
  id: string;
  name: string;
  path: string;
  archived?: boolean;
  longPathExample?: boolean;
  largeFilePlaceholder?: boolean;
  children?: FolderStructureConfig[];
}

export interface MigrationEdgeCaseConfig {
  id: string;
  title: string;
  description: string;
  riskLevel: RiskLevel;
  affectedPath: string;
}

export interface ConnectionConfig {
  id: string;
  name: string;
  connectorType: string;
  kind: "Source" | "Target";
  status: ConnectionStatus;
  tenantUrl?: string;
  clientIdPlaceholder?: string;
  authenticationType?: string;
  siteUrl?: string;
  documentLibrary?: string;
  message?: string;
  updatedAt: string;
}

export interface DiscoveryScanRequest {
  scanName: string;
  mode: "config" | "live";
  tenantUrl: string;
  adminUrl: string;
  siteUrls: string[];
  clientId: string;
  includeFiles: boolean;
  includePermissions: boolean;
  includeMetadata: boolean;
  includeSubsites: boolean;
  environmentConfigId?: string;
  environmentConfigPath?: string;
}

export interface StartDiscoveryScanResponse {
  scanId: string;
  status: BackendDiscoveryStatus;
  message: string;
}

export interface DiscoveryImportResponse {
  scanId: string;
  status: "completed";
  message: string;
  summary: DiscoverySummary;
}

export interface DiscoveryScanStatusResponse {
  scanId: string;
  status: BackendDiscoveryStatus;
  progress: number;
  currentStep: string;
  startedAt: string;
  completedAt?: string | null;
  errors: string[];
  warnings: string[];
}

export interface DiscoverySummary {
  siteCollections: number;
  subsites: number;
  libraries: number;
  lists: number;
  files: number;
  folders: number;
  totalStorageBytes: number;
  metadataFields: number;
  permissionGroups: number;
  brokenInheritanceCount: number;
  longPathRisks: number;
  largeFileRisks: number;
  missingMetadataIssues: number;
  readinessScore: number;
}

export interface DiscoveredInventoryItem {
  id: string;
  siteCollection: string;
  subsite: string;
  library: string;
  itemType: string;
  path: string;
  fileCount: number;
  sizeBytes: number;
  metadataCount: number;
  permissionStatus: string;
  riskLevel: RiskLevel;
  readinessStatus: string;
}

export interface PermissionRiskFinding {
  id: string;
  site: string;
  libraryOrFolder: string;
  inheritanceStatus: "Inherited" | "Broken" | string;
  groups: string[];
  users: string[];
  accessLevels: string[];
  riskLevel: RiskLevel;
  recommendedAction: string;
}

export interface MetadataFinding {
  id: string;
  site: string;
  library: string;
  fieldName: string;
  fieldType: MetadataField["type"] | string;
  required: boolean;
  missingValueCount: number;
  mappedTargetField: string;
  mappingRisk: RiskLevel;
}

export interface MigrationRiskFinding {
  id: string;
  riskType: string;
  site: string;
  libraryOrPath: string;
  path: string;
  riskLevel: RiskLevel;
  description: string;
  recommendedAction: string;
}

export interface DiscoveryScanResult {
  scanId: string;
  scanName: string;
  mode: "config" | "live" | string;
  status: BackendDiscoveryStatus;
  startedAt: string;
  completedAt?: string | null;
  summary: DiscoverySummary;
  siteCollections: unknown[];
  inventoryItems: DiscoveredInventoryItem[];
  permissionRisks: PermissionRiskFinding[];
  metadataFindings: MetadataFinding[];
  migrationRisks: MigrationRiskFinding[];
  warnings: string[];
  errors: string[];
}

export interface ReportDefinition {
  id: string;
  title: string;
  description: string;
  formats: ReportFormat[];
}

export interface GeneratedReport {
  id: string;
  reportId: string;
  title: string;
  format: ReportFormat;
  generatedAt: string;
  rows: unknown[];
}

export interface ToastNotification {
  id: string;
  tone: ToastTone;
  title: string;
  description?: string;
}

export interface DashboardStat {
  id: string;
  label: string;
  value: string | number;
  tone?: "default" | "success" | "warning" | "error" | "primary";
  caption?: string;
}

export interface RiskItem {
  id: string;
  riskType: string;
  count: number | string;
  severity: RiskLevel;
  affectedArea: string;
  recommendedAction: string;
}

export interface PermissionRisk {
  id: string;
  site: string;
  location: string;
  inheritanceStatus: "Inherited" | "Broken";
  groups: string;
  users: number | string;
  riskLevel: RiskLevel;
  recommendedAction: string;
}

export interface MetadataMapping {
  id: string;
  sourceField: string;
  fieldType: MetadataField["type"];
  usedIn: string;
  targetField: string;
  mappingStatus: MappingStatus;
  issue?: string;
}

export interface ModernizationItem {
  id: string;
  legacyAsset: string;
  sourceType: string;
  department: string;
  complexity: RiskLevel;
  recommendedTarget: string;
  confidence: number;
}

export interface MigrationJob {
  id: string;
  name: string;
  source: string;
  target: string;
  progress: number;
  filesMigrated: number;
  totalFiles: number;
  errors: number;
  started: string;
  status: JobStatus;
}

export interface ReportItem {
  id: string;
  title: string;
  description: string;
  lastGenerated: string;
  formats: ReportFormat[];
}

export interface AIRecommendation {
  id: string;
  category: string;
  issue: string;
  impact: string;
  suggestedAction: string;
  confidence: number;
  affectedLocation: string;
}

export interface ReadinessSummary {
  blockers: number;
  highRisks: number;
  mediumRisks: number;
  lowRisks: number;
  remediationActions: number;
  suggestedWaves: number;
}

export interface ReadinessAnalyzeResponse {
  assessmentId: string;
  scanId: string;
  status: "completed";
  readinessScore: number;
  riskLevel: string;
  summary: ReadinessSummary;
}

export interface ReadinessRiskFinding {
  id: string;
  category: string;
  severity: string;
  title: string;
  description: string;
  affectedLocation: string;
  affectedSite: string;
  affectedLibrary: string;
  affectedPath: string;
  evidence: string;
  impact: string;
  recommendedAction: string;
  canAutoRemediate: boolean;
  migrationBlocker: boolean;
}

export interface RemediationAction {
  id: string;
  priority: string;
  category: string;
  actionTitle: string;
  actionDescription: string;
  affectedLocations: string[];
  estimatedEffort: string;
  ownerRole: string;
  status: string;
  dependsOn: string[];
  expectedBenefit: string;
}

export interface MigrationWaveSuggestion {
  waveId: string;
  waveName: string;
  description: string;
  recommendedOrder: number;
  includedSites: string[];
  includedLibraries: string[];
  excludedRisks: string[];
  estimatedFiles: number;
  estimatedStorage: number;
  readinessScore: number;
  riskLevel: string;
  prerequisites: string[];
}

export interface ModernizationOpportunity {
  id: string;
  type: string;
  sourceName: string;
  location: string;
  potentialTarget: string;
  rationale: string;
  estimatedEffort: string;
}

export interface MigrationReadinessAssessment {
  assessmentId: string;
  scanId: string;
  generatedAt: string;
  status: "completed";
  readinessScore: number;
  riskLevel: string;
  summary: ReadinessSummary;
  riskFindings: ReadinessRiskFinding[];
  remediationActions: RemediationAction[];
  migrationWaves: MigrationWaveSuggestion[];
  modernizationOpportunities: ModernizationOpportunity[];
  warnings: string[];
  errors: string[];
}

export interface CreateMigrationPlanResponse {
  planId: string;
  assessmentId: string;
  status: string;
  message: string;
}

export interface MigrationPlan {
  planId: string;
  assessmentId: string;
  scanId: string;
  planName: string;
  description: string;
  status: string;
  createdAt: string;
  updatedAt: string;
  createdBy: string;
  sourceEnvironment: string;
  targetEnvironment: string;
  waves: MigrationPlanWave[];
  options: MigrationPlanOption[];
  checklist: MigrationPlanChecklistItem[];
  risks: ReadinessRiskFinding[];
  remediationPrerequisites: RemediationAction[];
  approvals: MigrationPlanApproval[];
  runbookPath: string;
  warnings: string[];
  errors: string[];
}

export interface MigrationPlanWave {
  waveId: string;
  waveName: string;
  order: number;
  description: string;
  riskLevel: string;
  readinessScore: number;
  includedItems: MigrationPlanItem[];
  excludedItems: MigrationPlanItem[];
  prerequisites: string[];
  estimatedFiles: number;
  estimatedStorage: number;
  estimatedDuration: string;
  ownerRole: string;
  approvalStatus: string;
  notes: string;
}

export interface MigrationPlanItem {
  itemId: string;
  siteCollection: string;
  library: string;
  path: string;
  itemType: string;
  sourceUrl: string;
  targetUrl: string;
  fileCount: number;
  storageBytes: number;
  metadataCount: number;
  permissionRisk: string;
  migrationAction: string;
  includeInMigration: boolean;
  reason: string;
}

export interface MigrationPlanOption {
  key: string;
  label: string;
  value: boolean;
  description: string;
}

export interface MigrationPlanChecklistItem {
  id: string;
  title: string;
  description: string;
  category: string;
  required: boolean;
  status: string;
  ownerRole: string;
}

export interface MigrationPlanValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  checklist: MigrationPlanChecklistItem[];
}

export interface MigrationRunbook {
  planId: string;
  fileName: string;
  markdown: string;
  generatedAt: string;
}

export interface MigrationPlanApproval {
  role: string;
  status: string;
  approvedBy: string;
  approvedAt?: string | null;
  notes: string;
}

export interface PreMigrationValidationSummary {
  errors: number;
  warnings: number;
  passedChecks: number;
  blockedWaves: number;
  readyWaves: number;
}

export interface PreMigrationValidationResponse {
  validationId: string;
  planId: string;
  status: string;
  decision: "go" | "conditional_go" | "no_go" | string;
  summary: PreMigrationValidationSummary;
}

export interface PreMigrationCheck {
  checkId: string;
  category: string;
  title: string;
  description: string;
  status: "passed" | "warning" | "failed" | "skipped" | "not_applicable" | string;
  severity: string;
  affectedWave: string;
  affectedItem: string;
  evidence: string;
  recommendedAction: string;
  requiredForGoLive: boolean;
}

export interface WaveValidationResult {
  waveId: string;
  waveName: string;
  status: string;
  passedChecks: number;
  warnings: number;
  errors: number;
}

export interface PreMigrationValidationResult {
  validationId: string;
  planId: string;
  generatedAt: string;
  status: string;
  decision: string;
  summary: PreMigrationValidationSummary;
  checks: PreMigrationCheck[];
  waveResults: WaveValidationResult[];
  blockers: string[];
  warnings: string[];
  recommendations: string[];
  exportPaths: Record<string, string>;
}

export interface ExecutionSimulationResponse {
  simulationId: string;
  planId: string;
  status: string;
  estimatedDuration: string;
  estimatedFiles: number;
  estimatedStorage: string;
  simulatedWaves: number;
  expectedFailures: number;
  expectedWarnings: number;
}

export interface ExecutionSimulationStep {
  stepId: string;
  stepName: string;
  order: number;
  description: string;
  estimatedDurationMinutes: number;
  status: string;
  dependencies: string[];
  expectedIssues: string[];
}

export interface ExecutionSimulationWave {
  waveId: string;
  waveName: string;
  order: number;
  itemCount: number;
  estimatedFiles: number;
  estimatedStorageBytes: number;
  estimatedDurationMinutes: number;
  riskLevel: string;
  readinessScore: number;
  expectedWarnings: number;
  expectedFailures: number;
  steps: ExecutionSimulationStep[];
}

export interface ExecutionSimulationIssue {
  issueId: string;
  severity: string;
  waveName: string;
  item: string;
  description: string;
  recommendedAction: string;
}

export interface ExecutionSimulationResult {
  simulationId: string;
  planId: string;
  generatedAt: string;
  status: string;
  estimatedDurationMinutes: number;
  estimatedFiles: number;
  estimatedStorageBytes: number;
  waves: ExecutionSimulationWave[];
  expectedIssues: ExecutionSimulationIssue[];
  checkpoints: string[];
  assumptions: string[];
  recommendations: string[];
}

export interface MigrationExecutionRequest {
  mode: "simulation" | "dry_run" | "live_disabled" | string;
  requireGoDecision: boolean;
  selectedWaveIds: string[];
  createdBy: string;
}

export interface CreateMigrationExecutionJobResponse {
  jobId: string;
  planId: string;
  status: string;
  mode: string;
  message: string;
}

export interface MigrationExecutionSummary {
  progressPercent: number;
  totalWaves: number;
  completedWaves: number;
  totalItems: number;
  completedItems: number;
  failedItems: number;
  skippedItems: number;
  warningCount: number;
  errorCount: number;
}

export interface MigrationExecutionCheckpoint {
  checkpointId: string;
  name: string;
  status: string;
  startedAt?: string | null;
  completedAt?: string | null;
  message: string;
  severity: string;
}

export interface MigrationExecutionTimelineEvent {
  eventId: string;
  createdAt: string;
  eventType: string;
  message: string;
  severity: string;
  waveExecutionId: string;
  itemExecutionId: string;
}

export interface MigrationExecutionError {
  errorId: string;
  createdAt: string;
  severity: string;
  waveExecutionId: string;
  itemExecutionId: string;
  message: string;
  recommendedAction: string;
}

export interface MigrationExecutionItem {
  itemExecutionId: string;
  sourceItemId: string;
  siteCollection: string;
  library: string;
  path: string;
  itemType: string;
  action: string;
  status: "pending" | "running" | "completed" | "skipped" | "failed" | "retry_pending" | string;
  progressPercent: number;
  simulatedSourceUrl: string;
  simulatedTargetUrl: string;
  warnings: string[];
  errors: string[];
  startedAt?: string | null;
  completedAt?: string | null;
}

export interface MigrationExecutionWave {
  waveExecutionId: string;
  sourceWaveId: string;
  waveName: string;
  order: number;
  status: string;
  progressPercent: number;
  totalItems: number;
  completedItems: number;
  failedItems: number;
  skippedItems: number;
  estimatedFiles: number;
  estimatedStorageBytes: number;
  startedAt?: string | null;
  completedAt?: string | null;
  items: MigrationExecutionItem[];
  checkpoints: MigrationExecutionCheckpoint[];
  errors: MigrationExecutionError[];
}

export interface MigrationExecutionJob {
  jobId: string;
  planId: string;
  validationId: string;
  simulationId: string;
  mode: string;
  status: string;
  createdAt: string;
  startedAt?: string | null;
  completedAt?: string | null;
  createdBy: string;
  summary: MigrationExecutionSummary;
  waves: MigrationExecutionWave[];
  checkpoints: MigrationExecutionCheckpoint[];
  timeline: MigrationExecutionTimelineEvent[];
  errors: MigrationExecutionError[];
  warnings: string[];
  reportPaths: Record<string, string>;
}

export interface SharePointMigrationCapabilities {
  canReadSource: boolean;
  canReadTarget: boolean;
  canWriteTarget: boolean;
  canUploadFiles: boolean;
  canCreateFolders: boolean;
  canApplyMetadata: boolean;
  canApplyPermissions: boolean;
}

export interface SharePointMigrationCapabilityCheck {
  checkId: string;
  title: string;
  status: string;
  severity: string;
  message: string;
}

export interface SharePointMigrationCapabilityResult {
  isReady: boolean;
  mode: string;
  checks: SharePointMigrationCapabilityCheck[];
  errors: string[];
  warnings: string[];
  capabilities: SharePointMigrationCapabilities;
}

export interface MigrationTransferPlanItem {
  itemId: string;
  sourcePath: string;
  targetPath: string;
  itemType: string;
  estimatedSizeBytes: number;
  metadataMappingStatus: string;
  permissionMappingStatus: string;
  eligibility: string;
  reason: string;
}

export interface MigrationBlockedItem {
  itemId: string;
  path: string;
  reason: string;
  recommendedAction: string;
}

export interface MigrationTransferPreview {
  previewId: string;
  jobId: string;
  mode: string;
  generatedAt: string;
  totalItems: number;
  eligibleItems: number;
  blockedItems: number;
  metadataMappings: Array<{ sourceField: string; targetField: string; mappingStatus: string; issue: string }>;
  permissionMappings: Array<{ sourcePrincipal: string; targetPrincipal: string; permissionLevel: string; mappingStatus: string; issue: string }>;
  transferPlan: MigrationTransferPlanItem[];
  blocked: MigrationBlockedItem[];
  warnings: string[];
  errors: string[];
}

export interface LivePilotMigrationRequest {
  mode: string;
  confirmationText: string;
  selectedWaveId: string;
  selectedLibrary: string;
  maxFiles: number;
  sourceSiteUrl: string;
  targetSiteUrl: string;
  targetLibrary: string;
  preserveMetadata: boolean;
  preservePermissions: boolean;
  overwriteExisting: boolean;
}

export interface LivePilotSafetyCheck {
  checkId: string;
  title: string;
  status: string;
  severity: string;
  message: string;
}

export interface LivePilotMigrationResult {
  pilotRunId: string;
  jobId: string;
  status: string;
  mode: string;
  message: string;
  generatedAt: string;
  filesAttempted: number;
  filesCopied: number;
  filesSkipped: number;
  safetyChecks: LivePilotSafetyCheck[];
  items: Array<{ itemId: string; sourcePath: string; targetPath: string; status: string; message: string }>;
  warnings: string[];
  errors: string[];
}

export interface WorkflowValidationRequest {
  source: string;
  useSampleFallback: boolean;
  createdBy: string;
  includeExecutionSimulation: boolean;
  includeTransferPreview: boolean;
}

export interface WorkflowValidationSummary {
  scanId: string;
  assessmentId: string;
  planId: string;
  validationId: string;
  simulationId: string;
  executionJobId: string;
  previewId: string;
}

export interface WorkflowValidationResponse {
  workflowRunId: string;
  status: string;
  overallResult: string;
  stepsPassed: number;
  stepsFailed: number;
  stepsWarning: number;
  summary: WorkflowValidationSummary;
}

export interface WorkflowValidationStep {
  stepId: string;
  order: number;
  name: string;
  description: string;
  status: string;
  startedAt?: string | null;
  completedAt?: string | null;
  durationMs: number;
  relatedArtifactId: string;
  warnings: string[];
  errors: string[];
  notes: string[];
}

export interface WorkflowValidationArtifact {
  artifactId: string;
  artifactType: string;
  displayName: string;
  status: string;
  location: string;
}

export interface WorkflowValidationIssue {
  issueId: string;
  severity: string;
  stepName: string;
  message: string;
  recommendedAction: string;
}

export interface WorkflowValidationRun {
  workflowRunId: string;
  startedAt: string;
  completedAt?: string | null;
  status: string;
  overallResult: string;
  source: string;
  createdBy: string;
  steps: WorkflowValidationStep[];
  artifacts: WorkflowValidationArtifact[];
  issues: WorkflowValidationIssue[];
  summary: WorkflowValidationSummary;
  reportPaths: Record<string, string>;
}

export interface DemoStatus {
  demoMode: boolean;
  seeded: boolean;
  latestScanId: string;
  latestAssessmentId: string;
  latestPlanId: string;
  latestExecutionJobId: string;
  latestPreviewId: string;
  latestWorkflowRunId: string;
  lastDemoChainResult: string;
  warnings: string[];
}

export interface ValidationSummary {
  siteCollections: number;
  subsites: number;
  libraries: number;
  lists: number;
  metadataFields: number;
  permissionGroups: number;
  edgeCases: number;
}

export interface ConfigValidationResponse {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  summary: ValidationSummary;
  source?: "backend" | "mock";
}

export interface SaveConfigResponse {
  configId: string;
  message: string;
  savedAt: string;
  source?: "backend" | "mock";
}

export interface GeneratedPackageResult {
  packageId: string;
  message: string;
  files: string[];
  downloadUrl: string;
  generatedAt?: string;
  summary?: ValidationSummary;
  source?: "backend" | "mock";
}

export interface PackageManifest {
  packageId: string;
  generatedAt: string;
  files: string[];
  summary: ValidationSummary;
  source?: "backend" | "mock";
}

export interface BackendApiError {
  status?: number;
  message: string;
  details?: unknown;
}
