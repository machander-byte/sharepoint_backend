import { useEffect, useState } from "react";
import PageHeader from "../components/PageHeader";
import { zmsApi } from "../services/zmsApi";

interface TeamsDiscovery {
  runId: string;
  summary: Record<string, number>;
  topology: Array<Record<string, unknown>>;
  risks: Array<{ id: string; category: string; severity: string; teamName: string; description: string; recommendation: string }>;
  teams: Array<Record<string, unknown>>;
}

export default function TeamsDiscoveryPage(): JSX.Element {
  const [result, setResult] = useState<TeamsDiscovery | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    zmsApi.getLatestTeamsDiscovery().then((latest) => {
      if (!cancelled) setResult(latest);
    });

    return () => {
      cancelled = true;
    };
  }, []);

  const start = async () => {
    setLoading(true);
    setError(null);
    try {
      const next = await zmsApi.startTeamsDiscovery();
      setResult(next);
    } catch (nextError) {
      setError(nextError instanceof Error ? nextError.message : "Teams discovery could not be started.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Teams Discovery"
        subtitle="Teams topology, ownership, guest access, SharePoint mapping, and planning risks."
        actions={
          <button type="button" className="inline-flex items-center rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white" onClick={() => void start()} disabled={loading}>
            {loading ? "Scanning" : "Start Teams Discovery"}
          </button>
        }
      />

      {error ? <div className="rounded-xl border border-error/30 bg-error-soft p-4 text-sm text-error">{error}</div> : null}

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">
        {Object.entries(result?.summary ?? { teams: 0, channels: 0, guests: 0, risks: 0 }).map(([label, value]) => (
          <article key={label} className="rounded-xl border border-border bg-surface p-5 shadow-card">
            <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{label}</p>
            <p className="mt-2 text-3xl font-bold text-text-primary">{value}</p>
          </article>
        ))}
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <h2 className="font-bold text-text-primary">Topology</h2>
        <div className="mt-4 overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead>
              <tr className="text-text-muted">
                <th className="py-2">Team</th>
                <th className="py-2">Dependency</th>
                <th className="py-2">Target</th>
              </tr>
            </thead>
            <tbody>
              {(result?.topology ?? []).slice(0, 30).map((item, index) => (
                <tr key={index} className="border-t border-border">
                  <td className="py-3">{String(item.teamName ?? item.team ?? "-")}</td>
                  <td className="py-3">{String(item.dependencyType ?? item.type ?? "-")}</td>
                  <td className="py-3">{String(item.target ?? item.location ?? item.sharePointSiteUrl ?? "-")}</td>
                </tr>
              ))}
              {!result ? <tr><td colSpan={3} className="py-4 text-text-muted">No Teams discovery run available.</td></tr> : null}
            </tbody>
          </table>
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <h2 className="font-bold text-text-primary">Risk Findings</h2>
        <div className="mt-4 grid gap-3">
          {(result?.risks ?? []).map((risk) => (
            <div key={risk.id} className="rounded-lg border border-border bg-surface-container p-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <h3 className="font-bold text-text-primary">{risk.category}</h3>
                <span className="text-xs font-bold uppercase tracking-wide text-text-subtle">{risk.severity}</span>
              </div>
              <p className="mt-2 text-sm text-text-muted">{risk.teamName}: {risk.description}</p>
              <p className="mt-2 text-sm font-semibold text-primary">{risk.recommendation}</p>
            </div>
          ))}
          {!result ? <p className="text-sm text-text-muted">No Teams risk findings available.</p> : null}
        </div>
      </section>
    </div>
  );
}
