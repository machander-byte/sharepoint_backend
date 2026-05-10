import { FormEvent, useMemo, useState } from "react";
import ConnectionCard from "../components/ConnectionCard/ConnectionCard";
import ConfirmDialog from "../components/ConfirmDialog/ConfirmDialog";
import EmptyState from "../components/EmptyState/EmptyState";
import GoogleDriveFolderPicker, { GoogleDriveFolderSelection } from "../components/google/GoogleDriveFolderPicker";
import { useAppStore } from "../hooks/useAppStore";
import { formatConnectionType } from "../utils/formatters";
import { ConnectionType, CreateConnectionInput } from "../utils/models";

const initialForm: CreateConnectionInput = {
  name: "",
  type: "GoogleDrive",
  url: "",
  rootPath: "",
  folderId: "",
  folderUrl: "",
  folderName: "",
  username: "",
  password: "",
  clientId: "",
  clientSecret: "",
  tenantId: "",
  documentLibraryName: ""
};

function getEndpointLabel(type: ConnectionType): string {
  switch (type) {
    case "SharePointOnline":
      return "SharePoint site URL";
    case "SharePointOnPrem":
      return "SharePoint source URL";
    case "FileShare":
      return "File share path or UNC";
    case "GoogleDrive":
      return "Google Drive folder URL or ID";
    default:
      return "Endpoint";
  }
}

function extractGoogleDriveFolderId(candidate: string): string {
  const trimmed = candidate.trim();
  if (!trimmed) {
    return "";
  }

  const folderMatch = trimmed.match(/\/drive\/(?:u\/\d+\/)?folders\/([A-Za-z0-9_-]+)/i);
  if (folderMatch?.[1]) {
    return folderMatch[1];
  }

  return /^[A-Za-z0-9_-]{10,}$/.test(trimmed) ? trimmed : "";
}

function buildGoogleDriveFolderUrl(folderId: string): string {
  return `https://drive.google.com/drive/folders/${folderId}`;
}

function isValidGoogleDriveFolderUrlOrId(candidate: string): boolean {
  return Boolean(extractGoogleDriveFolderId(candidate));
}

function validateConnection(form: CreateConnectionInput): string | null {
  if (!form.name.trim()) {
    return "Connection name is required.";
  }

  if (form.type === "GoogleDrive") {
    const folderId = form.folderId.trim() || extractGoogleDriveFolderId(form.folderUrl);

    if (!folderId && !form.folderUrl.trim()) {
      return "Google Drive folder link is required.";
    }

    if (form.folderUrl.trim() && !isValidGoogleDriveFolderUrlOrId(form.folderUrl)) {
      return "Paste a valid Google Drive folder link.";
    }

  }

  if (form.type === "SharePointOnline") {
    if (!form.url.trim()) {
      return "SharePoint site URL is required.";
    }

    if (!form.tenantId.trim()) {
      return "Microsoft Entra tenant ID is required.";
    }

    if (!form.clientId.trim()) {
      return "Microsoft Entra client ID is required.";
    }

    if (!form.clientSecret.trim()) {
      return "Microsoft Entra client secret is required.";
    }

    if (!form.documentLibraryName.trim()) {
      return "SharePoint document library name is required.";
    }
  }

  return null;
}

