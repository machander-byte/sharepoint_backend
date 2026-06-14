import { Bot, CheckCircle2, Database, FileText, FolderInput, Rocket, ShieldCheck } from "lucide-react";
import type { V2ReadOnlySnapshot, V2RuntimeStatus } from "../data/v2ReadOnlyAdapter";
import { V2Card, V2EvidenceRow, V2PageHeader, V2StatusPill } from "../components/V2Primitives";

interface V2TutorialProps {
  runtime: V2RuntimeStatus;
  snapshot: V2ReadOnlySnapshot;
}

const workflowSteps = [
  {
    title: "1. Connect sources and destinations",
    icon: FolderInput,
    detail: "Add Google Drive, SharePoint Online, or file-share connections from the Connections area. Use test tenants first and confirm credentials before running discovery."
  },
  {
    title: "2. Discover and assess",
    icon: Database,
    detail: "Run discovery to inventory files, folders, permissions, metadata, long paths, large files, and risk findings. Review readiness before planning migration waves."
  },
  {
    title: "3. Plan the wave",
    icon: FileText,
    detail: "Generate a migration plan, review the runbook, check blocked items, and export reports for stakeholder sign-off."
  },
  {
    title: "4. Run a controlled pilot",
    icon: Rocket,
    detail: "Start with a small capped pilot. Confirm file counts, bytes, logs, retries, and target-side verification before increasing scale."
  },
  {
    title: "5. Validate and monitor",
    icon: CheckCircle2,
    detail: "Use Monitor, Validate, and Reports to confirm queue status, job state, validation results, and exportable evidence."
  },
  {
    title: "6. Use AI carefully",
    icon: Bot,
    detail: "AI Advisor should explain risks and next actions. Treat recommendations as guidance until the backend explicitly reports real AI availability."
  }
];

export function V2Tutorial({ runtime, snapshot }: V2TutorialProps): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="Tutorial"
        title="How to use ZMS"
        description="A guided workflow for operating Zettalogix Migration Suite without confusing historical evidence with live records."
      />

      <div className="zms-v2-grid">
        <V2Card title="Before you start" className="zms-v2-span-6">
          <p className="zms-v2-copy">
            ZMS is a migration control plane. The safe path is connect, discover, assess, plan, pilot, validate, and then scale. Do not run a live migration into a production target until the backend, credentials, reports, and recovery checks are healthy.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2EvidenceRow label="Backend runtime" value={runtime.apiStatus} tone={runtime.apiStatus === "Healthy" ? "success" : "warning"} />
            <V2EvidenceRow label="Live data source" value={snapshot.source === "api" ? "Live API records" : "No live records shown"} tone={snapshot.source === "api" ? "success" : "warning"} />
            <V2EvidenceRow label="Database startup" value={runtime.databaseStartupStatus ?? "Not reported"} tone={runtime.apiStatus === "Healthy" ? "success" : "warning"} />
          </div>
        </V2Card>

        <V2Card title="Current safety rules" className="zms-v2-span-6">
          <ul className="zms-v2-list">
            <li><ShieldCheck size={16} /> Rotate exposed credentials before any company review.</li>
            <li><ShieldCheck size={16} /> Keep backend secrets out of Vercel and frontend source.</li>
            <li><ShieldCheck size={16} /> Use a fresh SharePoint target for pilot and scale tests.</li>
            <li><ShieldCheck size={16} /> Record evidence only after `/api/version`, `/api/status`, and `/api/health` are reachable.</li>
          </ul>
        </V2Card>

        {workflowSteps.map((step) => {
          const Icon = step.icon;
          return (
            <V2Card title={step.title} className="zms-v2-span-4" key={step.title}>
              <Icon size={28} color="var(--v2-primary)" />
              <p className="zms-v2-copy">{step.detail}</p>
              <div style={{ marginTop: 16 }}>
                <V2StatusPill tone="neutral">Guided step</V2StatusPill>
              </div>
            </V2Card>
          );
        })}

        <V2Card title="How the dashboard data works" className="zms-v2-span-12">
          <p className="zms-v2-copy">
            Live counts come only from the deployed API. If the API is unavailable or degraded, ZMS now shows empty live-data states instead of mock records. Prior migration evidence remains visible only as historical validation evidence, so users know what was proven before and what is happening now.
          </p>
        </V2Card>
      </div>
    </>
  );
}
