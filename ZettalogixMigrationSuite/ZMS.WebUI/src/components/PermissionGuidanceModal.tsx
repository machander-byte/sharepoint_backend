import { ShieldAlert, X } from "lucide-react";

interface PermissionGuidanceModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function PermissionGuidanceModal({ isOpen, onClose }: PermissionGuidanceModalProps): JSX.Element | null {
  if (!isOpen) {
    return null;
  }

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/45 p-4">
      <div className="w-full max-w-xl rounded-2xl border border-border bg-surface shadow-panel">
        <div className="flex items-start justify-between border-b border-border px-5 py-4">
          <div className="flex gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-error-soft text-error">
              <ShieldAlert className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-xl font-bold text-text-primary">Microsoft Graph Permissions Required</h2>
              <p className="mt-1 text-sm text-text-muted">Permission guidance for SharePoint Online target validation.</p>
            </div>
          </div>
          <button type="button" className="rounded-lg p-2 text-text-muted hover:bg-surface-container" onClick={onClose} aria-label="Close permission guidance">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="space-y-4 p-5">
          <p className="rounded-xl border border-error/20 bg-error-soft p-4 text-sm font-semibold leading-6 text-error">
            Microsoft Graph permission missing: Files.ReadWrite.All. Grant admin consent in Microsoft Entra ID, then retest the connection.
          </p>
          <div className="rounded-xl border border-border bg-surface-container p-4">
            <h3 className="font-bold text-text-primary">Required Graph permissions</h3>
            <ul className="mt-3 list-disc space-y-2 pl-5 text-sm text-text-muted">
              <li>Files.ReadWrite.All</li>
              <li>Sites.ReadWrite.All</li>
            </ul>
          </div>
          <p className="text-sm leading-6 text-text-muted">
            This is frontend-only guidance. The backend phase should validate app registration permissions and admin consent status.
          </p>
        </div>
      </div>
    </div>
  );
}
