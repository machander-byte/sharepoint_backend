import styles from "./HelpCenterPage.module.css";

interface ResourceLink {
  label: string;
  href: string;
  description: string;
}

interface ChecklistItem {
  title: string;
  detail: string;
}

const migrationFlow: ChecklistItem[] = [
  {
    title: "Prepare the source",
    detail: "For Google Drive, share the source folder with the Google account used to generate the backend refresh token. For file shares, make sure the API host can read the local or UNC path."
  },
  {
    title: "Prepare the SharePoint target",
    detail: "Create or confirm the target SharePoint site and document library before saving the SharePoint Online connection."
  },
  {
    title: "Register reusable connections",
    detail: "Create one Google Drive, File Share, or SharePoint On-Prem source connection and one SharePoint Online destination connection."
  },
  {
    title: "Test both endpoints",
    detail: "Use the connection test buttons before creating a job. Fix credential, permission, and library errors while the job is still in draft setup."
  },
  {
    title: "Create and start the migration job",
    detail: "Select the saved source and destination, confirm the target site and library, then start the job from Migration Jobs."
  },
  {
    title: "Review failures with guidance",
    detail: "Open the job detail page for failed or completed-with-errors jobs. Error details now show likely cause, checks, and documentation links."
  }
];

const requirementGroups = [
  {
    title: "Google Drive Source",
    icon: "folder",
    items: [
      "Google Cloud project with Google Drive API enabled.",
      "OAuth consent screen configured for the migration app.",
      "Backend refresh token generated with offline access and Drive read scope.",
      "API host environment variables: GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, GOOGLE_REFRESH_TOKEN.",
      "Optional Picker browser values in ZMS.WebUI/.env: VITE_GOOGLE_CLIENT_ID, VITE_GOOGLE_API_KEY, VITE_GOOGLE_APP_ID."
    ]
  },
  {
    title: "SharePoint Online Target",
    icon: "cloud_upload",
    items: [
      "Microsoft Entra app registration in the same tenant as the SharePoint site.",
      "Tenant ID, application client ID, and current client secret value.",
      "Microsoft Graph application permissions for site/library write access, with admin consent.",
      "Target SharePoint site URL and exact document library display name.",
      "Account/admin access to validate the uploaded files after migration."
    ]
  },
  {
    title: "Deployment Host",
    icon: "dns",
    items: [
      ".NET 8 runtime or self-contained API publish output.",
      "Supabase Postgres pooler connection string supplied through ConnectionStrings__ZmsDatabase.",
      "HTTPS endpoint, production CORS origin, and reverse proxy routing for /api.",
      "Persistent ASP.NET Core Data Protection key storage for encrypted connection secrets.",
      "Static web build from npm run build deployed behind the same public origin or a configured API URL."
    ]
  }
];

const resourceLinks: ResourceLink[] = [
  {
    label: "Microsoft Entra app registrations",
    href: "https://entra.microsoft.com/#view/Microsoft_AAD_RegisteredApps/ApplicationsListBlade",
    description: "Create the app registration, copy tenant/client IDs, create a client secret, and grant admin consent."
  },
  {
    label: "Register a Microsoft Entra application",
    href: "https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app",
    description: "Microsoft guide for app registration basics and credential setup."
  },
  {
    label: "Microsoft Graph permissions reference",
    href: "https://learn.microsoft.com/en-us/graph/permissions-reference",
    description: "Use this to confirm Graph application permissions such as Sites.ReadWrite.All and Files.ReadWrite.All."
  },
  {
    label: "Microsoft Graph file uploads",
    href: "https://learn.microsoft.com/en-us/graph/api/driveitem-put-content?view=graph-rest-1.0",
    description: "Reference for SharePoint/OneDrive small file upload behavior used by the target connector."
  },
  {
    label: "Microsoft Graph large upload sessions",
    href: "https://learn.microsoft.com/en-us/graph/api/driveitem-createuploadsession?view=graph-rest-1.0",
    description: "Reference for resumable uploads used when source files are larger than the simple upload limit."
  },
  {
    label: "Enable Google Drive API",
    href: "https://console.cloud.google.com/apis/library/drive.googleapis.com",
    description: "Open the Google Cloud console directly to enable Drive API for the selected project."
  },
  {
    label: "Configure Google OAuth consent",
    href: "https://developers.google.com/workspace/guides/configure-oauth-consent",
    description: "Google guide for consent screen, user type, test users, and required scopes."
  },
  {
    label: "Create Google access credentials",
    href: "https://developers.google.com/workspace/guides/create-credentials",
    description: "Create OAuth client IDs and API keys for backend OAuth and the browser Picker."
  },
  {
    label: "Google OAuth offline access",
    href: "https://developers.google.com/identity/protocols/oauth2/web-server",
    description: "Use this when generating or replacing the backend refresh token."
  },
  {
    label: "Google Picker API",
    href: "https://developers.google.com/workspace/drive/api/guides/picker",
    description: "Reference for the folder picker used by the Connections page."
  }
];

