import { Download, FileText } from "lucide-react";
import { useEffect, useState } from "react";
import PageHeader from "../components/PageHeader";
import { reports } from "../data/zmsMockData";
import { zmsApi } from "../services/zmsApi";
import { useZmsDispatch, useZmsState } from "../state/ZmsStateProvider";
import { toastActions } from "../state/toastActions";
import { DiscoveryScanResult, ExecutionSimulationResult, GeneratedReport, LivePilotMigrationResult, MigrationExecutionJob, MigrationPlan, MigrationReadinessAssessment, MigrationTransferPreview, PreMigrationValidationResult, ReportFormat, ReportItem, WorkflowValidationRun } from "../types/zms";
import { downloadCsv } from "../utils/downloadCsv";
import { downloadJson } from "../utils/downloadJson";

function rowsForReport(report: ReportItem, state: ReturnType<typeof useZmsState>): Array<Record<string, unknown>> {
  if (report.id === "environment-inventory" && state.generatedEnvironmentConfig) {
    return state.generatedEnvironmentConfig.siteCollections.flatMap((site) =>
      site.libraries.map((library) => ({
        siteCollection: site.title,
        siteUrl: site.url,
        library: library.title,
        sampleFileCount: library.sampleFileCount,
        metadataFields: library.metadataFieldIds.length
      }))
    );
  }

  if (report.id === "permission-risk" && state.discovery.result) {
    return state.discovery.result.permissionRisks
      .map((item) => ({
        site: item.site,
        libraryOrFolder: item.libraryOrFolder,
        risk: item.riskLevel,
        recommendedAction: item.recommendedAction
      }));
  }

  if (state.discovery.result) {
    return state.discovery.result.inventoryItems.map((item) => ({
      siteCollection: item.siteCollection,
      library: item.library,
      files: item.fileCount,
      readiness: item.readinessStatus
    }));
  }

  return [
    {
      report: report.title,
      status: "Mock report data",
      generatedAt: new Date().toISOString()
    }
  ];
}

function discoverySourceLabel(result: DiscoveryScanResult | null): string {
  if (!result) return "Not generated";
  if (result.mode === "live-import") return "Live Import";
  if (result.mode === "config") return "Config Mode";
  if (result.mode === "live") return "Live Mode";
  return result.mode;
}

