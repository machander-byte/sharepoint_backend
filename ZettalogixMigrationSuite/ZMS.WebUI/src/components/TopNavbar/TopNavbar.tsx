import { ChangeEvent, KeyboardEvent, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { pageTitles } from "../../utils/constants";
import { useAuth } from "../../hooks/useAuth";
import { useAppStore } from "../../hooks/useAppStore";
import { formatConnectionType, formatJobStatus } from "../../utils/formatters";
import styles from "./TopNavbar.module.css";

interface SearchResult {
  id: string;
  icon: string;
  label: string;
  meta: string;
  path: string;
}

const staticResults: SearchResult[] = [
  {
    id: "help-google",
    icon: "folder",
    label: "Google Drive requirements",
    meta: "Help Center setup checklist",
    path: "/help#external-resources"
  },
  {
    id: "help-sharepoint",
    icon: "cloud_upload",
    label: "SharePoint Online requirements",
    meta: "Help Center setup checklist",
    path: "/help#external-resources"
  },
  {
    id: "help-errors",
    icon: "troubleshoot",
    label: "Migration error fixes",
    meta: "Help Center error resolution",
    path: "/help"
  },
  {
    id: "new-connection",
    icon: "hub",
    label: "Connections",
    meta: "Add and test source or target endpoints",
    path: "/connections"
  },
  {
    id: "migration-jobs",
    icon: "moving",
    label: "Migration Jobs",
    meta: "Create, start, and monitor jobs",
    path: "/migrations"
  }
];

function matchesSearch(query: string, values: Array<string | undefined>): boolean {
  return values.some((value) => value?.toLowerCase().includes(query));
}

export default function TopNavbar(): JSX.Element {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, signOut } = useAuth();
  const jobs = useAppStore((state) => state.jobs);
  const connections = useAppStore((state) => state.connections);
  const resetSessionData = useAppStore((state) => state.resetSessionData);
  const [searchTerm, setSearchTerm] = useState("");
  const [searchOpen, setSearchOpen] = useState(false);

  const titleConfig = location.pathname.startsWith("/migrations/")
    ? {
        title: "Active Migration",
        subtitle: "Inspect throughput, execution history, and intervention controls for the selected pipeline."
      }
    : pageTitles[location.pathname] ?? pageTitles["/dashboard"];

  const activeCount = jobs.filter((job) => job.status === "Running").length;
  const failedCount = jobs.filter((job) => job.status === "Failed").length;
  const fullName = typeof user?.user_metadata?.full_name === "string" ? user.user_metadata.full_name : "";
  const displayName = fullName || user?.email || "ZMS User";
  const userEmail = user?.email ?? "Authenticated user";
  const initials = displayName
    .split(/\s|@/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part: string) => part[0]?.toUpperCase())
    .join("") || "ZU";
  const normalizedSearch = searchTerm.trim().toLowerCase();
  const searchResults = useMemo<SearchResult[]>(() => {
    if (normalizedSearch.length < 2) {
      return [];
    }

    const jobResults = jobs
      .filter((job) =>
        matchesSearch(normalizedSearch, [
          job.name,
          job.sourcePath,
          job.targetSite,
          job.targetLibrary,
          job.status
        ])
      )
      .slice(0, 5)
      .map<SearchResult>((job) => ({
        id: `job-${job.id}`,
        icon: "moving",
        label: job.name,
        meta: `${formatJobStatus(job.status)} | ${job.targetLibrary || job.targetSite}`,
        path: `/migrations/${job.id}`
      }));

    const connectionResults = connections
      .filter((connection) =>
        matchesSearch(normalizedSearch, [
          connection.name,
          connection.url,
          connection.rootPath,
          connection.documentLibraryName,
          connection.type,
          connection.status
        ])
      )
      .slice(0, 5)
      .map<SearchResult>((connection) => ({
        id: `connection-${connection.id}`,
        icon: "hub",
        label: connection.name,
        meta: `${formatConnectionType(connection.type)} | ${connection.status}`,
        path: "/connections"
      }));

    const helpResults = staticResults.filter((result) =>
      matchesSearch(normalizedSearch, [result.label, result.meta])
    );

    return [...jobResults, ...connectionResults, ...helpResults].slice(0, 8);
  }, [connections, jobs, normalizedSearch]);

  const goToResult = (result: SearchResult) => {
    setSearchTerm("");
    setSearchOpen(false);
    navigate(result.path);
  };

  const updateSearch = (event: ChangeEvent<HTMLInputElement>) => {
    setSearchTerm(event.target.value);
    setSearchOpen(true);
  };

  const handleSearchKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "Enter" && searchResults.length > 0) {
      event.preventDefault();
      goToResult(searchResults[0]);
    }

    if (event.key === "Escape") {
      setSearchOpen(false);
    }
  };

  const handleSignOut = async () => {
    resetSessionData();
    await signOut();
    navigate("/login", { replace: true });
  };

  return (
    <header className={`${styles.navbar} glass-card`}>
      <div className={styles.titleBlock}>
        <span className="eyebrow">Migration Control Plane</span>
        <h1>{titleConfig.title}</h1>
        <p>{titleConfig.subtitle}</p>
      </div>

      <div className={styles.utilityArea}>
        <label className={styles.search}>
          <span className="material-symbols-outlined">search</span>
          <input
            type="text"
            placeholder="Search jobs, sites, libraries"
            value={searchTerm}
            onChange={updateSearch}
            onFocus={() => setSearchOpen(true)}
            onBlur={() => window.setTimeout(() => setSearchOpen(false), 140)}
            onKeyDown={handleSearchKeyDown}
          />
          {searchTerm ? (
            <button
              type="button"
              className={styles.clearSearch}
              aria-label="Clear search"
              onMouseDown={(event) => event.preventDefault()}
              onClick={() => setSearchTerm("")}
            >
              <span className="material-symbols-outlined">close</span>
            </button>
          ) : null}
          {searchOpen && normalizedSearch.length >= 2 ? (
            <div className={styles.searchResults}>
              {searchResults.length > 0 ? (
                searchResults.map((result) => (
                  <button key={result.id} type="button" onMouseDown={() => goToResult(result)}>
                    <span className="material-symbols-outlined">{result.icon}</span>
                    <span>
                      <strong>{result.label}</strong>
                      <small>{result.meta}</small>
                    </span>
                  </button>
                ))
              ) : (
                <div className={styles.noResults}>No matching jobs, connections, or help topics.</div>
              )}
            </div>
          ) : null}
        </label>

        <div className={styles.pill}>Running {activeCount}</div>
        <div className={styles.pill}>At Risk {failedCount}</div>

        <button type="button" className={styles.iconButton} aria-label="Notifications">
          <span className="material-symbols-outlined">notifications</span>
        </button>

        <div className={styles.profile}>
          <div className={styles.avatar}>{initials}</div>
          <div>
            <strong>{displayName}</strong>
            <span>{userEmail}</span>
          </div>
        </div>

        <button type="button" className={styles.signOutButton} onClick={() => void handleSignOut()}>
          <span className="material-symbols-outlined">logout</span>
          Logout
        </button>
      </div>
    </header>
  );
}
