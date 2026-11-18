import { Gift } from "lucide-react";
import { formatDate } from "../../lib/format";
import { JobStatusBadge } from "./JobStatusBadge";
import { RefreshButton } from "./RefreshButton";
import type { AdminJob } from "./types";

export function JobsTab({
  jobs,
  onRefresh,
  onCompensate,
}: {
  jobs: AdminJob[];
  onRefresh: () => void;
  onCompensate: (job: AdminJob) => void;
}) {
  return (
    <div className="admin-section">
      <h2>Son İşler (50 kayıt)</h2>
      <div className="admin-card">
        <div className="table-wrap">
          {jobs.length > 0 ? (
            <table>
              <thead>
                <tr>
                  <th>Durum</th>
                  <th>ISBN</th>
                  <th>Token</th>
                  <th>Kullanıcı ID</th>
                  <th>Hata detayı</th>
                  <th>Tarih</th>
                  <th><span className="sr-only">İşlem</span></th>
                </tr>
              </thead>
              <tbody>
                {jobs.map((job) => (
                  <tr key={job.id}>
                    <td><JobStatusBadge status={job.status} /></td>
                    <td>
                      <strong>
                        {job.processedIsbns}/{job.totalIsbns}
                      </strong>
                    </td>
                    <td>{job.consumedTokens}</td>
                    <td>
                      <span className="mono-id" title={job.userId}>{job.userId.slice(0, 8)}…</span>
                    </td>
                    <td className="cell-sub cell-message">{job.errorMessage ?? "-"}</td>
                    <td className="cell-sub cell-nowrap">{formatDate(job.createdAt)}</td>
                    <td>
                      <button
                        type="button"
                        className="ghost btn-xs"
                        onClick={() => onCompensate(job)}
                        title="Bu kullanıcıya problem telafisi veya token ekle"
                      >
                        <Gift size={12} className="icon" aria-hidden="true" /> Telafi tokeni
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="admin-empty">Henüz kayıtlı iş bulunmuyor.</p>
          )}
        </div>
      </div>
      <RefreshButton onClick={onRefresh} />
    </div>
  );
}
