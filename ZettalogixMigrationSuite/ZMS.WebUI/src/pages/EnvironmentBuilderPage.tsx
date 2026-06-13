import { Download, Eye, Play, RotateCcw } from "lucide-react";
import { useMemo, useState } from "react";
import EnvironmentPreviewModal from "../components/EnvironmentPreviewModal";
import GeneratedPackageCard from "../components/GeneratedPackageCard";
import PackageGenerationModal, { PackageStep } from "../components/PackageGenerationModal";
import PackageManifestViewer from "../components/PackageManifestViewer";
import PageHeader from "../components/PageHeader";
import SiteCollectionCard from "../components/SiteCollectionCard";
import Stepper from "../components/Stepper";
import { siteCollections } from "../data/zmsMockData";
import { useZmsDispatch, useZmsState } from "../state/ZmsStateProvider";
import { toastActions } from "../state/toastActions";
import { EnvironmentConfig, GeneratedPackageResult, PackageManifest } from "../types/zms";
import { zmsApi } from "../services/zmsApi";

const steps = [
  "Site Collections",
  "Subsites",
  "Libraries & Lists",
  "Metadata",
  "Permissions",
  "Sample Data",
  "Review & Generate"
];

const optionLabels = [
  ["includeDefaultSubsites", "Include default subsites"],
  ["generateSampleDocuments", "Generate sample documents"],
  ["includeMetadataColumns", "Include metadata columns"],
  ["createPermissionGroups", "Create permission groups"],
  ["addMigrationEdgeCases", "Add migration edge cases"],
  ["includeArchivedFolders", "Include archived folders"],
  ["includeLongPathExamples", "Include long path examples"],
  ["includeLargeFilePlaceholders", "Include large file placeholders"]
] as const;

const packageStepLabels = [
  "Build EnvironmentConfig",
  "Validate configuration",
  "Save configuration",
  "Generate scripts and documentation",
  "Package ready"
];

function createPackageSteps(activeIndex = -1): PackageStep[] {
  return packageStepLabels.map((label, index) => ({
    label,
    status: index < activeIndex ? "success" : index === activeIndex ? "running" : "pending"
  }));
}

