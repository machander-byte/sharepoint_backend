import { Plus, Search } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import ConnectionCard from "../components/ConnectionCard";
import ConnectionModal from "../components/ConnectionModal";
import PageHeader from "../components/PageHeader";
import PermissionGuidanceModal from "../components/PermissionGuidanceModal";
import { ConnectionInput, zmsApi } from "../services/zmsApi";
import { useZmsDispatch, useZmsState } from "../state/ZmsStateProvider";
import { toastActions } from "../state/toastActions";
import { Connection } from "../types/zms";

const filters = ["All", "Source", "Target", "Connected", "Warning", "Disconnected", "Config Required"] as const;
type ConnectionFilter = (typeof filters)[number];

function messageFromError(error: unknown): string {
  if (error instanceof Error) return error.message;
  if (typeof error === "object" && error && "details" in error) {
    const details = (error as { details?: unknown }).details;
    if (typeof details === "string") return details;
    if (typeof details === "object" && details && "title" in details) {
      return String((details as { title?: unknown }).title);
    }
  }
  return "The backend request failed.";
}

export default function ConnectionsPage(): JSX.Element {
  const { connections } = useZmsState();
  const dispatch = useZmsDispatch();
  const [searchTerm, setSearchTerm] = useState("");
  const [filter, setFilter] = useState<ConnectionFilter>("All");
  const [modalOpen, setModalOpen] = useState(false);
  const [guidanceOpen, setGuidanceOpen] = useState(false);
  const [editingConnection, setEditingConnection] = useState<Connection | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState("");

  const loadConnections = async () => {
    setLoading(true);
    setLoadError("");
    try {
      const nextConnections = await zmsApi.getConnections();
      dispatch({ type: "SET_CONNECTIONS", payload: nextConnections });
    } catch (error) {
      const message = messageFromError(error);
      setLoadError(message);
      dispatch({ type: "ADD_TOAST", payload: toastActions.error("Connections failed to load", message) });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadConnections();
  }, []);

  const filteredConnections = useMemo(() => {
    const normalized = searchTerm.trim().toLowerCase();
    return connections.filter((connection) => {
      const matchesSearch = !normalized || [connection.name, connection.provider, connection.tenant, connection.status, connection.kind]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(normalized));

      const matchesFilter =
        filter === "All" ||
        connection.kind === filter ||
        connection.status === filter;

      return matchesSearch && matchesFilter;
    });
  }, [connections, filter, searchTerm]);

  const openNewConnection = () => {
    setEditingConnection(null);
    setModalOpen(true);
  };

  const saveConnection = async (input: ConnectionInput) => {
    const connection = await zmsApi.createConnection(input);
    dispatch({ type: "UPSERT_CONNECTION", payload: connection });
    dispatch({ type: "ADD_TOAST", payload: toastActions.success("Connection saved", `${connection.name} was saved to the backend.`) });
  };

  const testConnection = async (connection: Connection) => {
    try {
      const result = await zmsApi.testConnection(connection.id);
      dispatch({
        type: "UPDATE_CONNECTION",
        payload: {
          id: connection.id,
          patch: {
            status: result.status,
            warning: result.status === "Warning" ? result.message : undefined,
            message: result.status === "Connected" ? result.message : connection.message,
            actions: result.status === "Warning" ? ["Fix Permissions", "Configure", "Test"] : ["Test", "Configure"]
          }
        }
      });
      dispatch({
        type: "ADD_TOAST",
        payload: result.status === "Connected"
          ? toastActions.success("Connection test passed", result.message)
          : toastActions.warning("Connection test warning", result.message)
      });
    } catch (error) {
      const message = messageFromError(error);
      dispatch({ type: "ADD_TOAST", payload: toastActions.error("Connection test failed", message) });
    }
  };

  const handleAction = (action: string, connection: Connection) => {
    if (action === "Configure") {
      setEditingConnection(connection);
      setModalOpen(true);
      return;
    }
    if (action === "Test") {
      void testConnection(connection);
      return;
    }
    if (action === "Fix Permissions") {
      setGuidanceOpen(true);
      return;
    }
    dispatch({ type: "ADD_TOAST", payload: toastActions.info("Mock action unavailable", `${action} will be connected in a later phase.`) });
  };

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Connections"
        subtitle="Create, load, and test backend-backed source and target connection profiles."
        actions={
          <div className="flex flex-wrap gap-2">
            <button type="button" className="inline-flex items-center gap-2 rounded-lg border border-border px-4 py-2 text-sm font-bold text-text-primary hover:bg-surface-container" onClick={() => void loadConnections()}>
              Refresh
            </button>
            <button type="button" className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary/90" onClick={openNewConnection}>
              <Plus className="h-4 w-4" />
              New Connection
            </button>
          </div>
        }
      />

      {loading ? (
        <div className="rounded-xl border border-border bg-surface p-4 text-sm text-text-muted shadow-card">
          Loading backend connections...
        </div>
      ) : null}

      {loadError ? (
        <div className="rounded-xl border border-error/25 bg-error-soft/70 p-4 text-sm text-error">
          {loadError}
        </div>
      ) : null}

      <section className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex w-full max-w-md items-center gap-2 rounded-xl border border-border bg-surface px-3 py-2 shadow-card">
          <Search className="h-4 w-4 text-text-muted" />
          <input
            className="w-full bg-transparent text-sm text-text-primary placeholder:text-text-subtle"
            placeholder="Search connections..."
            type="search"
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
          />
        </div>
        <div className="flex gap-2 overflow-x-auto pb-1">
          {filters.map((item) => (
            <button
              key={item}
              type="button"
              className={filter === item
                ? "shrink-0 rounded-lg bg-primary px-3 py-2 text-sm font-bold text-white"
                : "shrink-0 rounded-lg border border-border bg-surface px-3 py-2 text-sm font-bold text-text-primary hover:bg-surface-container"}
              onClick={() => setFilter(item)}
            >
              {item}
            </button>
          ))}
        </div>
      </section>

      <section className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
        {filteredConnections.map((connection) => (
          <ConnectionCard key={connection.id} connection={connection} onAction={handleAction} />
        ))}
      </section>

      <ConnectionModal
        isOpen={modalOpen}
        connection={editingConnection}
        onClose={() => setModalOpen(false)}
        onSave={saveConnection}
      />
      <PermissionGuidanceModal isOpen={guidanceOpen} onClose={() => setGuidanceOpen(false)} />
    </div>
  );
}
