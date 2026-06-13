import { useEffect, useMemo, useState } from "react";
import ConfirmDialog from "../components/ConfirmDialog/ConfirmDialog";
import FormWizard from "../components/FormWizard/FormWizard";
import JobTable from "../components/JobTable/JobTable";
import { useAppStore } from "../hooks/useAppStore";
import { useJobsPolling } from "../hooks/useJobsPolling";
import { api } from "../services/api";

export default function MigrationsPage(): JSX.Element {
  const jobs = useAppStore((state) => state.jobs);
  const connections = useAppStore((state) => state.connections);
  const createJob = useAppStore((state) => state.createJob);
  const startJob = useAppStore((state) => state.startJob);
  const pauseJob = useAppStore((state) => state.pauseJob);
  const resumeJob = useAppStore((state) => state.resumeJob);
  const cancelJob = useAppStore((state) => state.cancelJob);
  const retryJob = useAppStore((state) => state.retryJob);
  const bootstrap = useAppStore((state) => state.bootstrap);
  const loading = useAppStore((state) => state.loading.jobsMutation);

  const [wizardOpen, setWizardOpen] = useState(false);
  const [pauseTarget, setPauseTarget] = useState<string | null>(null);

  useJobsPolling(true);

  const pauseJobName = useMemo(() => jobs.find((job) => job.id === pauseTarget)?.name ?? "", [jobs, pauseTarget]);
  const runningJobs = jobs.filter((job) => job.status === "Running");
  const failedJobs = jobs.filter((job) => job.status === "Failed");
  const pausedJobs = jobs.filter((job) => job.status === "Paused");

  useEffect(() => {
    void bootstrap();
  }, [bootstrap]);

  return (
    <>
      <section className="split-panel">
        <article className="surface-card">
          <span className="eyebrow">Queue Metrics</span>
          <div className="meta-grid">
            <div className="metric-box">
              <span>Total jobs</span>
              <strong>{jobs.length}</strong>
              <p>All migrations currently tracked in the monitor.</p>
            </div>
            <div className="metric-box">
              <span>Running</span>
              <strong>{runningJobs.length}</strong>
              <p>Jobs actively processing source files into SharePoint Online.</p>
            </div>
            <div className="metric-box">
              <span>Paused / failed</span>
              <strong>{pausedJobs.length + failedJobs.length}</strong>
              <p>Jobs requiring operator attention before the next processing wave.</p>
            </div>
          </div>
        </article>

        <article className="tonal-card">
          <div className="section-heading">
            <div>
              <span className="eyebrow">Intervention Panel</span>
              <h2>Operator actions</h2>
              <p>Launch a new migration wave or review jobs that have entered failed state.</p>
            </div>
          </div>
          <div className="page-stack">
            <div className="metric-box">
              <span>Failed jobs</span>
              <strong>{failedJobs.length}</strong>
              <p>Use the detail view to inspect log entries and decide whether to resume, retry, or reconfigure.</p>
            </div>
            <button type="button" className="primary-button" onClick={() => setWizardOpen(true)}>
              <span className="material-symbols-outlined">add</span>
              New migration
            </button>
            <button type="button" className="ghost-button" onClick={() => void api.downloadReport("/jobs.csv")}>
              <span className="material-symbols-outlined">download</span>
              Download all runs
            </button>
          </div>
        </article>
      </section>

      <section className="surface-card">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Job Monitor</span>
            <h2>Migration ledger</h2>
            <p>The queue reflects the current API state and stays in sync through the polling store.</p>
          </div>
        </div>
        <JobTable
          jobs={jobs}
          onStart={(id) => void startJob(id)}
          onPause={(id) => setPauseTarget(id)}
          onResume={(id) => void resumeJob(id)}
          onCancel={(id) => void cancelJob(id)}
          onRetry={(id) => void retryJob(id)}
        />
      </section>

      <FormWizard
        isOpen={wizardOpen}
        connections={connections}
        loading={loading}
        onClose={() => setWizardOpen(false)}
        onSubmit={async (input) => {
          await createJob(input);
        }}
      />

      <ConfirmDialog
        isOpen={Boolean(pauseTarget)}
        title="Pause migration?"
        description={`Pause "${pauseJobName}" so the operator can inspect the current wave before resuming.`}
        confirmLabel="Pause job"
        onClose={() => setPauseTarget(null)}
        onConfirm={() => {
          if (pauseTarget) {
            void pauseJob(pauseTarget);
          }
          setPauseTarget(null);
        }}
      />
    </>
  );
}
