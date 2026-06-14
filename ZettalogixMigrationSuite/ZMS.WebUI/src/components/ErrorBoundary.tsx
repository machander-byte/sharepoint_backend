import { Component, type ErrorInfo, type ReactNode } from "react";

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public state: ErrorBoundaryState = {
    hasError: false
  };

  public static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  public componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    console.error("ZMS UI error boundary captured an error.", {
      message: error.message,
      componentStack: errorInfo.componentStack
    });
  }

  public render(): ReactNode {
    if (this.state.hasError) {
      return (
        <main style={{
          minHeight: "100vh",
          display: "grid",
          placeItems: "center",
          padding: "24px",
          background: "#f8fafc",
          color: "#0f172a"
        }}>
          <section style={{
            width: "min(520px, 100%)",
            border: "1px solid #dbe3ef",
            borderRadius: "8px",
            background: "#ffffff",
            padding: "24px",
            boxShadow: "0 16px 40px rgba(15, 23, 42, 0.08)"
          }}>
            <p style={{ margin: "0 0 8px", color: "#64748b", fontSize: "13px", fontWeight: 700, textTransform: "uppercase" }}>
              ZMS
            </p>
            <h1 style={{ margin: "0 0 12px", fontSize: "24px" }}>Something went wrong</h1>
            <p style={{ margin: "0 0 18px", color: "#475569", lineHeight: 1.6 }}>
              The page could not finish loading. No sensitive diagnostic details are shown here.
            </p>
            <button
              type="button"
              onClick={() => window.location.reload()}
              style={{
                border: "0",
                borderRadius: "6px",
                background: "#0f172a",
                color: "#ffffff",
                cursor: "pointer",
                fontWeight: 700,
                padding: "10px 14px"
              }}
            >
              Retry
            </button>
          </section>
        </main>
      );
    }

    return this.props.children;
  }
}
