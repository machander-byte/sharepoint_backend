export interface ErrorGuidance {
  title: string;
  summary: string;
  checks: string[];
  docs?: {
    label: string;
    href: string;
  };
}

interface ErrorRule {
  matches: string[];
  guidance: ErrorGuidance;
}

const errorRules: ErrorRule[] = [
  {
    matches: ["api is not reachable", "failed to fetch", "networkerror"],
    guidance: {
      title: "Backend API is not reachable",
      summary: "The web UI cannot reach the ASP.NET Core API configured by VITE_API_BASE_URL.",
      checks: [
        "Start ZMS.API and confirm it is listening on the same URL shown in the frontend .env file.",
        "Check the API health endpoint, CORS allowed origins, and HTTPS/HTTP mismatch.",
        "If deployed, verify the reverse proxy routes /api requests to the API service."
      ]
    }
  },
  {
    matches: ["saved connection secret could not be decrypted", "dataprotection:keyringpath", "dataprotection:keystorage", "key ring was lost"],
    guidance: {
      title: "Saved connection secret cannot be decrypted",
      summary: "The API is running with a different or missing ASP.NET Core Data Protection key ring.",
      checks: [
        "Configure DataProtection__KeyStorage=Database, or use DataProtection__KeyRingPath with a persistent shared folder.",
        "Restore the original key ring if it was moved or deleted.",
        "If the key ring is permanently lost, recreate the affected saved connection."
      ]
    }
  },
  {
    matches: ["google drive backend credentials are not configured", "google_client_id", "google_client_secret", "google_refresh_token"],
    guidance: {
      title: "Google backend OAuth credentials are missing",
      summary: "Google Drive migration uses backend OAuth credentials so the worker can read files without browser state.",
      checks: [
        "Set GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET, and GOOGLE_REFRESH_TOKEN on the API host.",
        "Generate the refresh token with offline access and a Drive read scope.",
        "Restart the API after changing environment variables."
      ],
      docs: {
        label: "Google OAuth offline access",
        href: "https://developers.google.com/identity/protocols/oauth2/web-server"
      }
    }
  },
  {
    matches: ["google drive credentials are invalid", "invalid_grant", "invalid_client", "oauth2.googleapis.com/token"],
    guidance: {
      title: "Google OAuth credentials are invalid or expired",
      summary: "The API could not exchange the configured refresh token for a Drive access token.",
      checks: [
        "Confirm the client ID and client secret are from the same Google Cloud project as the refresh token.",
        "Create a new refresh token if the user revoked consent, the OAuth client was recreated, or the test user changed.",
        "Confirm the OAuth consent screen includes the Drive scope used by the app."
      ],
      docs: {
        label: "Create Google credentials",
        href: "https://developers.google.com/workspace/guides/create-credentials"
      }
    }
  },
  {
    matches: ["google drive request failed with status 401", "google drive request failed with status 403", "google drive folder is not accessible"],
    guidance: {
      title: "Google Drive folder cannot be read",
      summary: "The folder exists outside the access granted to the backend Google account or the supplied ID is not a folder.",
      checks: [
        "Share the source folder with the Google account that granted the refresh token.",
        "Paste a folder URL, not a file URL.",
        "Confirm the Drive API is enabled in the Google Cloud project."
      ],
      docs: {
        label: "Enable Google Drive API",
        href: "https://console.cloud.google.com/apis/library/drive.googleapis.com"
      }
    }
  },
  {
    matches: ["failed to acquire a microsoft graph access token", "invalid_client", "unauthorized_client", "login.microsoftonline.com"],
    guidance: {
      title: "Microsoft Graph token request failed",
      summary: "SharePoint Online upload needs a valid Microsoft Entra app registration using client credentials.",
      checks: [
        "Verify tenant ID, application client ID, and the client secret value, not the secret ID.",
        "Confirm the secret has not expired.",
        "Grant admin consent for the Microsoft Graph application permissions used by this app."
      ],
      docs: {
        label: "Register a Microsoft Entra app",
        href: "https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app"
      }
    }
  },
  {
    matches: ["sharepoint graph request failed with status 401", "sharepoint graph request failed with status 403", "accessdenied", "unauthorized"],
    guidance: {
      title: "SharePoint permission is denied",
      summary: "Microsoft Graph accepted the token request but the app is not allowed to read or write the target site/library.",
      checks: [
        "Grant Microsoft Graph application permissions such as Sites.ReadWrite.All and Files.ReadWrite.All, then grant admin consent.",
        "Confirm the target site URL belongs to the tenant used by the app registration.",
        "If using a restricted Sites.Selected model, grant the app explicit access to the target site."
      ],
      docs: {
        label: "Microsoft Graph permissions reference",
        href: "https://learn.microsoft.com/en-us/graph/permissions-reference"
      }
    }
  },
  {
    matches: ["document library", "was not found", "drive was not found"],
    guidance: {
      title: "SharePoint document library was not found",
      summary: "The target site resolved, but the library name entered in the connection or job does not match a document library.",
      checks: [
        "Open the SharePoint site and confirm the exact document library display name.",
        "Try Documents or Shared Documents if the default library was renamed only in the URL.",
        "Retest the SharePoint connection after updating the saved target connection."
      ],
      docs: {
        label: "Microsoft Graph files and drives",
        href: "https://learn.microsoft.com/en-us/graph/api/resources/onedrive"
      }
    }
  },
  {
    matches: ["large-file upload", "createuploadsession", "upload session", "250 mb"],
    guidance: {
      title: "Large file upload did not complete",
      summary: "Files over the small-upload limit use a resumable Microsoft Graph upload session.",
      checks: [
        "Confirm the source connector reports the correct file size.",
        "Check network timeout/proxy limits between the worker and Microsoft Graph.",
        "Restart the job after fixing credentials or connectivity; failed items remain visible in the job detail log."
      ],
      docs: {
        label: "Microsoft Graph large file upload",
        href: "https://learn.microsoft.com/en-us/graph/api/driveitem-createuploadsession?view=graph-rest-1.0"
      }
    }
  },
  {
    matches: ["file share", "could not be found", "access to the path", "path lookup"],
    guidance: {
      title: "Source path is unavailable",
      summary: "The worker cannot read the source location from the machine running the API/engine.",
      checks: [
        "Use a local path or UNC path that is reachable from the API host, not only from the browser machine.",
        "Grant the API process account read permission to the folder.",
        "Retest the source connection before starting another migration run."
      ]
    }
  },
  {
    matches: ["429", "throttle", "too many requests", "temporarily unavailable", "503"],
    guidance: {
      title: "Remote service throttled the migration",
      summary: "Google Drive or Microsoft Graph asked the worker to slow down or retry later.",
      checks: [
        "Reduce concurrency and batch size for the next run.",
        "Wait for the service throttle window to clear before retrying failed items.",
        "Check tenant-level service health if throttling persists."
      ]
    }
  }
];

export function getErrorGuidance(message?: string | null): ErrorGuidance | null {
  const normalized = message?.toLowerCase() ?? "";
  if (!normalized) {
    return null;
  }

  return errorRules.find((rule) => rule.matches.some((match) => normalized.includes(match)))?.guidance ?? null;
}

export function formatErrorForToast(message: string): string {
  const guidance = getErrorGuidance(message);
  if (!guidance) {
    return message;
  }

  return `${message} ${guidance.summary} First check: ${guidance.checks[0]}`;
}
