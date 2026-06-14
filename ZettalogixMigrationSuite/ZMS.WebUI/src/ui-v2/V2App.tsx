import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { V2Sidebar } from "./components/V2Sidebar";
import { V2TopBar } from "./components/V2TopBar";
import { type V2PageId, v2Pages } from "./data/v2DashboardData";
import { getFallbackV2Snapshot, loadV2ReadOnlySnapshot, type V2ReadOnlySnapshot, type V2RuntimeStatus } from "./data/v2ReadOnlyAdapter";
import { V2AIAdvisor } from "./pages/V2AIAdvisor";
import { V2Assess } from "./pages/V2Assess";
import { V2CommandCenter } from "./pages/V2CommandCenter";
import { V2Destinations } from "./pages/V2Destinations";
import { V2Governance } from "./pages/V2Governance";
import { V2Migrate } from "./pages/V2Migrate";
import { V2Monitor } from "./pages/V2Monitor";
import { V2Plan } from "./pages/V2Plan";
import { V2Reports } from "./pages/V2Reports";
import { V2Settings } from "./pages/V2Settings";
import { V2Sources } from "./pages/V2Sources";
import { V2Tutorial } from "./pages/V2Tutorial";
import { V2Validate } from "./pages/V2Validate";
import "./styles/v2-theme.css";

const validPageIds = new Set<V2PageId>(v2Pages.map((page) => page.id));
const fallbackSnapshot = getFallbackV2Snapshot();

function renderPage(page: V2PageId, runtime: V2RuntimeStatus, snapshot: V2ReadOnlySnapshot): JSX.Element {
  switch (page) {
    case "command-center":
      return <V2CommandCenter runtime={runtime} snapshot={snapshot} />;
    case "tutorial":
      return <V2Tutorial runtime={runtime} snapshot={snapshot} />;
    case "sources":
      return <V2Sources />;
    case "destinations":
      return <V2Destinations />;
    case "assess":
      return <V2Assess />;
    case "plan":
      return <V2Plan />;
    case "migrate":
      return <V2Migrate />;
    case "monitor":
      return <V2Monitor runtime={runtime} snapshot={snapshot} />;
    case "validate":
      return <V2Validate />;
    case "reports":
      return <V2Reports />;
    case "ai-advisor":
      return <V2AIAdvisor />;
    case "governance":
      return <V2Governance />;
    case "settings":
      return <V2Settings />;
    default:
      return <V2CommandCenter runtime={runtime} snapshot={snapshot} />;
  }
}

function pageFromPath(pathname: string): V2PageId {
  const segment = pathname.replace(/\/+$/, "").split("/").filter(Boolean)[1];
  return segment && validPageIds.has(segment as V2PageId) ? segment as V2PageId : "command-center";
}

export default function V2App(): JSX.Element {
  const location = useLocation();
  const navigate = useNavigate();
  const [activePage, setActivePage] = useState<V2PageId>(() => pageFromPath(location.pathname));
  const [snapshot, setSnapshot] = useState<V2ReadOnlySnapshot>(fallbackSnapshot);
  const [loadError, setLoadError] = useState<string | null>(null);
  const runtime = snapshot.runtime;

  useEffect(() => {
    let cancelled = false;

    async function loadSnapshot(): Promise<void> {
      try {
        const nextSnapshot = await loadV2ReadOnlySnapshot();
        if (!cancelled) {
          setSnapshot(nextSnapshot);
          setLoadError(null);
        }
      } catch {
        if (!cancelled) {
          setSnapshot({
            ...fallbackSnapshot,
            errors: ["Runtime status could not load. No fallback records are shown as real data."]
          });
          setLoadError("Runtime status could not load. You can still navigate the review UI, but live API data is unavailable.");
        }
      }
    }

    void loadSnapshot();

    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    setActivePage(pageFromPath(location.pathname));
  }, [location.pathname]);

  const navigateV2 = (page: V2PageId) => {
    setActivePage(page);
    navigate(`/v2/${page}`);
  };

  return (
    <div className="zms-v2-root">
      <div className="zms-v2-shell">
        <V2Sidebar activePage={activePage} onNavigate={navigateV2} />
        <div className="zms-v2-main">
          <V2TopBar activePage={activePage} runtime={runtime} />
          <main className="zms-v2-content">
            {loadError ? (
              <div className="zms-v2-alert" role="status">
                <strong>Backend status unavailable.</strong>
                <span>{loadError}</span>
              </div>
            ) : null}
            {renderPage(activePage, runtime, snapshot)}
          </main>
        </div>
      </div>
    </div>
  );
}
