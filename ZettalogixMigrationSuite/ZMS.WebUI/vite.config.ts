import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { fileURLToPath, URL } from "node:url";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  build: {
    rolldownOptions: {
      output: {
        strictExecutionOrder: true,
        codeSplitting: {
          groups: [
            {
              name: "react-vendor",
              test: /node_modules[\\/](react|react-dom|react-router|react-router-dom|scheduler)[\\/]/,
              priority: 30
            },
            {
              name: "supabase-vendor",
              test: /node_modules[\\/]@supabase[\\/]/,
              priority: 20
            },
            {
              name: "ui-vendor",
              test: /node_modules[\\/](lucide-react|radix-ui|@radix-ui)[\\/]/,
              priority: 15
            },
            {
              name: "vendor",
              test: /node_modules[\\/]/,
              maxSize: 250_000,
              priority: 10
            }
          ]
        }
      }
    }
  },
  resolve: {
    alias: {
      "@": fileURLToPath(new URL("./src", import.meta.url))
    }
  },
  server: {
    port: 5173
  },
  preview: {
    allowedHosts: ["sharepoint-sj6m.onrender.com"]
  }
});
