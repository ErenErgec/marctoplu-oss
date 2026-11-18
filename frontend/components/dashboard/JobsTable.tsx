import { useEffect, useState } from "react";
import {
  ChevronDown,
  CircleAlert,
  Download,
  FileCode,
  FileSpreadsheet,
  FileText,
  Inbox,
  Library,
  MoreHorizontal,
  RefreshCw,
  Trash2,
  X,
} from "lucide-react";
import { formatDate, formatStatus } from "../../lib/format";
import { DOWNLOAD_FORMATS, type DownloadFormat, type Job } from "../../lib/types";

const ACTIVE_STATUSES = new Set(["queued", "running", "cancel_requested"]);

type OpenMenu = { jobId: string; kind: "download" | "more"; top: number; right: number };

/** Menüyü butonun altına, sığmıyorsa üstüne konumlar (tablo kaydırılabilir kapta olduğu için fixed). */
function menuPosition(button: HTMLElement, menuHeight: number) {
  const rect = button.getBoundingClientRect();
  const openUpwards = window.innerHeight - rect.bottom < menuHeight + 16;
  return {
    top: openUpwards ? Math.max(8, rect.top - menuHeight - 6) : rect.bottom + 6,
    right: Math.max(12, window.innerWidth - rect.right),
  };
}

