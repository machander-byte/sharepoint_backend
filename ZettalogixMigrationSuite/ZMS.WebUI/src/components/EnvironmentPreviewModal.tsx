import { Code2, ListTree, TableProperties, X } from "lucide-react";
import { useMemo, useState } from "react";
import { EnvironmentConfig } from "../types/zms";

interface EnvironmentPreviewModalProps {
  isOpen: boolean;
  config: EnvironmentConfig | null;
  onClose: () => void;
}

type PreviewTab = "Tree View" | "Summary" | "JSON Preview";

const tabs: Array<{ label: PreviewTab; icon: typeof ListTree }> = [
  { label: "Tree View", icon: ListTree },
  { label: "Summary", icon: TableProperties },
  { label: "JSON Preview", icon: Code2 }
];

export default function EnvironmentPreviewModal({ isOpen, config, onClose }: EnvironmentPreviewModalProps): JSX.Element | null {
  const [activeTab, setActiveTab] = useState<PreviewTab>("Tree View");

  const summary = useMemo(() => {
    if (!config) {
      return [];
    }

    return [
      ["Site Collections", config.siteCollections.length],
      ["Subsites", config.siteCollections.reduce((sum, site) => sum + site.subsites.length, 0)],
      ["Libraries", config.siteCollections.reduce((sum, site) => sum + site.libraries.length, 0)],
      ["Lists", config.siteCollections.reduce((sum, site) => sum + site.lists.length, 0)],
      ["Metadata Fields", config.siteCollections.reduce((sum, site) => sum + site.metadataFields.length, 0)],
      ["Permission Groups", config.siteCollections.reduce((sum, site) => sum + site.permissionGroups.length, 0)],
      ["Edge Cases", config.siteCollections.reduce((sum, site) => sum + site.edgeCases.length, 0)]
    ] as Array<[string, number]>;
  }, [config]);

  if (!isOpen || !config) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/45 p-4">
      <div className="flex max-h-[88vh] w-full max-w-6xl flex-col overflow-hidden rounded-2xl border border-border bg-surface shadow-panel">
        <div className="flex items-start justify-between gap-4 border-b border-border px-5 py-4">
          <div>
            <h2 className="text-xl font-bold text-text-primary">Environment Structure Preview</h2>
            <p className="mt-1 text-sm text-text-muted">Review the generated SharePoint Online configuration before export.</p>
          </div>
          <button type="button" className="rounded-lg p-2 text-text-muted hover:bg-surface-container" onClick={onClose} aria-label="Close preview">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="border-b border-border px-5 py-3">
          <div className="flex gap-2 overflow-x-auto">
            {tabs.map((tab) => {
              const Icon = tab.icon;
              return (
                <button
                  key={tab.label}
                  type="button"
                  className={activeTab === tab.label
                    ? "inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-2 text-sm font-bold text-white"
                    : "inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-semibold text-text-muted hover:bg-surface-container"}
                  onClick={() => setActiveTab(tab.label)}
                >
                  <Icon className="h-4 w-4" />
                  {tab.label}
                </button>
              );
            })}
          </div>
        </div>

        <div className="overflow-auto p-5">
          {activeTab === "Tree View" ? (
            <div className="space-y-5 font-mono text-sm leading-7 text-text-primary">
              {config.siteCollections.map((site) => (
                <div key={site.id} className="rounded-xl bg-surface-container p-4">
                  <div className="font-bold">{site.title}</div>
                  {site.subsites.map((subsite, subsiteIndex) => (
                    <div key={subsite.id} className="pl-4">
                      {subsiteIndex === site.subsites.length - 1 ? "+--" : "|--"} {subsite.title}
                      {subsiteIndex === site.subsites.length - 1 ? (
                        <div className="pl-4">
                          {site.libraries.map((library, libraryIndex) => (
                            <div key={library.id}>
                              {libraryIndex === site.libraries.length - 1 ? "+--" : "|--"} {library.title}
                            </div>
                          ))}
                        </div>
                      ) : null}
                    </div>
                  ))}
                </div>
              ))}
            </div>
          ) : null}

          {activeTab === "Summary" ? (
            <div className="overflow-hidden rounded-xl border border-border">
              <table className="w-full text-left">
                <thead className="bg-surface-container text-xs font-bold uppercase tracking-wide text-text-muted">
                  <tr>
                    <th className="px-4 py-3">Metric</th>
                    <th className="px-4 py-3">Value</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border text-sm">
                  {summary.map(([metric, value]) => (
                    <tr key={metric}>
                      <td className="px-4 py-3 font-semibold text-text-primary">{metric}</td>
                      <td className="px-4 py-3 font-mono text-text-primary">{value}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}

          {activeTab === "JSON Preview" ? (
            <pre className="max-h-[58vh] overflow-auto rounded-xl bg-slate-950 p-4 text-xs leading-6 text-slate-100">
              {JSON.stringify(config, null, 2)}
            </pre>
          ) : null}
        </div>
      </div>
    </div>
  );
}
