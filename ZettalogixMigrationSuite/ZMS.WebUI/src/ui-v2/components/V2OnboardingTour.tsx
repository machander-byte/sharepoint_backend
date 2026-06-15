import type { V2PageId } from "../data/v2DashboardData";

export const zmsOnboardingStorageKey = "zms_onboarding_completed";

export interface V2TourStep {
  title: string;
  targetPage: V2PageId;
  description: string;
}

interface V2OnboardingTourProps {
  mode: "welcome" | "tour";
  currentStepIndex: number;
  steps: V2TourStep[];
  onStart: () => void;
  onSkip: () => void;
  onBack: () => void;
  onNext: () => void;
  onFinish: () => void;
}

export function V2OnboardingTour({
  mode,
  currentStepIndex,
  steps,
  onStart,
  onSkip,
  onBack,
  onNext,
  onFinish
}: V2OnboardingTourProps): JSX.Element {
  if (mode === "welcome") {
    return (
      <div className="zms-v2-tour-overlay" role="dialog" aria-modal="true" aria-labelledby="zms-v2-tour-welcome-title">
        <section className="zms-v2-tour-card zms-v2-tour-card-center">
          <span className="zms-v2-tour-kicker">First-time setup</span>
          <h2 id="zms-v2-tour-welcome-title">Welcome to ZMS</h2>
          <p>This guided tour will help you understand how to use the migration command center.</p>
          <div className="zms-v2-tour-actions">
            <button type="button" className="zms-v2-tour-secondary" onClick={onSkip}>
              Skip
            </button>
            <button type="button" className="zms-v2-tour-primary" onClick={onStart}>
              Start tour
            </button>
          </div>
        </section>
      </div>
    );
  }

  const step = steps[currentStepIndex];
  const stepNumber = currentStepIndex + 1;
  const isFirstStep = currentStepIndex === 0;
  const isLastStep = currentStepIndex === steps.length - 1;
  const progress = `${Math.round((stepNumber / steps.length) * 100)}%`;

  return (
    <div className="zms-v2-tour-overlay" role="dialog" aria-modal="true" aria-labelledby="zms-v2-tour-step-title">
      <section className="zms-v2-tour-card">
        <div className="zms-v2-tour-progress-row">
          <span className="zms-v2-tour-kicker">Step {stepNumber} of {steps.length}</span>
          <button type="button" className="zms-v2-tour-link" onClick={onSkip}>
            Skip tour
          </button>
        </div>
        <div className="zms-v2-tour-progress" aria-hidden="true">
          <span style={{ width: progress }} />
        </div>
        <h2 id="zms-v2-tour-step-title">{step.title}</h2>
        <p>{step.description}</p>
        <div className="zms-v2-tour-actions">
          <button type="button" className="zms-v2-tour-secondary" onClick={onBack} disabled={isFirstStep}>
            Back
          </button>
          {isLastStep ? (
            <button type="button" className="zms-v2-tour-primary" onClick={onFinish}>
              Finish
            </button>
          ) : (
            <button type="button" className="zms-v2-tour-primary" onClick={onNext}>
              Next
            </button>
          )}
        </div>
      </section>
    </div>
  );
}
