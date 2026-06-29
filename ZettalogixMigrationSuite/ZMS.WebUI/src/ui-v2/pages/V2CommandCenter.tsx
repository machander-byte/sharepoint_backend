import { Activity, CheckCircle2, Database, FileText } from "lucide-react";
import { stageRows } from "../data/v2DashboardData";
import type { V2ReadOnlySnapshot, V2RuntimeStatus } from "../data/v2ReadOnlyAdapter";
import { V2Card, V2EvidenceRow, V2LimitationBanner, V2MetricCard, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

interface V2CommandCenterProps {
  runtime: V2RuntimeStatus;
  snapshot: V2ReadOnlySnapshot;
}

export function V2CommandCenter({ runtime, snapshot }: V2CommandCenterProps): JSX.Element {
  const liveMetrics = [
    { label: "Live connections", value: `${snapshot.connectionCount}`, status: snapshot.source === "api" ? "Live API" : "No live data" },
    { label: "Latest job", value: snapshot.latestJobStatus, status: snapshot.source === "api" ? snapshot.latestJobName : "No live data" },
    { label: "Reports", value: `${snapshot.reportCount}`, status: snapshot.source === "api" ? "Live API" : "No live data" },
    { label: "AI recommendations", value: `${snapshot.aiRecommendationCount}`, status: snapshot.source === "api" ? "Live API" : "No live data" }
  ];

  return (
    <>
      <V2PageHeader
        eyebrow="Command Center"
        title="Live migration control plane"
        description="A V2 shell for ZMS that separates live API data from historical validation evidence."
        actions={<button className="zms-v2-action" type="button">Refresh adapter evidence</button>}
      />

      <div className="zms-v2-grid">
        {liveMetrics.map((metric, index) => (
          <V2MetricCard
            key={metric.label}
            label={metric.label}
            value={metric.value}
            status={metric.status}
            icon={index === 3 ? Database : CheckCircle2}
            tone={snapshot.source === "api" ? "success" : "warning"}
          />
        ))}

        <V2Card title="Live API status" className="zms-v2-span-8">
          <p className="zms-v2-copy">
            The command center only shows live records when the deployed backend is healthy. Historical validation evidence is shown separately below and is not counted as current workspace data.
          </p>
          <div style={{ marginTop: 18 }}>
            <V2EvidenceRow label="Runtime" value={runtime.apiStatus} tone={runtime.apiStatus === "Healthy" ? "success" : "warning"} />
            <V2EvidenceRow label="Queue" value={runtime.queueStatus ?? "No live queue data"} tone={snapshot.source === "api" ? "success" : "warning"} />
            <V2EvidenceRow label="Database startup" value={runtime.databaseStartupStatus ?? "Not reported"} tone={runtime.apiStatus === "Healthy" ? "success" : "warning"} />
            <V2EvidenceRow label="Data source" value={snapshot.source === "api" ? "Live API" : "No live records shown"} tone={snapshot.source === "api" ? "success" : "warning"} />
          </div>
        </V2Card>

        <V2Card title="Stage 2 next gate" className="zms-v2-span-4">
          <Activity size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Next recommended validation is a 1,000-file non-production migration into a fresh SharePoint target with Graph verification.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Pending</V2StatusPill>
          </div>
        </V2Card>

        <V2Card className="zms-v2-span-12">
          <V2LimitationBanner />
        </V2Card>

        <V2Card title="Certification progress" className="zms-v2-span-12">
          <V2Table
            headers={["Stage", "Scope", "Files", "Failures", "Retries", "Verification", "Status"]}
            rows={stageRows.map((row) => [
              row.stage,
              row.scope,
              row.files,
              row.failures,
              row.retries,
              row.verification,
              <V2StatusPill key={row.stage} tone={row.status === "Passed" ? "success" : "warning"}>{row.status}</V2StatusPill>
            ])}
          />
        </V2Card>

        <V2Card title="Evidence artifacts" className="zms-v2-span-12">
          <p className="zms-v2-copy">
            Historical evidence from previous controlled validation runs. These rows are not live production records.
          </p>
          <ul className="zms-v2-list">
            <li><FileText size={16} /> Stage 1 231-file report confirms 231/231 files, 0 failures, 0 retries, and matching bytes.</li>
            <li><FileText size={16} /> Live migration validation report documents Stage 0 22/22 result.</li>
            <li><FileText size={16} /> Full production readiness is not claimed from these stages alone.</li>
          </ul>
        </V2Card>
      </div>
    </>
  );
}
