import { CircleCheck, LifeBuoy, Lightbulb, X } from "lucide-react";

export type SupportForm = { email: string; jobId: string; message: string };

export function SupportModal({
  form,
  sent,
  sending,
  error,
  onFormChange,
  onSubmit,
  onClose,
}: {
  form: SupportForm;
  sent: boolean;
  sending: boolean;
  error: string;
  onFormChange: (form: SupportForm) => void;
  onSubmit: () => void;
  onClose: () => void;
}) {
  return (
    <div
      style={{
        position: "fixed",
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        background: "var(--overlay)",
        backdropFilter: "blur(6px)",
        WebkitBackdropFilter: "blur(6px)",
        zIndex: 9999,
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        padding: 16,
      }}
      onClick={onClose}
    >
      <div
        className="modal-card"
        onClick={(e) => e.stopPropagation()}
      >
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 16 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
            <div
              style={{
                width: 40,
                height: 40,
                borderRadius: 12,
                background: "var(--primary-surface)",
                border: "1px solid var(--primary-border)",
                color: "var(--primary)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
              }}
              aria-hidden="true"
            >
              <LifeBuoy size={20} className="icon" />
            </div>
            <div>
              <h3 style={{ margin: 0, fontSize: 18, fontWeight: 800, color: "var(--text-main)" }}>
                Destek & Sorun Bildirimi
              </h3>
              <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--text-muted)" }}>
                Yaşadığınız problemi iletin, yönetici ekibimiz incelesin.
              </p>
            </div>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label="Kapat"
            style={{ background: "none", border: "none", cursor: "pointer", color: "var(--text-muted)", display: "inline-flex", padding: 4 }}
          >
            <X size={18} className="icon" />
          </button>
        </div>

        {sent ? (
          <div style={{ textAlign: "center", padding: "20px 0" }}>
            <CircleCheck size={44} className="icon" aria-hidden="true" style={{ color: "var(--status-success)", marginBottom: 12 }} />
            <h4 style={{ fontSize: 16, fontWeight: 700, marginBottom: 8, color: "var(--text-main)" }}>
              Talebiniz Alındı!
            </h4>
            <p style={{ fontSize: 13, color: "var(--text-muted)", lineHeight: 1.5, marginBottom: 20 }}>
              Bildiriminiz yönetici ekibine ulaştı. İnceleme sonrasında size e-posta ile dönüş yapılacak, gerekirse hesabınıza telafi tokeni tanımlanacaktır.
            </p>
            <button
              type="button"
              className="btn-primary"
              style={{ width: "100%", height: 42 }}
              onClick={onClose}
            >
              Tamam
            </button>
          </div>
        ) : (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              if (!sending) onSubmit();
            }}
          >
            <div style={{ display: "flex", flexDirection: "column", gap: 14 }}>
              <div>
                <label style={{ display: "block", fontSize: 12, fontWeight: 600, marginBottom: 6, color: "var(--text-main)" }}>
                  İletişim E-Posta Adresiniz *
                </label>
                <input
                  type="email"
                  required
                  placeholder="ornek@eposta.com"
                  value={form.email}
                  onChange={(e) => onFormChange({ ...form, email: e.target.value })}
                  style={{ width: "100%", height: 40, padding: "0 12px", borderRadius: 10, border: "1px solid var(--border-light)", fontSize: 13 }}
                />
              </div>

              {form.jobId && (
                <div>
                  <label style={{ display: "block", fontSize: 12, fontWeight: 600, marginBottom: 6, color: "var(--text-main)" }}>
                    İlgili İşlem / İş ID
                  </label>
                  <input
                    type="text"
                    disabled
                    value={form.jobId}
                    style={{ width: "100%", height: 38, padding: "0 12px", borderRadius: 10, border: "1px solid var(--border-light)", background: "var(--surface-subtle)", fontSize: 12, fontFamily: "monospace" }}
                  />
                </div>
              )}

              <div>
                <label style={{ display: "block", fontSize: 12, fontWeight: 600, marginBottom: 6, color: "var(--text-main)" }}>
                  Sorun Açıklaması *
                </label>
                <textarea
                  required
                  minLength={10}
                  maxLength={4000}
                  rows={4}
                  placeholder="Karşılaştığınız sorunu, eksik veya hatalı işlenen ISBN sayısını belirtiniz..."
                  value={form.message}
                  onChange={(e) => onFormChange({ ...form, message: e.target.value })}
                  style={{ width: "100%", padding: "10px 12px", borderRadius: 10, border: "1px solid var(--border-light)", fontSize: 13, resize: "vertical", fontFamily: "inherit" }}
                />
              </div>

              <div
                style={{
                  padding: "10px 14px",
                  borderRadius: 10,
                  background: "var(--surface-hover)",
                  border: "1px solid var(--border-light)",
                  fontSize: 12,
                  color: "var(--text-muted)",
                  lineHeight: 1.4,
                }}
              >
                <Lightbulb size={14} className="icon" aria-hidden="true" /> Yöneticimiz bildiriminiz üzerine hesabınızı inceleyip eksik veya hatalı işlemler için doğrudan hesabınıza token tanımlayabilir.
              </div>

              {error && (
                <div
                  role="alert"
                  style={{
                    padding: "10px 14px",
                    borderRadius: 10,
                    background: "var(--status-danger-bg)",
                    border: "1px solid var(--status-danger-border)",
                    fontSize: 12,
                    color: "var(--status-danger)",
                    lineHeight: 1.4,
                  }}
                >
                  {error}
                </div>
              )}

              <div style={{ display: "flex", gap: 10, marginTop: 6 }}>
                <button
                  type="submit"
                  className="btn-primary"
                  style={{ flex: 1, height: 42 }}
                  disabled={sending}
                >
                  {sending ? "Gönderiliyor..." : "Bildirimi Gönder"}
                </button>
                <button
                  type="button"
                  className="btn-glass"
                  style={{ height: 42 }}
                  onClick={onClose}
                >
                  Vazgeç
                </button>
              </div>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
