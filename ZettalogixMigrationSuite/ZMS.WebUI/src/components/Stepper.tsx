import { Check } from "lucide-react";
import { cn } from "../lib/utils";

interface StepperProps {
  steps: string[];
  currentStep: number;
}

export default function Stepper({ steps, currentStep }: StepperProps): JSX.Element {
  return (
    <div className="overflow-x-auto rounded-xl border border-border bg-surface p-4 shadow-card">
      <div className="flex min-w-[820px] items-center justify-between gap-4">
        {steps.map((step, index) => {
          const stepNumber = index + 1;
          const active = stepNumber === currentStep;
          const complete = stepNumber < currentStep;

          return (
            <div key={step} className="flex flex-1 items-center gap-3">
              <div
                className={cn(
                  "flex h-9 w-9 shrink-0 items-center justify-center rounded-full border text-sm font-bold",
                  active && "border-primary bg-primary text-white",
                  complete && "border-success bg-success text-white",
                  !active && !complete && "border-border bg-surface-container text-text-muted"
                )}
              >
                {complete ? <Check className="h-4 w-4" /> : stepNumber}
              </div>
              <span className={cn("text-sm font-semibold", active ? "text-primary" : "text-text-muted")}>{step}</span>
              {index < steps.length - 1 ? <div className="h-px flex-1 bg-border" /> : null}
            </div>
          );
        })}
      </div>
    </div>
  );
}
