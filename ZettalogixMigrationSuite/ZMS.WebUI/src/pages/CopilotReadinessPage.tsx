import { useEffect, useState } from "react";
import PageHeader from "../components/PageHeader";
import { zmsApi } from "../services/zmsApi";

interface CopilotReadiness {
  overallScore: number;
  riskTier: string;
  summary: string;
  categoryScores: Record<string, number>;
  topFindings: Array<{ category: string; severity: string; location: string; description: string; recommendation: string }>;
  recommendedActions: string[];
}

export default function CopilotReadinessPage(): JSX.Element {
  const [readiness, setReadiness] = useState<CopilotReadiness | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;
    zmsApi.getCopilotReadinessLatest()
      .then((result) => {
        if (!cancelled) setReadiness(result);
      })
      .catch((nextError: unknown) => setError(nextError instanceof Error ? nextError.message : "Copilot readiness could not be loaded."))
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Copilot Readiness" subtitle="Governance, oversharing, stale content, and access-risk scoring from discovery data." />

      <section className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Overall Score</p>
          <p className="mt-3 text-4xl font-bold text-primary">{readiness?.overallScore ?? 0}%</p>
          <p className="mt-2 text-sm text-text-muted">{loading ? "Loading" : readiness?.riskTier ?? "No discovery data"}</p>
        </article>
        <article className="rounded-xl border border-border bg-surface p-5 shadow-card lg:col-span-2">
          <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Summary</p>
          <p className="mt-3 text-sm leading-6 text-text-primary">
            {error ?? readiness?.summary ?? "Run discovery before Copilot readiness can be calculated."}
          </p>
        </article>
      </section>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {Object.entries(readiness?.categoryScores ?? {}).map(([category, score]) => (
          <article key={category} className="rounded-xl border border-border bg-surface p-5 shadow-card">
            <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{category}</p>
            <p className="mt-2 text-2xl font-bold text-text-primary">{score}%</p>
          </article>
        ))}
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <h2 className="font-bold text-text-primary">Top Findings</h2>
        <div className="mt-4 overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-text-muted">
                <th className="py-2">Severity</th>
                <th className="py-2">Category</th>
                <th className="py-2">Location</th>
                <th className="py-2">Recommendation</th>
              </tr>
            </thead>
            <tbody>
              {(readiness?.topFindings ?? []).map((finding, index) => (
                <tr key={`${finding.category}-${index}`} className="border-t border-border">
                  <td className="py-3 font-semibold">{finding.severity}</td>
                  <td className="py-3">{finding.category}</td>
                  <td className="py-3">{finding.location || "-"}</td>
                  <td className="py-3">{finding.recommendation}</td>
                </tr>
              ))}
              {!readiness ? <tr><td colSpan={4} className="py-4 text-text-muted">No readiness findings available.</td></tr> : null}
            </tbody>
          </table>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <h2 className="font-bold text-text-primary">Recommended Actions</h2>
        <div className="mt-4 grid gap-3">
          {(readiness?.recommendedActions ?? []).map((action) => (
            <div key={action} className="rounded-lg border border-border bg-surface-container p-3 text-sm font-semibold text-text-primary">
              {action}
            </div>
          ))}
          {!readiness ? <p className="text-sm text-text-muted">No recommended actions available.</p> : null}
        </div>
      </section>
    </div>
  );
}
