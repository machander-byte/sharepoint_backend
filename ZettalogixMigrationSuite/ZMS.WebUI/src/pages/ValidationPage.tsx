import { useEffect, useMemo, useState } from "react";
import { api } from "../services/api";
import { MigrationJob, ValidationFindingRecord, ValidationItemRecord, ValidationRunRecord } from "../utils/models";

const emptyValidationMessage = "No validation run has been recorded yet. Start a migration validation to compare source and target items.";

function formatReadableLabel(value?: string | null): string {
  const normalized = value?.trim();
  if (!normalized) return "-";

  return normalized
    .replace(/_/g, " ")
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .toLowerCase()
    .replace(/\b\w/g, (letter) => letter.toUpperCase());
}

export default function ValidationPage(): JSX.Element {
  const [jobs, setJobs] = useState<MigrationJob[]>([]);
  const [selectedJobId, setSelectedJobId] = useState("");
  const [run, setRun] = useState<ValidationRunRecord | null>(null);
  const [findings, setFindings] = useState<ValidationFindingRecord[]>([]);
  const [items, setItems] = useState<ValidationItemRecord[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    api.getJobs()
      .then((nextJobs) => {
        if (cancelled) return;
        setJobs(nextJobs);
        setSelectedJobId(nextJobs[0]?.id ?? "");
      })
      .catch((nextError: unknown) => setError(nextError instanceof Error ? nextError.message : "Validation data could not be loaded."));

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!selectedJobId) return;

    let cancelled = false;
    setLoading(true);
    setError(null);
    api.getLatestValidation(selectedJobId)
      .then(async (latest) => {
        if (cancelled) return;
        setRun(latest);
        if (!latest) {
          setFindings([]);
          setItems([]);
          return;
        }

        const [nextFindings, nextItems] = await Promise.all([
          api.getValidationFindings(latest.id),
          api.getValidationItems(latest.id)
        ]);
        if (!cancelled) {
          setFindings(nextFindings);
          setItems(nextItems);
        }
      })
      .catch((nextError: unknown) => setError(nextError instanceof Error ? nextError.message : "Validation data could not be loaded."))
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [selectedJobId]);

  const selectedJob = useMemo(() => jobs.find((job) => job.id === selectedJobId), [jobs, selectedJobId]);

  const startValidation = async () => {
    if (!selectedJobId) return;
    setLoading(true);
    setError(null);
    try {
      const nextRun = await api.startValidation(selectedJobId);
      const [nextFindings, nextItems] = await Promise.all([
        api.getValidationFindings(nextRun.id),
        api.getValidationItems(nextRun.id)
      ]);
      setRun(nextRun);
      setFindings(nextFindings);
      setItems(nextItems);
    } catch (nextError) {
      setError(nextError instanceof Error ? nextError.message : "Validation could not be started.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="page-stack">
      <article className="surface-card">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Validation</span>
            <h2>Post-migration validation</h2>
            <p>Compares persisted migration items with recorded target paths, status, size, metadata, and permission availability.</p>
          </div>
          <div className="action-group">
            <select value={selectedJobId} onChange={(event) => setSelectedJobId(event.target.value)}>
              {jobs.length === 0 ? (
                <option value="">No migration jobs</option>
              ) : (
                jobs.map((job) => (
                  <option key={job.id} value={job.id}>{job.name}</option>
                ))
              )}
            </select>
            <button type="button" className="primary-button" onClick={() => void startValidation()} disabled={!selectedJobId || loading}>
              {loading ? "Running" : "Start validation"}
            </button>
          </div>
        </div>

        {error ? <div className="error-panel"><p>{error}</p></div> : null}

        <div className="meta-grid">
          <div className="metric-box">
            <span>Status</span>
            <strong>{run ? formatReadableLabel(run.status) : "Not started"}</strong>
            <p>{selectedJob?.name ?? "No migration job selected"}</p>
          </div>
          <div className="metric-box">
            <span>Passed</span>
            <strong>{run?.passedCount ?? 0}</strong>
            <p>{run?.summary ?? emptyValidationMessage}</p>
          </div>
          <div className="metric-box">
            <span>Warnings</span>
            <strong>{run?.warningCount ?? 0}</strong>
            <p>{findings.length} finding records available.</p>
          </div>
          <div className="metric-box">
            <span>Failed</span>
            <strong>{run?.failedCount ?? 0}</strong>
            <p>Items requiring operator review after validation.</p>
          </div>
        </div>
      </article>

      {run ? (
        <article className="surface-card">
          <div className="section-heading">
            <div>
              <span className="eyebrow">Exports</span>
              <h2>Validation reports</h2>
            </div>
            <div className="action-group">
              {["summary.csv", "failed-items.csv", "metadata-mismatch.csv", "permission-mismatch.csv", "report.json"].map((type) => (
                <button key={type} type="button" className="ghost-button" onClick={() => void api.downloadValidationExport(run.id, type)}>
                  {type}
                </button>
              ))}
            </div>
          </div>
        </article>
      ) : null}

      <article className="surface-card">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Findings</span>
            <h2>Validation findings</h2>
          </div>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Severity</th>
                <th>Category</th>
                <th>Source</th>
                <th>Target</th>
                <th>Recommended action</th>
              </tr>
            </thead>
            <tbody>
              {findings.length === 0 ? (
                <tr><td colSpan={5} className="table-empty">{emptyValidationMessage}</td></tr>
              ) : findings.map((finding) => (
                <tr key={finding.id}>
                  <td>{formatReadableLabel(finding.severity)}</td>
                  <td>{formatReadableLabel(finding.category)}</td>
                  <td>{finding.sourcePath}</td>
                  <td>{finding.targetPath || "-"}</td>
                  <td>{finding.recommendedAction}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </article>

      <article className="surface-card">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Items</span>
            <h2>Item comparison</h2>
          </div>
        </div>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Status</th>
                <th>Difference</th>
                <th>Source</th>
                <th>Target</th>
                <th>Message</th>
              </tr>
            </thead>
            <tbody>
              {items.length === 0 ? (
                <tr><td colSpan={5} className="table-empty">{emptyValidationMessage}</td></tr>
              ) : (
                items.slice(0, 50).map((item) => (
                  <tr key={item.id}>
                    <td>{formatReadableLabel(item.status)}</td>
                    <td>{formatReadableLabel(item.differenceType)}</td>
                    <td>{item.sourcePath}</td>
                    <td>{item.targetPath || "-"}</td>
                    <td>{item.message}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </article>
    </section>
  );
}
