import { Download, Filter, Play, Search, Terminal, Upload } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import DataTable from "../components/DataTable";
import EmptyState from "../components/EmptyState";
import PageHeader from "../components/PageHeader";
import RiskBadge from "../components/RiskBadge";
import StatCard from "../components/StatCard";
import { siteCollections } from "../data/zmsMockData";
import { zmsApi } from "../services/zmsApi";
import { useZmsDispatch, useZmsState } from "../state/ZmsStateProvider";
import { toastActions } from "../state/toastActions";
import { DiscoveryScanRequest } from "../types/zms";
import { generateEnvironmentConfig } from "../utils/generateEnvironmentConfig";

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => window.setTimeout(resolve, ms));
}

function splitSiteUrls(value: string): string[] {
  return value
    .split(/\r?\n|,/)
    .map((url) => url.trim())
    .filter(Boolean);
}

function formatBytes(value: number): string {
  if (value <= 0) return "0 GB";
  return `${(value / 1024 / 1024 / 1024).toFixed(2)} GB`;
}

export default function DiscoveryPage(): JSX.Element {
  const state = useZmsState();
  const dispatch = useZmsDispatch();
  const fallbackConfig = useMemo(
    () =>
      state.generatedEnvironmentConfig ??
      generateEnvironmentConfig(
        siteCollections.filter((site) => state.selectedSiteCollectionIds.includes(site.id)),
        state.builderOptions,
        state.tenantValues
      ),
    [state.builderOptions, state.generatedEnvironmentConfig, state.selectedSiteCollectionIds, state.tenantValues]
  );

  const [searchTerm, setSearchTerm] = useState("");
  const [scanName, setScanName] = useState("ZMS Test Environment Discovery");
  const [mode, setMode] = useState<DiscoveryScanRequest["mode"]>("config");
  const [tenantUrl, setTenantUrl] = useState(state.tenantValues.rootUrl);
  const [adminUrl, setAdminUrl] = useState(state.tenantValues.adminUrl);
  const [clientId, setClientId] = useState(state.tenantValues.clientIdPlaceholder || "PNP_CLIENT_ID_PLACEHOLDER");
  const [siteUrlsText, setSiteUrlsText] = useState(fallbackConfig.siteCollections.map((site) => site.url).join("\n"));
  const [includeFiles, setIncludeFiles] = useState(true);
  const [includePermissions, setIncludePermissions] = useState(true);
  const [includeMetadata, setIncludeMetadata] = useState(true);
  const [includeSubsites, setIncludeSubsites] = useState(true);
  const [currentStep, setCurrentStep] = useState("Idle");
  const [importFile, setImportFile] = useState<File | null>(null);
  const [isImporting, setIsImporting] = useState(false);
  const [isAnalyzingReadiness, setIsAnalyzingReadiness] = useState(false);
  const [showDiscoveryCommand, setShowDiscoveryCommand] = useState(false);
  const result = state.discovery.result;

  useEffect(() => {
    let cancelled = false;
    zmsApi.getLatestDiscoveryResults().then((latest) => {
      if (!cancelled && latest) {
        dispatch({ type: "SET_DISCOVERY_RESULT", payload: latest });
        setCurrentStep(latest.status === "partial" ? "Latest discovery has partial results" : "Latest discovery loaded");
      }
    });

    return () => {
      cancelled = true;
    };
  }, [dispatch]);
  const readOnlyDiscoveryCommand = `pwsh ./scripts/11-Run-Discovery-ReadOnly.ps1 \`
  -ConfigPath "./config/zms-spo-environment.json" \`
  -ClientId "YOUR-PNP-APP-CLIENT-ID" \`
  -OutputPath "./discovery-output" \`
  -IncludeFiles \`
  -IncludePermissions \`
  -IncludeMetadata \`
  -IncludeSubsites \`
  -VerboseLogging`;

  const inventoryRows =
    result?.inventoryItems.filter((item) => {
      const query = searchTerm.trim().toLowerCase();
      if (!query) return true;
      return [item.siteCollection, item.subsite, item.library, item.itemType, item.path].some((value) =>
        value.toLowerCase().includes(query)
      );
    }) ?? [];

  const startDiscovery = async () => {
    const siteUrls = splitSiteUrls(siteUrlsText);
    if (siteUrls.length === 0) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.error("Discovery cannot start", "At least one site URL is required.") });
      return;
    }

    const request: DiscoveryScanRequest = {
      scanName,
      mode,
      tenantUrl,
      adminUrl,
      siteUrls,
      clientId,
      includeFiles,
      includePermissions,
      includeMetadata,
      includeSubsites,
      environmentConfigPath: mode === "config" ? "samples/zms-spo-environment-config.sample.json" : undefined
    };

    dispatch({ type: "SET_DISCOVERY_PROGRESS", payload: { status: "running", progress: 0 } });
    setCurrentStep("Starting discovery scan");

    try {
      const start = await zmsApi.startDiscoveryScan(request, fallbackConfig);
      let finalStatus = start.status;
      let progress = finalStatus === "completed" ? 100 : 0;

      for (let attempt = 0; attempt < 120 && !["completed", "failed", "cancelled"].includes(finalStatus); attempt += 1) {
        await sleep(700);
        const status = await zmsApi.getDiscoveryStatus(start.scanId);
        finalStatus = status.status;
        progress = status.progress;
        setCurrentStep(status.currentStep);
        dispatch({
          type: "SET_DISCOVERY_PROGRESS",
          payload: {
            status: finalStatus === "completed" ? "completed" : finalStatus === "failed" || finalStatus === "cancelled" ? "failed" : "running",
            progress
          }
        });
      }

      const scanResult = await zmsApi.getDiscoveryResults(start.scanId, fallbackConfig);
      dispatch({ type: "SET_DISCOVERY_RESULT", payload: scanResult });
      setCurrentStep(scanResult.status === "completed" ? "Discovery scan completed" : "Discovery scan finished");

      if (scanResult.status === "completed") {
        dispatch({
          type: "ADD_TOAST",
          payload: toastActions.success("Discovery scan completed", `${scanResult.summary.libraries} libraries scanned.`)
        });
      } else {
        dispatch({
          type: "ADD_TOAST",
          payload: toastActions.error("Discovery scan failed", scanResult.errors[0] ?? "Scan did not complete.")
        });
      }
    } catch (error) {
      dispatch({ type: "SET_DISCOVERY_PROGRESS", payload: { status: "failed", progress: 0 } });
      dispatch({ type: "ADD_TOAST", payload: toastActions.error("Discovery scan failed", error instanceof Error ? error.message : "Unexpected error.") });
    }
  };

  const exportScan = async (exportType: "csv" | "json" | "permissions.csv" | "metadata.csv" | "risks.csv") => {
    if (!result) return;
    const outcome = await zmsApi.downloadDiscoveryExport(result.scanId, exportType);
    dispatch({
      type: "ADD_TOAST",
      payload: toastActions.success(
        "Discovery export ready",
        outcome.source === "backend" ? "Export downloaded from backend scan storage." : "Export downloaded from local scan data."
      )
    });
  };

  const importLiveDiscovery = async () => {
    if (!importFile) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.error("Import needs a file", "Select scan-result.json first.") });
      return;
    }

    setIsImporting(true);
    try {
      const response = await zmsApi.importDiscoveryResult(importFile);
      const importedResult = await zmsApi.getDiscoveryResults(response.scanId, fallbackConfig);
      dispatch({ type: "SET_DISCOVERY_RESULT", payload: importedResult });
      setCurrentStep("Live discovery result imported");
      dispatch({
        type: "ADD_TOAST",
        payload: toastActions.success("Live discovery result imported successfully.", `${response.summary.libraries} libraries are now available.`)
      });
    } catch (error) {
      dispatch({
        type: "ADD_TOAST",
        payload: toastActions.error("Discovery import failed", error instanceof Error ? error.message : "scan-result.json could not be imported.")
      });
    } finally {
      setIsImporting(false);
    }
  };

  const analyzeReadiness = async () => {
    if (!result) return;
    setIsAnalyzingReadiness(true);
    try {
      const response = await zmsApi.analyzeReadiness(result.scanId);
      dispatch({
        type: "ADD_TOAST",
        payload: toastActions.success("Readiness analysis completed", `${response.summary.blockers} blockers and ${response.summary.remediationActions} remediation actions generated.`)
      });
    } catch (error) {
      dispatch({
        type: "ADD_TOAST",
        payload: toastActions.error("Readiness analysis failed", error instanceof Error ? error.message : "Assessment could not be generated.")
      });
    } finally {
      setIsAnalyzingReadiness(false);
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Discovery Center"
        subtitle="Scan sites, libraries, permissions, metadata, and migration risks."
        actions={
          <button
            type="button"
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90 disabled:opacity-60"
            disabled={state.discovery.status === "running"}
            onClick={() => void startDiscovery()}
          >
            <Play className="h-4 w-4" />
            {state.discovery.status === "running" ? "Scanning..." : "Start Discovery"}
          </button>
        }
      />

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <div className="grid grid-cols-1 gap-4 lg:grid-cols-4">
          <label className="flex flex-col gap-2 text-sm font-semibold text-text-muted">
            Scan Mode
            <select
              className="rounded-lg border border-border bg-surface-container px-3 py-2 text-text-primary"
              value={mode}
              onChange={(event) => setMode(event.target.value as DiscoveryScanRequest["mode"])}
            >
              <option value="config">Config Mode</option>
              <option value="live">Live Mode</option>
            </select>
          </label>
          <label className="flex flex-col gap-2 text-sm font-semibold text-text-muted lg:col-span-2">
            Scan Name
            <input
              className="rounded-lg border border-border bg-surface-container px-3 py-2 text-text-primary"
              value={scanName}
              onChange={(event) => setScanName(event.target.value)}
            />
          </label>
          <label className="flex flex-col gap-2 text-sm font-semibold text-text-muted">
            Client ID
            <input
              className="rounded-lg border border-border bg-surface-container px-3 py-2 text-text-primary"
              value={clientId}
              onChange={(event) => setClientId(event.target.value)}
            />
          </label>
          <label className="flex flex-col gap-2 text-sm font-semibold text-text-muted">
            Tenant URL
            <input
              className="rounded-lg border border-border bg-surface-container px-3 py-2 text-text-primary"
              value={tenantUrl}
              onChange={(event) => setTenantUrl(event.target.value)}
            />
          </label>
          <label className="flex flex-col gap-2 text-sm font-semibold text-text-muted">
            Admin URL
            <input
              className="rounded-lg border border-border bg-surface-container px-3 py-2 text-text-primary"
              value={adminUrl}
              onChange={(event) => setAdminUrl(event.target.value)}
            />
          </label>
          <label className="flex flex-col gap-2 text-sm font-semibold text-text-muted lg:col-span-2">
            Site URLs
            <textarea
              className="min-h-24 rounded-lg border border-border bg-surface-container px-3 py-2 font-mono text-sm text-text-primary"
              value={siteUrlsText}
              onChange={(event) => setSiteUrlsText(event.target.value)}
            />
          </label>
        </div>
        <div className="mt-4 flex flex-wrap gap-4">
          {[
            ["Include files", includeFiles, setIncludeFiles],
            ["Include permissions", includePermissions, setIncludePermissions],
            ["Include metadata", includeMetadata, setIncludeMetadata],
            ["Include subsites", includeSubsites, setIncludeSubsites]
          ].map(([label, checked, setter]) => (
            <label key={label as string} className="inline-flex items-center gap-2 text-sm font-semibold text-text-primary">
              <input
                type="checkbox"
                className="h-4 w-4 rounded border-border text-primary"
                checked={checked as boolean}
                onChange={(event) => (setter as (value: boolean) => void)(event.target.checked)}
              />
              {label as string}
            </label>
          ))}
        </div>
      </section>

      <section className="rounded-xl border border-border bg-surface p-5 shadow-card">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <h2 className="text-lg font-bold text-text-primary">Import Live Discovery Result</h2>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-text-muted">
              Run the read-only discovery script in your SharePoint environment, then upload scan-result.json here.
            </p>
          </div>
          <button
            type="button"
            className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container"
            onClick={() => setShowDiscoveryCommand((value) => !value)}
          >
            <Terminal className="h-4 w-4" />
            View read-only discovery command
          </button>
        </div>
        {showDiscoveryCommand ? (
          <pre className="mt-4 overflow-x-auto rounded-lg border border-border bg-surface-container p-4 text-xs leading-6 text-text-primary">
            {readOnlyDiscoveryCommand}
          </pre>
        ) : null}
        <div className="mt-4 flex flex-col gap-3 md:flex-row md:items-center">
          <input
            type="file"
            accept="application/json,.json"
            className="w-full rounded-lg border border-border bg-surface-container px-3 py-2 text-sm text-text-primary"
            onChange={(event) => setImportFile(event.target.files?.[0] ?? null)}
          />
          <button
            type="button"
            className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90 disabled:opacity-60"
            disabled={isImporting}
            onClick={() => void importLiveDiscovery()}
          >
            <Upload className="h-4 w-4" />
            {isImporting ? "Importing..." : "Import Discovery Result"}
          </button>
        </div>
      </section>

      {state.discovery.status === "running" ? (
        <section className="rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
          <div className="mb-2 flex justify-between text-sm">
            <span className="font-bold text-text-primary">{currentStep}</span>
            <span className="font-mono text-primary">{state.discovery.progress}%</span>
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-surface-container-high">
            <div className="h-full rounded-full bg-primary transition-all" style={{ width: `${state.discovery.progress}%` }} />
          </div>
        </section>
      ) : null}

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-6">
        <StatCard label="Site Collections" value={result?.summary.siteCollections ?? 0} />
        <StatCard label="Subsites" value={result?.summary.subsites ?? 0} />
        <StatCard label="Libraries" value={result?.summary.libraries ?? 0} />
        <StatCard label="Files" value={(result?.summary.files ?? 0).toLocaleString()} />
        <StatCard label="Storage" value={formatBytes(result?.summary.totalStorageBytes ?? 0)} />
        <StatCard label="Readiness" value={`${result?.summary.readinessScore ?? 0}%`} tone="primary" />
      </section>

      {result ? (
        <section className="rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-lg font-bold text-text-primary">Migration Readiness Analysis</h2>
              <p className="mt-1 text-sm text-text-muted">
                Generate blockers, remediation actions, migration waves, and an executive readiness report from this discovery scan.
              </p>
            </div>
            <button
              type="button"
              className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90 disabled:opacity-60"
              disabled={isAnalyzingReadiness}
              onClick={() => void analyzeReadiness()}
            >
              <Play className="h-4 w-4" />
              {isAnalyzingReadiness ? "Analyzing..." : "Analyze Readiness"}
            </button>
          </div>
        </section>
      ) : null}

      {result?.warnings.length || result?.errors.length ? (
        <section className="rounded-xl border border-warning/30 bg-surface p-5 shadow-card">
          <h2 className="text-lg font-bold text-text-primary">Discovery Status</h2>
          <p className="mt-2 text-sm text-text-muted">
            Status: {result.status}. Partial Graph scans preserve available inventory and surface missing permissions or throttling as warnings.
          </p>
          <div className="mt-4 grid gap-2">
            {[...(result.warnings ?? []), ...(result.errors ?? [])].slice(0, 6).map((message) => (
              <div key={message} className="rounded-lg border border-border bg-surface-container p-3 text-sm text-text-primary">
                {message}
              </div>
            ))}
          </div>
        </section>
      ) : null}

      <section className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <StatCard label="Permission Groups" value={result?.summary.permissionGroups ?? 0} />
        <StatCard label="Broken Inheritance" value={result?.summary.brokenInheritanceCount ?? 0} tone="error" />
        <StatCard label="Long Path Risks" value={result?.summary.longPathRisks ?? 0} tone="warning" />
        <StatCard label="Large File Risks" value={result?.summary.largeFileRisks ?? 0} tone="warning" />
        <StatCard label="Metadata Issues" value={result?.summary.missingMetadataIssues ?? 0} tone="error" />
      </section>

      <section className="rounded-xl border border-border bg-surface p-4 shadow-card">
        <div className="mb-4 flex flex-col gap-3 xl:flex-row xl:items-center xl:justify-between">
          <div className="flex flex-wrap items-center gap-2">
            <h2 className="text-lg font-bold text-text-primary">Inventory Results</h2>
            {["Site Collection", "Library", "Folder", "File", "List"].map((filter) => (
              <span key={filter} className="inline-flex items-center gap-1 rounded-lg border border-border px-2 py-1 text-xs font-semibold text-text-muted">
                <Filter className="h-3 w-3" />
                {filter}
              </span>
            ))}
          </div>
          <div className="flex flex-col gap-2 sm:flex-row">
            {result ? (
              <div className="flex flex-wrap gap-2">
                <button className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void exportScan("csv")}>
                  <Download className="h-4 w-4" />
                  CSV
                </button>
                <button className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void exportScan("json")}>
                  <Download className="h-4 w-4" />
                  JSON
                </button>
              </div>
            ) : null}
            <div className="flex max-w-sm items-center gap-2 rounded-lg border border-border bg-surface-container px-3 py-2">
              <Search className="h-4 w-4 text-text-muted" />
              <input
                className="w-full bg-transparent text-sm"
                placeholder="Search inventory..."
                value={searchTerm}
                onChange={(event) => setSearchTerm(event.target.value)}
              />
            </div>
          </div>
        </div>
        {!result ? (
          <EmptyState title="No discovery results yet" description="Start a discovery scan to populate the inventory table." />
        ) : (
          <DataTable
            rows={inventoryRows}
            getRowKey={(row) => row.id}
            columns={[
              { header: "Site Collection", render: (row) => <span className="font-semibold text-primary">{row.siteCollection}</span> },
              { header: "Subsite", render: (row) => row.subsite || "Root" },
              { header: "Library", render: (row) => row.library || "-" },
              { header: "Type", render: (row) => row.itemType },
              { header: "Path", render: (row) => <span className="font-mono text-xs">{row.path}</span> },
              { header: "Files", render: (row) => <span className="font-mono">{row.fileCount.toLocaleString()}</span> },
              { header: "Storage", render: (row) => <span className="font-mono">{formatBytes(row.sizeBytes)}</span> },
              { header: "Metadata", render: (row) => <span className="font-mono">{row.metadataCount}</span> },
              { header: "Risk", render: (row) => <RiskBadge level={row.riskLevel} /> },
              { header: "Readiness", render: (row) => <span className="font-semibold text-text-primary">{row.readinessStatus}</span> }
            ]}
          />
        )}
      </section>

      {result ? (
        <section className="rounded-xl border border-border bg-surface p-4 shadow-card">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <h2 className="text-lg font-bold text-text-primary">Migration Risks</h2>
            <button className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold" onClick={() => void exportScan("risks.csv")}>
              <Download className="h-4 w-4" />
              Risk CSV
            </button>
          </div>
          <DataTable
            rows={result.migrationRisks.slice(0, 12)}
            getRowKey={(row) => row.id}
            emptyMessage="No migration risks found."
            columns={[
              { header: "Risk Type", render: (row) => <span className="font-semibold">{row.riskType}</span> },
              { header: "Site", render: (row) => row.site },
              { header: "Location", render: (row) => row.libraryOrPath },
              { header: "Risk", render: (row) => <RiskBadge level={row.riskLevel} /> },
              { header: "Recommended Action", render: (row) => <span className="font-semibold text-primary">{row.recommendedAction}</span> }
            ]}
          />
        </section>
      ) : null}
    </div>
  );
}
