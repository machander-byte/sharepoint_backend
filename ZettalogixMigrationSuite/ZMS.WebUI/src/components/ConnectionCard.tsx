import { Cloud, CloudUpload, DatabaseZap, FolderInput, HardDrive, LucideIcon, PackageOpen } from "lucide-react";
import StatusBadge from "./StatusBadge";
import { Connection } from "../types/zms";

interface ConnectionCardProps {
  connection: Connection;
  onAction?: (action: string, connection: Connection) => void;
}

const providerIcons: Record<string, LucideIcon> = {
  "SharePoint Online": Cloud,
  "SharePoint Server": DatabaseZap,
  "Box Enterprise": PackageOpen,
  "Google Workspace": CloudUpload,
  SMB: HardDrive
};

export default function ConnectionCard({ connection, onAction }: ConnectionCardProps): JSX.Element {
  const Icon = providerIcons[connection.provider] ?? FolderInput;

  return (
    <article className="flex min-h-[260px] flex-col rounded-xl border border-border bg-surface p-5 shadow-card">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary-soft text-primary">
            <Icon className="h-5 w-5" />
          </div>
          <div>
            <h3 className="font-bold text-text-primary">{connection.name}</h3>
            <p className="mt-1 text-xs font-bold uppercase tracking-wide text-text-subtle">{connection.kind}</p>
          </div>
        </div>
        <StatusBadge status={connection.status} />
      </div>

      <div className="mt-5 flex-1 space-y-3 text-sm">
        {connection.tenant ? (
          <div className="flex justify-between gap-4">
            <span className="text-text-muted">Tenant</span>
            <span className="text-right font-mono text-text-primary">{connection.tenant}</span>
          </div>
        ) : null}
        {connection.authMethod ? (
          <div className="flex justify-between gap-4">
            <span className="text-text-muted">Auth Method</span>
            <span className="text-right font-medium text-text-primary">{connection.authMethod}</span>
          </div>
        ) : null}
        {connection.lastSync ? (
          <div className="flex justify-between gap-4">
            <span className="text-text-muted">Last Sync</span>
            <span className="text-right font-medium text-text-primary">{connection.lastSync}</span>
          </div>
        ) : null}
        {connection.warning ? (
          <div className="rounded-lg border border-error/20 bg-error-soft/70 p-3 text-sm text-error">
            {connection.warning}
          </div>
        ) : null}
        {connection.message ? (
          <p className="rounded-lg border border-border bg-surface-container p-3 leading-6 text-text-muted">{connection.message}</p>
        ) : null}
      </div>

      <div className="mt-5 flex flex-wrap justify-end gap-2 border-t border-border pt-4">
        {connection.actions.map((action) => (
          <button
            key={action}
            type="button"
            onClick={() => onAction?.(action, connection)}
            className={action.includes("Fix") || action.includes("Download")
              ? "rounded-lg bg-primary px-3 py-2 text-sm font-semibold text-white hover:bg-primary/90"
              : "rounded-lg border border-border px-3 py-2 text-sm font-semibold text-text-primary hover:bg-surface-container"}
          >
            {action}
          </button>
        ))}
      </div>
    </article>
  );
}
