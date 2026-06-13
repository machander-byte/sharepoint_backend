import { X } from "lucide-react";
import { FormEvent, useEffect, useState } from "react";
import { ConnectionInput } from "../services/zmsApi";
import { Connection } from "../types/zms";
import GoogleDriveFolderPicker from "./google/GoogleDriveFolderPicker";

interface ConnectionModalProps {
  isOpen: boolean;
  connection?: Connection | null;
  onClose: () => void;
  onSave: (connection: ConnectionInput) => Promise<void>;
}

const connectorTypes = [
  "SharePoint Online Source",
  "SharePoint Online Target",
  "SharePoint On-Prem",
  "Google Drive",
  "File Share"
] as const;

type ConnectorType = (typeof connectorTypes)[number];

function providerFromType(type: ConnectorType): string {
  if (type.startsWith("SharePoint Online")) {
    return "SharePoint Online";
  }
  if (type === "SharePoint On-Prem") {
    return "SharePoint Server";
  }
  if (type === "Google Drive") {
    return "Google Workspace";
  }
  if (type === "File Share") {
    return "SMB";
  }
  return "Migration Source";
}

function kindFromType(type: ConnectorType): "Source" | "Target" {
  return type.includes("Target") ? "Target" : "Source";
}

function connectorTypeFromConnection(connection: Connection): ConnectorType {
  if (connection.provider === "SharePoint Online") {
    return connection.kind === "Target" ? "SharePoint Online Target" : "SharePoint Online Source";
  }
  if (connection.provider === "SharePoint Server") return "SharePoint On-Prem";
  if (connection.provider === "Google Workspace") return "Google Drive";
  return "File Share";
}

function messageFromError(error: unknown): string {
  if (error instanceof Error) return error.message;
  if (typeof error === "object" && error && "details" in error) {
    const details = (error as { details?: unknown }).details;
    if (typeof details === "string") return details;
    if (typeof details === "object" && details && "title" in details) {
      return String((details as { title?: unknown }).title);
    }
  }
  return "Connection save failed.";
}

