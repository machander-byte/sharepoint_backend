import { KeyRound, Settings2, ShieldCheck, Users } from "lucide-react";
import { internalSafetyLimits, migrationEvidence } from "../data/v2DashboardData";
import { V2Card, V2EvidenceRow, V2PageHeader, V2StatusPill } from "../components/V2Primitives";

interface V2SettingsProps {
  onRestartTour: () => void;
}

export function V2Settings({ onRestartTour }: V2SettingsProps): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Settings"
        title="Environment and safety settings"
        description="Settings preview for backend-only secrets, RBAC posture, internal safety limits, and integration boundaries."
        actions={(
          <button type="button" className="zms-v2-action" onClick={onRestartTour}>
            Restart guided tour
          </button>
        )}
      />

      <div className="zms-v2-grid">
        <V2Card title="Environment" className="zms-v2-span-4">
          <Settings2 size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            V2 uses adapter data with optional anonymous health/status reads. It does not require backend secrets in frontend env.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill>Adapter first</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Auth and RBAC" className="zms-v2-span-4">
          <Users size={28} color="var(--v2-success)" />
          <p className="zms-v2-copy">
            The `/v2` route is mounted inside the existing authenticated route guard. Current production routes remain unchanged.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Protected route</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Secrets" className="zms-v2-span-4">
          <KeyRound size={28} color="var(--v2-warning)" />
          <p className="zms-v2-copy">
            Backend secrets must stay in backend configuration or hosting secret stores. Frontend Vite env must only contain browser-safe values.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Rotate pasted secrets before demo</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Guided onboarding" className="zms-v2-span-4">
          <ShieldCheck size={28} color="var(--v2-success)" />
          <p className="zms-v2-copy">
            Restart the UI-only tour at any time. It changes only browser navigation and local onboarding state; it does not run migrations or update backend data.
          </p>
          <div style={{ marginTop: 16 }}>
            <button type="button" className="zms-v2-action" onClick={onRestartTour}>
              Restart guided tour
            </button>
          </div>
        </V2Card>

        <V2Card title="Current validation status" className="zms-v2-span-4">
          <V2EvidenceRow label="Backend tests" value={migrationEvidence.backendTests} tone="success" />
          <V2EvidenceRow label="Frontend build" value={migrationEvidence.frontendBuild} tone="success" />
          <V2EvidenceRow label="Supabase" value={migrationEvidence.supabase} tone="success" />
        </V2Card>

        <V2Card title="Internal safety limits" className="zms-v2-span-8">
          <ul className="zms-v2-list">
            {internalSafetyLimits.map((limit) => (
              <li key={limit}><ShieldCheck size={15} color="var(--v2-success)" /> {limit}</li>
            ))}
          </ul>
        </V2Card>
      </div>
    </>
  );
}
