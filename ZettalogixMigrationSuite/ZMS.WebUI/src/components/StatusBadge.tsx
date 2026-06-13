import { CheckCircle2, Clock3, PauseCircle, PlugZap, TriangleAlert, XCircle } from "lucide-react";
import { cn } from "../lib/utils";
import { ConnectionStatus, JobStatus, MappingStatus } from "../types/zms";

type StatusBadgeValue = ConnectionStatus | JobStatus | MappingStatus | "Inherited" | "Broken" | string;

interface StatusBadgeProps {
  status: StatusBadgeValue;
}

const classes: Record<string, string> = {
  Connected: "bg-success-soft text-success",
  Completed: "bg-success-soft text-success",
  Mapped: "bg-success-soft text-success",
  Inherited: "bg-success-soft text-success",
  Running: "bg-info-soft text-info",
  Suggested: "bg-info-soft text-info",
  Warning: "bg-warning-soft text-warning",
  "Config Required": "bg-warning-soft text-warning",
  Scheduled: "bg-surface-container text-text-muted",
  Unmapped: "bg-surface-container text-text-muted",
  Disconnected: "bg-surface-container text-text-muted",
  Failed: "bg-error-soft text-error",
  Conflict: "bg-error-soft text-error",
  Broken: "bg-error-soft text-error"
};

function getIcon(status: StatusBadgeValue) {
  if (status === "Connected" || status === "Completed" || status === "Mapped" || status === "Inherited") {
    return CheckCircle2;
  }
  if (status === "Running" || status === "Suggested") {
    return PlugZap;
  }
  if (status === "Warning" || status === "Config Required") {
    return TriangleAlert;
  }
  if (status === "Scheduled" || status === "Unmapped" || status === "Disconnected") {
    return status === "Scheduled" ? Clock3 : PauseCircle;
  }
  return XCircle;
}

export default function StatusBadge({ status }: StatusBadgeProps): JSX.Element {
  const Icon = getIcon(status);

  return (
    <span className={cn("inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold", classes[status] ?? "bg-surface-container text-text-muted")}>
      <Icon className="h-3.5 w-3.5" />
      {status}
    </span>
  );
}
