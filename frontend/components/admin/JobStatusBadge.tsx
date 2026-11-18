import { formatStatus } from "../../lib/format";

const ACTIVE_STATUSES = new Set(["queued", "running", "cancel_requested"]);

/** Kullanıcı paneliyle aynı etiket ve renkler (lib/format.ts). */
export function JobStatusBadge({ status }: { status: string }) {
  const s = formatStatus(status);
  return (
    <span className={s.className}>
      {ACTIVE_STATUSES.has(status.toLowerCase()) && <span className="live-pulse-dot" aria-hidden="true" />}
      {s.text}
    </span>
  );
}