export default function ReportsPage(): JSX.Element {
  const state = useZmsState();
  const dispatch = useZmsDispatch();
  const [latestDiscovery, setLatestDiscovery] = useState<DiscoveryScanResult | null>(state.discovery.result);
  const [latestReadiness, setLatestReadiness] = useState<MigrationReadinessAssessment | null>(null);
  const [latestPlan, setLatestPlan] = useState<MigrationPlan | null>(null);
  const [latestPreValidation, setLatestPreValidation] = useState<PreMigrationValidationResult | null>(null);
  const [latestSimulation, setLatestSimulation] = useState<ExecutionSimulationResult | null>(null);
  const [latestExecutionJob, setLatestExecutionJob] = useState<MigrationExecutionJob | null>(null);
  const [latestPreview, setLatestPreview] = useState<MigrationTransferPreview | null>(null);
  const [latestPilot, setLatestPilot] = useState<LivePilotMigrationResult | null>(null);
  const [latestWorkflow, setLatestWorkflow] = useState<WorkflowValidationRun | null>(null);

  useEffect(() => {
    if (state.discovery.result) {
      setLatestDiscovery(state.discovery.result);
      return;
    }

    setLatestDiscovery(null);
    setLatestReadiness(null);
    setLatestPlan(null);
    setLatestPreValidation(null);
    setLatestSimulation(null);
    setLatestExecutionJob(null);
    setLatestPreview(null);
    setLatestPilot(null);
    setLatestWorkflow(null);
  }, [state.discovery.result]);

  const exportReport = (report: ReportItem, format: ReportFormat) => {
    if (format === "PDF") {
      dispatch({ type: "ADD_TOAST", payload: toastActions.info("PDF export will be connected in backend phase.") });
      return;
    }

    const rows = rowsForReport(report, state);
    const generatedReport: GeneratedReport = {
      id: `generated-report-${Date.now()}`,
      reportId: report.id,
      title: report.title,
      format,
      generatedAt: new Date().toISOString(),
      rows
    };
    dispatch({ type: "ADD_GENERATED_REPORT", payload: generatedReport });

    if (format === "CSV") {
      downloadCsv(`${report.id}.csv`, rows);
    } else {
      downloadJson(`${report.id}.json`, generatedReport);
    }
    dispatch({ type: "ADD_TOAST", payload: toastActions.success("Report exported", `${report.title} exported as ${format}.`) });
  };

  const exportDiscoveryReport = async (exportType: "csv" | "json" | "permissions.csv" | "metadata.csv" | "risks.csv") => {
    if (!latestDiscovery) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.info("No completed discovery scan is available.") });
      return;
    }

    const result = await zmsApi.downloadDiscoveryExport(latestDiscovery.scanId, exportType);
    dispatch({
      type: "ADD_TOAST",
      payload: toastActions.success(
        "Discovery report exported",
        result.source === "backend" ? "Export downloaded from backend scan storage." : "Export downloaded from local scan data."
      )
    });
  };

  const exportReadinessReport = async (exportType: "json" | "csv" | "markdown") => {
    if (!latestReadiness) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.info("No completed readiness assessment is available.") });
      return;
    }

    const result = await zmsApi.downloadReadinessExport(latestReadiness.assessmentId, exportType);
    dispatch({
      type: "ADD_TOAST",
      payload: toastActions.success(
        "Readiness report exported",
        result.source === "backend" ? "Export downloaded from readiness assessment storage." : "Export downloaded from local readiness data."
      )
    });
  };

  const exportMigrationPlanReport = async (exportType: "json" | "csv" | "markdown") => {
    if (!latestPlan) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.info("No migration plan is available.") });
      return;
    }
    const result = await zmsApi.downloadMigrationPlanExport(latestPlan.planId, exportType);
    dispatch({ type: "ADD_TOAST", payload: toastActions.success("Migration plan exported", result.source === "backend" ? "Export downloaded from migration plan storage." : "Export downloaded from local plan data.") });
  };

  const exportPreMigrationReport = async (kind: "validation" | "simulation", exportType: "json" | "csv" | "markdown") => {
    if (kind === "validation") {
      if (!latestPreValidation) {
        dispatch({ type: "ADD_TOAST", payload: toastActions.info("No pre-migration validation is available.") });
        return;
      }
      await zmsApi.downloadPreMigrationValidationExport(latestPreValidation.validationId, exportType);
    } else {
      if (!latestSimulation) {
        dispatch({ type: "ADD_TOAST", payload: toastActions.info("No execution simulation is available.") });
        return;
      }
      await zmsApi.downloadExecutionSimulationExport(latestSimulation.simulationId, exportType === "json" ? "json" : "markdown");
    }
    dispatch({ type: "ADD_TOAST", payload: toastActions.success("Pre-migration report exported") });
  };

  const exportExecutionReport = async (exportType: "json" | "csv" | "markdown") => {
    if (!latestExecutionJob) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.info("No execution simulation job is available.") });
      return;
    }
    await zmsApi.downloadMigrationExecutionReport(latestExecutionJob.jobId, exportType);
    dispatch({ type: "ADD_TOAST", payload: toastActions.success("Execution report exported") });
  };

  const discoveryReports = [
    {
      id: "discovery-inventory-csv",
      title: "Discovery Inventory CSV",
      description: "Flat site, library, folder, file, metadata, permission, and readiness inventory.",
      exportType: "csv" as const,
      format: "CSV"
    },
    {
      id: "discovery-results-json",
      title: "Discovery Results JSON",
      description: "Complete discovery scan result with summary, inventory, permissions, metadata, and risks.",
      exportType: "json" as const,
      format: "JSON"
    },
    {
      id: "discovery-permission-risk-csv",
      title: "Permission Risk CSV",
      description: "Broken inheritance, restricted areas, assigned groups, users, and recommended actions.",
      exportType: "permissions.csv" as const,
      format: "CSV"
    },
    {
      id: "discovery-metadata-findings-csv",
      title: "Metadata Findings CSV",
      description: "Discovered fields, required status, missing values, target mappings, and mapping risk.",
      exportType: "metadata.csv" as const,
      format: "CSV"
    },
    {
      id: "discovery-migration-risk-csv",
      title: "Migration Risk CSV",
      description: "Broken permissions, long paths, large files, duplicates, missing metadata, archives, and restricted content.",
      exportType: "risks.csv" as const,
      format: "CSV"
    }
  ];
  const readinessReports = [
    ["Readiness Assessment Report", "Full readiness assessment JSON.", "json", "JSON"],
    ["Remediation Plan CSV", "Prioritized remediation action plan.", "csv", "CSV"],
    ["Migration Wave Plan CSV", "Suggested migration waves and prerequisites.", "csv", "CSV"],
    ["Risk Findings CSV", "Blockers, warnings, and readiness risk findings.", "csv", "CSV"],
    ["Executive Readiness Summary", "Executive markdown summary for stakeholders.", "markdown", "MD"]
  ] as const;
  const migrationPlanReports = [
    ["Migration Plan JSON", "Full structured migration plan.", "json", "JSON"],
    ["Migration Plan CSV", "Plan item export with wave and action fields.", "csv", "CSV"],
    ["Migration Runbook Markdown", "Planning runbook markdown.", "markdown", "MD"],
    ["Wave Summary CSV", "Wave summary from the current plan.", "csv", "CSV"],
    ["Excluded Items CSV", "Excluded and blocked content from the current plan.", "csv", "CSV"],
    ["Pre-Migration Checklist JSON", "Checklist state within the current plan.", "json", "JSON"]
  ] as const;
  const preMigrationReports = [
    ["Go/No-Go Validation Report", "Markdown go/no-go report.", "validation", "markdown", "MD"],
    ["Validation Checks CSV", "Validation checks and recommended actions.", "validation", "csv", "CSV"],
    ["Blocked Items CSV", "Blocked validation checks and affected items.", "validation", "csv", "CSV"],
    ["Execution Simulation Report", "Simulation markdown report.", "simulation", "markdown", "MD"],
    ["Wave Simulation CSV", "Wave simulation timeline and estimates.", "simulation", "json", "JSON"],
    ["Expected Issues CSV", "Expected warning/failure issues.", "simulation", "json", "JSON"]
  ] as const;
  const executionReports = [
    ["Execution Job Report Markdown", "Simulation execution report with waves, progress, and timeline.", "markdown", "MD"],
    ["Execution Items CSV", "Simulated item statuses, warnings, and failures.", "csv", "CSV"],
    ["Execution Errors CSV", "Execution error details from the simulated job.", "csv", "CSV"],
    ["Wave Execution Summary CSV", "Wave-level status and progress summary.", "csv", "CSV"],
    ["Execution Timeline JSON", "Full execution timeline and job record.", "json", "JSON"]
  ] as const;
  const adapterReports = [
    ["Transfer Preview JSON", "Full transfer preview.", "preview", "json", "JSON"],
    ["Transfer Plan CSV", "Eligible, warning, and blocked transfer plan items.", "preview", "csv", "CSV"],
    ["Metadata Mapping Preview CSV", "Preview metadata mappings.", "preview", "csv", "CSV"],
    ["Permission Mapping Preview CSV", "Preview permission mappings.", "preview", "csv", "CSV"],
    ["Blocked Migration Items CSV", "Blocked migration items and recommendations.", "preview", "csv", "CSV"],
    ["Pilot Migration Report Markdown", "Locked pilot safety report.", "pilot", "markdown", "MD"]
  ] as const;
  const workflowReports = [
    ["End-to-End Workflow Report Markdown", "Full operator workflow validation report.", "markdown", "MD"],
    ["End-to-End Workflow Result JSON", "Complete structured workflow validation result.", "json", "JSON"],
    ["Workflow Issues CSV", "Issues are included in workflow JSON until dedicated CSV export is added.", "json", "JSON"],
    ["Workflow Artifacts JSON", "Generated workflow artifact list.", "json", "JSON"]
  ] as const;

  return (
    <div className="flex flex-col gap-6">
      <PageHeader title="Reports" subtitle="Export inventory, risk, readiness, and validation reports." />

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
        {discoveryReports.map((report) => (
          <article key={report.id} className="flex min-h-[220px] flex-col rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                <FileText className="h-5 w-5" />
              </div>
              <div>
                <h2 className="font-bold text-text-primary">{report.title}</h2>
                <p className="mt-2 text-sm leading-6 text-text-muted">{report.description}</p>
              </div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">
                Last scan: {latestDiscovery?.completedAt ? new Date(latestDiscovery.completedAt).toLocaleDateString() : "Not generated"}
              </p>
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">
                Source: {discoverySourceLabel(latestDiscovery)}
              </p>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container disabled:opacity-60"
                disabled={!latestDiscovery}
                onClick={() => void exportDiscoveryReport(report.exportType)}
              >
                <Download className="h-4 w-4" />
                {report.format}
              </button>
            </div>
          </article>
        ))}

        {readinessReports.map(([title, description, exportType, format]) => (
          <article key={title} className="flex min-h-[220px] flex-col rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                <FileText className="h-5 w-5" />
              </div>
              <div>
                <h2 className="font-bold text-text-primary">{title}</h2>
                <p className="mt-2 text-sm leading-6 text-text-muted">{description}</p>
              </div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">
                Readiness: {latestReadiness ? `${latestReadiness.readinessScore}% / ${latestReadiness.riskLevel}` : "Not generated"}
              </p>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container disabled:opacity-60"
                disabled={!latestReadiness}
                onClick={() => void exportReadinessReport(exportType)}
              >
                <Download className="h-4 w-4" />
                {format}
              </button>
            </div>
          </article>
        ))}

        {migrationPlanReports.map(([title, description, exportType, format]) => (
          <article key={title} className="flex min-h-[220px] flex-col rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                <FileText className="h-5 w-5" />
              </div>
              <div>
                <h2 className="font-bold text-text-primary">{title}</h2>
                <p className="mt-2 text-sm leading-6 text-text-muted">{description}</p>
              </div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">
                Plan: {latestPlan ? latestPlan.status : "Not generated"}
              </p>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container disabled:opacity-60"
                disabled={!latestPlan}
                onClick={() => void exportMigrationPlanReport(exportType)}
              >
                <Download className="h-4 w-4" />
                {format}
              </button>
            </div>
          </article>
        ))}

        {preMigrationReports.map(([title, description, kind, exportType, format]) => (
          <article key={title} className="flex min-h-[220px] flex-col rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                <FileText className="h-5 w-5" />
              </div>
              <div>
                <h2 className="font-bold text-text-primary">{title}</h2>
                <p className="mt-2 text-sm leading-6 text-text-muted">{description}</p>
              </div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">
                Source: {kind === "validation" ? latestPreValidation?.decision ?? "Not generated" : latestSimulation ? "Simulation ready" : "Not generated"}
              </p>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container disabled:opacity-60"
                disabled={kind === "validation" ? !latestPreValidation : !latestSimulation}
                onClick={() => void exportPreMigrationReport(kind, exportType)}
              >
                <Download className="h-4 w-4" />
                {format}
              </button>
            </div>
          </article>
        ))}

        {executionReports.map(([title, description, exportType, format]) => (
          <article key={title} className="flex min-h-[220px] flex-col rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                <FileText className="h-5 w-5" />
              </div>
              <div>
                <h2 className="font-bold text-text-primary">{title}</h2>
                <p className="mt-2 text-sm leading-6 text-text-muted">{description}</p>
              </div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">
                Execution: {latestExecutionJob ? `${latestExecutionJob.status} / ${latestExecutionJob.mode}` : "Not generated"}
              </p>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container disabled:opacity-60"
                disabled={!latestExecutionJob}
                onClick={() => void exportExecutionReport(exportType)}
              >
                <Download className="h-4 w-4" />
                {format}
              </button>
            </div>
          </article>
        ))}

        {adapterReports.map(([title, description, kind, exportType, format]) => (
          <article key={title} className="flex min-h-[220px] flex-col rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary"><FileText className="h-5 w-5" /></div>
              <div><h2 className="font-bold text-text-primary">{title}</h2><p className="mt-2 text-sm leading-6 text-text-muted">{description}</p></div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">Adapter: {kind === "preview" ? latestPreview ? "Preview ready" : "Not generated" : latestPilot?.status ?? "Not generated"}</p>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container disabled:opacity-60"
                disabled={kind === "preview" ? !latestPreview : !latestPilot}
                onClick={() => kind === "preview" && latestPreview ? void zmsApi.downloadSharePointPreviewReport(latestPreview.previewId, exportType === "json" ? "json" : "csv") : latestPilot ? void zmsApi.downloadLivePilotReport(latestPilot.pilotRunId, exportType as "json" | "csv" | "markdown") : undefined}
              >
                <Download className="h-4 w-4" />
                {format}
              </button>
            </div>
          </article>
        ))}

        {workflowReports.map(([title, description, exportType, format]) => (
          <article key={title} className="flex min-h-[220px] flex-col rounded-xl border border-primary-muted bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary"><FileText className="h-5 w-5" /></div>
              <div><h2 className="font-bold text-text-primary">{title}</h2><p className="mt-2 text-sm leading-6 text-text-muted">{description}</p></div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">Workflow: {latestWorkflow?.overallResult ?? "Not generated"}</p>
              <button
                type="button"
                className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container disabled:opacity-60"
                disabled={!latestWorkflow}
                onClick={() => latestWorkflow ? void zmsApi.downloadWorkflowValidationExport(latestWorkflow.workflowRunId, exportType) : undefined}
              >
                <Download className="h-4 w-4" />
                {format}
              </button>
            </div>
          </article>
        ))}

        {reports.map((report) => (
          <article key={report.id} className="flex min-h-[240px] flex-col rounded-xl border border-border bg-surface p-5 shadow-card">
            <div className="flex items-start gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                <FileText className="h-5 w-5" />
              </div>
              <div>
                <h2 className="font-bold text-text-primary">{report.title}</h2>
                <p className="mt-2 text-sm leading-6 text-text-muted">{report.description}</p>
              </div>
            </div>
            <div className="mt-auto pt-5">
              <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-text-subtle">
                Last generated: {report.lastGenerated}
              </p>
              <div className="flex flex-wrap gap-2">
                {report.formats.map((format) => (
                  <button
                    key={format}
                    type="button"
                    className="inline-flex items-center gap-2 rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container"
                    onClick={() => exportReport(report, format)}
                  >
                    <Download className="h-4 w-4" />
                    {format}
                  </button>
                ))}
              </div>
            </div>
          </article>
        ))}
      </section>
    </div>
  );
}
