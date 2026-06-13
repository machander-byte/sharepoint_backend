import type { Provider } from "@supabase/supabase-js";
import type { FormEvent } from "react";
import { useState } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import styles from "./AuthPage.module.css";

interface LocationState {
  from?: {
    pathname?: string;
  };
}

const providerLabels: Array<{ provider: Provider; icon: string; label: string }> = [
  { provider: "google", icon: "account_circle", label: "Continue with Google" }
];

const buildCommit = import.meta.env.VITE_APP_COMMIT?.trim() || "local";
const buildTime = import.meta.env.VITE_APP_BUILD_TIME?.trim() || "local";

export default function AuthPage(): JSX.Element {
  const location = useLocation();
  const { loading, session, signInWithOAuth, signInWithEmail } = useAuth();
  const [pendingProvider, setPendingProvider] = useState<Provider | null>(null);
  const [pendingEmail, setPendingEmail] = useState(false);
  const [email, setEmail] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  const locationState = location.state as LocationState | null;
  const redirectPath = locationState?.from?.pathname ?? "/dashboard";

  if (!loading && session) {
    return <Navigate to={redirectPath} replace />;
  }

  const beginOAuth = async (provider: Provider) => {
    setErrorMessage("");
    setSuccessMessage("");
    setPendingProvider(provider);

    try {
      await signInWithOAuth(provider);
    } catch (error) {
      setPendingProvider(null);
      setErrorMessage(error instanceof Error ? error.message : "Supabase OAuth sign in failed.");
    }
  };

  const beginEmailSignIn = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const trimmedEmail = email.trim();

    if (!trimmedEmail) {
      setErrorMessage("Enter your email address.");
      return;
    }

    setErrorMessage("");
    setSuccessMessage("");
    setPendingEmail(true);

    try {
      await signInWithEmail(trimmedEmail);
      setSuccessMessage("Check your email for the sign-in link.");
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : "Email sign in failed.");
    } finally {
      setPendingEmail(false);
    }
  };

  return (
    <main className={styles.authShell}>
      <section className={styles.authHero} aria-label="ZMS validation summary">
        <div className={styles.heroBrand}>
          <div className={styles.brandMark}>Z</div>
          <div>
            <span>Zettalogix</span>
            <strong>Migration Suite</strong>
          </div>
        </div>

        <div>
          <span className={styles.heroEyebrow}>UI V2 access</span>
          <h1>Migration control plane</h1>
          <p>
            Sign in to review migration evidence, readiness, reports, governance, AI recommendations,
            and internal safety limits.
          </p>
        </div>

        <div className={styles.proofGrid}>
          <div>
            <strong>231/231</strong>
            <span>Stage 1 files passed</span>
          </div>
          <div>
            <strong>0</strong>
            <span>Failed files</span>
          </div>
          <div>
            <strong>0</strong>
            <span>Retries</span>
          </div>
          <div>
            <strong>46/46</strong>
            <span>Backend tests</span>
          </div>
        </div>

        <div className={styles.limitationNote}>
          <strong>Known gap</strong>
          <span>File migration integrity passed. Empty-folder preservation is a known gap.</span>
        </div>

        <p className={styles.buildFingerprint} aria-label="ZMS frontend deployment fingerprint">
          ZMS frontend build {buildCommit} · {buildTime}
        </p>
      </section>

      <section className={styles.authPanel}>
        <span className={styles.panelEyebrow}>Secure access</span>
        <h2>Sign in to ZMS</h2>
        <p>Use Google or a verified email link to open the authenticated migration workspace.</p>

        <div className={styles.providerStack}>
          {providerLabels.map((item) => (
            <button
              key={item.provider}
              type="button"
              className={styles.providerButton}
              onClick={() => void beginOAuth(item.provider)}
              disabled={Boolean(pendingProvider)}
            >
              <span className="material-symbols-outlined">{item.icon}</span>
              {pendingProvider === item.provider ? "Redirecting..." : item.label}
            </button>
          ))}
        </div>

        <div className={styles.divider}>
          <span>or</span>
        </div>

        <form className={styles.emailForm} onSubmit={beginEmailSignIn}>
          <label>
            Email address
            <input
              type="email"
              autoComplete="email"
              placeholder="you@example.com"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>
          <button type="submit" className={styles.emailButton} disabled={pendingEmail || Boolean(pendingProvider)}>
            <span className="material-symbols-outlined">mail</span>
            {pendingEmail ? "Sending link..." : "Send sign-in link"}
          </button>
        </form>

        {successMessage ? <p className={styles.authSuccess}>{successMessage}</p> : null}
        {errorMessage ? <p className={styles.authError}>{errorMessage}</p> : null}
      </section>
    </main>
  );
}
