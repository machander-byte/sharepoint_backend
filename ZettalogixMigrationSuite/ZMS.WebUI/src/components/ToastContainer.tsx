import Toast from "./Toast";
import { useZmsDispatch, useZmsState } from "../state/ZmsStateProvider";

export default function ToastContainer(): JSX.Element {
  const { toasts } = useZmsState();
  const dispatch = useZmsDispatch();

  return (
    <div className="pointer-events-none fixed right-4 top-20 z-[80] flex w-[calc(100vw-2rem)] max-w-sm flex-col gap-3">
      {toasts.map((toast) => (
        <Toast key={toast.id} toast={toast} onDismiss={(id) => dispatch({ type: "REMOVE_TOAST", payload: id })} />
      ))}
    </div>
  );
}
