export type V2PageId =
  | "command-center"
  | "tutorial"
  | "sources"
  | "destinations"
  | "assess"
  | "plan"
  | "migrate"
  | "monitor"
  | "validate"
  | "reports"
  | "ai-advisor"
  | "governance"
  | "settings";

export interface V2PageDefinition {
  id: V2PageId;
  label: string;
  group: "Operate" | "Prepare" | "Assure";
}

export const v2Pages: V2PageDefinition[] = [
  { id: "command-center", label: "Command Center", group: "Operate" },
  { id: "tutorial", label: "Tutorial", group: "Operate" },
  { id: "sources", label: "Sources", group: "Operate" },
  { id: "destinations", label: "Destinations", group: "Operate" },
  { id: "assess", label: "Assess", group: "Prepare" },
  { id: "plan", label: "Plan", group: "Prepare" },
  { id: "migrate", label: "Migrate", group: "Prepare" },
  { id: "monitor", label: "Monitor", group: "Prepare" },
  { id: "validate", label: "Validate", group: "Assure" },
  { id: "reports", label: "Reports", group: "Assure" },
  { id: "ai-advisor", label: "AI Advisor", group: "Assure" },
  { id: "governance", label: "Governance", group: "Assure" },
  { id: "settings", label: "Settings", group: "Assure" }
];

export const migrationEvidence = {
  source: "Google Drive",
  target: "SharePoint Online",
  stage0: {
    name: "Stage 0",
    files: "22/22",
    failed: 0,
    retries: 0,
    result: "Passed"
  },
  stage1: {
    name: "Stage 1",
    files: "231/231",
    failed: 0,
    retries: 0,
    sourceBytes: 2_589_962,
    graphVerifiedBytes: 2_589_962,
    validation: "Passed",
    result: "Passed"
  },
  backendTests: "46/46 passed",
  frontendBuild: "Passed",
  queue: "Empty",
  supabase: "Connected during live validation",
  limitation:
    "File migration integrity passed. Empty-folder preservation is implemented and test-covered; live certification is pending backend redeploy."
};

export const commandMetrics = [
  { label: "Stage 1 files", value: "231/231", status: "Passed" },
  { label: "Failed files", value: "0", status: "Passed" },
  { label: "Retries", value: "0", status: "Passed" },
  { label: "Graph bytes", value: "2,589,962", status: "Verified" }
];

export const stageRows = [
  {
    stage: "Stage 0",
    scope: "Google Drive -> SharePoint",
    files: "22/22",
    failures: "0",
    retries: "0",
    verification: "Source and target bytes matched",
    status: "Passed"
  },
  {
    stage: "Stage 1",
    scope: "Google Drive -> SharePoint",
    files: "231/231",
    failures: "0",
    retries: "0",
    verification: "Microsoft Graph byte verification passed",
    status: "Passed"
  },
  {
    stage: "Stage 2",
    scope: "1,000-file non-production run",
    files: "Pending",
    failures: "Pending",
    retries: "Pending",
    verification: "Fresh target and Graph verification required",
    status: "Next"
  }
];

export const riskSummary = [
  {
    name: "Permission cleanup",
    level: "Needs review",
    detail: "Oversharing and unique-permission areas should be reviewed before larger waves."
  },
  {
    name: "Metadata readiness",
    level: "Needs review",
    detail: "Missing required values and mapping risks should be remediated before production use."
  },
  {
    name: "Long path handling",
    level: "Known risk",
    detail: "Path shortening recommendations are available; live edge-case validation remains pending."
  },
  {
    name: "Empty folders",
    level: "Known gap",
    detail: "Current live proof verifies files and file-bearing paths, not first-class empty folder preservation."
  }
];

export const reportExports = [
  "Discovery Inventory CSV",
  "Permission Risk CSV",
  "Migration Risk CSV",
  "Readiness Report",
  "Migration Plan CSV",
  "Migration Runbook Markdown",
  "Go/No-Go Validation Report",
  "Execution Job Report",
  "Transfer Preview Report",
  "Live Migration Validation Report",
  "AI Feature Test Report",
  "Security Checklist"
];

export const aiCapabilities = [
  { name: "Permission risk recommendation", classification: "Rule-based / fallback ready", status: "Implemented foundation" },
  { name: "Metadata standardization", classification: "Rule-based / fallback ready", status: "Implemented foundation" },
  { name: "Long path remediation", classification: "Rule-based / fallback ready", status: "Implemented foundation" },
  { name: "Migration wave suggestion", classification: "Rule-based", status: "Covered by planner tests" },
  { name: "ETA estimate", classification: "Rule-based unless AI backend available", status: "Fallback available" },
  { name: "Ollama-backed advisor", classification: "Real AI when backend is available", status: "Not claimed in V2 without runtime proof" }
];

export const internalSafetyLimits = [
  "Live pilot disabled unless ZMS_ENABLE_LIVE_MIGRATION=true.",
  "Default live pilot cap is 10 files unless ZMS_LIVE_PILOT_MAX_FILES is configured.",
  "Confirmation text must exactly match ENABLE LIVE PILOT MIGRATION.",
  "Large-scale Stage 2 and Stage 3 runs require fresh targets and Graph verification.",
  "Commercial plan controls are intentionally out of scope; use internal safety limits only."
];
