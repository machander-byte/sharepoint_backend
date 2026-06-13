import { Activity, Database, Server, ShieldAlert } from "lucide-react";
import type { V2ReadOnlySnapshot, V2RuntimeStatus } from "../data/v2ReadOnlyAdapter";
import { migrationEvidence } from "../data/v2DashboardData";
import { V2Card, V2EvidenceRow, V2LimitationBanner, V2PageHeader, V2StatusPill } from "../components/V2Primitives";

interface V2MonitorProps {
  runtime: V2RuntimeStatus;
  snapshot: V2ReadOnlySnapshot;
}

export function V2Monitor({ runtime, snapshot }: V2MonitorProps): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Monitor"
        title="Operator monitoring"
        description="Read-only status view for API health, queue state, logging posture, and migration monitoring."
      />

      <div className="zms-v2-grid">
        <V2Card title="Runtime health" className="zms-v2-span-4">
          <Server size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            V2 attempts anonymous `/api/status` and `/api/version` reads when `VITE_API_BASE_URL` is configured.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone={runtime.apiStatus === "Healthy" ? "success" : "warning"}>{runtime.apiStatus}</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Queue" className="zms-v2-span-4">
          <Activity size={28} color="var(--v2-success)" />
          <p className="zms-v2-copy">
            Adapter evidence reports the migration queue as {migrationEvidence.queue.toLowerCase()} after the verified run.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">{runtime.queueStatus ?? migrationEvidence.queue}</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Monitoring gap" className="zms-v2-span-4">
          <ShieldAlert size={28} color="var(--v2-warning)" />
          <p className="zms-v2-copy">
            Sentry configuration is supported, but controlled Sentry capture is not claimed without a configured DSN and safe test event.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Controlled capture pending</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Observed status" className="zms-v2-span-6">
          <V2EvidenceRow label="API status source" value={runtime.apiStatus === "Healthy" ? "Live /api/status" : "Adapter fallback"} tone={runtime.apiStatus === "Healthy" ? "success" : "warning"} />
          <V2EvidenceRow label="API version" value={runtime.version ?? "Not available"} tone={runtime.version ? "success" : "warning"} />
          <V2EvidenceRow label="Database provider" value={runtime.databaseProvider ?? "Not available"} tone={runtime.databaseProvider ? "success" : "warning"} />
          <V2EvidenceRow label="Read-only data source" value={snapshot.source === "api" ? "Live API" : "Fallback adapter"} tone={snapshot.source === "api" ? "success" : "warning"} />
        </V2Card>

        <V2Card title="Read-only API snapshot" className="zms-v2-span-6">
          <V2EvidenceRow label="Connections" value={`${snapshot.connectionCount}`} tone="success" />
          <V2EvidenceRow label="Latest job" value={snapshot.latestJobStatus} tone={snapshot.latestJobStatus === "Passed" ? "success" : "neutral"} />
          <V2EvidenceRow label="Readiness" value={`${snapshot.latestReadinessScore} / ${snapshot.latestReadinessStatus}`} tone="neutral" />
          <V2EvidenceRow label="Reports" value={`${snapshot.reportCount}`} tone="success" />
          <V2EvidenceRow label="AI recommendations" value={`${snapshot.aiRecommendationCount}`} tone="success" />
        </V2Card>

        <V2Card title="Operator timeline" className="zms-v2-span-6">
          <div className="zms-v2-row">
            <span><Database size={16} /> Stage 1 Graph byte verification</span>
            <V2StatusPill tone="success">Passed</V2StatusPill>
          </div>
          <div className="zms-v2-row">
            <span><Activity size={16} /> Stage 2 1,000-file run</span>
            <V2StatusPill tone="warning">Next</V2StatusPill>
          </div>
        </V2Card>

        <V2Card className="zms-v2-span-12">
          <V2LimitationBanner />
        </V2Card>
      </div>
    </>
  );
}
