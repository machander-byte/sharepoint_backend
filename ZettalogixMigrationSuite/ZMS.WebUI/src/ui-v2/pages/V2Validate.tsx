import { CheckCircle2, Database, FileCheck2, FolderInput } from "lucide-react";
import { migrationEvidence } from "../data/v2DashboardData";
import { V2Card, V2EvidenceRow, V2LimitationBanner, V2MetricCard, V2PageHeader, V2StatusPill } from "../components/V2Primitives";

export function V2Validate(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Validate"
        title="Validation and byte verification"
        description="Validation preview for Go/No-Go checks, ZMS validation, Microsoft Graph byte checks, and known limitations."
      />

      <div className="zms-v2-grid">
        <V2MetricCard label="ZMS validation" value="231/231" status="Passed" icon={FileCheck2} tone="success" />
        <V2MetricCard label="Source bytes" value={migrationEvidence.stage1.sourceBytes.toLocaleString()} status="Recorded" icon={Database} tone="success" />
        <V2MetricCard label="Graph bytes" value={migrationEvidence.stage1.graphVerifiedBytes.toLocaleString()} status="Matched" icon={CheckCircle2} tone="success" />
        <V2MetricCard label="Empty folders" value="Gap" status="Not first-class yet" icon={FolderInput} tone="warning" />

        <V2Card title="Validation evidence" className="zms-v2-span-6">
          <V2EvidenceRow label="Stage 1 file count" value={migrationEvidence.stage1.files} tone="success" />
          <V2EvidenceRow label="Validation result" value={migrationEvidence.stage1.validation} tone="success" />
          <V2EvidenceRow label="Failed files" value="0" tone="success" />
          <V2EvidenceRow label="Retries" value="0" tone="success" />
        </V2Card>

        <V2Card title="Decision boundary" className="zms-v2-span-6">
          <p className="zms-v2-copy">
            The current evidence supports a company demo of verified file migration behavior. It does not support a production readiness claim until larger scale, recovery, monitoring, and security checks are complete.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Production readiness pending</V2StatusPill>
          </div>
        </V2Card>

        <V2Card className="zms-v2-span-12">
          <V2LimitationBanner />
        </V2Card>
      </div>
    </>
  );
}
