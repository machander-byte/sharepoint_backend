import { CheckCircle2, Download, Eye, Loader2, RotateCcw, TriangleAlert, X, XCircle } from "lucide-react";
import { GeneratedPackageResult } from "../types/zms";

export type PackageStepStatus = "pending" | "running" | "success" | "warning" | "error";

export interface PackageStep {
  label: string;
  status: PackageStepStatus;
}

interface PackageGenerationModalProps {
  isOpen: boolean;
  steps: PackageStep[];
  warnings: string[];
  errors: string[];
  packageResult: GeneratedPackageResult | null;
  isWorking: boolean;
  onClose: () => void;
  onRetry: () => void;
  onDownload: () => void;
  onViewManifest: () => void;
}

function StepIcon({ status }: { status: PackageStepStatus }): JSX.Element {
  if (status === "running") return <Loader2 className="h-4 w-4 animate-spin text-primary" />;
  if (status === "success") return <CheckCircle2 className="h-4 w-4 text-success" />;
  if (status === "warning") return <TriangleAlert className="h-4 w-4 text-warning" />;
  if (status === "error") return <XCircle className="h-4 w-4 text-error" />;
  return <span className="h-4 w-4 rounded-full border border-border bg-surface" />;
}

export default function PackageGenerationModal({
  isOpen,
  steps,
  warnings,
  errors,
  packageResult,
  isWorking,
  onClose,
  onRetry,
  onDownload,
  onViewManifest
}: PackageGenerationModalProps): JSX.Element | null {
  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-[72] flex items-center justify-center bg-slate-950/45 p-4">
      <div className="flex max-h-[88vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl border border-border bg-surface shadow-panel">
        <div className="flex items-start justify-between gap-4 border-b border-border px-5 py-4">
          <div>
            <h2 className="text-xl font-bold text-text-primary">Generate Environment Automation Package</h2>
            <p className="mt-1 text-sm text-text-muted">Validate configuration and generate a downloadable PowerShell automation bundle.</p>
          </div>
          <button type="button" className="rounded-lg p-2 text-text-muted hover:bg-surface-container" onClick={onClose} aria-label="Close package generation">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="overflow-auto p-5">
          <div className="rounded-xl border border-border bg-surface-container p-4">
            <p className="mb-4 text-sm font-bold uppercase tracking-wide text-text-subtle">Workflow</p>
            <div className="space-y-3">
              {steps.map((step) => (
                <div key={step.label} className="flex items-center gap-3 rounded-lg bg-surface px-3 py-2">
                  <StepIcon status={step.status} />
                  <span className="text-sm font-semibold text-text-primary">{step.label}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="mt-4 rounded-xl border border-info/20 bg-info-soft/50 p-4 text-sm text-info">
            <strong>Current phase:</strong> package generation only. ZMS is not executing tenant changes from the browser.
          </div>

          {warnings.length > 0 ? (
            <div className="mt-4 rounded-xl border border-warning/25 bg-warning-soft p-4">
              <h3 className="mb-2 text-sm font-bold text-warning">Validation warnings</h3>
              <ul className="space-y-1 text-sm text-text-primary">
                {warnings.map((warning) => (
                  <li key={warning}>- {warning}</li>
                ))}
              </ul>
            </div>
          ) : null}

          {errors.length > 0 ? (
            <div className="mt-4 rounded-xl border border-error/25 bg-error-soft p-4">
              <h3 className="mb-2 text-sm font-bold text-error">Generation stopped</h3>
              <ul className="space-y-1 text-sm text-text-primary">
                {errors.map((error) => (
                  <li key={error}>- {error}</li>
                ))}
              </ul>
            </div>
          ) : null}

          {packageResult ? (
            <div className="mt-4 rounded-xl border border-success/25 bg-success-soft p-4">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-bold text-success">Package ready</p>
                  <p className="mt-1 font-mono text-xs text-text-muted">{packageResult.packageId}</p>
                </div>
                <p className="text-sm font-semibold text-text-primary">{packageResult.files.length} files generated</p>
              </div>
            </div>
          ) : null}
        </div>

        <div className="flex flex-wrap justify-end gap-2 border-t border-border px-5 py-4">
          {errors.length > 0 ? (
            <button type="button" className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" onClick={onRetry}>
              <RotateCcw className="h-4 w-4" />
              Retry
            </button>
          ) : null}
          {packageResult ? (
            <>
              <button type="button" className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" onClick={onViewManifest}>
                <Eye className="h-4 w-4" />
                View Manifest
              </button>
              <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90" onClick={onDownload}>
                <Download className="h-4 w-4" />
                Download Package
              </button>
            </>
          ) : null}
          <button type="button" className="rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" onClick={onClose}>
            {isWorking ? "Hide" : "Close"}
          </button>
        </div>
      </div>
    </div>
  );
}
