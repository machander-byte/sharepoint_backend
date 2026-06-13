import { Clipboard, Download, Eye } from "lucide-react";
import { useState } from "react";
import PackageManifestViewer from "../components/PackageManifestViewer";
import PageHeader from "../components/PageHeader";
import StatusBadge from "../components/StatusBadge";
import { zmsApi } from "../services/zmsApi";
import { useZmsDispatch, useZmsState } from "../state/ZmsStateProvider";
import { toastActions } from "../state/toastActions";
import { PackageManifest } from "../types/zms";

export default function PackageCenterPage(): JSX.Element {
  const state = useZmsState();
  const dispatch = useZmsDispatch();
  const [manifest, setManifest] = useState<PackageManifest | null>(null);
  const [manifestOpen, setManifestOpen] = useState(false);

  const downloadPackage = async (packageId: string) => {
    const result = await zmsApi.downloadEnvironmentPackage(packageId, state.generatedEnvironmentConfig ?? undefined);
    dispatch({
      type: "ADD_TOAST",
      payload: result.source === "mock"
        ? toastActions.warning("Backend unavailable", "Downloaded JSON fallback instead of ZIP.")
        : toastActions.success("Package downloaded successfully.")
    });
  };

  const viewManifest = async (packageId: string) => {
    const packageResult = state.generatedPackages.find((pkg) => pkg.packageId === packageId);
    const nextManifest = await zmsApi.getPackageManifest(packageId, packageResult?.summary);
    setManifest(nextManifest);
    setManifestOpen(true);
  };

  const copyPackageId = async (packageId: string) => {
    await navigator.clipboard.writeText(packageId);
    dispatch({ type: "ADD_TOAST", payload: toastActions.success("Package ID copied.") });
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Environment Packages"
        subtitle="Download generated SharePoint automation bundles."
      />

      <section className="overflow-hidden rounded-xl border border-border bg-surface shadow-card">
        <div className="border-b border-border bg-surface-container px-4 py-3">
          <h2 className="text-sm font-bold text-text-primary">Generated Packages</h2>
        </div>
        {state.generatedPackages.length === 0 ? (
          <div className="p-8 text-center">
            <p className="text-sm font-semibold text-text-primary">No generated packages yet.</p>
            <p className="mt-1 text-sm text-text-muted">Generate a package from Environment Builder to see it here.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="border-b border-border bg-surface-container text-xs font-bold uppercase tracking-wide text-text-subtle">
                <tr>
                  <th className="px-4 py-3">Package ID</th>
                  <th className="px-4 py-3">Generated Date</th>
                  <th className="px-4 py-3">Site Collections</th>
                  <th className="px-4 py-3">Files</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {state.generatedPackages.map((pkg) => (
                  <tr key={pkg.packageId} className="hover:bg-surface-container/60">
                    <td className="px-4 py-3 font-mono text-xs text-text-primary">{pkg.packageId}</td>
                    <td className="px-4 py-3 text-text-muted">{pkg.generatedAt ? new Date(pkg.generatedAt).toLocaleString() : "Current session"}</td>
                    <td className="px-4 py-3 font-mono text-text-primary">{pkg.summary?.siteCollections ?? "-"}</td>
                    <td className="px-4 py-3 font-mono text-text-primary">{pkg.files.length}</td>
                    <td className="px-4 py-3"><StatusBadge status={pkg.source === "mock" ? "Warning" : "Connected"} /></td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-2">
                        <button type="button" className="rounded-lg p-2 text-text-muted hover:bg-surface-container" onClick={() => void copyPackageId(pkg.packageId)} title="Copy Package ID">
                          <Clipboard className="h-4 w-4" />
                        </button>
                        <button type="button" className="rounded-lg p-2 text-text-muted hover:bg-surface-container" onClick={() => void viewManifest(pkg.packageId)} title="View Manifest">
                          <Eye className="h-4 w-4" />
                        </button>
                        <button type="button" className="rounded-lg p-2 text-primary hover:bg-primary-soft" onClick={() => void downloadPackage(pkg.packageId)} title="Download ZIP">
                          <Download className="h-4 w-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <PackageManifestViewer isOpen={manifestOpen} manifest={manifest} onClose={() => setManifestOpen(false)} />
    </div>
  );
}
