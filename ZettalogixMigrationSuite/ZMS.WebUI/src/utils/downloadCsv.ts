function escapeCsv(value: unknown): string {
  const text = String(value ?? "");
  if (/[",\r\n]/.test(text)) {
    return `"${text.replace(/"/g, '""')}"`;
  }
  return text;
}

export function downloadCsv(fileName: string, rows: Array<Record<string, unknown>>): void {
  const headers = Object.keys(rows[0] ?? { status: "No data" });
  const bodyRows = rows.length > 0 ? rows : [{ status: "No data" }];
  const csv = [headers.join(","), ...bodyRows.map((row) => headers.map((header) => escapeCsv(row[header])).join(","))].join("\r\n");
  const blob = new Blob(["\uFEFF", csv], { type: "text/csv;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}
