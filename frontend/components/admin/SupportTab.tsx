import { Check, Gift, RotateCcw } from "lucide-react";
import { formatDate } from "../../lib/format";
import { JobStatusBadge } from "./JobStatusBadge";
import { RefreshButton } from "./RefreshButton";
import type { SupportTicket, SupportTicketFilter } from "./types";

const FILTERS: { value: SupportTicketFilter; label: string }[] = [
  { value: "open", label: "Açık" },
  { value: "resolved", label: "Çözüldü" },
  { value: "all", label: "Tümü" },
];

export function SupportTab({
  tickets,
  filter,
  updatingId,
  onFilterChange,
  onRefresh,
  onSetStatus,
  onCompensate,
}: {
  tickets: SupportTicket[];
  filter: SupportTicketFilter;
  updatingId: string | null;
  onFilterChange: (filter: SupportTicketFilter) => void;
  onRefresh: () => void;
  onSetStatus: (ticket: SupportTicket, status: "open" | "resolved") => void;
  onCompensate: (ticket: SupportTicket) => void;
}) {
  return (
    <div className="admin-section">
      <h2>Destek Talepleri</h2>
      <div className="admin-chip-row" role="group" aria-label="Talep durumu filtresi" style={{ marginBottom: 12 }}>
        {FILTERS.map((f) => (
          <button
            key={f.value}
            type="button"
            className={`admin-chip${filter === f.value ? " is-active" : ""}`}
            aria-pressed={filter === f.value}
            onClick={() => onFilterChange(f.value)}
          >
            {f.label}
          </button>
        ))}
      </div>
      <div className="admin-card">
        <div className="table-wrap">
          {tickets.length > 0 ? (
            <table>
              <thead>
                <tr>
                  <th>Tarih</th>
                  <th>E-posta</th>
                  <th>İlgili İş</th>
                  <th>Mesaj</th>
                  <th>Durum</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {tickets.map((ticket) => (
                  <tr key={ticket.id}>
                    <td className="cell-sub cell-nowrap">{formatDate(ticket.createdAt)}</td>
                    <td>
                      <a href={`mailto:${ticket.email}`}>{ticket.email}</a>
                    </td>
                    <td>
                      {ticket.jobId ? (
                        <div className="admin-row-actions is-stacked">
                          <span className="mono-id" title={ticket.jobId}>{ticket.jobId.slice(0, 8)}…</span>
                          {ticket.jobStatus && <JobStatusBadge status={ticket.jobStatus} />}
                        </div>
                      ) : (
                        <span className="cell-sub">-</span>
                      )}
                    </td>
                    <td className="cell-message">
                      {ticket.message}
                    </td>
                    <td>
                      <span className={ticket.status === "open" ? "status status-queued" : "status status-success"}>
                        {ticket.status === "open" ? "Açık" : "Çözüldü"}
                      </span>
                    </td>
                    <td>
                      <div className="admin-row-actions is-stacked">
                        <button type="button" className="ghost btn-xs" onClick={() => onCompensate(ticket)}>
                          <Gift size={12} className="icon" aria-hidden="true" /> Telafi tokeni
                        </button>
                        <button
                          type="button"
                          className="ghost btn-xs"
                          disabled={updatingId === ticket.id}
                          onClick={() => onSetStatus(ticket, ticket.status === "open" ? "resolved" : "open")}
                        >
                          {ticket.status === "open" ? (
                            <>
                              <Check size={12} className="icon" aria-hidden="true" /> Çözüldü
                            </>
                          ) : (
                            <>
                              <RotateCcw size={12} className="icon" aria-hidden="true" /> Yeniden aç
                            </>
                          )}
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="admin-empty">Bu filtrede destek talebi bulunmuyor.</p>
          )}
        </div>
      </div>
      <RefreshButton onClick={onRefresh} />
    </div>
  );
}
