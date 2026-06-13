import { CheckCircle2, PauseCircle, PlayCircle, RefreshCw, RotateCcw, ShieldAlert, Square, XCircle } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import DataTable from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import StatCard from "../components/StatCard";
import StatusBadge from "../components/StatusBadge";
import { zmsApi } from "../services/zmsApi";
import { LivePilotMigrationResult, MigrationExecutionJob, MigrationTransferPreview, SharePointMigrationCapabilityResult } from "../types/zms";

function formatBytes(value: number): string {
  if (value <= 0) return "0 GB";
  return `${(value / 1024 / 1024 / 1024).toFixed(2)} GB`;
}

export default function JobsPage(): JSX.Element {
  const [job, setJob] = useState<MigrationExecutionJob | null>(null);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [capability, setCapability] = useState<SharePointMigrationCapabilityResult | null>(null);
  const [preview, setPreview] = useState<MigrationTransferPreview | null>(null);
  const [pilot, setPilot] = useState<LivePilotMigrationResult | null>(null);

  const loadLatest = async () => {
    setJob(await zmsApi.getLatestMigrationExecutionJob());
  };

  useEffect(() => {
    void loadLatest();
  }, []);

  const createJob = async () => {
    setBusy(true);
    setMessage("");
    try {
      const plan = await zmsApi.getLatestMigrationPlan();
      if (!plan) {
        setMessage("Create a migration plan first.");
        return;
      }
      const response = await zmsApi.createMigrationExecutionJobFromPlan(plan.planId, { mode: "simulation", requireGoDecision: false, createdBy: "Migration Lead" });
      setMessage(response.message);
      setJob(await zmsApi.getMigrationExecutionJob(response.jobId));
    } finally {
      setBusy(false);
    }
  };

  const mutate = async (action: "start" | "pause" | "resume" | "cancel" | "retry") => {
    if (!job) return;
    setBusy(true);
    try {
      const updated = action === "start"
        ? await zmsApi.startMigrationExecutionJob(job.jobId)
        : action === "pause"
          ? await zmsApi.pauseMigrationExecutionJob(job.jobId)
          : action === "resume"
            ? await zmsApi.resumeMigrationExecutionJob(job.jobId)
            : action === "cancel"
              ? await zmsApi.cancelMigrationExecutionJob(job.jobId)
              : await zmsApi.retryFailedMigrationExecutionJob(job.jobId);
      if (updated) setJob(updated);
    } finally {
      setBusy(false);
    }
  };

  const canStart = job?.status === "created" || job?.status === "queued";
  const canPause = job?.status === "running";
  const canResume = job?.status === "paused";
  const canCancel = job?.status === "running" || job?.status === "paused" || job?.status === "created";
  const canRetry = Boolean(job?.summary.failedItems);
  const latestEvents = useMemo(() => [...(job?.timeline ?? [])].sort((a, b) => a.createdAt.localeCompare(b.createdAt)).slice(-12).reverse(), [job]);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Execution Command Center"
        subtitle="Create and control simulated migration execution jobs. Real SharePoint migration is not enabled."
        actions={
          <button className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white disabled:opacity-60" disabled={busy} onClick={() => void createJob()}>
            <PlayCircle className="h-4 w-4" />
            Create Simulation Job
          </button>
        }
      />

      <section className="rounded-xl border border-warning bg-warning/10 p-5 text-sm text-text-primary">
        <div className="flex items-start gap-3">
          <ShieldAlert className="mt-0.5 h-5 w-5 text-warning" />
          <div>
            <h2 className="font-bold">Simulation Mode Only</h2>
            <p className="mt-1 text-text-muted">No SharePoint tenant changes are performed. No files are copied, uploaded, deleted, or modified.</p>
          </div>
        </div>
      </section>

      {message ? <p className="rounded-lg border border-border bg-surface p-3 text-sm text-text-muted">{message}</p> : null}

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Status" value={job?.status ?? "No job"} icon={RefreshCw} tone={job?.status === "failed" ? "error" : "primary"} />
        <StatCard label="Progress" value={job ? `${job.summary.progressPercent}%` : "0%"} icon={RefreshCw} />
        <StatCard label="Completed Items" value={job?.summary.completedItems ?? 0} icon={CheckCircle2} tone="success" />
        <StatCard label="Failed Items" value={job?.summary.failedItems ?? 0} icon={XCircle} tone={job?.summary.failedItems ? "error" : "default"} />
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">Execution Job Summary</h2>
            <p className="mt-1 text-sm text-text-muted">
              {job ? `Job ${job.jobId} / mode ${job.mode} / plan ${job.planId}` : "Create a simulation job from the latest migration plan."}
            </p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button disabled={!canStart || busy} className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={() => void mutate("start")}><PlayCircle className="h-4 w-4" />Start</button>
            <button disabled={!canPause || busy} className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={() => void mutate("pause")}><PauseCircle className="h-4 w-4" />Pause</button>
            <button disabled={!canResume || busy} className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={() => void mutate("resume")}><PlayCircle className="h-4 w-4" />Resume</button>
            <button disabled={!canCancel || busy} className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={() => void mutate("cancel")}><Square className="h-4 w-4" />Cancel</button>
            <button disabled={!canRetry || busy} className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={() => void mutate("retry")}><RotateCcw className="h-4 w-4" />Retry Failed</button>
          </div>
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-6">
          <div className="rounded-lg bg-surface-container p-3"><strong>{job?.summary.totalWaves ?? 0}</strong><br />Waves</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{job?.summary.completedWaves ?? 0}</strong><br />Completed Waves</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{job?.summary.totalItems ?? 0}</strong><br />Items</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{job?.summary.skippedItems ?? 0}</strong><br />Skipped</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{job?.summary.warningCount ?? 0}</strong><br />Warnings</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{job ? new Date(job.createdAt).toLocaleString() : "-"}</strong><br />Created</div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">SharePoint Migration Adapter Preview</h2>
            <p className="mt-1 text-sm text-text-muted">Live pilot migration is disabled by default. This panel validates capabilities and transfer planning only.</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <button className="rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={async () => setCapability(await zmsApi.validateSharePointMigrationCapabilities())}>Validate Capabilities</button>
            <button disabled={!job} className="rounded-lg border border-border px-3 py-2 text-sm font-bold disabled:opacity-50" onClick={async () => job && setPreview(await zmsApi.generateSharePointTransferPreview(job.jobId))}>Generate Transfer Preview</button>
            <button disabled={!job} className="rounded-lg border border-error px-3 py-2 text-sm font-bold text-error opacity-60 disabled:opacity-40" onClick={async () => job && setPilot(await zmsApi.runLockedLivePilot(job.jobId, { selectedWaveId: job.waves[0]?.sourceWaveId ?? "" }))}>Run Locked Pilot Migration</button>
          </div>
        </div>
        <div className="mt-4 grid grid-cols-2 gap-3 text-sm md:grid-cols-4">
          <div className="rounded-lg bg-surface-container p-3"><strong>{capability ? (capability.isReady ? "Ready" : "Blocked") : "-"}</strong><br />Capability Status</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{preview?.eligibleItems ?? 0}</strong><br />Eligible Items</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{preview?.blockedItems ?? 0}</strong><br />Blocked Items</div>
          <div className="rounded-lg bg-surface-container p-3"><strong>{pilot?.status ?? "Disabled"}</strong><br />Pilot Status</div>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <h2 className="text-lg font-bold text-text-primary">Wave Execution Board</h2>
        <div className="mt-4 grid gap-4 xl:grid-cols-2">
          {(job?.waves ?? []).map((wave) => (
            <article key={wave.waveExecutionId} className="rounded-lg border border-border bg-surface-container p-4">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Order {wave.order}</p>
                  <h3 className="mt-1 font-bold text-text-primary">{wave.waveName}</h3>
                </div>
                <StatusBadge status={wave.status} />
              </div>
              <div className="mt-4 h-2 overflow-hidden rounded-full bg-surface-container-high">
                <div className="h-full rounded-full bg-primary" style={{ width: `${wave.progressPercent}%` }} />
              </div>
              <div className="mt-4 grid grid-cols-4 gap-2 text-xs">
                <div><strong>{wave.totalItems}</strong><br />Items</div>
                <div><strong>{wave.completedItems}</strong><br />Done</div>
                <div><strong>{wave.failedItems}</strong><br />Failed</div>
                <div><strong>{wave.skippedItems}</strong><br />Skipped</div>
              </div>
              <p className="mt-3 text-xs text-text-subtle">{wave.estimatedFiles.toLocaleString()} files / {formatBytes(wave.estimatedStorageBytes)}</p>
            </article>
          ))}
          {!job ? <p className="rounded-lg bg-surface-container p-4 text-sm text-text-muted">No simulation execution job exists yet.</p> : null}
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <h2 className="text-lg font-bold text-text-primary">Execution Timeline</h2>
        <div className="mt-4">
          <DataTable
            rows={latestEvents}
            getRowKey={(row) => row.eventId}
            emptyMessage="Start a simulation job to generate execution timeline events."
            columns={[
              { header: "Time", render: (row) => new Date(row.createdAt).toLocaleString() },
              { header: "Event", render: (row) => <span className="font-semibold">{row.eventType}</span> },
              { header: "Severity", render: (row) => row.severity },
              { header: "Message", render: (row) => row.message }
            ]}
          />
        </div>
      </section>
    </div>
  );
}