export default function EnvironmentBuilderPage(): JSX.Element {
  const state = useZmsState();
  const dispatch = useZmsDispatch();
  const [previewOpen, setPreviewOpen] = useState(false);
  const [previewConfig, setPreviewConfig] = useState<EnvironmentConfig | null>(null);
  const [validationError, setValidationError] = useState<string | null>(null);
  const [packageModalOpen, setPackageModalOpen] = useState(false);
  const [packageSteps, setPackageSteps] = useState<PackageStep[]>(createPackageSteps());
  const [packageWarnings, setPackageWarnings] = useState<string[]>([]);
  const [packageErrors, setPackageErrors] = useState<string[]>([]);
  const [packageWorking, setPackageWorking] = useState(false);
  const [activePackage, setActivePackage] = useState<GeneratedPackageResult | null>(state.lastGeneratedPackage);
  const [manifestOpen, setManifestOpen] = useState(false);
  const [manifest, setManifest] = useState<PackageManifest | null>(null);

  const selectedSites = useMemo(
    () => siteCollections.filter((site) => state.selectedSiteCollectionIds.includes(site.id)),
    [state.selectedSiteCollectionIds]
  );

  const selectedSiteForPanel = selectedSites[0] ?? null;
  const selectedOptionCount = Object.values(state.builderOptions).filter(Boolean).length;

  const validateBuilder = (): boolean => {
    if (selectedSites.length === 0) {
      setValidationError("Select at least one site collection before previewing or generating.");
      return false;
    }
    if (!state.tenantValues.targetUrlPrefix.trim()) {
      setValidationError("Target URL prefix cannot be empty.");
      return false;
    }
    if (selectedOptionCount === 0) {
      setValidationError("Select at least one builder option before generating a configuration.");
      return false;
    }
    setValidationError(null);
    return true;
  };

  const createConfig = async (): Promise<EnvironmentConfig | null> => {
    if (!validateBuilder()) {
      return null;
    }
    return zmsApi.generateEnvironmentConfig(selectedSites, state.builderOptions, state.tenantValues);
  };

  const setStepStatus = (index: number, status: PackageStep["status"]) => {
    setPackageSteps((current) => current.map((step, stepIndex) => (stepIndex === index ? { ...step, status } : step)));
  };

  const previewStructure = async () => {
    const config = await createConfig();
    if (!config) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.warning("Preview unavailable", validationError ?? "Resolve validation errors first.") });
      return;
    }
    setPreviewConfig(config);
    setPreviewOpen(true);
  };

  const generatePackage = async () => {
    setPackageModalOpen(true);
    setPackageWorking(true);
    setPackageWarnings([]);
    setPackageErrors([]);
    setPackageSteps(createPackageSteps(0));
    dispatch({ type: "GENERATE_CONFIG_STARTED" });

    const config = await createConfig();
    if (!config) {
      setStepStatus(0, "error");
      setPackageErrors([validationError ?? "Resolve validation errors first."]);
      setPackageWorking(false);
      dispatch({ type: "PACKAGE_GENERATION_FAILED", payload: { error: validationError ?? "Resolve validation errors first." } });
      dispatch({ type: "ADD_TOAST", payload: toastActions.warning("Config not generated", validationError ?? "Resolve validation errors first.") });
      return;
    }

    setPreviewConfig(config);
    setStepStatus(0, "success");

    try {
      setStepStatus(1, "running");
      dispatch({ type: "VALIDATION_STARTED" });
      const validation = await zmsApi.validateEnvironmentConfig(config);
      setPackageWarnings(validation.warnings);

      if (validation.source === "mock") {
        dispatch({ type: "ADD_TOAST", payload: toastActions.warning("Backend unavailable", "Using mock validation and package generation fallback.") });
      }

      if (!validation.isValid) {
        setStepStatus(1, "error");
        setPackageErrors(validation.errors);
        dispatch({ type: "VALIDATION_FAILED", payload: { errors: validation.errors, warnings: validation.warnings } });
        dispatch({ type: "ADD_TOAST", payload: toastActions.error("Config validation failed", validation.errors[0] ?? "Review validation errors.") });
        return;
      }

      setStepStatus(1, validation.warnings.length > 0 ? "warning" : "success");
      dispatch({ type: "VALIDATION_SUCCEEDED", payload: { warnings: validation.warnings } });
      dispatch({
        type: "ADD_TOAST",
        payload: validation.warnings.length > 0
          ? toastActions.warning("Validation completed with warnings.")
          : toastActions.success("Environment config validated successfully.")
      });

      setStepStatus(2, "running");
      const saveResponse = await zmsApi.saveEnvironmentConfigToBackend(config);
      setStepStatus(2, saveResponse.source === "mock" ? "warning" : "success");
      dispatch({ type: "SET_ENVIRONMENT_CONFIG", payload: config });
      dispatch({
        type: "ADD_TOAST",
        payload: saveResponse.source === "mock"
          ? toastActions.warning("Backend unavailable", "Config saved to local mock state.")
          : toastActions.success("Environment config saved successfully.", `Config ID: ${saveResponse.configId}`)
      });

      setStepStatus(3, "running");
      dispatch({ type: "PACKAGE_GENERATION_STARTED" });
      const packageResult = await zmsApi.generateEnvironmentPackage(config);
      setStepStatus(3, packageResult.source === "mock" ? "warning" : "success");
      setStepStatus(4, "success");
      setActivePackage(packageResult);
      dispatch({ type: "PACKAGE_GENERATION_SUCCEEDED", payload: packageResult });
      dispatch({
        type: "ADD_TOAST",
        payload: packageResult.source === "mock"
          ? toastActions.warning("Backend unavailable. Using mock package generation.")
          : toastActions.success("Automation package generated successfully.")
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Package generation failed.";
      setPackageErrors([message]);
      setStepStatus(3, "error");
      dispatch({ type: "PACKAGE_GENERATION_FAILED", payload: { error: message } });
      dispatch({ type: "ADD_TOAST", payload: toastActions.error("Package generation failed.", message) });
    } finally {
      setPackageWorking(false);
    }
  };

  const exportConfig = async () => {
    if (!state.generatedEnvironmentConfig) {
      dispatch({ type: "ADD_TOAST", payload: toastActions.warning("No config to export", "Generate the environment config first.") });
      return;
    }
    await zmsApi.exportEnvironmentConfig(state.generatedEnvironmentConfig);
    dispatch({ type: "ADD_TOAST", payload: toastActions.success("Environment config exported successfully.") });
  };

  const resetBuilder = () => {
    dispatch({ type: "RESET_BUILDER" });
    setValidationError(null);
    setPreviewConfig(null);
    dispatch({ type: "ADD_TOAST", payload: toastActions.info("Environment builder reset", "Default selections and options were restored.") });
  };

  const currentPackage = activePackage ?? state.lastGeneratedPackage;

  const downloadPackage = async () => {
    if (!currentPackage) {
      return;
    }

    dispatch({ type: "PACKAGE_DOWNLOAD_STARTED" });
    try {
      const result = await zmsApi.downloadEnvironmentPackage(currentPackage.packageId, state.generatedEnvironmentConfig ?? previewConfig ?? undefined);
      dispatch({ type: "PACKAGE_DOWNLOAD_SUCCEEDED" });
      dispatch({
        type: "ADD_TOAST",
        payload: result.source === "mock"
          ? toastActions.warning("Backend unavailable", "Downloaded JSON fallback instead of ZIP.")
          : toastActions.success("Package downloaded successfully.")
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Package download failed.";
      dispatch({ type: "PACKAGE_DOWNLOAD_FAILED", payload: { error: message } });
      dispatch({ type: "ADD_TOAST", payload: toastActions.error("Package download failed.", message) });
    }
  };

  const viewManifest = async () => {
    if (!currentPackage) {
      return;
    }
    const nextManifest = await zmsApi.getPackageManifest(currentPackage.packageId, currentPackage.summary);
    setManifest(nextManifest);
    setManifestOpen(true);
    if (nextManifest.source === "mock") {
      dispatch({ type: "ADD_TOAST", payload: toastActions.warning("Backend unavailable", "Showing mock package manifest.") });
    }
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Enterprise Test Environment Builder"
        subtitle="Create realistic SharePoint environments for migration testing."
        actions={
          <>
            <button type="button" className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" onClick={resetBuilder}>
              <RotateCcw className="h-4 w-4" />
              Reset Builder
            </button>
            {state.generatedEnvironmentConfig ? (
              <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90" onClick={() => void exportConfig()}>
                <Download className="h-4 w-4" />
                Export Config JSON
              </button>
            ) : null}
          </>
        }
      />

      <Stepper steps={steps} currentStep={1} />

      {validationError ? (
        <div className="rounded-xl border border-error/20 bg-error-soft px-4 py-3 text-sm font-semibold text-error">
          {validationError}
        </div>
      ) : null}

      {currentPackage ? (
        <GeneratedPackageCard
          packageResult={currentPackage}
          onDownload={() => void downloadPackage()}
          onViewManifest={() => void viewManifest()}
        />
      ) : null}

      <section className="grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_400px]">
        <div>
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-lg font-bold text-text-primary">Available Templates</h2>
              <p className="mt-1 text-sm text-text-muted">{selectedSites.length} of {siteCollections.length} selected</p>
            </div>
            <div className="flex gap-2">
              <button
                type="button"
                className="rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container"
                onClick={() => dispatch({ type: "SET_SELECTED_SITE_COLLECTIONS", payload: siteCollections.map((site) => site.id) })}
              >
                Select All
              </button>
              <button
                type="button"
                className="rounded-lg border border-border px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container"
                onClick={() => dispatch({ type: "SET_SELECTED_SITE_COLLECTIONS", payload: [] })}
              >
                Clear All
              </button>
            </div>
          </div>
          <div className="grid grid-cols-1 gap-4 md:grid-cols-2 2xl:grid-cols-3">
            {siteCollections.map((site) => (
              <SiteCollectionCard
                key={site.id}
                site={site}
                selectable
                selected={state.selectedSiteCollectionIds.includes(site.id)}
                onToggle={(siteId) => dispatch({ type: "TOGGLE_SITE_COLLECTION", payload: siteId })}
              />
            ))}
          </div>
        </div>

        <aside className="h-fit rounded-xl border border-border bg-surface p-5 shadow-card xl:sticky xl:top-24">
          <h2 className="border-b border-border pb-4 text-lg font-bold text-text-primary">Configuration Details</h2>
          <div className="mt-5">
            <h3 className="font-semibold text-primary">{selectedSiteForPanel?.name ?? "No site selected"}</h3>
            <p className="mt-2 text-sm leading-6 text-text-muted">
              {selectedSiteForPanel
                ? `${selectedSiteForPanel.subsites.length} subsites, ${selectedSiteForPanel.libraries.length} libraries, ${selectedSiteForPanel.edgeCases.length} edge cases.`
                : "Select a site collection to inspect its generated structure."}
            </p>
          </div>

          <div className="mt-5 space-y-3">
            {optionLabels.map(([key, label]) => (
              <label key={key} className="flex cursor-pointer items-start gap-3 rounded-lg border border-border bg-surface-container p-3">
                <input
                  type="checkbox"
                  checked={state.builderOptions[key]}
                  onChange={(event) => dispatch({ type: "SET_BUILDER_OPTION", payload: { key, value: event.target.checked } })}
                  className="mt-1 h-4 w-4 accent-primary"
                />
                <span>
                  <span className="block text-sm font-semibold text-text-primary">{label}</span>
                  <span className="text-xs leading-5 text-text-muted">Stored in state and applied to generated JSON.</span>
                </span>
              </label>
            ))}
          </div>

          <label className="mt-5 block">
            <span className="mb-2 block text-sm font-semibold text-text-muted">Target URL Prefix</span>
            <input
              className="w-full rounded-lg border border-border bg-surface px-3 py-2 font-mono text-sm text-text-primary focus:border-primary"
              value={state.tenantValues.targetUrlPrefix}
              onChange={(event) => dispatch({ type: "SET_TENANT_VALUES", payload: { targetUrlPrefix: event.target.value } })}
            />
          </label>

          <div className="mt-6 flex flex-col gap-3">
            <button
              type="button"
              className="inline-flex items-center justify-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container"
              onClick={() => void previewStructure()}
            >
              <Eye className="h-4 w-4" />
              Preview Structure
            </button>
            <button
              type="button"
              className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90"
              onClick={() => void generatePackage()}
            >
              <Play className="h-4 w-4" />
              Generate Environment
            </button>
          </div>
        </aside>
      </section>

      <EnvironmentPreviewModal isOpen={previewOpen} config={previewConfig} onClose={() => setPreviewOpen(false)} />
      <PackageGenerationModal
        isOpen={packageModalOpen}
        steps={packageSteps}
        warnings={packageWarnings}
        errors={packageErrors}
        packageResult={currentPackage}
        isWorking={packageWorking}
        onClose={() => setPackageModalOpen(false)}
        onRetry={() => void generatePackage()}
        onDownload={() => void downloadPackage()}
        onViewManifest={() => void viewManifest()}
      />
      <PackageManifestViewer isOpen={manifestOpen} manifest={manifest} onClose={() => setManifestOpen(false)} />
    </div>
  );
}
