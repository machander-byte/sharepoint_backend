import type { ReactNode } from "react";
import type { LucideIcon } from "lucide-react";
import { AlertTriangle, CheckCircle2 } from "lucide-react";
import { migrationEvidence } from "../data/v2DashboardData";

type Tone = "success" | "warning" | "danger" | "neutral";

interface V2PageHeaderProps {
  eyebrow: string;
  title: string;
  description: string;
  actions?: ReactNode;
}

export function V2PageHeader({ eyebrow, title, description, actions }: V2PageHeaderProps): JSX.Element {
  return (
    <div className="zms-v2-page-header">
      <div>
        <div className="zms-v2-eyebrow">{eyebrow}</div>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {actions ? <div className="zms-v2-actions">{actions}</div> : null}
    </div>
  );
}

interface V2CardProps {
  title?: string;
  children: ReactNode;
  className?: string;
}

export function V2Card({ title, children, className = "zms-v2-span-12" }: V2CardProps): JSX.Element {
  return (
    <section className={`zms-v2-card ${className}`}>
      {title ? <h2>{title}</h2> : null}
      {children}
    </section>
  );
}

interface V2MetricCardProps {
  label: string;
  value: string;
  status: string;
  icon: LucideIcon;
  tone?: Tone;
}

export function V2MetricCard({ label, value, status, icon: Icon, tone = "success" }: V2MetricCardProps): JSX.Element {
  return (
    <V2Card className="zms-v2-span-3">
      <Icon size={22} color="var(--v2-primary)" />
      <span className="zms-v2-metric-value">{value}</span>
      <span className="zms-v2-metric-label">{label}</span>
      <div style={{ marginTop: 12 }}>
        <V2StatusPill tone={tone}>{status}</V2StatusPill>
      </div>
    </V2Card>
  );
}

interface V2StatusPillProps {
  children: ReactNode;
  tone?: Tone;
}

export function V2StatusPill({ children, tone = "neutral" }: V2StatusPillProps): JSX.Element {
  const className = tone === "neutral" ? "zms-v2-pill" : `zms-v2-pill ${tone}`;
  return <span className={className}>{children}</span>;
}

export function V2LimitationBanner(): JSX.Element {
  return (
    <div className="zms-v2-limitation">
      <strong>Implementation status:</strong> {migrationEvidence.limitation}
    </div>
  );
}

interface V2TableProps {
  headers: string[];
  rows: Array<Array<ReactNode>>;
}

export function V2Table({ headers, rows }: V2TableProps): JSX.Element {
  return (
    <div className="zms-v2-table-wrap">
      <table className="zms-v2-table">
        <thead>
          <tr>
            {headers.map((header) => (
              <th key={header}>{header}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex}>
              {row.map((cell, cellIndex) => (
                <td key={`${rowIndex}-${cellIndex}`}>{cell}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

interface V2EvidenceRowProps {
  label: string;
  value: string;
  tone?: Tone;
}

export function V2EvidenceRow({ label, value, tone = "neutral" }: V2EvidenceRowProps): JSX.Element {
  return (
    <div className="zms-v2-row">
      <span className="zms-v2-copy" style={{ margin: 0 }}>{label}</span>
      <V2StatusPill tone={tone}>{value}</V2StatusPill>
    </div>
  );
}

export function V2PassedIcon(): JSX.Element {
  return <CheckCircle2 size={16} color="var(--v2-success)" />;
}

export function V2WarningIcon(): JSX.Element {
  return <AlertTriangle size={16} color="var(--v2-warning)" />;
}
