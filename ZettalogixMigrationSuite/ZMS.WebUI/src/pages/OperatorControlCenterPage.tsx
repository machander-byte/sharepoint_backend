import { Download, Map, PlayCircle, RotateCcw, ShieldCheck } from "lucide-react";
import { useEffect, useState } from "react";
import DataTable from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import StatCard from "../components/StatCard";
import { zmsApi } from "../services/zmsApi";
import { DemoStatus, WorkflowValidationRun } from "../types/zms";

export default function OperatorControlCenterPage(): JSX.Element {
  const [run, setRun] = useState<WorkflowValidationRun | null>(null);
  const [demoStatus, setDemoStatus] = useState<DemoStatus | null>(null);
  const [showWalkthrough, setShowWalkthrough] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    let cancelled = false;
    zmsApi.getLatestWorkflowValidation().then((result) => {
      if (!cancelled) setRun(result);
    });
    zmsApi.getDemoStatus().then((result) => {
      if (!cancelled) setDemoStatus(result);
    });
    return () => {
      cancelled = true;
    };
  }, []);

  const runFullChain = async () => {
    setBusy(true);
    try {
      const response = await zmsApi.runFullWorkflowValidation();
      setRun(await zmsApi.getWorkflowValidationRun(response.workflowRunId));
    } finally {
      setBusy(false);
    }
  };

  const demoAction = async (action: "reset" | "seed" | "chain") => {
    setBusy(true);
    try {
      const status = action === "reset"
        ? await zmsApi.resetDemoData()
        : action === "seed"
          ? await zmsApi.seedDemoData()
          : await zmsApi.runDemoScriptedChain();
      setDemoStatus(status);
      setRun(await zmsApi.getLatestWorkflowValidation());
    } finally {
      setBusy(false);
    }
  };

  const passed = run?.steps.filter((step) => step.status === "passed").length ?? 0;
  const warnings = run?.steps.filter((step) => step.status === "warning").length ?? 0;
  const failures = run?.steps.filter((step) => step.status === "failed").length ?? 0;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Operator Control Center"
        subtitle="Run and validate the complete ZMS migration preparation workflow."
        actions={
          <button className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white disabled:opacity-60" disabled={busy} onClick={() => void runFullChain()}>
            <PlayCircle className="h-4 w-4" />
            {busy ? "Running..." : "Run Full Workflow Validation"}
          </button>
        }
      />

      <section className="rounded-xl border border-warning bg-warning/10 p-5 text-sm">
        <div className="flex items-start gap-3">
          <ShieldCheck className="mt-0.5 h-5 w-5 text-warning" />
          <div>
            <h2 className="font-bold text-text-primary">Safe validation only</h2>
            <p className="mt-1 text-text-muted">This workflow validation does not perform real SharePoint migration or tenant-changing operations.</p>
          </div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">Demo Control Panel</h2>
            <p className="mt-1 text-sm text-text-muted">Prepare a clean, safe recording flow with sample data. No tenant changes are performed.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold" disabled={busy} onClick={() => void demoAction("reset")}><RotateCcw className="h-4 w-4" />Reset Demo Data</button>
            <button className="rounded-lg border border-border px-3 py-2 text-sm font-bold" disabled={busy} onClick={() => void demoAction("seed")}>Seed Demo Data</button>
            <button className="rounded-lg bg-primary px-3 py-2 text-sm font-bold text-white" disabled={busy} onClick={() => void demoAction("chain")}>Run Full Demo Chain</button>
            <button className="inline-flex items-center gap-2 rounded-lg border border-primary px-3 py-2 text-sm font-bold text-primary" onClick={() => setShowWalkthrough(true)}><Map className="h-4 w-4" />Start Demo Walkthrough</button>
          </div>
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-4">
          <div className="rounded-lg bg-surface-container p-3"><strong>{demoStatus?.demoMode ? "On" : "Off"}</strong><br />Demo Mode</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{demoStatus?.seeded ? "Seeded" : "Not seeded"}</strong><br />Demo Data</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{demoStatus?.lastDemoChainResult || "-"}</strong><br />Last Chain</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{demoStatus?.latestWorkflowRunId ? "Ready" : "-"}</strong><br />Workflow</div>
        </div>
        <div className="mt-4 grid grid-cols-1 gap-2 text-xs md:grid-cols-2">
          {["latestScanId", "latestAssessmentId", "latestPlanId", "latestExecutionJobId", "latestPreviewId", "latestWorkflowRunId"].map((key) => (
            <div key={key} className="rounded-lg bg-surface-container p-2">
              <strong>{key}</strong>
              <p className="mt-1 break-all text-text-muted">{String((demoStatus as unknown as Record<string, string> | null)?.[key] ?? "-")}</p>
            </div>
          ))}
        </div>
        <div className="mt-4 flex flex-wrap gap-2 text-sm">
          {[
            ["/dashboard", "Open Dashboard"],
            ["/discovery", "Open Discovery"],
            ["/planner", "Open Planner"],
            ["/jobs", "Open Jobs"],
            ["/reports", "Open Reports"]
          ].map(([href, label]) => <a key={href} className="rounded-lg border border-border px-3 py-2 font-bold" href={href}>{label}</a>)}
        </div>
      </section>

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <StatCard label="Overall Result" value={run?.overallResult ?? "Not run"} tone={run?.overallResult === "fail" ? "error" : run?.overallResult === "pass_with_warnings" ? "warning" : "success"} />
        <StatCard label="Steps Passed" value={passed} />
        <StatCard label="Warnings" value={warnings} tone={warnings ? "warning" : "default"} />
        <StatCard label="Failures" value={failures} tone={failures ? "error" : "default"} />
        <StatCard label="Artifacts" value={run?.artifacts.length ?? 0} />
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">Workflow Status</h2>
            <p className="mt-1 text-sm text-text-muted">
              {run ? `Last run ${new Date(run.startedAt).toLocaleString()} by ${run.createdBy}` : "Run the full chain to generate workflow status."}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button disabled={!run} className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={() => run && void zmsApi.downloadWorkflowValidationExport(run.workflowRunId, "json")}>
              <Download className="h-4 w-4" />
              Export JSON
            </button>
            <button disabled={!run} className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={() => run && void zmsApi.downloadWorkflowValidationExport(run.workflowRunId, "markdown")}>
              <Download className="h-4 w-4" />
              Export Markdown
            </button>
          </div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <h2 className="text-lg font-bold text-text-primary">Step Timeline</h2>
        <div className="mt-4">
          <DataTable
            rows={run?.steps ?? []}
            getRowKey={(row) => row.stepId}
            emptyMessage="Run full workflow validation to populate the timeline."
            columns={[
              { header: "Step", render: (row) => <span className="font-semibold">{row.order}. {row.name}</span> },
              { header: "Status", render: (row) => row.status },
              { header: "Duration", render: (row) => `${row.durationMs} ms` },
              { header: "Artifact", render: (row) => row.relatedArtifactId || "-" },
              { header: "Warnings/Errors", render: (row) => [...row.warnings, ...row.errors].slice(0, 2).join("; ") || "-" }
            ]}
          />
        </div>
      </section>

      <section className="grid grid-cols-1 gap-4 xl:grid-cols-2">
        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <h2 className="text-lg font-bold text-text-primary">Artifact Summary</h2>
          <div className="mt-4 grid grid-cols-1 gap-2 text-sm">
            {Object.entries(run?.summary ?? {}).map(([key, value]) => (
              <div key={key} className="rounded-lg bg-surface-container p-3">
                <strong>{key}</strong>
                <p className="mt-1 break-all text-text-muted">{value || "-"}</p>
              </div>
            ))}
          </div>
        </article>

        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <h2 className="text-lg font-bold text-text-primary">Issues</h2>
          <div className="mt-4 grid gap-3">
            {(run?.issues ?? []).slice(0, 10).map((issue) => (
              <div key={issue.issueId} className="rounded-lg bg-surface-container p-3 text-sm">
                <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{issue.severity} / {issue.stepName}</p>
                <p className="mt-1 font-semibold text-text-primary">{issue.message}</p>
                <p className="mt-1 text-text-muted">{issue.recommendedAction}</p>
              </div>
            ))}
            {run && run.issues.length === 0 ? <p className="text-sm text-text-muted">No workflow issues were recorded.</p> : null}
          </div>
        </article>
      </section>

      {showWalkthrough ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/60 p-4">
          <div className="max-h-[90vh] w-full max-w-4xl overflow-auto rounded-xl bg-surface p-6 shadow-panel">
            <div className="flex items-start justify-between gap-4">
              <div>
                <h2 className="text-xl font-bold text-text-primary">Start Demo Walkthrough</h2>
                <p className="mt-1 text-sm text-text-muted">Use this path while recording the submission video.</p>
              </div>
              <button className="rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => setShowWalkthrough(false)}>Close</button>
            </div>
            <div className="mt-5 grid gap-3">
              {[
                ["Dashboard", "Explain ZMS as an enterprise migration control plane.", "Open dashboard", "Cards show 5 sites, 25 libraries, 1,250 files, and readiness status."],
                ["Environment Builder", "Show safe package generation for a SharePoint test environment.", "Preview structure and generate package", "Package is generated without browser-side tenant changes."],
                ["Discovery", "Show read-only discovery/import and risk inventory.", "Open Discovery", "Inventory, permissions, metadata, and risks are visible."],
                ["Migration Planner", "Show waves, checklist, and planning runbook.", "Create/validate plan", "Four migration waves and prerequisites are displayed."],
                ["Pre-Migration Validation", "Explain Go/No-Go safety checks.", "Run validation", "Warnings are shown without running migration."],
                ["Execution Simulation", "Show simulation estimates and checkpoints.", "Run simulation", "Duration, issues, and wave timeline appear."],
                ["Jobs", "Show simulation job lifecycle.", "Create/start simulation job", "Timeline and wave progress update."],
                ["Transfer Preview", "Explain locked adapter foundation.", "Generate transfer preview", "Eligible and blocked items are shown; live migration disabled."],
                ["Operator", "Validate the complete chain.", "Run Full Workflow Validation", "Timeline, artifacts, and issues are generated."],
                ["Reports", "Show downloadable evidence.", "Export reports", "Demo export/report payload downloads."]
              ].map(([page, say, click, expected], index) => (
                <div key={page} className="rounded-lg border border-border bg-surface-container p-4 text-sm">
                  <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Step {index + 1}</p>
                  <h3 className="mt-1 font-bold text-text-primary">{page}</h3>
                  <p className="mt-2"><strong>Say:</strong> {say}</p>
                  <p className="mt-1"><strong>Click:</strong> {click}</p>
                  <p className="mt-1"><strong>Expected:</strong> {expected}</p>
                </div>
              ))}
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