export default function ConnectionModal({ isOpen, connection, onClose, onSave }: ConnectionModalProps): JSX.Element | null {
  const [connectorType, setConnectorType] = useState<ConnectorType>("SharePoint Online Source");
  const [name, setName] = useState("");
  const [endpointUrl, setEndpointUrl] = useState("");
  const [rootPath, setRootPath] = useState("");
  const [folderUrl, setFolderUrl] = useState("");
  const [tenantId, setTenantId] = useState("");
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [authenticationType, setAuthenticationType] = useState("App-only client secret");
  const [siteUrl, setSiteUrl] = useState("");
  const [documentLibrary, setDocumentLibrary] = useState("Documents");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    if (connection) {
      const type = connectorTypeFromConnection(connection);
      setConnectorType(type);
      setName(connection.name);
      setAuthenticationType(connection.authMethod ?? "App-only client secret");
      setEndpointUrl(connection.tenant?.startsWith("http") ? connection.tenant : connection.tenant ? `https://${connection.tenant}` : "");
      setSiteUrl(connection.tenant?.startsWith("http") ? connection.tenant : connection.tenant ? `https://${connection.tenant}/sites/ZMS` : "");
      setRootPath("");
      setFolderUrl("");
      setDocumentLibrary("Documents");
    } else {
      setConnectorType("SharePoint Online Source");
      setName("");
      setEndpointUrl("https://zettalogix.sharepoint.com/sites/ZMS-HR-Portal");
      setRootPath("");
      setFolderUrl("");
      setAuthenticationType("App-only client secret");
      setSiteUrl("https://zettalogix.sharepoint.com/sites/ZMS-HR-Portal");
      setDocumentLibrary("Documents");
    }
    setTenantId("");
    setClientId("");
    setClientSecret("");
    setUsername("");
    setPassword("");
    setErrors({});
    setSaving(false);
  }, [connection, isOpen]);

  if (!isOpen) {
    return null;
  }

  const isSharePointOnline = connectorType.startsWith("SharePoint Online");
  const isGoogleDrive = connectorType === "Google Drive";
  const isFileShare = connectorType === "File Share";
  const isOnPrem = connectorType === "SharePoint On-Prem";

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const nextErrors: Record<string, string> = {};
    if (!connectorType) {
      nextErrors.connectorType = "Connector type is required.";
    }
    if (!name.trim()) {
      nextErrors.name = "Connection name is required.";
    }
    if (isSharePointOnline && !siteUrl.trim()) {
      nextErrors.siteUrl = "SharePoint site URL is required.";
    }
    if (isSharePointOnline && !tenantId.trim()) {
      nextErrors.tenantId = "Microsoft Entra tenant ID is required.";
    }
    if (isSharePointOnline && !clientId.trim()) {
      nextErrors.clientId = "Microsoft Entra client ID is required.";
    }
    if (isSharePointOnline && !clientSecret.trim()) {
      nextErrors.clientSecret = "Microsoft Entra client secret is required.";
    }
    if (isSharePointOnline && !documentLibrary.trim()) {
      nextErrors.documentLibrary = "Document library is required.";
    }
    if (isGoogleDrive && !folderUrl.trim()) {
      nextErrors.folderUrl = "Google Drive folder URL or folder ID is required.";
    }
    if (isFileShare && !rootPath.trim()) {
      nextErrors.rootPath = "File share root path is required.";
    }
    if (isOnPrem && !endpointUrl.trim()) {
      nextErrors.endpointUrl = "SharePoint On-Prem URL is required.";
    }

    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    setSaving(true);
    try {
      await onSave({
        id: connection?.id,
        name: name.trim(),
        kind: kindFromType(connectorType),
        provider: providerFromType(connectorType),
        status: connection?.status ?? "Disconnected",
        tenant: endpointUrl.trim() || siteUrl.trim() || folderUrl.trim() || rootPath.trim() || undefined,
        authMethod: authenticationType,
        url: isSharePointOnline ? siteUrl.trim() : endpointUrl.trim(),
        rootPath: isFileShare ? rootPath.trim() : undefined,
        folderUrl: isGoogleDrive ? folderUrl.trim() : undefined,
        username: username.trim() || undefined,
        password: password.trim() || undefined,
        tenantId: isSharePointOnline ? tenantId.trim() : undefined,
        clientId: isSharePointOnline ? clientId.trim() : undefined,
        clientSecret: isSharePointOnline ? clientSecret.trim() : undefined,
        documentLibraryName: isSharePointOnline ? documentLibrary.trim() : undefined
      });
      onClose();
    } catch (error) {
      setErrors({ form: messageFromError(error) });
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-slate-950/45 p-4">
      <form className="w-full max-w-2xl rounded-2xl border border-border bg-surface shadow-panel" onSubmit={submit}>
        <div className="flex items-start justify-between border-b border-border px-5 py-4">
          <div>
            <h2 className="text-xl font-bold text-text-primary">{connection ? "Configure Connection" : "New Connection"}</h2>
            <p className="mt-1 text-sm text-text-muted">Connection profiles are saved through the backend and scoped to the signed-in user.</p>
          </div>
          <button type="button" className="rounded-lg p-2 text-text-muted hover:bg-surface-container" onClick={onClose} aria-label="Close connection modal">
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="grid gap-4 p-5 md:grid-cols-2">
          {errors.form ? (
            <div className="md:col-span-2 rounded-lg border border-error/25 bg-error-soft/70 p-3 text-sm text-error">
              {errors.form}
            </div>
          ) : null}

          <label>
            <span className="mb-2 block text-sm font-semibold text-text-muted">Connector Type</span>
            <select className="w-full rounded-lg border border-border px-3 py-2" value={connectorType} onChange={(event) => setConnectorType(event.target.value as ConnectorType)}>
              {connectorTypes.map((type) => (
                <option key={type}>{type}</option>
              ))}
            </select>
            {errors.connectorType ? <span className="mt-1 block text-xs text-error">{errors.connectorType}</span> : null}
          </label>

          <label>
            <span className="mb-2 block text-sm font-semibold text-text-muted">Connection Name</span>
            <input className="w-full rounded-lg border border-border px-3 py-2" value={name} onChange={(event) => setName(event.target.value)} />
            {errors.name ? <span className="mt-1 block text-xs text-error">{errors.name}</span> : null}
          </label>

          {isSharePointOnline ? (
            <>
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Site URL</span>
                <input className="w-full rounded-lg border border-border px-3 py-2" value={siteUrl} onChange={(event) => setSiteUrl(event.target.value)} />
                {errors.siteUrl ? <span className="mt-1 block text-xs text-error">{errors.siteUrl}</span> : null}
              </label>
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Tenant ID</span>
                <input className="w-full rounded-lg border border-border px-3 py-2 font-mono text-sm" value={tenantId} onChange={(event) => setTenantId(event.target.value)} />
                {errors.tenantId ? <span className="mt-1 block text-xs text-error">{errors.tenantId}</span> : null}
              </label>
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Client ID</span>
                <input className="w-full rounded-lg border border-border px-3 py-2 font-mono text-sm" value={clientId} onChange={(event) => setClientId(event.target.value)} />
                {errors.clientId ? <span className="mt-1 block text-xs text-error">{errors.clientId}</span> : null}
              </label>
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Client Secret</span>
                <input className="w-full rounded-lg border border-border px-3 py-2" type="password" value={clientSecret} onChange={(event) => setClientSecret(event.target.value)} />
                {errors.clientSecret ? <span className="mt-1 block text-xs text-error">{errors.clientSecret}</span> : null}
              </label>
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Authentication Type</span>
                <select className="w-full rounded-lg border border-border px-3 py-2" value={authenticationType} onChange={(event) => setAuthenticationType(event.target.value)}>
                  <option>App-only client secret</option>
                  <option>App-only certificate</option>
                  <option>Managed identity</option>
                </select>
              </label>
              <label className="md:col-span-2">
                <span className="mb-2 block text-sm font-semibold text-text-muted">Document Library</span>
                <input className="w-full rounded-lg border border-border px-3 py-2" value={documentLibrary} onChange={(event) => setDocumentLibrary(event.target.value)} />
                {errors.documentLibrary ? <span className="mt-1 block text-xs text-error">{errors.documentLibrary}</span> : null}
              </label>
            </>
          ) : isGoogleDrive ? (
            <div className="md:col-span-2">
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Google Drive Folder URL or ID</span>
                <input className="w-full rounded-lg border border-border px-3 py-2" value={folderUrl} onChange={(event) => setFolderUrl(event.target.value)} />
                {errors.folderUrl ? <span className="mt-1 block text-xs text-error">{errors.folderUrl}</span> : null}
              </label>
              <div className="mt-3">
                <GoogleDriveFolderPicker
                  disabled={saving}
                  onFolderSelected={(folder) => {
                    setFolderUrl(folder.url);
                    if (!name.trim()) {
                      setName(folder.name);
                    }
                  }}
                />
              </div>
              <span className="mt-2 block text-xs text-text-muted">Folder picker selects the source folder. Backend Google OAuth credentials must also be configured before this connection can be tested.</span>
            </div>
          ) : isFileShare ? (
            <label className="md:col-span-2">
              <span className="mb-2 block text-sm font-semibold text-text-muted">Root Path</span>
              <input className="w-full rounded-lg border border-border px-3 py-2" placeholder="\\\\server\\share or D:\\migration-source" value={rootPath} onChange={(event) => setRootPath(event.target.value)} />
              {errors.rootPath ? <span className="mt-1 block text-xs text-error">{errors.rootPath}</span> : null}
            </label>
          ) : (
            <>
              <label className="md:col-span-2">
                <span className="mb-2 block text-sm font-semibold text-text-muted">SharePoint On-Prem URL</span>
                <input className="w-full rounded-lg border border-border px-3 py-2" value={endpointUrl} onChange={(event) => setEndpointUrl(event.target.value)} />
                {errors.endpointUrl ? <span className="mt-1 block text-xs text-error">{errors.endpointUrl}</span> : null}
              </label>
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Username Optional</span>
                <input className="w-full rounded-lg border border-border px-3 py-2" value={username} onChange={(event) => setUsername(event.target.value)} />
              </label>
              <label>
                <span className="mb-2 block text-sm font-semibold text-text-muted">Password Optional</span>
                <input className="w-full rounded-lg border border-border px-3 py-2" type="password" value={password} onChange={(event) => setPassword(event.target.value)} />
              </label>
            </>
          )}
        </div>

        <div className="flex justify-end gap-3 border-t border-border px-5 py-4">
          <button type="button" className="rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" disabled={saving} className="rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90 disabled:opacity-60">
            {saving ? "Saving..." : "Save Connection"}
          </button>
        </div>
      </form>
    </div>
  );
}
