import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { V2OnboardingTour, type V2TourStep, zmsOnboardingStorageKey } from "./components/V2OnboardingTour";
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

const tourSteps: V2TourStep[] = [
  {
    title: "Command Center",
    targetPage: "command-center",
    description: "This is your main dashboard. It shows migration health, readiness, validation, and latest activity."
  },
  {
    title: "Sources",
    targetPage: "sources",
    description: "Connect and review source platforms like Google Drive, SharePoint, and file shares."
  },
  {
    title: "Assess",
    targetPage: "assess",
    description: "Run discovery and risk analysis to understand blockers before migration."
  },
  {
    title: "Plan",
    targetPage: "plan",
    description: "Create migration waves, checklists, approvals, and runbooks."
  },
  {
    title: "Migrate / Monitor",
    targetPage: "monitor",
    description: "Track live migration jobs, queue status, failed items, and retry behavior."
  },
  {
    title: "Validate",
    targetPage: "validate",
    description: "Compare source and target files after migration to verify counts, size, and status."
  },
  {
    title: "Reports",
    targetPage: "reports",
    description: "Download executive, technical, validation, and readiness reports."
  },
  {
    title: "AI Advisor",
    targetPage: "ai-advisor",
    description: "Review AI-assisted or rule-based recommendations for risks, remediation, and migration planning."
  },
  {
    title: "Governance",
    targetPage: "governance",
    description: "Review oversharing, external access, Copilot readiness, and governance risks."
  },
  {
    title: "Account and Logout",
    targetPage: "settings",
    description: "Your session is protected by login. Use the Log out action in the top bar when you finish reviewing or sharing a device."
  },
  {
    title: "Finish",
    targetPage: "tutorial",
    description: "You are ready to use ZMS. You can restart this tour anytime from Tutorial or Settings."
  }
];

function renderPage(
  page: V2PageId,
  runtime: V2RuntimeStatus,
  snapshot: V2ReadOnlySnapshot,
  onRestartTour: () => void
): JSX.Element {
  switch (page) {
    case "command-center":
      return <V2CommandCenter runtime={runtime} snapshot={snapshot} />;
    case "tutorial":
      return <V2Tutorial runtime={runtime} snapshot={snapshot} onRestartTour={onRestartTour} />;
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
      return <V2Settings onRestartTour={onRestartTour} />;
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
  const [tourMode, setTourMode] = useState<"hidden" | "welcome" | "tour">("hidden");
  const [tourStepIndex, setTourStepIndex] = useState(0);
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
    try {
      if (window.localStorage.getItem(zmsOnboardingStorageKey) !== "true") {
        setTourMode("welcome");
      }
    } catch {
      setTourMode("welcome");
    }
  }, []);

  useEffect(() => {
    setActivePage(pageFromPath(location.pathname));
  }, [location.pathname]);

  const navigateV2 = (page: V2PageId) => {
    setActivePage(page);
    navigate(`/v2/${page}`);
  };

  const navigateToTourStep = (stepIndex: number) => {
    const boundedIndex = Math.max(0, Math.min(stepIndex, tourSteps.length - 1));
    setTourStepIndex(boundedIndex);
    navigateV2(tourSteps[boundedIndex].targetPage);
  };

  const startTour = () => {
    setTourMode("tour");
    navigateToTourStep(0);
  };

  const completeTour = () => {
    try {
      window.localStorage.setItem(zmsOnboardingStorageKey, "true");
    } catch {
      // If storage is unavailable, hiding the tour still keeps the current session usable.
    }
    setTourMode("hidden");
  };

  const restartTour = () => {
    try {
      window.localStorage.removeItem(zmsOnboardingStorageKey);
    } catch {
      // Storage can be unavailable in private modes; the UI-only tour can still run.
    }
    setTourMode("tour");
    navigateToTourStep(0);
  };

  const tourTargetPage = tourMode === "tour" ? tourSteps[tourStepIndex]?.targetPage : null;

  return (
    <div className="zms-v2-root">
      <div className="zms-v2-shell">
        <V2Sidebar activePage={activePage} tourTargetPage={tourTargetPage} onNavigate={navigateV2} />
        <div className="zms-v2-main">
          <V2TopBar activePage={activePage} runtime={runtime} />
          <main className="zms-v2-content">
            {loadError ? (
              <div className="zms-v2-alert" role="status">
                <strong>Backend status unavailable.</strong>
                <span>{loadError}</span>
              </div>
            ) : null}
            {renderPage(activePage, runtime, snapshot, restartTour)}
          </main>
        </div>
      </div>
      {tourMode !== "hidden" ? (
        <V2OnboardingTour
          mode={tourMode}
          currentStepIndex={tourStepIndex}
          steps={tourSteps}
          onStart={startTour}
          onSkip={completeTour}
          onBack={() => navigateToTourStep(tourStepIndex - 1)}
          onNext={() => navigateToTourStep(tourStepIndex + 1)}
          onFinish={completeTour}
        />
      ) : null}
    </div>
  );
}
