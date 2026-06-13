import { Activity, CheckCircle2, Database, FileText } from "lucide-react";
import { commandMetrics, migrationEvidence, stageRows } from "../data/v2DashboardData";
import { V2Card, V2EvidenceRow, V2LimitationBanner, V2MetricCard, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

export function V2CommandCenter(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Command Center"
        title="Validated migration control plane"
        description="A V2 preview shell for ZMS using current verified evidence from Google Drive to SharePoint validation."
        actions={<button className="zms-v2-action" type="button">Refresh adapter evidence</button>}
      />

      <div className="zms-v2-grid">
        {commandMetrics.map((metric, index) => (
          <V2MetricCard
            key={metric.label}
            label={metric.label}
            value={metric.value}
            status={metric.status}
            icon={index === 3 ? Database : CheckCircle2}
            tone="success"
          />
        ))}

        <V2Card title="Current verified status" className="zms-v2-span-8">
          <p className="zms-v2-copy">
            {migrationEvidence.source} -&gt; {migrationEvidence.target} file migration has passed Stage 0 and Stage 1 validation.
            ZMS validation passed and Microsoft Graph byte verification matched the Stage 1 source bytes.
          </p>
          <div style={{ marginTop: 18 }}>
            <V2EvidenceRow label="Backend tests" value={migrationEvidence.backendTests} tone="success" />
            <V2EvidenceRow label="Frontend build" value={migrationEvidence.frontendBuild} tone="success" />
            <V2EvidenceRow label="Queue" value={migrationEvidence.queue} tone="success" />
            <V2EvidenceRow label="Supabase" value={migrationEvidence.supabase} tone="success" />
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
