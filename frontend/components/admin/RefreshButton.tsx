import { RefreshCw } from "lucide-react";

export function RefreshButton({ onClick }: { onClick: () => void }) {
  return (
    <button type="button" className="admin-refresh-btn" onClick={onClick}>
      <RefreshCw size={14} className="icon" aria-hidden="true" />
      Yenile
    </button>
  );
}
