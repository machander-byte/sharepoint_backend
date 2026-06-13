import { FileQuestion } from "lucide-react";

interface EmptyStateProps {
  title: string;
  description: string;
}

export default function EmptyState({ title, description }: EmptyStateProps): JSX.Element {
  return (
    <div className="rounded-xl border border-dashed border-border bg-surface p-8 text-center">
      <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-surface-container text-text-muted">
        <FileQuestion className="h-6 w-6" />
      </div>
      <h2 className="text-lg font-bold text-text-primary">{title}</h2>
      <p className="mt-2 text-sm text-text-muted">{description}</p>
    </div>
  );
}
