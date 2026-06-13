import { cn } from "../lib/utils";
import { RiskLevel } from "../types/zms";

interface RiskBadgeProps {
  level: RiskLevel;
}

const classes: Record<RiskLevel, string> = {
  Low: "bg-surface-container text-text-muted",
  Medium: "bg-warning-soft text-warning",
  High: "bg-error-soft text-error",
  Critical: "bg-error text-white"
};

export default function RiskBadge({ level }: RiskBadgeProps): JSX.Element {
  return <span className={cn("inline-flex rounded-full px-2.5 py-1 text-xs font-bold", classes[level])}>{level}</span>;
}
