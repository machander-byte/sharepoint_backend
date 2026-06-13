import { LucideIcon } from "lucide-react";
import { ReactNode } from "react";
import { cn } from "../lib/utils";

interface StatCardProps {
  label: string;
  value: ReactNode;
  icon?: LucideIcon;
  caption?: string;
  tone?: "default" | "primary" | "success" | "warning" | "error";
}

const toneClasses = {
  default: "bg-surface text-text-primary",
  primary: "bg-primary-soft/55 text-primary",
  success: "bg-success-soft text-success",
  warning: "bg-warning-soft text-warning",
  error: "bg-error-soft text-error"
};

export default function StatCard({ label, value, icon: Icon, caption, tone = "default" }: StatCardProps): JSX.Element {
  return (
    <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-sm font-medium text-text-muted">{label}</p>
          <p className="mt-3 text-3xl font-bold tracking-tight text-text-primary">{value}</p>
        </div>
        {Icon ? (
          <div className={cn("flex h-10 w-10 items-center justify-center rounded-xl", toneClasses[tone])}>
            <Icon className="h-5 w-5" />
          </div>
        ) : null}
      </div>
      {caption ? <p className="mt-3 text-sm leading-6 text-text-muted">{caption}</p> : null}
    </article>
  );
}
