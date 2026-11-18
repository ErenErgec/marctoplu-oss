export function formatStatus(status: string) {
  const normalized = status.toLowerCase();
  if (normalized === "completed") return { text: "Tamamlandı", className: "status status-success" };
  if (normalized === "running") return { text: "İşleniyor", className: "status status-running" };
  if (normalized === "cancel_requested") return { text: "İptal Ediliyor", className: "status status-running" };
  if (normalized === "canceled" || normalized === "cancelled") return { text: "İptal Edildi", className: "status status-failed" };
  if (normalized === "queued") return { text: "Bekliyor", className: "status status-queued" };
  if (normalized === "failed") return { text: "Başarısız", className: "status status-failed" };
  return { text: status, className: "status" };
}

export function formatDate(value: string) {
  const d = new Date(value);
  if (Number.isNaN(d.getTime())) return "-";
  return d.toLocaleString("tr-TR", { dateStyle: "short", timeStyle: "short" });
}
