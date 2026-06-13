import { CheckCircle2, Info, TriangleAlert, X, XCircle } from "lucide-react";
import { useEffect } from "react";
import { cn } from "../lib/utils";
import { ToastNotification } from "../types/zms";

interface ToastProps {
  toast: ToastNotification;
  onDismiss: (id: string) => void;
}

const toneClasses = {
  success: "border-success/30 bg-success-soft text-success",
  warning: "border-warning/30 bg-warning-soft text-warning",
  error: "border-error/30 bg-error-soft text-error",
  info: "border-primary-muted bg-primary-soft text-primary"
};

const icons = {
  success: CheckCircle2,
  warning: TriangleAlert,
  error: XCircle,
  info: Info
};

export default function Toast({ toast, onDismiss }: ToastProps): JSX.Element {
  const Icon = icons[toast.tone];

  useEffect(() => {
    const timeout = window.setTimeout(() => onDismiss(toast.id), 4200);
    return () => window.clearTimeout(timeout);
  }, [onDismiss, toast.id]);

  return (
    <div className={cn("pointer-events-auto flex w-full max-w-sm gap-3 rounded-xl border p-4 shadow-panel", toneClasses[toast.tone])}>
      <Icon className="mt-0.5 h-5 w-5 shrink-0" />
      <div className="min-w-0 flex-1">
        <p className="font-bold">{toast.title}</p>
        {toast.description ? <p className="mt-1 text-sm leading-5 opacity-90">{toast.description}</p> : null}
      </div>
      <button type="button" className="shrink-0 rounded-lg p-1 hover:bg-white/45" onClick={() => onDismiss(toast.id)} aria-label="Dismiss toast">
        <X className="h-4 w-4" />
      </button>
    </div>
  );
}
