import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ConfirmDialog from "../components/ConfirmDialog/ConfirmDialog";
import EmptyState from "../components/EmptyState/EmptyState";
import ProgressBar from "../components/ProgressBar/ProgressBar";
import { useAppStore } from "../hooks/useAppStore";
import { useJobsPolling } from "../hooks/useJobsPolling";
import { api } from "../services/api";
import { getErrorGuidance } from "../utils/errorHelp";
import { formatDate, formatJobStatus } from "../utils/formatters";

export default function MigrationDetailPage(): JSX.Element {
  const { id } = useParams();
  const navigate = useNavigate();
  const jobs = useAppStore((state) => state.jobs);
  const startJob = useAppStore((state) => state.startJob);
  const pauseJob = useAppStore((state) => state.pauseJob);
  const resumeJob = useAppStore((state) => state.resumeJob);
  const cancelJob = useAppStore((state) => state.cancelJob);
  const retryJob = useAppStore((state) => state.retryJob);
  const [confirmOpen, setConfirmOpen] = useState(false);

  useJobsPolling(true);

  const job = useMemo(() => jobs.find((item) => item.id === id), [id, jobs]);

  if (!job) {
    return (
      <EmptyState
        title="Migration not found"
        description="The selected migration does not exist in the current dataset."
        action={
          <button type="button" className="primary-button" onClick={() => navigate("/migrations")}>
            Back to monitor
          </button>
        }
      />
    );
  }

  return (
    <>
      <section className="page-stack">
        <article className="surface-card">
          <div className="section-heading">
            <div>
              <span className="eyebrow">Active Job</span>
              <h2>{job.name}</h2>
              <p>The detail surface combines live progress, recent activity, and operator controls from the exported design.</p>
            </div>
            <div className="action-group">
              <button type="button" className="primary-button" onClick={() => void startJob(job.id)} disabled={job.status === "Completed"}>
                Start
              </button>
              <button type="button" className="ghost-button" onClick={() => setConfirmOpen(true)} disabled={job.status !== "Running"}>
                Pause
              </button>
              <button type="button" className="ghost-button" onClick={() => void resumeJob(job.id)} disabled={job.status !== "Paused"}>
                Resume
              </button>
              <button type="button" className="ghost-button" onClick={() => void retryJob(job.id)} disabled={!["Failed", "CompletedWithErrors"].includes(job.status)}>
                Retry
              </button>
              <button type="button" className="ghost-button" onClick={() => void cancelJob(job.id)} disabled={["Completed", "CompletedWithErrors", "Failed"].includes(job.status)}>
                Cancel
              </button>
              <button type="button" className="ghost-button" onClick={() => void api.downloadReport(`/jobs/${job.id}/summary.csv`)}>
                Summary CSV
              </button>
              <button type="button" className="ghost-button" onClick={() => void api.downloadReport(`/jobs/${job.id}/items.csv`)}>
                Items CSV
              </button>
              <button type="button" className="ghost-button" onClick={() => void api.downloadReport(`/jobs/${job.id}/logs.csv`)}>
                Logs CSV
              </button>
            </div>
          </div>

          <div className="page-stack">
            <div className="metric-box">
              <span>Pipeline progress</span>
              <strong>{job.progress}%</strong>
              <p>
                {job.migratedFiles} of {job.totalFiles} files migrated with {job.failedFiles} failures recorded.
              </p>
              <div style={{ marginTop: 14 }}>
                <ProgressBar value={job.progress} />
              </div>
            </div>
            <div className="detail-list">
            <div>
              <span>Status</span>
              <strong>{formatJobStatus(job.status)} / {job.enterpriseState}</strong>
            </div>
              <div>
                <span>Retries</span>
                <strong>{job.retryCount}</strong>
              </div>
              <div>
                <span>Correlation</span>
                <strong>{job.correlationId ?? "Not assigned"}</strong>
              </div>
              <div>
                <span>Started</span>
                <strong>{formatDate(job.startedAt)}</strong>
              </div>
              <div>
                <span>Source</span>
                <strong>{job.sourceLibraryName ? `${job.sourcePath} / ${job.sourceLibraryName}` : job.sourcePath}</strong>
              </div>
              <div>
                <span>Destination</span>
                <strong>
                  {job.targetSite} / {job.targetLibrary}
                  {job.targetRootPath ? ` / ${job.targetRootPath}` : ""}
                </strong>
              </div>
            </div>
          </div>
        </article>

        {job.lastError ? (
          <article className="error-panel">
            <div>
              <span className="eyebrow">Failure Analysis</span>
              <h2>{getErrorGuidance(job.lastError)?.title ?? "Migration error requires review"}</h2>
              <p>{job.lastError}</p>
            </div>
            {getErrorGuidance(job.lastError) ? (
              <div className="guidance-list">
                <strong>{getErrorGuidance(job.lastError)?.summary}</strong>
                <ul>
                  {getErrorGuidance(job.lastError)?.checks.map((check) => <li key={check}>{check}</li>)}
                </ul>
                {getErrorGuidance(job.lastError)?.docs ? (
                  <a
                    className="external-link"
                    href={getErrorGuidance(job.lastError)?.docs?.href}
                    target="_blank"
                    rel="noreferrer"
                  >
                    <span className="material-symbols-outlined">open_in_new</span>
                    {getErrorGuidance(job.lastError)?.docs?.label}
                  </a>
                ) : null}
              </div>
            ) : null}
          </article>
        ) : null}

        <section className="detail-grid">
          <article className="surface-card">
            <div className="section-heading">
              <div>
                <span className="eyebrow">Recent Operations</span>
                <h2>Execution log</h2>
                <p>Event history is loaded from the reporting API and refreshed through the polling hook.</p>
              </div>
            </div>

            <div className="timeline">
              {job.history.length === 0 ? (
                <div className="timeline-item">
                  <strong>No log entries yet.</strong>
                  <span>The reporting API will show activity after the job starts writing logs.</span>
                </div>
              ) : (
                job.history.slice(0, 8).map((event) => {
                  const guidance = getErrorGuidance(`${event.message} ${event.details ?? ""}`);

                  return (
                    <div key={event.id} className={`timeline-item ${event.level}`}>
                      <strong>{event.message}</strong>
                      <span>{formatDate(event.timestamp)}</span>
                      {event.details ? <p className="timeline-detail">{event.details}</p> : null}
                      {guidance && event.level !== "info" ? (
                        <div className="timeline-guidance">
                          <strong>{guidance.title}</strong>
                          <p>{guidance.summary}</p>
                          <ul>
                            {guidance.checks.slice(0, 3).map((check) => <li key={check}>{check}</li>)}
                          </ul>
                        </div>
                      ) : null}
                    </div>
                  );
                })
              )}
            </div>
          </article>

          <article className="surface-card">
            <div className="section-heading">
              <div>
                <span className="eyebrow">Throughput</span>
                <h2>Execution profile</h2>
              </div>
            </div>

            <div className="page-stack">
              <div className="metric-box">
                <span>Completed batches</span>
                <strong>{Math.max(1, Math.round(job.migratedFiles / 80))}</strong>
                <p>Approximate processed batches based on persisted completed item counts.</p>
              </div>
              <div className="metric-box">
                <span>Metadata policy</span>
                <strong>{job.preserveMetadata ? "Preserved" : "Standard copy"}</strong>
                <p>The job keeps this flag alongside the source and target mapping throughout execution.</p>
              </div>
              <div className="metric-box">
                <span>Created</span>
                <strong>{formatDate(job.createdAt)}</strong>
                <p>The migration blueprint was created through the API and remains visible in the dashboard.</p>
              </div>
            </div>
          </article>
        </section>
      </section>

      <ConfirmDialog
        isOpen={confirmOpen}
        title="Pause current job?"
        description="Pause the migration to inspect current throughput and recent operations before more batches are processed."
        confirmLabel="Pause migration"
        onClose={() => setConfirmOpen(false)}
        onConfirm={() => {
          void pauseJob(job.id);
          setConfirmOpen(false);
        }}
      />
    </>
  );
}