export function JobsTable({
  jobs,
  cancelingId,
  deletingId,
  downloadingState,
  onRefresh,
  onDownload,
  onCancel,
  onDelete,
  onReportIssue,
}: {
  jobs: Job[];
  cancelingId: string | null;
  deletingId: string | null;
  downloadingState: { id: string; format: DownloadFormat } | null;
  onRefresh: () => void;
  onDownload: (id: string, format: DownloadFormat) => void;
  onCancel: (id: string) => void;
  onDelete: (id: string) => void;
  onReportIssue: (job: Job) => void;
}) {
  const [openMenu, setOpenMenu] = useState<OpenMenu | null>(null);

  useEffect(() => {
    function handleClose(e: Event) {
      if (e.type === "keydown" && (e as KeyboardEvent).key !== "Escape") return;
      if (e.type === "click" && (e.target as HTMLElement)?.closest?.(".job-menu, .job-menu-trigger")) return;
      setOpenMenu(null);
    }
    window.addEventListener("click", handleClose);
    window.addEventListener("keydown", handleClose);
    window.addEventListener("scroll", handleClose, true);
    window.addEventListener("resize", handleClose);
    return () => {
      window.removeEventListener("click", handleClose);
      window.removeEventListener("keydown", handleClose);
      window.removeEventListener("scroll", handleClose, true);
      window.removeEventListener("resize", handleClose);
    };
  }, []);

  function toggleMenu(e: React.MouseEvent<HTMLButtonElement>, jobId: string, kind: OpenMenu["kind"]) {
    if (openMenu?.jobId === jobId && openMenu.kind === kind) {
      setOpenMenu(null);
      return;
    }
    setOpenMenu({ jobId, kind, ...menuPosition(e.currentTarget, kind === "download" ? 190 : 96) });
  }

  function runAndClose(action: () => void) {
    setOpenMenu(null);
    action();
  }

  const menuJob = openMenu ? jobs.find((j) => j.id === openMenu.jobId) : undefined;

  return (
    <section className="glass-panel">
      <div className="panel-header">
        <h2 className="panel-title">İşlerim</h2>
        <button type="button" className="btn-glass btn-sm" onClick={onRefresh}>
          <RefreshCw size={14} className="icon" aria-hidden="true" /> Yenile
        </button>
      </div>

      {jobs.length === 0 ? (
        <div className="empty-state">
          <Inbox size={32} className="icon" aria-hidden="true" />
          <p className="empty-state-title">Henüz bir işiniz yok</p>
          <p className="empty-state-hint">Yukarıdan Excel dosyanızı yükleyerek ilk kataloglama işinizi başlatın.</p>
        </div>
      ) : (
        <div className="data-table-container">
          <table className="data-table jobs-table">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>İlerleme</th>
                <th>Token</th>
                <th>Durum</th>
                <th><span className="sr-only">Eylemler</span></th>
              </tr>
            </thead>
            <tbody>
              {jobs.map((job) => {
                const status = job.status.toLowerCase();
                const s = formatStatus(job.status);
                const isActive = ACTIVE_STATUSES.has(status);
                const isProcessing = status === "queued" || status === "running";
                const hasProblem = status === "failed" || Boolean(job.errorMessage);
                const progressPct = job.totalIsbns > 0 ? Math.min(100, Math.round((job.processedIsbns / job.totalIsbns) * 100)) : 0;
                // Yalnızca bulunan kayıtlar ücretlendirilir; canlıda şu ana kadar bulunanlar gösterilir
                const consumed = job.consumedTokens;
                const isDownloading = downloadingState?.id === job.id;

                return (
                  <tr key={job.id}>
                    <td data-label="Tarih" className="cell-muted">{formatDate(job.createdAt)}</td>
                    <td data-label="İlerleme">
                      <div className="job-progress">
                        <div className="job-progress-meta">
                          <strong>{job.processedIsbns} / {job.totalIsbns} ISBN</strong>
                          {isProcessing && <span className="job-progress-pct">%{progressPct}</span>}
                        </div>
                        {isProcessing && (
                          <div className="progress-track" role="progressbar" aria-valuenow={progressPct} aria-valuemin={0} aria-valuemax={100}>
                            <div className="progress-fill" style={{ width: `${progressPct}%` }} />
                          </div>
                        )}
                      </div>
                    </td>
                    <td data-label="Token">
                      <div className="token-cell">
                        <span className={consumed > 0 ? "token-consumed" : "cell-muted"}>
                          {consumed > 0 ? `−${consumed}` : isProcessing ? "Hesaplanıyor…" : "0"}
                        </span>
                        {isProcessing && <div className="cell-hint">En fazla {job.totalIsbns}</div>}
                      </div>
                    </td>
                    <td data-label="Durum">
                      <span className={s.className}>
                        {isActive && <span className="live-pulse-dot" aria-hidden="true" />}
                        {s.text}
                      </span>
                    </td>
                    <td className="cell-actions">
                      <div className="job-actions">
                        {job.canDownload && (
                          <button
                            type="button"
                            className="btn-primary btn-sm job-menu-trigger"
                            aria-haspopup="menu"
                            aria-expanded={openMenu?.jobId === job.id && openMenu.kind === "download"}
                            onClick={(e) => toggleMenu(e, job.id, "download")}
                            disabled={isDownloading}
                          >
                            {isDownloading ? (
                              <>
                                <span className="live-pulse-dot" aria-hidden="true" />
                                {downloadingState ? `${DOWNLOAD_FORMATS[downloadingState.format].label}…` : null}
                              </>
                            ) : (
                              <>
                                <Download size={14} className="icon" aria-hidden="true" /> İndir
                                <ChevronDown size={14} className="icon" aria-hidden="true" />
                              </>
                            )}
                          </button>
                        )}
                        {isActive && (
                          <button
                            type="button"
                            className="btn-glass btn-sm btn-danger-soft"
                            onClick={() => onCancel(job.id)}
                            disabled={cancelingId === job.id || status === "cancel_requested"}
                          >
                            <X size={14} className="icon" aria-hidden="true" /> İptal
                          </button>
                        )}
                        <button
                          type="button"
                          className="btn-glass btn-sm btn-icon job-menu-trigger"
                          aria-label="Diğer işlemler"
                          aria-haspopup="menu"
                          aria-expanded={openMenu?.jobId === job.id && openMenu.kind === "more"}
                          onClick={(e) => toggleMenu(e, job.id, "more")}
                        >
                          <MoreHorizontal size={16} className="icon" />
                        </button>
                      </div>
                      {hasProblem && job.errorMessage && (
                        <div className="job-error" title={job.errorMessage}>
                          <CircleAlert size={12} className="icon" aria-hidden="true" /> {job.errorMessage}
                        </div>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {openMenu && menuJob && (
        <div className="job-menu" role="menu" style={{ top: openMenu.top, right: openMenu.right }}>
          {openMenu.kind === "download" ? (
            <>
              <button type="button" role="menuitem" onClick={() => runAndClose(() => onDownload(menuJob.id, "xlsx"))}>
                <FileSpreadsheet size={16} className="icon" aria-hidden="true" />
                <span>Excel</span>
                <span className="job-menu-ext">.xlsx</span>
              </button>
              <button type="button" role="menuitem" onClick={() => runAndClose(() => onDownload(menuJob.id, "mrc"))}>
                <Library size={16} className="icon" aria-hidden="true" />
                <span>MARC21 <span className="job-menu-hint">Koha, YORDAM</span></span>
                <span className="job-menu-ext">.mrc</span>
              </button>
              <button type="button" role="menuitem" onClick={() => runAndClose(() => onDownload(menuJob.id, "marcxml"))}>
                <FileCode size={16} className="icon" aria-hidden="true" />
                <span>MARCXML</span>
                <span className="job-menu-ext">.xml</span>
              </button>
              <button type="button" role="menuitem" onClick={() => runAndClose(() => onDownload(menuJob.id, "ris"))}>
                <FileText size={16} className="icon" aria-hidden="true" />
                <span>RIS <span className="job-menu-hint">Zotero, EndNote</span></span>
                <span className="job-menu-ext">.ris</span>
              </button>
            </>
          ) : (
            <>
              <button type="button" role="menuitem" onClick={() => runAndClose(() => onReportIssue(menuJob))}>
                <CircleAlert size={16} className="icon" aria-hidden="true" />
                <span>Sorun bildir</span>
              </button>
              <button
                type="button"
                role="menuitem"
                className="job-menu-danger"
                disabled={deletingId === menuJob.id}
                onClick={() => runAndClose(() => onDelete(menuJob.id))}
              >
                <Trash2 size={16} className="icon" aria-hidden="true" />
                <span>Kaydı sil</span>
              </button>
            </>
          )}
        </div>
      )}
    </section>
  );
}
