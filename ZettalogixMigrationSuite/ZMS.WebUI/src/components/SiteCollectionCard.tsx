import { ArrowRight, Building2, FolderTree } from "lucide-react";
import { Link } from "react-router-dom";
import { SiteCollection } from "../types/zms";
import RiskBadge from "./RiskBadge";

interface SiteCollectionCardProps {
  site: SiteCollection;
  selectable?: boolean;
  selected?: boolean;
  onToggle?: (id: string) => void;
}

function CardContent({ site, selected }: { site: SiteCollection; selected?: boolean }) {
  const highRiskCount = site.edgeCases.filter((edgeCase) => edgeCase.riskLevel === "High" || edgeCase.riskLevel === "Critical").length;

  return (
    <>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary-soft text-primary">
            <Building2 className="h-5 w-5" />
          </div>
          <div>
            <h3 className="font-bold text-text-primary">{site.name}</h3>
            <p className="text-sm text-text-muted">{site.department}</p>
          </div>
        </div>
        <div className="flex items-center gap-2">
          {selected !== undefined ? (
            <span className={selected ? "rounded-full bg-primary px-2.5 py-1 text-xs font-bold text-white" : "rounded-full bg-surface-container px-2.5 py-1 text-xs font-bold text-text-muted"}>
              {selected ? "Selected" : "Available"}
            </span>
          ) : null}
          {highRiskCount > 0 ? <RiskBadge level={highRiskCount > 1 ? "High" : "Medium"} /> : null}
        </div>
      </div>

      <p className="mt-4 line-clamp-2 text-sm leading-6 text-text-muted">{site.description}</p>

      <div className="mt-5 grid grid-cols-2 gap-3 text-sm">
        <div className="rounded-lg bg-surface-container p-3">
          <span className="text-text-muted">Subsites</span>
          <strong className="mt-1 block text-lg text-text-primary">{site.subsites.length}</strong>
        </div>
        <div className="rounded-lg bg-surface-container p-3">
          <span className="text-text-muted">Libraries</span>
          <strong className="mt-1 block text-lg text-text-primary">{site.libraries.length}</strong>
        </div>
        <div className="rounded-lg bg-surface-container p-3">
          <span className="text-text-muted">Lists</span>
          <strong className="mt-1 block text-lg text-text-primary">{site.lists.length}</strong>
        </div>
        <div className="rounded-lg bg-surface-container p-3">
          <span className="text-text-muted">Metadata</span>
          <strong className="mt-1 block text-lg text-text-primary">{site.metadataFields.length}</strong>
        </div>
        <div className="rounded-lg bg-surface-container p-3">
          <span className="text-text-muted">Groups</span>
          <strong className="mt-1 block text-lg text-text-primary">{site.permissionGroups.length}</strong>
        </div>
        <div className="rounded-lg bg-surface-container p-3">
          <span className="text-text-muted">Edge Cases</span>
          <strong className="mt-1 block text-lg text-text-primary">{site.edgeCases.length}</strong>
        </div>
      </div>

      <div className="mt-5 flex items-center justify-between border-t border-border pt-4 text-sm font-semibold text-primary">
        <span className="inline-flex items-center gap-2">
          <FolderTree className="h-4 w-4" />
          View structure
        </span>
        <ArrowRight className="h-4 w-4 transition group-hover:translate-x-1" />
      </div>
    </>
  );
}

export default function SiteCollectionCard({ site, selectable, selected, onToggle }: SiteCollectionCardProps): JSX.Element {
  const className = `group rounded-xl border bg-surface p-5 shadow-card transition hover:-translate-y-0.5 hover:border-primary hover:shadow-panel ${
    selected ? "border-primary ring-2 ring-primary-soft" : "border-border"
  }`;

  if (selectable) {
    return (
      <article className={className}>
        <button type="button" className="w-full text-left" onClick={() => onToggle?.(site.id)}>
          <CardContent site={site} selected={selected} />
        </button>
        <Link className="mt-3 inline-flex text-sm font-bold text-primary hover:underline" to={`/environment/${site.id}`}>
          Open detail
        </Link>
      </article>
    );
  }

  return (
    <Link to={`/environment/${site.id}`} className={className}>
      <CardContent site={site} />
    </Link>
  );
}
