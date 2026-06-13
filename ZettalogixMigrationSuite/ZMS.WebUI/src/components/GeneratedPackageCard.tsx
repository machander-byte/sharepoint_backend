import { Archive, Download, Eye, FileCode2, Info } from "lucide-react";
import { GeneratedPackageResult } from "../types/zms";

interface GeneratedPackageCardProps {
  packageResult: GeneratedPackageResult;
  onDownload: () => void;
  onViewManifest: () => void;
}

export default function GeneratedPackageCard({ packageResult, onDownload, onViewManifest }: GeneratedPackageCardProps): JSX.Element {
  const summary = packageResult.summary;

  return (
    <section className="rounded-2xl border border-success/30 bg-success-soft/50 p-5 shadow-card">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="flex gap-4">
          <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-success text-white">
            <Archive className="h-6 w-6" />
          </div>
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-lg font-bold text-text-primary">Environment Automation Package</h2>
              <span className="rounded-full bg-success-soft px-2.5 py-1 text-xs font-bold text-success">Package Ready</span>
              {packageResult.source === "mock" ? <span className="rounded-full bg-warning-soft px-2.5 py-1 text-xs font-bold text-warning">Mock Fallback</span> : null}
            </div>
            <p className="mt-1 font-mono text-sm text-text-muted">{packageResult.packageId}</p>
            <p className="mt-2 text-sm leading-6 text-text-muted">
              This ZIP contains generated config, PnP.PowerShell scripts, docs, and report templates. It does not execute tenant changes from the browser.
            </p>
            <p className="mt-3 inline-flex items-center gap-2 rounded-lg border border-warning/30 bg-warning-soft px-3 py-2 text-sm font-semibold text-warning">
              <Info className="h-4 w-4 shrink-0" />
              Run preflight and dry-run before executing tenant changes.
            </p>
          </div>
        </div>

        <div className="flex flex-wrap gap-2">
          <button type="button" className="inline-flex items-center gap-2 rounded-lg border border-border bg-surface px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" onClick={onViewManifest}>
            <Eye className="h-4 w-4" />
            View Manifest
          </button>
          <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90" onClick={onDownload}>
            <Download className="h-4 w-4" />
            Download ZIP
          </button>
        </div>
      </div>

      <div className="mt-5 grid grid-cols-2 gap-3 md:grid-cols-4 xl:grid-cols-8">
        {summary
          ? Object.entries(summary).map(([label, value]) => (
              <div key={label} className="rounded-xl border border-border bg-surface p-3">
                <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{label}</p>
                <p className="mt-1 font-mono text-lg font-bold text-text-primary">{value}</p>
              </div>
            ))
          : null}
        <div className="rounded-xl border border-border bg-surface p-3">
          <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Files</p>
          <p className="mt-1 flex items-center gap-2 font-mono text-lg font-bold text-text-primary">
            <FileCode2 className="h-4 w-4 text-primary" />
            {packageResult.files.length}
          </p>
        </div>
      </div>
    </section>
  );
}
