import { Download, FileText, ShieldCheck } from "lucide-react";
import { reportExports } from "../data/v2DashboardData";
import { V2Card, V2LimitationBanner, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

export function V2Reports(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Reports"
        title="Evidence and export center"
        description="Report preview showing implemented exports, encoding posture, evidence boundaries, and current limitations."
      />

      <div className="zms-v2-grid">
        <V2Card title="Export status" className="zms-v2-span-8">
          <V2Table
            headers={["Report", "Data source", "Status"]}
            rows={reportExports.map((report) => [
              report,
              "Existing backend or V2 adapter evidence",
              <V2StatusPill key={report}>Implemented / verify live download</V2StatusPill>
            ])}
          />
        </V2Card>

        <V2Card title="Export hardening" className="zms-v2-span-4">
          <Download size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Frontend CSV utility now emits UTF-8 BOM and CRLF row endings for better Excel compatibility. Backend report exports already use UTF-8 CSV output.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Build verified</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Report claim rules" className="zms-v2-span-6">
          <ul className="zms-v2-list">
            <li><FileText size={15} /> Report current migration evidence exactly: 231/231 files, 0 failed, 0 retries.</li>
            <li><ShieldCheck size={15} /> Do not include secrets, tokens, connection strings, or backend credentials in exported evidence.</li>
            <li><FileText size={15} /> Do not claim empty-folder preservation or production-scale certification.</li>
          </ul>
        </V2Card>

        <V2Card title="Next verification" className="zms-v2-span-6">
          <p className="zms-v2-copy">
            Next pass should download and open each report from an authenticated run, verify encoding, verify counts, and confirm no secrets are present.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Authenticated export opening still pending</V2StatusPill>
          </div>
        </V2Card>

        <V2Card className="zms-v2-span-12">
          <V2LimitationBanner />
        </V2Card>
      </div>
    </>
  );
}
