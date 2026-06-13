import {
  Activity,
  Bot,
  CheckCircle2,
  CloudUpload,
  FileText,
  FolderInput,
  Gauge,
  HardDrive,
  Layers3,
  ListChecks,
  Rocket,
  Settings2,
  ShieldCheck
} from "lucide-react";
import { type V2PageId, v2Pages } from "../data/v2DashboardData";

const pageIcons = {
  "command-center": Gauge,
  sources: CloudUpload,
  destinations: HardDrive,
  assess: ListChecks,
  plan: Layers3,
  migrate: Rocket,
  monitor: Activity,
  validate: CheckCircle2,
  reports: FileText,
  "ai-advisor": Bot,
  governance: ShieldCheck,
  settings: Settings2
} satisfies Record<V2PageId, typeof Gauge>;

interface V2SidebarProps {
  activePage: V2PageId;
  onNavigate: (page: V2PageId) => void;
}

export function V2Sidebar({ activePage, onNavigate }: V2SidebarProps): JSX.Element {
  const groups = ["Operate", "Prepare", "Assure"] as const;

  return (
    <aside className="zms-v2-sidebar" aria-label="ZMS UI V2 navigation">
      <div className="zms-v2-brand">
        <strong>Zettalogix</strong>
        <span>Migration Suite V2</span>
      </div>

      {groups.map((group) => (
        <nav className="zms-v2-nav-group" key={group} aria-label={group}>
          <div className="zms-v2-nav-group-label">{group}</div>
          {v2Pages.filter((page) => page.group === group).map((page) => {
            const Icon = pageIcons[page.id];
            const active = activePage === page.id;
            return (
              <button
                className={`zms-v2-nav-button${active ? " is-active" : ""}`}
                key={page.id}
                type="button"
                onClick={() => onNavigate(page.id)}
              >
                <Icon size={17} />
                <span>{page.label}</span>
              </button>
            );
          })}
        </nav>
      ))}

      <div className="zms-v2-sidebar-note">
        <FolderInput size={18} color="var(--v2-success)" />
        <span>Safety posture</span>
        <p className="zms-v2-copy">
          Internal safety limits enabled. Commercial plan controls are not part of this preview.
        </p>
      </div>
    </aside>
  );
}
