"use client";

import { useState } from "react";
import Link from "next/link";

export default function AccountDeletionPage() {
  const [email, setEmail] = useState("");
  const [reason, setReason] = useState("");
  const [submitted, setSubmitted] = useState(false);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!email) return;
    setSubmitted(true);
  }

  return (
    <div className="app-container" style={{ maxWidth: 840, margin: "0 auto", padding: "40px 20px" }}>
      {/* Header */}
      <header className="navbar" style={{ marginBottom: 32 }}>
        <div className="nav-brand">
          <div className="brand-icon">
            <img src="/logo.png" alt="Logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
          </div>
          <span>MARCTOPLU</span>
        </div>

        <nav className="nav-menu">
          <Link href="/" className="nav-link">Ana Sayfa</Link>
          <Link href="/gizlilik" className="nav-link">Gizlilik Politikası</Link>
        </nav>

        <Link href="/" className="btn-glass">
          ← Ana Sayfaya Dön
        </Link>
      </header>

      {/* Content */}
      <div className="glass-panel" style={{ padding: "36px 32px" }}>
        <div style={{ borderBottom: "1px solid var(--border-light)", paddingBottom: 16, marginBottom: 24 }}>
          <div style={{ display: "inline-block", padding: "4px 12px", borderRadius: 20, background: "var(--status-danger-bg-hover)", color: "#991B1B", fontSize: 12, fontWeight: 700, marginBottom: 12 }}>
            HESAP VE VERİ YÖNETİMİ
          </div>
          <h1 style={{ fontSize: 26, fontWeight: 800, color: "var(--text-main)", marginBottom: 6 }}>
            Hesap ve Veri Silme Talebi
          </h1>
          <p style={{ fontSize: 13, color: "var(--text-secondary)" }}>
            Google Play ve Apple App Store veri güvenliği standartları uyarınca hazırlanmıştır.
          </p>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 24, fontSize: 14.5, color: "var(--text-muted)", lineHeight: 1.6 }}>
          <section>
            <p>
              MARCTOPLU kullanıcıları diledikleri zaman hesaplarını ve hesaplarıyla ilişkili tüm kişisel verilerini kalıcı olarak silme hakkına sahiptir.
            </p>
          </section>

          {/* Option 1 */}
          <div style={{ background: "var(--surface-hover)", border: "1px solid var(--border-light)", borderRadius: 14, padding: "20px 24px" }}>
            <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--text-main)", marginBottom: 8, display: "flex", alignItems: "center", gap: 8 }}>
              <span>📱</span> Yöntem 1: Mobil Uygulama İçinden Doğrudan Silme (Anında)
            </h2>
            <p style={{ marginBottom: 10, fontSize: 13.5 }}>
              Uygulama telefonunuzda yüklü ise en hızlı yol uygulama içinden tek tıkla silmektir:
            </p>
            <ol style={{ paddingLeft: 20, display: "flex", flexDirection: "column", gap: 4, fontSize: 13.5 }}>
              <li>MARCTOPLU uygulamasını açın.</li>
              <li>Alt menüden <strong>Profilim</strong> sekmesine geçin.</li>
              <li>Sayfanın en altındaki <strong>&quot;Hesabımı ve Verilerimi Sil&quot;</strong> butonuna dokunun ve onaylayın.</li>
            </ol>
            <p style={{ marginTop: 10, fontSize: 12.5, color: "var(--text-secondary)" }}>
              ✓ Hesabınız, kimlik bilgileriniz ve Firebase oturumunuz anında sistemlerimizden kalıcı olarak silinir.
            </p>
          </div>

          {/* Option 2 */}
          <div style={{ background: "var(--bg-card)", border: "1px solid var(--border-medium)", borderRadius: 14, padding: "20px 24px" }}>
            <h2 style={{ fontSize: 16, fontWeight: 700, color: "var(--text-main)", marginBottom: 8, display: "flex", alignItems: "center", gap: 8 }}>
              <span>🌐</span> Yöntem 2: Web Üzerinden Silme Talebi İletme
            </h2>
            <p style={{ marginBottom: 14, fontSize: 13.5 }}>
              Uygulamayı sildiyseniz veya cihazınıza erişemiyorsanız, aşağıdaki formdan veya doğrudan <a href="mailto:info@marctoplu.com">info@marctoplu.com</a> adresine kayıtlı e-postanızla yazarak silme talebinde bulunabilirsiniz:
            </p>

            {submitted ? (
              <div style={{ background: "var(--status-success-bg)", border: "1px solid var(--status-success-border)", padding: "16px 20px", borderRadius: 12, color: "var(--status-success)" }}>
                <div style={{ fontWeight: 700, marginBottom: 4 }}>Talebiniz Alındı</div>
                <p style={{ fontSize: 13 }}>
                  <strong>{email}</strong> adresine kayıtlı hesabınızın ve verilerinizin silinmesi için işlem başlatıldı. Talebiniz en geç 24 saat içinde tamamlanacak ve bilgilendirileceksiniz.
                </p>
              </div>
            ) : (
              <form onSubmit={handleSubmit} style={{ display: "flex", flexDirection: "column", gap: 14 }}>
                <div>
                  <label style={{ display: "block", fontSize: 13, fontWeight: 600, marginBottom: 6, color: "var(--text-main)" }}>
                    Kayıtlı E-posta Adresiniz:
                  </label>
                  <input
                    type="email"
                    required
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="ornek@alanadi.com"
                    style={{
                      width: "100%",
                      padding: "10px 14px",
                      borderRadius: 10,
                      border: "1px solid var(--border-medium)",
                      fontSize: 14,
                    }}
                  />
                </div>

                <div>
                  <label style={{ display: "block", fontSize: 13, fontWeight: 600, marginBottom: 6, color: "var(--text-main)" }}>
                    Ayrılma Sebebi (Opsiyonel):
                  </label>
                  <textarea
                    rows={3}
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    placeholder="Geri bildiriminiz bizim için değerlidir..."
                    style={{
                      width: "100%",
                      padding: "10px 14px",
                      borderRadius: 10,
                      border: "1px solid var(--border-medium)",
                      fontSize: 14,
                      resize: "vertical",
                    }}
                  />
                </div>

                <button
                  type="submit"
                  style={{
                    alignSelf: "flex-start",
                    background: "#B91C1C",
                    color: "#FFFFFF",
                    border: "none",
                    padding: "11px 22px",
                    borderRadius: 10,
                    fontWeight: 700,
                    fontSize: 14,
                    cursor: "pointer",
                  }}
                >
                  Hesap Silme Talebini Gönder
                </button>
              </form>
            )}
          </div>

          {/* What happens on deletion */}
          <section>
            <h3 style={{ fontSize: 15, fontWeight: 700, color: "var(--text-main)", marginBottom: 8 }}>
              Hesabınız Silindiğinde Ne Olur?
            </h3>
            <ul style={{ paddingLeft: 20, display: "flex", flexDirection: "column", gap: 6, fontSize: 13.5 }}>
              <li>Ad, soyad, e-posta ve oturum kayıtlarınız kalıcı olarak veritabanından silinir.</li>
              <li>Kalan token bakiyeniz sıfırlanır ve cüzdanınız kapatılır.</li>
              <li>Geçmişte işlediğiniz MARC katalog kayıtları sistem güvenliği için kişisel veri bağlantısı tamamen koparılarak anonimleştirilir.</li>
              <li>İşlem geri alınamaz.</li>
            </ul>
          </section>
        </div>
      </div>
    </div>
  );
}
