import { Link } from "react-router-dom";
import { api } from "../../services/api";
import { formatDate, formatJobStatus } from "../../utils/formatters";
import { MigrationJob } from "../../utils/models";
import ProgressBar from "../ProgressBar/ProgressBar";
import EmptyState from "../EmptyState/EmptyState";
import styles from "./JobTable.module.css";

interface JobTableProps {
  jobs: MigrationJob[];
  onStart: (id: string) => void;
  onPause: (id: string) => void;
  onResume: (id: string) => void;
  onCancel: (id: string) => void;
  onRetry: (id: string) => void;
}

export default function JobTable({ jobs, onStart, onPause, onResume, onCancel, onRetry }: JobTableProps): JSX.Element {
  if (jobs.length === 0) {
    return <EmptyState title="No migration jobs yet." description="Create a job or run a simulation to see execution details here." />;
  }

  return (
    <div className={styles.tableWrap}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>Migration</th>
            <th>Status</th>
            <th>Enterprise State</th>
            <th>Progress</th>
            <th>Retries</th>
            <th>Target</th>
            <th>Updated</th>
            <th>Control</th>
          </tr>
        </thead>
        <tbody>
          {jobs.map((job) => (
            <tr key={job.id}>
              <td>
                <strong>{job.name}</strong>
                <span className={styles.subtle}>
                  {job.sourceLibraryName ? `${job.sourcePath} / ${job.sourceLibraryName}` : job.sourcePath} to {job.targetLibrary}
                </span>
              </td>
              <td>
                <span className={`status-chip ${job.status.toLowerCase()}`}>{formatJobStatus(job.status)}</span>
              </td>
              <td>
                <strong>{job.enterpriseState}</strong>
                <span className={styles.subtle}>{job.history[0]?.message ?? "No timeline event yet"}</span>
              </td>
              <td>
                <ProgressBar value={job.progress} />
                <span className={styles.subtle}>
                  {job.migratedFiles} of {job.totalFiles} files
                </span>
              </td>
              <td>{job.retryCount}</td>
              <td>
                <span>{job.targetSite}</span>
                <span className={styles.subtle}>
                  {job.targetRootPath ? `${job.targetLibrary} / ${job.targetRootPath}` : job.targetLibrary}
                </span>
              </td>
              <td>{formatDate(job.updatedAt)}</td>
              <td className={styles.actions}>
                <Link to={`/migrations/${job.id}`}>Open</Link>
                {job.status === "Running" ? (
                  <button type="button" onClick={() => onPause(job.id)}>
                    Pause
                  </button>
                ) : job.status === "Paused" ? (
                  <button type="button" onClick={() => onResume(job.id)}>
                    Resume
                  </button>
                ) : (
                  <button type="button" onClick={() => onStart(job.id)} disabled={["Completed", "CompletedWithErrors"].includes(job.status)}>
                    Start
                  </button>
                )}
                <button type="button" onClick={() => onRetry(job.id)} disabled={!["Failed", "CompletedWithErrors"].includes(job.status)}>
                  Retry
                </button>
                <button type="button" onClick={() => onCancel(job.id)} disabled={["Completed", "CompletedWithErrors", "Failed"].includes(job.status)}>
                  Cancel
                </button>
                <button type="button" onClick={() => void api.downloadReport(`/jobs/${job.id}/summary.csv`)}>
                  Report
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
