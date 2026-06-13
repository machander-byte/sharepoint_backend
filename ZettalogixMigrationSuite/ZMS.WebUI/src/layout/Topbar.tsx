import { Bell, LogOut, Menu, Search } from "lucide-react";
import { useMemo } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

interface TopbarProps {
  onMenuClick: () => void;
}

const routeLabels: Array<[string, string]> = [
  ["/dashboard", "Migration Command Center"],
  ["/environment", "Environment"],
  ["/connections", "Connections"],
  ["/discovery", "Discovery"],
  ["/planner", "Planner"],
  ["/permissions", "Permissions"],
  ["/metadata", "Metadata"],
  ["/modernization", "Modernization"],
  ["/copilot-readiness", "Copilot Readiness"],
  ["/teams", "Teams Discovery"],
  ["/migrations", "Live Migrations"],
  ["/jobs", "Simulation Jobs"],
  ["/validation", "Validation"],
  ["/packages", "Environment Packages"],
  ["/reports", "Reports"],
  ["/ai", "AI Recommendations"],
  ["/help", "Help Center"],
  ["/settings", "Settings"]
];

export default function Topbar({ onMenuClick }: TopbarProps): JSX.Element {
  const location = useLocation();
  const navigate = useNavigate();
  const { signOut, user } = useAuth();
  const title = useMemo(() => {
    const match = routeLabels.find(([path]) => location.pathname.startsWith(path));
    return match?.[1] ?? "zettalogixmigrationsuite";
  }, [location.pathname]);
  const userEmail = user?.email ?? "Authenticated user";
  const initials = userEmail
    .split("@")[0]
    .split(/[._\-\s]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("") || "SA";

  const handleSignOut = async () => {
    await signOut();
    navigate("/login", { replace: true });
  };

  return (
    <header className="fixed left-0 right-0 top-0 z-30 border-b border-border bg-surface/95 backdrop-blur md:left-[280px]">
      <div className="flex h-16 items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
        <div className="flex min-w-0 items-center gap-3">
          <button
            type="button"
            className="rounded-lg p-2 text-text-muted hover:bg-surface-container md:hidden"
            aria-label="Open navigation"
            onClick={onMenuClick}
          >
            <Menu className="h-5 w-5" />
          </button>
          <div className="min-w-0">
            <p className="truncate text-sm font-bold text-primary sm:hidden">ZMS</p>
            <p className="hidden truncate text-sm font-bold text-primary sm:block">zettalogixmigrationsuite</p>
            <p className="hidden text-xs text-text-muted md:block">{title}</p>
          </div>
        </div>

        <div className="hidden w-full max-w-md items-center gap-2 rounded-xl border border-border bg-surface-container px-3 py-2 md:flex">
          <Search className="h-4 w-4 text-text-muted" />
          <input
            className="w-full bg-transparent text-sm text-text-primary placeholder:text-text-subtle"
            placeholder="Search sites, reports, jobs"
            type="search"
          />
        </div>

        <div className="flex items-center gap-2 sm:gap-3">
          <span className="hidden rounded-full border border-border bg-surface-container px-3 py-1 text-xs font-semibold text-text-muted xl:inline-flex">
            {userEmail}
          </span>
          <button
            type="button"
            className="relative rounded-lg border border-border bg-surface p-2 text-text-muted shadow-card hover:bg-surface-container"
            aria-label="Notifications"
          >
            <Bell className="h-5 w-5" />
            <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-error" />
          </button>
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-sm font-bold text-white">
            {initials}
          </div>
          <button
            type="button"
            className="rounded-lg border border-border bg-surface p-2 text-text-muted shadow-card hover:bg-surface-container"
            aria-label="Sign out"
            title="Sign out"
            onClick={() => void handleSignOut()}
          >
            <LogOut className="h-5 w-5" />
          </button>
        </div>
      </div>
    </header>
  );
}
