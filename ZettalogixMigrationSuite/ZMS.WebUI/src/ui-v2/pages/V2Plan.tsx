import { BookOpenText, CheckCircle2, Layers3, ShieldCheck } from "lucide-react";
import { internalSafetyLimits } from "../data/v2DashboardData";
import { V2Card, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

const planRows = [
  ["Wave 1", "Low-risk pilot", "Use validated source/target pair", "Planning foundation present"],
  ["Wave 2", "Business content", "Resolve permission and metadata warnings first", "Pending"],
  ["Wave 3", "Restricted content", "Requires owner approval and access review", "Pending"],
  ["Wave 4", "Archive cleanup", "Decide archive versus migrate strategy", "Pending"]
];

export function V2Plan(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Plan"
        title="Migration planning and runbook generation"
        description="Planning preview for waves, checklist, approvals, exclusions, validation, and runbook generation."
      />

      <div className="zms-v2-grid">
        <V2Card title="Planner status" className="zms-v2-span-4">
          <Layers3 size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Migration plan generation, validation, and runbook generation are covered by backend tests.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Automated coverage present</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Runbook status" className="zms-v2-span-4">
          <BookOpenText size={28} color="var(--v2-success)" />
          <p className="zms-v2-copy">
            Runbook markdown generation is available as a planning artifact.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Implemented foundation</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Internal safety limits" className="zms-v2-span-4">
          <ShieldCheck size={28} color="var(--v2-warning)" />
          <p className="zms-v2-copy">
            V2 uses safety limits language only. Commercial plan controls are intentionally excluded.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Internal limits only</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Wave model" className="zms-v2-span-8">
          <V2Table
            headers={["Wave", "Purpose", "Gate", "Status"]}
            rows={planRows.map((row) => [
              row[0],
              row[1],
              row[2],
              <V2StatusPill key={row[0]} tone={row[3] === "Pending" ? "warning" : "success"}>{row[3]}</V2StatusPill>
            ])}
          />
        </V2Card>

        <V2Card title="Safety checklist" className="zms-v2-span-4">
          <ul className="zms-v2-list">
            {internalSafetyLimits.map((limit) => (
              <li key={limit}><CheckCircle2 size={15} color="var(--v2-success)" /> {limit}</li>
            ))}
          </ul>
        </V2Card>
      </div>
    </>
  );
}