export default function ConnectionsPage(): JSX.Element {
  const connections = useAppStore((state) => state.connections);
  const createConnection = useAppStore((state) => state.createConnection);
  const testConnection = useAppStore((state) => state.testConnection);
  const deleteConnection = useAppStore((state) => state.deleteConnection);
  const loading = useAppStore((state) => state.loading.connectionsMutation);
  const [form, setForm] = useState<CreateConnectionInput>(initialForm);
  const [formMessage, setFormMessage] = useState<{ tone: "success" | "error"; text: string } | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    const validationError = validateConnection(form);
    if (validationError) {
      setFormMessage({ tone: "error", text: validationError });
      return;
    }

    try {
      await createConnection(form);
      setForm(initialForm);
      setFormMessage(null);
    } catch (error) {
      setFormMessage({
        tone: "error",
        text: error instanceof Error ? error.message : "Connection could not be saved."
      });
    }
  };

  const selectGoogleFolder = (folder: GoogleDriveFolderSelection) => {
    const folderUrl = folder.url || buildGoogleDriveFolderUrl(folder.id);
    setForm({
      ...form,
      url: folderUrl,
      rootPath: folder.id,
      folderId: folder.id,
      folderUrl,
      folderName: folder.name
    });
    setFormMessage({ tone: "success", text: "Folder selected successfully." });
  };

  const updateGoogleFolderUrl = (folderUrl: string) => {
    const folderId = extractGoogleDriveFolderId(folderUrl);
    setForm({
      ...form,
      url: folderUrl,
      folderUrl,
      folderId: folderId || form.folderId,
      rootPath: folderId || form.rootPath,
      folderName: folderId ? form.folderName : ""
    });
    setFormMessage(null);
  };

  const typeSummary = useMemo(() => {
    switch (form.type) {
      case "GoogleDrive":
        return "Paste a Google Drive folder link. The backend uses configured Google credentials for migration.";
      case "SharePointOnline":
        return "Use Microsoft Entra application credentials so the backend can upload to SharePoint Online libraries.";
      case "FileShare":
        return "Point the worker at a local or network-accessible file share path.";
      case "SharePointOnPrem":
        return "Register the legacy SharePoint endpoint for source discovery and migration planning.";
      default:
        return "Register a reusable migration endpoint.";
    }
  }, [form.type]);
  const deleteTargetName = useMemo(
    () => connections.find((connection) => connection.id === deleteTarget)?.name ?? "",
    [connections, deleteTarget]
  );

  return (
    <>
    <div className="page-stack">
      <section className="split-panel">
        <article className="surface-card">
          <div className="section-heading">
            <div>
              <span className="eyebrow">Register Gateway</span>
              <h2>Add source or destination connection</h2>
              <p>Store reusable endpoints with the credentials needed for real discovery, validation, and direct transfer.</p>
            </div>
          </div>

          <form className="form-grid" onSubmit={submit}>
            <label>
              Connection name
              <input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} />
            </label>
            <label>
              Type
              <select
                value={form.type}
                onChange={(event) => {
                  setForm({ ...form, type: event.target.value as ConnectionType });
                  setFormMessage(null);
                }}
              >
                <option value="GoogleDrive">Google Drive</option>
                <option value="SharePointOnline">SharePoint Online</option>
                <option value="FileShare">File Share</option>
                <option value="SharePointOnPrem">SharePoint On-Prem</option>
              </select>
            </label>

            {form.type === "GoogleDrive" ? (
              <>
                <div className="form-note full-width">
                  <strong>Google Drive Folder Link</strong>
                  <p>Paste the Google Drive folder link you want to migrate.</p>
                </div>
                <div className="full-width">
                  <GoogleDriveFolderPicker onFolderSelected={selectGoogleFolder} disabled={loading} />
                </div>
                <label className="full-width">
                  Google Drive Folder Link
                  <input
                    value={form.folderUrl}
                    placeholder="https://drive.google.com/drive/folders/..."
                    onChange={(event) => updateGoogleFolderUrl(event.target.value)}
                  />
                </label>
                {form.folderName ? (
                  <div className="selected-folder full-width">
                    <span className="material-symbols-outlined">folder</span>
                    <div>
                      <strong>{form.folderName}</strong>
                      <p>{form.folderUrl}</p>
                    </div>
                  </div>
                ) : null}
              </>
            ) : form.type !== "SharePointOnline" ? (
              <label className="full-width">
                {getEndpointLabel(form.type)}
                <input value={form.url} onChange={(event) => setForm({ ...form, url: event.target.value })} />
              </label>
            ) : null}

            {form.type === "FileShare" ? (
              <label className="full-width">
                Root path override
                <input
                  placeholder="Optional if the endpoint field already contains the share path"
                  value={form.rootPath}
                  onChange={(event) => setForm({ ...form, rootPath: event.target.value })}
                />
              </label>
            ) : null}

            {form.type === "SharePointOnPrem" ? (
              <>
                <label>
                  Username
                  <input value={form.username} onChange={(event) => setForm({ ...form, username: event.target.value })} />
                </label>
                <label>
                  Password
                  <input
                    type="password"
                    value={form.password}
                    onChange={(event) => setForm({ ...form, password: event.target.value })}
                  />
                </label>
              </>
            ) : null}

            {form.type === "SharePointOnline" ? (
              <>
                <label>
                  Tenant ID
                  <input value={form.tenantId} onChange={(event) => setForm({ ...form, tenantId: event.target.value })} />
                </label>
                <label>
                  Client ID
                  <input value={form.clientId} onChange={(event) => setForm({ ...form, clientId: event.target.value })} />
                </label>
                <label className="full-width">
                  SharePoint Site URL
                  <input value={form.url} onChange={(event) => setForm({ ...form, url: event.target.value })} />
                </label>
                <label className="full-width">
                  Client secret
                  <input
                    type="password"
                    value={form.clientSecret}
                    onChange={(event) => setForm({ ...form, clientSecret: event.target.value })}
                  />
                </label>
                <label className="full-width">
                  Document Library Name
                  <input
                    placeholder="Example: Documents or Shared Documents"
                    value={form.documentLibraryName}
                    onChange={(event) => setForm({ ...form, documentLibraryName: event.target.value })}
                  />
                </label>
              </>
            ) : null}

            {form.type === "GoogleDrive" ? (
              <p className="inline-message full-width">
                Google authentication is configured on the backend. This screen only needs the source folder link.
              </p>
            ) : null}

            {formMessage ? (
              <p className={`inline-message ${formMessage.tone} full-width`}>{formMessage.text}</p>
            ) : null}

            <div className="form-actions full-width">
              <button type="submit" className="primary-button" disabled={loading}>
                {loading ? "Saving..." : "Save connection"}
              </button>
            </div>
          </form>
        </article>

        <article className="tonal-card">
          <div className="page-stack">
            <div className="metric-box">
              <span>Connection mode</span>
              <strong>{formatConnectionType(form.type)}</strong>
              <p>{typeSummary}</p>
            </div>
            <div className="metric-box">
              <span>Registered gateways</span>
              <strong>{connections.length}</strong>
              <p>Available for discovery, target validation, and job creation from the migration wizard.</p>
            </div>
          </div>
        </article>
      </section>

      {connections.length === 0 ? (
        <EmptyState title="No connections available" description="Register a source or target endpoint to begin." />
      ) : (
        <section className="card-grid">
          {connections.map((connection) => (
            <ConnectionCard
              key={connection.id}
              connection={connection}
              onTest={(id) => void testConnection(id)}
              onDelete={setDeleteTarget}
            />
          ))}
        </section>
      )}
    </div>
      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        title="Delete connection?"
        description={`Remove "${deleteTargetName}" from your workspace. Existing job history remains available, but new migrations cannot use this connection.`}
        confirmLabel="Delete connection"
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => {
          if (deleteTarget) {
            void deleteConnection(deleteTarget);
          }
          setDeleteTarget(null);
        }}
      />
    </>
  );
}
