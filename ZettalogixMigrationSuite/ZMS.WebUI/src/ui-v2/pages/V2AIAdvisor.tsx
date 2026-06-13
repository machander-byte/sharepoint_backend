import { Bot, Brain, Sparkles } from "lucide-react";
import { aiCapabilities } from "../data/v2DashboardData";
import { V2Card, V2PageHeader, V2StatusPill, V2Table } from "../components/V2Primitives";

export function V2AIAdvisor(): JSX.Element {
  return (
    <>
      <V2PageHeader
        eyebrow="AI Advisor"
        title="Recommendation workspace"
        description="AI/recommendation preview that labels rule-based, fallback, and real-AI behavior without overstating model availability."
      />

      <div className="zms-v2-grid">
        <V2Card title="Advisor mode" className="zms-v2-span-4">
          <Brain size={28} color="var(--v2-primary)" />
          <p className="zms-v2-copy">
            V2 treats recommendations as adapter-backed unless the AI backend and Ollama path are proven available at runtime.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="warning">Do not claim real AI by default</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Fallback behavior" className="zms-v2-span-4">
          <Bot size={28} color="var(--v2-success)" />
          <p className="zms-v2-copy">
            Deterministic fallback guidance is allowed when the AI backend is unavailable.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill tone="success">Fallback ready</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Current truth" className="zms-v2-span-4">
          <Sparkles size={28} color="var(--v2-warning)" />
          <p className="zms-v2-copy">
            Recommendations should focus on permissions, metadata, long paths, archive strategy, waves, failed items, and governance cleanup.
          </p>
          <div style={{ marginTop: 16 }}>
            <V2StatusPill>Evidence-labeled</V2StatusPill>
          </div>
        </V2Card>

        <V2Card title="Recommendation matrix" className="zms-v2-span-12">
          <V2Table
            headers={["Capability", "Classification", "Status"]}
            rows={aiCapabilities.map((capability) => [
              capability.name,
              capability.classification,
              <V2StatusPill key={capability.name} tone={capability.status.includes("Not claimed") ? "warning" : "neutral"}>{capability.status}</V2StatusPill>
            ])}
          />
        </V2Card>
      </div>
    </>
  );
}
