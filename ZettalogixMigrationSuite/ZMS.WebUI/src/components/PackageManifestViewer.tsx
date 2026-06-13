import { Check, Clipboard, FileText, Folder, X } from "lucide-react";
import { useMemo, useState } from "react";
import { PackageManifest } from "../types/zms";

interface PackageManifestViewerProps {
  isOpen: boolean;
  manifest: PackageManifest | null;
  onClose: () => void;
}

const groupOrder = ["root", "config", "shared-libraries", "scripts", "execution", "docs", "reports", "logs"];

function fileGroup(file: string): string {
  if (file.startsWith("scripts/lib/")) return "shared-libraries";
  if (file.startsWith("scripts/")) return "scripts";
  if (file.startsWith("execution/")) return "execution";
  return file.includes("/") ? file.split("/")[0] : "root";
}

function groupFiles(files: string[]): Record<string, string[]> {
  return files.reduce<Record<string, string[]>>((groups, file) => {
    const group = fileGroup(file);
    groups[group] = groups[group] ?? [];
    groups[group].push(file);
    return groups;
  }, {});
}

function groupLabel(group: string): string {
  if (group === "root") return "README";
  if (group === "shared-libraries") return "Shared Libraries";
  return group.charAt(0).toUpperCase() + group.slice(1);
}

export default function PackageManifestViewer({ isOpen, manifest, onClose }: PackageManifestViewerProps): JSX.Element | null {
  const [copied, setCopied] = useState(false);
  const groups = useMemo(() => groupFiles(manifest?.files ?? []), [manifest]);
  const groupEntries = useMemo(
    () => Object.entries(groups).sort(([a], [b]) => groupOrder.indexOf(a) - groupOrder.indexOf(b)),
    [groups]
  );

  if (!isOpen || !manifest) {
    return null;
  }

  const copyPackageId = async () => {
    await navigator.clipboard.writeText(manifest.packageId);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 1500);
  };

  return (
    <div className="fixed inset-0 z-[75] flex items-center justify-center bg-slate-950/45 p-4">
      <div className="flex max-h-[88vh] w-full max-w-4xl flex-col overflow-hidden rounded-2xl border border-border bg-surface shadow-panel">
        <div className="flex items-start justify-between gap-4 border-b border-border px-5 py-4">
          <div>
            <h2 className="text-xl font-bold text-text-primary">Package Manifest</h2>
            <p className="mt-1 text-sm text-text-muted">Generated automation bundle files and validation summary.</p>
          </div>
          <button type="button" className="rounded-lg p-2 text-text-muted hover:bg-surface-container" onClick={onClose} aria-label="Close manifest">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="grid gap-4 border-b border-border bg-surface-container/60 px-5 py-4 md:grid-cols-4">
          <div className="md:col-span-2">
            <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Package ID</p>
            <button type="button" className="mt-1 inline-flex items-center gap-2 font-mono text-sm font-bold text-primary" onClick={() => void copyPackageId()}>
              {manifest.packageId}
              {copied ? <Check className="h-4 w-4 text-success" /> : <Clipboard className="h-4 w-4" />}
            </button>
          </div>
          <div>
            <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Generated</p>
            <p className="mt-1 text-sm font-semibold text-text-primary">{new Date(manifest.generatedAt).toLocaleString()}</p>
          </div>
          <div>
            <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">Files</p>
            <p className="mt-1 text-sm font-semibold text-text-primary">{manifest.files.length}</p>
          </div>
        </div>

        <div className="overflow-auto p-5">
          <div className="mb-5 grid grid-cols-2 gap-3 md:grid-cols-7">
            {Object.entries(manifest.summary).map(([label, value]) => (
              <div key={label} className="rounded-xl border border-border bg-surface-container p-3">
                <p className="text-xs font-bold uppercase tracking-wide text-text-subtle">{label}</p>
                <p className="mt-1 font-mono text-lg font-bold text-text-primary">{value}</p>
              </div>
            ))}
          </div>

          <div className="space-y-4">
            {groupEntries.map(([group, files]) => (
              <section key={group} className="rounded-xl border border-border bg-surface p-4">
                <div className="mb-3 flex items-center gap-2 text-sm font-bold text-text-primary">
                  <Folder className="h-4 w-4 text-primary" />
                  {groupLabel(group)}
                </div>
                <div className="space-y-2">
                  {files.map((file) => (
                    <div key={file} className="flex items-center gap-2 rounded-lg bg-surface-container px-3 py-2 font-mono text-xs text-text-muted">
                      <FileText className="h-4 w-4 text-text-subtle" />
                      {file}
                    </div>
                  ))}
                </div>
              </section>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
