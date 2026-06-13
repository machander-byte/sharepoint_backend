import { createToast } from "./zmsActions";

export const toastActions = {
  success: (title: string, description?: string) => createToast("success", title, description),
  warning: (title: string, description?: string) => createToast("warning", title, description),
  error: (title: string, description?: string) => createToast("error", title, description),
  info: (title: string, description?: string) => createToast("info", title, description)
};
