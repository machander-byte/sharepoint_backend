import { BookOpen, Bot, CheckCircle2, Database, FileText, FolderInput, LifeBuoy, ListChecks, LogOut, Rocket, ShieldCheck } from "lucide-react";
import type { V2ReadOnlySnapshot, V2RuntimeStatus } from "../data/v2ReadOnlyAdapter";
import { V2Card, V2EvidenceRow, V2PageHeader, V2StatusPill } from "../components/V2Primitives";

interface V2TutorialProps {
  runtime: V2RuntimeStatus;
  snapshot: V2ReadOnlySnapshot;
  onRestartTour: () => void;
}

const workflowSteps = [
  {
    title: "Connect sources",
    icon: FolderInput,
    detail: "Add and review Google Drive, SharePoint, or file-share source platforms. Use test tenants first and confirm credentials before running discovery."
  },
  {
    title: "Review destinations",
    icon: FolderInput,
    detail: "Confirm target SharePoint sites, libraries, permissions, and destination readiness before planning migration waves."
  },
  {
    title: "Discover and assess",
    icon: Database,
    detail: "Run discovery to inventory files, folders, permissions, metadata, long paths, large files, and risk findings. Review readiness before planning migration waves."
  },
  {
    title: "Plan the wave",
    icon: FileText,
    detail: "Generate a migration plan, review the runbook, check blocked items, and export reports for stakeholder sign-off."
  },
  {
    title: "Prepare migration execution",
    icon: Rocket,
    detail: "Review migration execution controls and pilot safety limits. Do not trigger a real migration unless it is explicitly approved."
  },
  {
    title: "Monitor a controlled pilot",
    icon: CheckCircle2,
    detail: "Start with a small capped pilot. Confirm file counts, bytes, logs, retries, and target-side verification before increasing scale."
  },
  {
    title: "Validate and report",
    icon: CheckCircle2,
    detail: "Use Validate and Reports to confirm source-target counts, status, validation results, and exportable evidence."
  },
  {
    title: "Use AI carefully",
    icon: Bot,
    detail: "AI Advisor should explain risks and next actions. Treat recommendations as guidance until the backend explicitly reports real AI availability."
  }
];

const limitations = [
  "Empty-folder preservation is not fully certified.",
  "1,000-file migration is pending.",
  "Subscription is not implemented.",
  "Additional connectors are roadmap."
];

export function V2Tutorial({ runtime, snapshot, onRestartTour }: V2TutorialProps): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Help & Learning Center"
        title="Learn ZMS"
        description="Guided onboarding, reviewer instructions, migration workflow, and known limitations for the final demo workspace."
        actions={(
          <button type="button" className="zms-v2-action" onClick={onRestartTour}>
            Start guided tour
          </button>
        )}
      />

      <div className="zms-v2-grid">
        <V2Card title="Start guided tour" className="zms-v2-span-4">
          <LifeBuoy size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Restart the first-time onboarding overlay to walk through Command Center, Sources, Assess, Plan, Monitor, Validate, Reports, AI Advisor, and Governance.
          </p>
          <div style={{ marginTop: 16 }}>
            <button type="button" className="zms-v2-action" onClick={onRestartTour}>
              Restart guided tour
            </button>
          </div>
        </V2Card>

        <V2Card title="How ZMS works" className="zms-v2-span-4">
          <BookOpen size={28} color="var(--v2-success)" />
          <p className="zms-v2-copy">
            ZMS is a migration control plane. The safe path is connect, discover, assess, plan, pilot, validate, and then scale. Do not run a live migration into a production target until the backend, credentials, reports, and recovery checks are healthy.
          </p>
        </V2Card>

        <V2Card title="Reviewer guide" className="zms-v2-span-4">
          <ListChecks size={28} color="var(--v2-warning)" />
          <p className="zms-v2-copy">
            Review the dashboard first, then open Monitor, Validate, Reports, AI Advisor, Governance, and this Help Center. Use read-only navigation for review unless a safe simulation is explicitly approved.
          </p>
        </V2Card>

        <V2Card title="Login and logout" className="zms-v2-span-4">
          <LogOut size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            Log in from `/login` with the reviewer account shared separately. When finished, use the Log out button in the top bar to end the Supabase session and return to the login screen.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Protected workspace</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Runtime context" className="zms-v2-span-4">
          <div style={{ marginTop: 16 }}>
            <V2EvidenceRow label="Backend runtime" value={runtime.apiStatus} tone={runtime.apiStatus === "Healthy" ? "success" : "warning"} />
            <V2EvidenceRow label="Live data source" value={snapshot.source === "api" ? "Live API records" : "No live records shown"} tone={snapshot.source === "api" ? "success" : "warning"} />
            <V2EvidenceRow label="Database startup" value={runtime.databaseStartupStatus ?? "Not reported"} tone={runtime.apiStatus === "Healthy" ? "success" : "warning"} />
          </div>
        </V2Card>

        <V2Card title="Current safety rules" className="zms-v2-span-4">
          <ul className="zms-v2-list">
            <li><ShieldCheck size={16} /> Rotate exposed credentials before any company review.</li>
            <li><ShieldCheck size={16} /> Keep backend secrets out of Vercel and frontend source.</li>
            <li><ShieldCheck size={16} /> Use a fresh SharePoint target for pilot and scale tests.</li>
            <li><ShieldCheck size={16} /> Record evidence only after `/api/version`, `/api/status`, and `/api/health` are reachable.</li>
          </ul>
        </V2Card>

        <V2Card title="Migration workflow" className="zms-v2-span-12">
          <p className="zms-v2-copy">
            The workflow below is the safe operating order for demo review and future migration pilots.
          </p>
        </V2Card>

        {workflowSteps.map((step, index) => {
          const Icon = step.icon;
          return (
            <V2Card title={`${index + 1}. ${step.title}`} className="zms-v2-span-4" key={step.title}>
              <Icon size={28} color="var(--v2-primary)" />
              <p className="zms-v2-copy">{step.detail}</p>
              <div style={{ marginTop: 16 }}>
                <V2StatusPill tone="neutral">Workflow step</V2StatusPill>
              </div>
            </V2Card>
          );
        })}

        <V2Card title="Known limitations" className="zms-v2-span-6">
          <ul className="zms-v2-list">
            {limitations.map((limitation) => (
              <li key={limitation}><ShieldCheck size={16} color="var(--v2-warning)" /> {limitation}</li>
            ))}
          </ul>
        </V2Card>

        <V2Card title="How dashboard data works" className="zms-v2-span-6">
          <p className="zms-v2-copy">
            Live counts come only from the deployed API. If the API is unavailable or degraded, ZMS shows empty live-data states instead of mock records. Prior migration evidence remains visible only as historical validation evidence, so users know what was proven before and what is happening now.
          </p>
        </V2Card>
      </div>
    </>
  );
}
