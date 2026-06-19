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
  authError?: string;
}

const providerLabels: Array<{ provider: Provider; label: string }> = [
  { provider: "google", label: "Continue with Google" }
];

export default function AuthPage(): JSX.Element {
  const location = useLocation();
  const { loading, session, signInWithOAuth, signInWithEmail } = useAuth();
  const [pendingProvider, setPendingProvider] = useState<Provider | null>(null);
  const [pendingEmail, setPendingEmail] = useState(false);
  const [email, setEmail] = useState("");
  const locationState = location.state as LocationState | null;
  const [errorMessage, setErrorMessage] = useState(locationState?.authError ?? "");
  const [successMessage, setSuccessMessage] = useState("");

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
      <section className={styles.authHero} aria-label="ZMS sign-in summary">
        <div className={styles.heroBrand}>
          <div className={styles.brandMark}>Z</div>
          <div>
            <span>Zettalogix</span>
            <strong>Migration Suite</strong>
          </div>
        </div>

        <div>
          <span className={styles.heroEyebrow}>Final Demo / Pre-Production Release</span>
          <h1>ZMS reviewer workspace</h1>
          <p>
            Sign in to review the migration dashboard, validation evidence, reports, governance,
            AI advisor, and guided tutorial in the authenticated workspace.
          </p>
        </div>

        <div className={styles.limitationNote}>
          <strong>Reviewer note</strong>
          <span>Authenticated pages use live backend status and clearly labeled empty states when no run data exists.</span>
        </div>
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
            {pendingEmail ? "Sending link..." : "Send sign-in link"}
          </button>
        </form>

        {successMessage ? <p className={styles.authSuccess}>{successMessage}</p> : null}
        {errorMessage ? <p className={styles.authError}>{errorMessage}</p> : null}
      </section>
    </main>
  );
}
