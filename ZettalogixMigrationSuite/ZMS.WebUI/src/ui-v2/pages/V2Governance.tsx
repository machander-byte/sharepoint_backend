import { Lock, ShieldAlert, ShieldCheck, UserPlus } from "lucide-react";
import { V2Card, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

const governanceRows = [
  ["Oversharing risks", "Review", "Surface broad access and sharing links before larger migration waves."],
  ["External users", "Review", "Confirm external principal mapping before production cutover."],
  ["Broken inheritance", "Review", "Cleanup queue should prioritize unique permissions on restricted content."],
  ["Copilot readiness", "Foundation", "Copilot readiness API surface is present; live governance validation remains pending."],
  ["Sensitive content readiness", "Pending", "Run sensitivity and restricted-content checks before production readiness claims."]
];

export function V2Governance(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Governance"
        title="Access and Copilot readiness"
        description="Governance preview for oversharing, external access, inheritance breaks, sensitive content, and cleanup queues."
      />

      <div className="zms-v2-grid">
        <V2Card title="Governance scope" className="zms-v2-span-4">
          <ShieldCheck size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            V2 presents governance findings as readiness inputs, not as proof that all tenant governance has been remediated.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill>Assessment view</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Permission cleanup queue" className="zms-v2-span-4">
          <UserPlus size={28} color="var(--v2-warning)" />
          <p className="zms-v2-copy">
            Cleanup queue is shown as a worklist for owners and operators.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Owner review required</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Sensitive content" className="zms-v2-span-4">
          <Lock size={28} color="var(--v2-danger)" />
          <p className="zms-v2-copy">
            Restricted content should remain gated by approvals and pre-migration checks.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="danger">Do not auto-approve</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Governance matrix" className="zms-v2-span-12">
          <V2Table
            headers={["Area", "Status", "Action"]}
            rows={governanceRows.map((row) => [
              <><ShieldAlert size={15} /> {row[0]}</>,
              <V2StatusPill key={row[0]} tone={row[1] === "Pending" ? "warning" : "neutral"}>{row[1]}</V2StatusPill>,
              row[2]
            ])}
          />
        </V2Card>
      </div>
    </>
  );
}
