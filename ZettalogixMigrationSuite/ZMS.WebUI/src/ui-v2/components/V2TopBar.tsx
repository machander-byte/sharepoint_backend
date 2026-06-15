import { Bell, LogOut, Search, ShieldCheck } from "lucide-react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../../hooks/useAuth";
import { type V2PageId, v2Pages } from "../data/v2DashboardData";
import type { V2RuntimeStatus } from "../data/v2ReadOnlyAdapter";
import { V2StatusPill } from "./V2Primitives";

interface V2TopBarProps {
  activePage: V2PageId;
  runtime: V2RuntimeStatus;
}

export function V2TopBar({ activePage, runtime }: V2TopBarProps): JSX.Element {
  const navigate = useNavigate();
  const { signOut } = useAuth();
  const title = v2Pages.find((page) => page.id === activePage)?.label ?? "Command Center";
  const tone = runtime.apiStatus === "Healthy" ? "success" : runtime.apiStatus === "Unavailable" || runtime.apiStatus === "Degraded" ? "warning" : "neutral";

  const logOut = async () => {
    await signOut();
    navigate("/login", { replace: true });
  };

  return (
    <header className="zms-v2-topbar">
      <div>
        <div className="zms-v2-eyebrow">UI V2 Preview</div>
        <strong>{title}</strong>
      </div>

      <div className="zms-v2-actions" aria-label="Runtime status">
        <V2StatusPill tone={tone}>{runtime.apiStatus}</V2StatusPill>
        {runtime.queueStatus ? <V2StatusPill tone="success">{runtime.queueStatus}</V2StatusPill> : null}
        {runtime.version ? <V2StatusPill>API {runtime.version}</V2StatusPill> : null}
        <button className="zms-v2-action" type="button" aria-label="Search preview">
          <Search size={15} /> Search
        </button>
        <button className="zms-v2-action" type="button" aria-label="Notifications preview">
          <Bell size={15} /> Alerts
        </button>
        <button className="zms-v2-action" type="button" aria-label="Safety limits">
          <ShieldCheck size={15} /> Safety limits
        </button>
        <button className="zms-v2-action" type="button" aria-label="Log out" onClick={() => void logOut()}>
          <LogOut size={15} /> Log out
        </button>
      </div>
    </header>
  );
}