const errorGuide = [
  {
    title: "API not running",
    symptom: "The app says the API is not reachable.",
    action: "Start ZMS.API, confirm VITE_API_BASE_URL, check /api/health, and allow the frontend origin in Cors:AllowedOrigins."
  },
  {
    title: "Google credentials missing",
    symptom: "Connection test asks for GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, or GOOGLE_REFRESH_TOKEN.",
    action: "Set backend environment variables, restart the API, and verify the refresh token was created with offline access."
  },
  {
    title: "Google folder access denied",
    symptom: "Google returns 401, 403, or the folder is not accessible.",
    action: "Share the folder with the refresh-token account, enable Drive API, and paste a folder URL instead of a file URL."
  },
  {
    title: "Microsoft Graph token failed",
    symptom: "SharePoint test fails while acquiring a Microsoft Graph token.",
    action: "Recheck tenant ID, client ID, client secret value, secret expiry, and admin consent."
  },
  {
    title: "SharePoint library not found",
    symptom: "The target site resolves but the document library is missing.",
    action: "Use the exact library display name shown in SharePoint, commonly Documents or Shared Documents."
  },
  {
    title: "Upload failed or throttled",
    symptom: "Job log shows failed file uploads, 429, 503, or upload session errors.",
    action: "Reduce concurrency, confirm file size can be read from source, and retry after the remote service throttle clears."
  }
];

function ExternalLink({ link }: { link: ResourceLink }): JSX.Element {
  return (
    <a className={styles.resourceLink} href={link.href} target="_blank" rel="noreferrer">
      <span className="material-symbols-outlined">open_in_new</span>
      <span>
        <strong>{link.label}</strong>
        <small>{link.description}</small>
      </span>
    </a>
  );
}

export default function HelpCenterPage(): JSX.Element {
  return (
    <div className={styles.page}>
      <section className={styles.heroGrid}>
        <article className="surface-card">
          <span className="eyebrow">Migration Runbook</span>
          <h2>Complete the migration in this order</h2>
          <div className={styles.flowList}>
            {migrationFlow.map((item, index) => (
              <div key={item.title} className={styles.flowItem}>
                <span>{String(index + 1).padStart(2, "0")}</span>
                <div>
                  <strong>{item.title}</strong>
                  <p>{item.detail}</p>
                </div>
              </div>
            ))}
          </div>
        </article>

        <aside className={styles.quickPanel}>
          <span className="eyebrow">Before You Start</span>
          <strong>Minimum ready state</strong>
          <p>One healthy source connection, one healthy SharePoint Online target, a reachable Supabase Postgres database, and backend secrets supplied by environment variables.</p>
          <a className="primary-button" href="#external-resources">
            <span className="material-symbols-outlined">link</span>
            Open setup links
          </a>
        </aside>
      </section>

      <section className={styles.requirementsGrid}>
        {requirementGroups.map((group) => (
          <article key={group.title} className={styles.requirementCard}>
            <div className={styles.requirementHeader}>
              <span className="material-symbols-outlined">{group.icon}</span>
              <h3>{group.title}</h3>
            </div>
            <ul>
              {group.items.map((item) => <li key={item}>{item}</li>)}
            </ul>
          </article>
        ))}
      </section>

      <section className="surface-card" id="external-resources">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Direct Setup Links</span>
            <h2>Provider portals and requirements</h2>
            <p>These links take operators directly to the vendor pages needed to create credentials, grant permissions, and verify upload behavior.</p>
          </div>
        </div>
        <div className={styles.resourceGrid}>
          {resourceLinks.map((link) => <ExternalLink key={link.href} link={link} />)}
        </div>
      </section>

      <section className="surface-card">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Error Resolution</span>
            <h2>What common errors mean</h2>
            <p>Connection tests and job detail logs use the same guidance patterns, so operators can move from symptom to action quickly.</p>
          </div>
        </div>
        <div className={styles.errorGrid}>
          {errorGuide.map((error) => (
            <article key={error.title} className={styles.errorCard}>
              <strong>{error.title}</strong>
              <span>{error.symptom}</span>
              <p>{error.action}</p>
            </article>
          ))}
        </div>
      </section>

      <section className={styles.deploymentBand}>
        <div>
          <span className="eyebrow">Deployment Readiness</span>
          <h2>Production checklist</h2>
          <p>Publish the API, build the web UI, configure Supabase Postgres, supply secrets through the host environment, persist Data Protection keys in the database or a durable key ring, and keep frontend .env files free of secrets.</p>
        </div>
        <div className={styles.commandList}>
          <code>dotnet publish .\ZMS.API\ZMS.API.csproj -c Release -o .\artifacts\api</code>
          <code>npm ci && npm run build</code>
          <code>GET /api/health</code>
        </div>
      </section>
    </div>
  );
}
