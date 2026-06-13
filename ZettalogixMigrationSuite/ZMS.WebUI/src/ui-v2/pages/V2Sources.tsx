import { CloudUpload, FolderInput, HardDrive, ShieldCheck } from "lucide-react";
import { migrationEvidence } from "../data/v2DashboardData";
import { V2Card, V2EvidenceRow, V2LimitationBanner, V2PageHeader, V2StatusPill } from "../components/V2Primitives";

export function V2Sources(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Sources"
        title="Source inventory and connection readiness"
        description="Source-side V2 preview for Google Drive, SharePoint, and file-share discovery without changing the current Connections UI."
      />

      <div className="zms-v2-grid">
        <V2Card title="Google Drive source" className="zms-v2-span-6">
          <CloudUpload size={26} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Google Drive is the verified source for the current live proof. Stage 1 migrated {migrationEvidence.stage1.files} files with 0 failures and 0 retries.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2EvidenceRow label="Stage 0" value={`${migrationEvidence.stage0.files} passed`} tone="success" />
            <V2EvidenceRow label="Stage 1" value={`${migrationEvidence.stage1.files} passed`} tone="success" />
            <V2EvidenceRow label="Source bytes" value={migrationEvidence.stage1.sourceBytes.toLocaleString()} tone="success" />
          </div>
        </V2Card>

        <V2Card title="Other source connectors" className="zms-v2-span-6">
          <div className="zms-v2-row">
            <span><HardDrive size={16} /> File share connector</span>
            <V2StatusPill>Implemented foundation</V2StatusPill>
          </div>
          <div className="zms-v2-row">
            <span><FolderInput size={16} /> SharePoint source discovery</span>
            <V2StatusPill>API surface present</V2StatusPill>
          </div>
          <div className="zms-v2-row">
            <span><ShieldCheck size={16} /> Credential redaction</span>
            <V2StatusPill tone="success">Covered by tests</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Source truth rules" className="zms-v2-span-12">
          <ul className="zms-v2-list">
            <li>Show live migration proof only for file integrity that has been verified.</li>
            <li>Do not claim empty source folder preservation until first-class folder migration is implemented and tested.</li>
            <li>Do not expose refresh tokens, client secrets, or backend connection strings in frontend configuration.</li>
          </ul>
        </V2Card>

        <V2Card className="zms-v2-span-12">
          <V2LimitationBanner />
        </V2Card>
      </div>
    </>
  );
}
