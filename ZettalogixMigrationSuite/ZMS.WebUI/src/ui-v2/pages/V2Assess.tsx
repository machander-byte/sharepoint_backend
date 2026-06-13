import { AlertTriangle, FileWarning, ListChecks, ShieldCheck } from "lucide-react";
import { riskSummary } from "../data/v2DashboardData";
import { V2Card, V2LimitationBanner, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

export function V2Assess(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Assess"
        title="Readiness and risk analysis"
        description="Assessment preview for readiness scoring, blockers, warnings, permissions, metadata, path, and archive risks."
      />

      <div className="zms-v2-grid">
        <V2Card title="Readiness engine" className="zms-v2-span-4">
          <ListChecks size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Backend readiness, remediation grouping, and wave planning are covered by automated tests.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Tests passed</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Risk scoring" className="zms-v2-span-4">
          <AlertTriangle size={28} color="var(--v2-warning)" />
          <p className="zms-v2-copy">
            V2 keeps the current risk posture visible without claiming full production readiness.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Scale validation pending</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Security posture" className="zms-v2-span-4">
          <ShieldCheck size={28} color="var(--v2-success)" />
          <p className="zms-v2-copy">
            Secret redaction tests are present and backend-only secret placement is documented.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Redaction covered</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Risk summary" className="zms-v2-span-12">
          <V2Table
            headers={["Area", "Level", "Detail"]}
            rows={riskSummary.map((risk) => [
              <><FileWarning size={15} /> {risk.name}</>,
              <V2StatusPill key={risk.name} tone={risk.level === "Known gap" ? "warning" : "neutral"}>{risk.level}</V2StatusPill>,
              risk.detail
            ])}
          />
        </V2Card>

        <V2Card className="zms-v2-span-12">
          <V2LimitationBanner />
        </V2Card>
      </div>
    </>
  );
}
