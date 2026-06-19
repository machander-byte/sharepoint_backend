import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { clearSupabaseAuthStorage } from "../lib/client";
import AuthStatusPage from "./AuthStatusPage";

export default function AuthCallbackPage(): JSX.Element {
  const navigate = useNavigate();
  const { supabase } = useAuth();

  useEffect(() => {
    let active = true;

    async function completeSignIn() {
      try {
        const callbackUrl = new URL(window.location.href);
        const hashParams = new URLSearchParams(callbackUrl.hash.replace(/^#/, ""));
        const errorDescription = hashParams.get("error_description") ?? hashParams.get("error");

        if (errorDescription) {
          throw new Error(errorDescription);
        }

        const accessToken = hashParams.get("access_token");
        const refreshToken = hashParams.get("refresh_token");

        if (accessToken && refreshToken) {
          clearSupabaseAuthStorage();
          const { error } = await supabase.auth.setSession({
            access_token: accessToken,
            refresh_token: refreshToken
          });

          if (error) {
            throw error;
          }

          window.history.replaceState(null, document.title, "/auth/callback");
          if (active) {
            navigate("/dashboard", { replace: true });
          }

          return;
        }

        const code = callbackUrl.searchParams.get("code");
        if (code) {
          const { error } = await supabase.auth.exchangeCodeForSession(window.location.href);
          if (error) {
            throw error;
          }

          window.history.replaceState(null, document.title, "/auth/callback");
          if (active) {
            navigate("/dashboard", { replace: true });
          }

          return;
        }

        const { data, error } = await supabase.auth.getSession();
        if (error) {
          throw error;
        }

        if (active) {
          navigate(data.session ? "/dashboard" : "/login", { replace: true });
        }
      } catch {
        clearSupabaseAuthStorage();
        if (active) {
          navigate("/login", { replace: true, state: { authError: "Sign in could not be completed. Please try again." } });
        }
      }
    }

    void completeSignIn();

    return () => {
      active = false;
    };
  }, [navigate, supabase]);

  return <AuthStatusPage title="Completing sign in" message="Finishing the Supabase OAuth callback." />;
}
