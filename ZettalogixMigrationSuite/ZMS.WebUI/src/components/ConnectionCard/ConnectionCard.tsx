import { formatConnectionType, formatDate } from "../../utils/formatters";
import { getErrorGuidance } from "../../utils/errorHelp";
import { ConnectionRecord } from "../../utils/models";
import styles from "./ConnectionCard.module.css";

interface ConnectionCardProps {
  connection: ConnectionRecord;
  onTest: (id: string) => void;
  onDelete: (id: string) => void;
}

export default function ConnectionCard({ connection, onTest, onDelete }: ConnectionCardProps): JSX.Element {
  const testLabel =
    connection.type === "GoogleDrive"
      ? "Test Google Drive Source"
      : connection.type === "SharePointOnline"
        ? "Test SharePoint Online Target"
        : "Test connection";
  const guidance = getErrorGuidance(connection.lastTestMessage);

  return (
    <article className={styles.card}>
      <div className={styles.header}>
        <div>
          <span className={styles.type}>{formatConnectionType(connection.type)}</span>
          <h3>{connection.name}</h3>
        </div>
        <span className={`status-chip ${connection.status.toLowerCase()}`}>{connection.status}</span>
      </div>

      <p className={styles.description}>{connection.summary}</p>

      <dl className={styles.details}>
        <div>
          <dt>Endpoint</dt>
          <dd>{connection.url}</dd>
        </div>
        <div>
          <dt>Scope</dt>
          <dd>{connection.rootPath || "Default root"}</dd>
        </div>
        {connection.documentLibraryName ? (
          <div>
            <dt>Library</dt>
            <dd>{connection.documentLibraryName}</dd>
          </div>
        ) : null}
        {connection.hasClientSecret ? (
          <div>
            <dt>Saved secrets</dt>
            <dd>Client secret: ********</dd>
          </div>
        ) : null}
        <div>
          <dt>Last checked</dt>
          <dd>{formatDate(connection.lastChecked)}</dd>
        </div>
      </dl>

      {connection.lastTestMessage ? (
        <div className={styles.testMessage}>
          <strong>Last test message</strong>
          <p>{connection.lastTestMessage}</p>
          {guidance ? (
            <ul>
              {guidance.checks.slice(0, 2).map((check) => <li key={check}>{check}</li>)}
            </ul>
          ) : null}
        </div>
      ) : null}

      <div className={styles.actions}>
        <button type="button" className="ghost-button" onClick={() => onTest(connection.id)}>
          <span className="material-symbols-outlined">bolt</span>
          {testLabel}
        </button>
        <button type="button" className={styles.deleteButton} onClick={() => onDelete(connection.id)}>
          <span className="material-symbols-outlined">delete</span>
          Delete
        </button>
      </div>
    </article>
  );
}
