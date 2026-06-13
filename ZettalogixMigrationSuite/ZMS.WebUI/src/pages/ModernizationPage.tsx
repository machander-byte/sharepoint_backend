import { FileBarChart, FormInput, Workflow } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import DataTable from "../components/DataTable";
import PageHeader from "../components/PageHeader";
import RiskBadge from "../components/RiskBadge";
import { modernizationItems } from "../data/zmsMockData";
import { zmsApi } from "../services/zmsApi";
import { RiskLevel } from "../types/zms";

interface ModernizationRow {
  id: string;
  legacyAsset: string;
  sourceType: string;
  department: string;
  complexity: RiskLevel;
  recommendedTarget: string;
  confidence: number;
}

const pathways = [
  {
    title: "Workflow Modernization",
    icon: Workflow,
    items: ["SharePoint Designer to Power Automate", "Nintex to Power Automate / Logic Apps", "K2 to Azure Logic Apps"]
  },
  {
    title: "Forms Modernization",
    icon: FormInput,
    items: ["InfoPath to Power Apps", "Nintex Forms to Power Apps", "ASPX Forms to Modern SharePoint Forms"]
  },
  {
    title: "Reporting Modernization",
    icon: FileBarChart,
    items: ["SSRS to Power BI", "Excel Reports to Power BI", "Tableau / Cognos to Power BI"]
  }
];

export default function ModernizationPage(): JSX.Element {
  const [backendRows, setBackendRows] = useState<ModernizationRow[] | null>(null);
  const [sourceLabel, setSourceLabel] = useState("backend fallback");
  const [runId, setRunId] = useState<string | null>(null);
  const [draftSpec, setDraftSpec] = useState<string>("Select an asset to generate a draft modernization spec.");
  const [explanation, setExplanation] = useState<string>("No modernization explanation loaded yet.");

  useEffect(() => {
    let cancelled = false;

    zmsApi.importOnPremDemo().then((result) => {
      if (cancelled || !result) return;

      const assetsById = new Map(result.assets.map((asset) => [asset.id, asset]));
      setRunId(result.runId);
      setBackendRows(result.recommendations.map((recommendation) => {
        const asset = assetsById.get(recommendation.assetId);
        return {
          id: recommendation.assetId,
          legacyAsset: asset?.name ?? recommendation.assetId,
          sourceType: asset?.assetType ?? "LegacyAsset",
          department: extractDepartment(asset?.location ?? ""),
          complexity: toRiskLevel(recommendation.estimatedEffort),
          recommendedTarget: recommendation.modernizationTarget,
          confidence: recommendation.automationEligible ? 82 : 64
        };
      }));
      setSourceLabel(`on-prem import ${result.runId}`);
      void zmsApi.explainModernization(result.runId).then((response) => {
        if (!cancelled && response?.explanation) {
          setExplanation(response.explanation);
        }
      });
    });

    return () => {
      cancelled = true;
    };
  }, []);

  const rows = useMemo<ModernizationRow[]>(() => {
    if (backendRows) return backendRows;
    return modernizationItems.map((item) => ({
      id: item.id,
      legacyAsset: item.legacyAsset,
      sourceType: item.sourceType,
      department: item.department,
      complexity: item.complexity,
      recommendedTarget: item.recommendedTarget,
      confidence: item.confidence
    }));
  }, [backendRows]);

  return (
    <div className="flex flex-col gap-6">
      <PageHeader
        title="Modernization Assessment"
        subtitle={`Legacy workflow, form, and report analysis from ${sourceLabel}. Draft specs require human review.`}
      />

      <section className="grid grid-cols-1 gap-4 lg:grid-cols-3">
        {pathways.map((pathway) => {
          const Icon = pathway.icon;
          return (
            <article key={pathway.title} className="rounded-xl border border-border bg-surface p-5 shadow-card">
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary-soft text-primary">
                  <Icon className="h-5 w-5" />
                </div>
                <h2 className="font-bold text-text-primary">{pathway.title}</h2>
              </div>
              <div className="mt-5 space-y-3">
                {pathway.items.map((item) => (
                  <div key={item} className="rounded-lg border border-border bg-surface-container p-3 text-sm font-semibold text-text-primary">
                    {item}
                  </div>
                ))}
              </div>
            </article>
          );
        })}
      </section>

      <DataTable
        rows={rows}
        getRowKey={(row) => row.id}
        columns={[
          { header: "Legacy Asset", render: (row) => <button type="button" className="font-semibold text-primary" onClick={() => void loadDraftSpec(row.id)}>{row.legacyAsset}</button> },
          { header: "Source Type", render: (row) => row.sourceType },
          { header: "Department", render: (row) => row.department },
          { header: "Complexity", render: (row) => <RiskBadge level={row.complexity} /> },
          { header: "Recommended Modern Target", render: (row) => <span className="font-semibold text-primary">{row.recommendedTarget}</span> },
          {
            header: "Confidence",
            render: (row) => (
              <div className="flex items-center gap-2">
                <div className="h-2 w-24 overflow-hidden rounded-full bg-surface-container-high">
                  <div className="h-full rounded-full bg-primary" style={{ width: `${row.confidence}%` }} />
                </div>
                <span className="font-mono text-xs">{row.confidence}%</span>
              </div>
            )
          }
        ]}
      />

      <section className="grid grid-cols-1 gap-4 xl:grid-cols-2">
        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <h2 className="font-bold text-text-primary">Draft Spec Viewer</h2>
          <pre className="mt-4 max-h-96 overflow-auto rounded-lg bg-surface-container p-4 text-xs leading-6 text-text-primary">
            {draftSpec}
          </pre>
        </article>
        <article className="rounded-xl border border-border bg-surface p-5 shadow-card">
          <h2 className="font-bold text-text-primary">Ollama Explanation</h2>
          <p className="mt-4 text-sm leading-6 text-text-muted">{explanation}</p>
        </article>
      </section>
    </div>
  );

  async function loadDraftSpec(assetId: string): Promise<void> {
    if (!runId) {
      setDraftSpec("Backend modernization run is not available.");
      return;
    }

    const spec = await zmsApi.createModernizationDraftSpec(assetId);
    setDraftSpec(spec ? JSON.stringify(spec, null, 2) : "Draft spec is not available for this asset.");
  }
}

function toRiskLevel(value: string): RiskLevel {
  if (value === "High" || value === "Critical") return "High";
  if (value === "Low") return "Low";
  return "Medium";
}

function extractDepartment(location: string): string {
  const match = /\/sites\/([^/]+)/i.exec(location);
  return match?.[1] ?? "Enterprise";
}
