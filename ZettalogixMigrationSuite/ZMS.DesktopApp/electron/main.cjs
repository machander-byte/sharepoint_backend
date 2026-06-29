const { app, BrowserWindow } = require("electron");
const path = require("path");

function resolveStartTarget() {
  if (!app.isPackaged) {
    return {
      mode: "url",
      value: process.env.ZMS_DESKTOP_START_URL || "http://localhost:5173"
    };
  }

  return {
    mode: "file",
    value: path.resolve(__dirname, "..", "..", "ZMS.WebUI", "dist", "index.html")
  };
}

function createWindow() {
  const mainWindow = new BrowserWindow({
    width: 1440,
    height: 920,
    minWidth: 1100,
    minHeight: 760,
    backgroundColor: "#0e1b2d",
    title: "zettalogixmigrationsuite",
    webPreferences: {
      preload: path.join(__dirname, "preload.cjs"),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  const startTarget = resolveStartTarget();

  if (startTarget.mode === "url") {
    void mainWindow.loadURL(startTarget.value);
  } else {
    void mainWindow.loadFile(startTarget.value);
  }
}

app.whenReady().then(() => {
  createWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
