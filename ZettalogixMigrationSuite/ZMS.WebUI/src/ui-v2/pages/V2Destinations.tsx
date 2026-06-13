import { CheckCircle2, Database, HardDrive, ShieldCheck } from "lucide-react";
import { migrationEvidence } from "../data/v2DashboardData";
import { V2Card, V2EvidenceRow, V2PageHeader, V2StatusPill } from "../components/V2Primitives";

export function V2Destinations(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Destinations"
        title="SharePoint target validation"
        description="Destination-side status for SharePoint Online targets and Microsoft Graph verification evidence."
      />

      <div className="zms-v2-grid">
        <V2Card title="SharePoint Online target" className="zms-v2-span-6">
          <HardDrive size={26} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Stage 1 target bytes were verified independently by Microsoft Graph and matched source bytes exactly.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2EvidenceRow label="Target bytes by Graph" value={migrationEvidence.stage1.graphVerifiedBytes.toLocaleString()} tone="success" />
            <V2EvidenceRow label="ZMS validation" value={migrationEvidence.stage1.validation} tone="success" />
            <V2EvidenceRow label="Failed files" value="0" tone="success" />
          </div>
        </V2Card>

        <V2Card title="Destination controls" className="zms-v2-span-6">
          <div className="zms-v2-row">
            <span><CheckCircle2 size={16} /> Target write capability</span>
            <V2StatusPill tone="success">Proven for Stage 1 file copy</V2StatusPill>
          </div>
          <div className="zms-v2-row">
            <span><Database size={16} /> Graph byte verification</span>
            <V2StatusPill tone="success">Passed</V2StatusPill>
          </div>
          <div className="zms-v2-row">
            <span><ShieldCheck size={16} /> Permission writeback</span>
            <V2StatusPill tone="warning">Pilot-gated</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Destination hardening notes" className="zms-v2-span-12">
          <ul className="zms-v2-list">
            <li>Use fresh SharePoint targets for Stage 2 and later scale runs to avoid duplicate evidence.</li>
            <li>Keep Graph verification as the independent byte-check source for migration evidence.</li>
            <li>Do not claim full ShareGate parity or production-scale certification until larger staged runs pass.</li>
          </ul>
        </V2Card>
      </div>
    </>
  );
}
