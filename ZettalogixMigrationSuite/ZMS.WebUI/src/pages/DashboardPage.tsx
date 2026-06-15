import {
  Activity,
  Bot,
  Database,
  FileText,
  FolderKanban,
  FolderTree,
  Gauge,
  HardDrive,
  ListChecks,
  ShieldAlert,
  Tags
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import DataTable from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import RiskBadge from "../components/RiskBadge";
import StatCard from "../components/StatCard";
import { dashboardStats, recentActivity, riskOverview } from "../data/zmsMockData";
import { zmsApi } from "../services/zmsApi";
import { useZmsState } from "../state/ZmsStateProvider";
import { DashboardStat, DiscoveryScanResult, ExecutionSimulationResult, LivePilotMigrationResult, MigrationExecutionJob, MigrationPlan, MigrationReadinessAssessment, MigrationTransferPreview, PreMigrationValidationResult, RiskItem, RiskLevel, WorkflowValidationRun } from "../types/zms";

const statIcons = [FolderKanban, FolderTree, Database, FileText, HardDrive, ShieldAlert, Tags, ListChecks];

function formatBytes(value: number): string {
  if (value <= 0) return "0 GB";
  return `${(value / 1024 / 1024 / 1024).toFixed(2)} GB`;
}

function riskRank(level: RiskLevel): number {
  return { Low: 1, Medium: 2, High: 3, Critical: 4 }[level];
}

function statsFromDiscovery(result: DiscoveryScanResult): DashboardStat[] {
  return [
    { id: "site-collections", label: "Site Collections", value: result.summary.siteCollections, tone: "primary" },
    { id: "subsites", label: "Subsites", value: result.summary.subsites },
    { id: "libraries", label: "Libraries", value: result.summary.libraries },
    { id: "files", label: "Files", value: result.summary.files.toLocaleString() },
    { id: "storage", label: "Total Storage", value: formatBytes(result.summary.totalStorageBytes) },
    { id: "permission-risks", label: "Permission Risks", value: result.permissionRisks.length, tone: "error" },
    { id: "metadata-issues", label: "Metadata Issues", value: result.summary.missingMetadataIssues, tone: "warning" },
    { id: "readiness", label: "Migration Readiness", value: `${result.summary.readinessScore}%`, tone: "success" }
  ];
}

function risksFromDiscovery(result: DiscoveryScanResult): RiskItem[] {
  return Object.values(
    result.migrationRisks.reduce<Record<string, RiskItem & { severity: RiskLevel }>>((accumulator, risk) => {
      const existing = accumulator[risk.riskType];
      if (!existing) {
        accumulator[risk.riskType] = {
          id: risk.riskType.toLowerCase().replace(/[^a-z0-9]+/g, "-"),
          riskType: risk.riskType,
          count: 1,
          severity: risk.riskLevel,
          affectedArea: risk.site,
          recommendedAction: risk.recommendedAction
        };
        return accumulator;
      }

      existing.count = Number(existing.count) + 1;
      existing.affectedArea = Array.from(new Set(`${existing.affectedArea}, ${risk.site}`.split(/,\s*/))).slice(0, 4).join(", ");
      if (riskRank(risk.riskLevel) > riskRank(existing.severity)) {
        existing.severity = risk.riskLevel;
      }
      return accumulator;
    }, {})
  ).sort((left, right) => riskRank(right.severity) - riskRank(left.severity));
}

export default function DashboardPage(): JSX.Element {
  const state = useZmsState();
  const [latestDiscovery, setLatestDiscovery] = useState<DiscoveryScanResult | null>(state.discovery.result);
  const [latestReadiness, setLatestReadiness] = useState<MigrationReadinessAssessment | null>(null);
  const [latestPlan, setLatestPlan] = useState<MigrationPlan | null>(null);
  const [latestPreValidation, setLatestPreValidation] = useState<PreMigrationValidationResult | null>(null);
  const [latestSimulation, setLatestSimulation] = useState<ExecutionSimulationResult | null>(null);
  const [latestExecutionJob, setLatestExecutionJob] = useState<MigrationExecutionJob | null>(null);
  const [latestPreview, setLatestPreview] = useState<MigrationTransferPreview | null>(null);
  const [latestPilot, setLatestPilot] = useState<LivePilotMigrationResult | null>(null);
  const [latestWorkflow, setLatestWorkflow] = useState<WorkflowValidationRun | null>(null);
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  useEffect(() => {
    if (state.discovery.result) {
      setLatestDiscovery(state.discovery.result);
      return;
    }

    setLatestDiscovery(null);
    setLatestReadiness(null);
    setLatestPlan(null);
    setLatestPreValidation(null);
    setLatestSimulation(null);
    setLatestExecutionJob(null);
    setLatestPreview(null);
    setLatestPilot(null);
    setLatestWorkflow(null);
  }, [state.discovery.result]);

  const activeStats = latestDiscovery ? statsFromDiscovery(latestDiscovery) : dashboardStats;
  const activeRiskOverview = useMemo(() => (latestDiscovery ? risksFromDiscovery(latestDiscovery) : riskOverview), [latestDiscovery]);
  const analyzeLatestDiscovery = async () => {
    if (!latestDiscovery) return;
    setIsAnalyzing(true);
    try {
      const response = await zmsApi.analyzeReadiness(latestDiscovery.scanId);
      const assessment = await zmsApi.getReadinessAssessment(response.assessmentId);
      setLatestReadiness(assessment);
    } finally {
      setIsAnalyzing(false);
    }
  };

  const readinessScore = latestReadiness?.readinessScore ?? latestDiscovery?.summary.readinessScore ?? 78;
  const readinessFraction = Math.max(0, Math.min(1, readinessScore / 100));

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Migration Command Center"
        subtitle="Monitor environments, discovery progress, migration readiness, and risks."
      />

      <section className="rounded-xl border border-primary-muted bg-primary text-white shadow-card">
        <div className="flex flex-col gap-4 p-5 sm:flex-row sm:items-start sm:justify-between">
          <div className="flex gap-4">
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-white/15">
              <Bot className="h-5 w-5" />
            </div>
            <div>
              <h2 className="font-bold">Readiness Insight</h2>
              <p className="mt-1 max-w-3xl text-sm leading-6 text-white/85">
                {latestReadiness
                  ? `${latestReadiness.summary.blockers} blockers, ${latestReadiness.summary.highRisks} high risks, and ${latestReadiness.summary.remediationActions} remediation actions are ready for planning.`
                  : "Analyze the latest completed discovery scan to generate blockers, remediation actions, and migration wave suggestions."}
              </p>
            </div>
          </div>
          <button
            type="button"
            className="rounded-lg bg-white px-4 py-2 text-sm font-bold text-primary hover:bg-primary-soft disabled:opacity-60"
            disabled={!latestDiscovery || isAnalyzing}
            onClick={() => void analyzeLatestDiscovery()}
          >
            {isAnalyzing ? "Analyzing..." : "Analyze Latest Discovery"}
          </button>
        </div>
      </section>

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {activeStats.map((stat: DashboardStat, index) => (
          <StatCard key={stat.id} label={stat.label} value={stat.value} tone={stat.tone} icon={statIcons[index]} />
        ))}
      </section>

      <section className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <article className="rounded-xl border border-border bg-surface p-6 shadow-card">
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-bold text-text-primary">Migration Readiness</h2>
            <Gauge className="h-5 w-5 text-primary" />
          </div>
          <div className="mt-8 flex items-center justify-center">
            <div className="relative h-44 w-44">
              <svg className="h-full w-full -rotate-90" viewBox="0 0 120 120">
                <circle cx="60" cy="60" r="50" fill="none" stroke="#e4e9f2" strokeWidth="12" />
                <circle
                  cx="60"
                  cy="60"
                  r="50"
                  fill="none"
                  stroke="#00488d"
                  strokeDasharray={`${2 * Math.PI * 50}`}
                  strokeDashoffset={`${2 * Math.PI * 50 * (1 - readinessFraction)}`}
                  strokeLinecap="round"
                  strokeWidth="12"
                />
              </svg>
              <div className="absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-4xl font-bold text-primary">{readinessScore}%</span>
                <span className="text-xs font-bold uppercase tracking-wide text-text-muted">{latestReadiness?.riskLevel ?? "Ready"}</span>
              </div>
            </div>
          </div>
          <div className="mt-6 grid grid-cols-2 gap-2 text-center text-sm">
            <div className="rounded-lg bg-surface-container p-3"><strong>{latestReadiness?.summary.blockers ?? 0}</strong><br />Blockers</div>
            <div className="rounded-lg bg-surface-container p-3"><strong>{latestReadiness?.summary.highRisks ?? 0}</strong><br />High Risks</div>
            <div className="rounded-lg bg-surface-container p-3"><strong>{latestReadiness?.summary.remediationActions ?? 0}</strong><br />Actions</div>
            <div className="rounded-lg bg-surface-container p-3"><strong>{latestReadiness?.summary.suggestedWaves ?? 0}</strong><br />Waves</div>
          </div>
        </article>

        <div className="xl:col-span-2">
          <DataTable
            rows={activeRiskOverview}
            getRowKey={(row) => row.id}
            columns={[
              { header: "Risk Type", render: (row) => <span className="font-semibold">{row.riskType}</span> },
              { header: "Count", render: (row) => <span className="font-mono">{row.count}</span> },
              { header: "Severity", render: (row) => <RiskBadge level={row.severity} /> },
              { header: "Affected Area", render: (row) => row.affectedArea },
              { header: "Recommended Action", render: (row) => <span className="font-medium text-primary">{row.recommendedAction}</span> }
            ]}
          />
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">End-to-End Workflow Validation</h2>
            <p className="mt-1 text-sm text-text-muted">
              {latestWorkflow ? `Latest result: ${latestWorkflow.overallResult}` : "No full-chain workflow validation has run."}
            </p>
          </div>
          <a className="inline-flex items-center justify-center rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white" href="/operator">
            Open Operator Control Center
          </a>
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-4">
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestWorkflow?.overallResult ?? "-"}</strong><br />Overall Result</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestWorkflow ? new Date(latestWorkflow.startedAt).toLocaleDateString() : "-"}</strong><br />Last Run</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestWorkflow?.steps.filter((step) => step.status === "passed").length ?? 0}</strong><br />Steps Passed</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestWorkflow?.issues.length ?? 0}</strong><br />Issues</div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
        <h2 className="text-lg font-bold text-text-primary">SharePoint Adapter Readiness</h2>
        <p className="mt-1 text-sm text-text-muted">Adapter Mode: {latestPilot ? "Live Pilot Disabled/Blocked" : "Preview Only"}</p>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-4">
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPreview ? "Generated" : "Not generated"}</strong><br />Transfer Preview</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPreview?.eligibleItems ?? 0}</strong><br />Eligible</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPreview?.blockedItems ?? 0}</strong><br />Blocked</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPilot?.status ?? "Disabled"}</strong><br />Pilot Status</div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">Latest Execution Simulation Job</h2>
            <p className="mt-1 text-sm text-text-muted">
              {latestExecutionJob ? `Simulation job ${latestExecutionJob.status} / ${latestExecutionJob.summary.progressPercent}% complete` : "No execution simulation job has been created."}
            </p>
          </div>
          <a className="inline-flex items-center justify-center rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white" href="/jobs">
            Open Execution Job
          </a>
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-5">
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestExecutionJob?.status ?? "-"}</strong><br />Status</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestExecutionJob?.mode ?? "simulation"}</strong><br />Mode</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestExecutionJob?.summary.completedWaves ?? 0}/{latestExecutionJob?.summary.totalWaves ?? 0}</strong><br />Waves</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestExecutionJob?.summary.warningCount ?? 0}</strong><br />Warnings</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestExecutionJob?.summary.failedItems ?? 0}</strong><br />Failed Items</div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">Latest Migration Plan</h2>
            <p className="mt-1 text-sm text-text-muted">
              {latestPlan ? `${latestPlan.planName} / ${latestPlan.status}` : "No migration plan generated yet."}
            </p>
          </div>
          <a className="inline-flex items-center justify-center rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white" href="/planner">
            Open Migration Plan
          </a>
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-5">
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPlan?.status ?? "-"}</strong><br />Plan Status</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPlan?.waves.length ?? 0}</strong><br />Waves</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPlan?.risks.filter((risk) => risk.migrationBlocker).length ?? 0}</strong><br />Blockers</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPlan ? `${latestPlan.checklist.filter((item) => item.status === "completed").length}/${latestPlan.checklist.length}` : "0/0"}</strong><br />Checklist</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPlan && latestPlan.status === "ready_for_execution" ? "Yes" : "No"}</strong><br />Ready</div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">Pre-Migration Safety Gate</h2>
            <p className="mt-1 text-sm text-text-muted">
              {latestPreValidation ? `Decision: ${latestPreValidation.decision}` : "No pre-migration validation has run."}
            </p>
          </div>
          <a className="inline-flex items-center justify-center rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white" href="/planner">
            Open Pre-Migration Validation
          </a>
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-5">
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPreValidation?.summary.errors ?? 0}</strong><br />Errors</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPreValidation?.summary.warnings ?? 0}</strong><br />Warnings</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestSimulation ? `${Math.floor(latestSimulation.estimatedDurationMinutes / 60)}h ${latestSimulation.estimatedDurationMinutes % 60}m` : "-"}</strong><br />Simulation</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPreValidation?.summary.readyWaves ?? 0}</strong><br />Ready Waves</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{latestPreValidation?.summary.blockedWaves ?? 0}</strong><br />Blocked Waves</div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-6 shadow-card">
        <div className="mb-5 flex items-center gap-2">
          <Activity className="h-5 w-5 text-primary" />
          <h2 className="text-lg font-bold text-text-primary">Recent Activity</h2>
        </div>
        <div className="grid gap-3 md:grid-cols-2">
          {recentActivity.map((item) => (
            <div key={item.id} className="rounded-xl border border-border bg-surface-container p-4">
              <div className="flex items-start justify-between gap-4">
                <h3 className="font-semibold text-text-primary">{item.title}</h3>
                <span className="shrink-0 text-xs font-medium text-text-subtle">{item.time}</span>
              </div>
              <p className="mt-2 text-sm leading-6 text-text-muted">{item.detail}</p>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
