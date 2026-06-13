import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./app/App";
import { AuthProvider } from "./hooks/useAuth";
import { ZmsStateProvider } from "./state/ZmsStateProvider";
import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <ZmsStateProvider>
          <App />
        </ZmsStateProvider>
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
