import { PlayCircle, Rocket, ShieldCheck, Square } from "lucide-react";
import { migrationEvidence, stageRows } from "../data/v2DashboardData";
import { V2Card, V2LimitationBanner, V2MetricCard, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

export function V2Migrate(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Migrate"
        title="Live migration module"
        description="Execution preview showing proven file migration evidence and hard safety gates for future live pilots."
      />

      <div className="zms-v2-grid">
        <V2MetricCard label="Stage 1 files copied" value={migrationEvidence.stage1.files} status="Passed" icon={Rocket} tone="success" />
        <V2MetricCard label="Failed files" value="0" status="Passed" icon={Square} tone="success" />
        <V2MetricCard label="Retries" value="0" status="Passed" icon={PlayCircle} tone="success" />
        <V2MetricCard label="Live pilot cap" value="10" status="Default safety limit" icon={ShieldCheck} tone="warning" />

        <V2Card title="Live migration stages" className="zms-v2-span-12">
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

        <V2Card title="Execution safety gates" className="zms-v2-span-6">
          <ul className="zms-v2-list">
            <li>Live pilot is disabled by default unless `ZMS_ENABLE_LIVE_MIGRATION=true` is set for a test tenant.</li>
            <li>Request mode must be `live_pilot`.</li>
            <li>Confirmation text must exactly match `ENABLE LIVE PILOT MIGRATION`.</li>
            <li>Default file limit is 10 unless `ZMS_LIVE_PILOT_MAX_FILES` is configured.</li>
          </ul>
        </V2Card>

        <V2Card title="Honest claim boundary" className="zms-v2-span-6">
          <p className="zms-v2-copy">
            ZMS can claim live file migration integrity for the completed Google Drive to SharePoint stages.
            It should not claim production-scale certification, full folder preservation, commercial plan support, or full ShareGate parity.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Stage 2 pending</V2StatusPill>
          </div>
        </V2Card>

        <V2Card className="zms-v2-span-12">
          <V2LimitationBanner />
        </V2Card>
      </div>
    </>
  );
}
