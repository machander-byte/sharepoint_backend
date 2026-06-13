import {
  Bot,
  BriefcaseBusiness,
  ChartNoAxesColumn,
  CloudCog,
  Archive,
  Gauge,
  LayoutDashboard,
  ListChecks,
  MonitorCheck,
  NotebookTabs,
  Route,
  SearchCheck,
  Settings,
  ShieldCheck,
  Tags,
  Users,
  ClipboardCheck,
  CircleHelp,
  WandSparkles,
  X
} from "lucide-react";
import { NavLink, useLocation } from "react-router-dom";
import { cn } from "../lib/utils";

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
}

const navItems = [
  { label: "Dashboard", path: "/dashboard", icon: LayoutDashboard },
  { label: "Environment", path: "/environment", icon: Route },
  { label: "Connections", path: "/connections", icon: CloudCog },
  { label: "Discovery", path: "/discovery", icon: SearchCheck },
  { label: "Planner", path: "/planner", icon: NotebookTabs },
  { label: "Operator", path: "/operator", icon: MonitorCheck },
  { label: "Permissions", path: "/permissions", icon: ShieldCheck },
  { label: "Metadata", path: "/metadata", icon: Tags },
  { label: "Modernization", path: "/modernization", icon: WandSparkles },
  { label: "Copilot Readiness", path: "/copilot-readiness", icon: ShieldCheck },
  { label: "Teams", path: "/teams", icon: Users },
  { label: "Live Migrations", path: "/migrations", icon: BriefcaseBusiness },
  { label: "Simulation Jobs", path: "/jobs", icon: ListChecks },
  { label: "Validation", path: "/validation", icon: ClipboardCheck },
  { label: "Packages", path: "/packages", icon: Archive },
  { label: "Reports", path: "/reports", icon: ChartNoAxesColumn },
  { label: "AI", path: "/ai", icon: Bot },
  { label: "Help", path: "/help", icon: CircleHelp },
  { label: "Settings", path: "/settings", icon: Settings }
];

export default function Sidebar({ isOpen, onClose }: SidebarProps): JSX.Element {
  const location = useLocation();

  return (
    <>
      <div
        className={cn(
          "fixed inset-0 z-40 bg-slate-950/30 transition-opacity md:hidden",
          isOpen ? "opacity-100" : "pointer-events-none opacity-0"
        )}
        onClick={onClose}
      />
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-50 flex w-[280px] flex-col border-r border-border bg-surface shadow-panel transition-transform md:translate-x-0",
          isOpen ? "translate-x-0" : "-translate-x-full"
        )}
      >
        <div className="flex items-start justify-between border-b border-border px-5 py-5">
          <div>
            <div className="flex items-center gap-3">
              <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary text-sm font-bold text-white shadow-card">
                ZMS
              </div>
              <div>
                <p className="text-base font-bold text-primary">zettalogixmigrationsuite</p>
                <p className="text-xs font-medium text-text-muted">Enterprise migration platform</p>
              </div>
            </div>
            <div className="mt-4 rounded-xl border border-border bg-surface-container p-3">
              <div className="flex items-center gap-3">
                <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary-soft text-sm font-bold text-primary">
                  SA
                </div>
                <div>
                  <p className="text-sm font-semibold text-text-primary">zettalogixmigrationsuite</p>
                  <p className="text-xs text-text-muted">Migration Lead</p>
                </div>
              </div>
              <div className="mt-3 flex items-center justify-between border-t border-border pt-3 text-xs">
                <span className="font-semibold uppercase tracking-wide text-text-subtle">Version</span>
                <span className="font-mono text-text-muted">v4.2.0</span>
              </div>
            </div>
          </div>
          <button
            type="button"
            className="rounded-lg p-2 text-text-muted hover:bg-surface-container md:hidden"
            aria-label="Close navigation"
            onClick={onClose}
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <nav className="flex-1 overflow-y-auto px-3 py-4">
          <div className="flex flex-col gap-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const active =
                item.path === "/environment" || item.path === "/migrations"
                  ? location.pathname.startsWith(item.path)
                  : location.pathname === item.path;

              return (
                <NavLink
                  key={item.path}
                  to={item.path}
                  onClick={onClose}
                  className={cn(
                    "flex items-center gap-3 rounded-lg border-l-4 border-transparent px-3 py-2.5 text-sm font-semibold text-text-muted transition",
                    "hover:bg-surface-container hover:text-text-primary",
                    active && "border-l-primary bg-primary-soft/55 text-primary"
                  )}
                >
                  <Icon className="h-5 w-5" />
                  <span>{item.label}</span>
                </NavLink>
              );
            })}
          </div>
        </nav>

        <div className="border-t border-border p-4">
          <div className="rounded-xl border border-border bg-surface-container p-4">
            <div className="mb-2 flex items-center gap-2 text-primary">
              <Gauge className="h-4 w-4" />
              <span className="text-xs font-bold uppercase tracking-wide">Readiness</span>
            </div>
            <p className="text-sm font-semibold text-text-primary">78% migration-ready</p>
            <p className="mt-1 text-xs leading-5 text-text-muted">
              Static pilot workspace using realistic enterprise migration data.
            </p>
          </div>
        </div>
      </aside>
    </>
  );
}
