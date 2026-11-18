import { Smartphone } from "lucide-react";
import Link from "next/link";

export function GuestLanding() {
  return (
    <div className="auth-page">
      <div className="auth-card" style={{ textAlign: "center" }}>
        <div className="brand-icon" style={{ margin: "0 auto 16px", width: 56, height: 56, fontSize: 24 }}><img src="/logo.png" alt="Logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} /></div>
        <h2 style={{ fontSize: 24, marginBottom: 8 }}>MARCTOPLU</h2>
        <p style={{ color: "var(--text-muted)", fontSize: 14, marginBottom: 24 }}>
          Toplu ISBN taraması ve MARC21 veri kataloğu yönetimi için giriş yapın.
        </p>
        <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
          <div style={{ display: "flex", gap: 12, justifyContent: "center" }}>
            <Link href="/login" className="btn-primary" style={{ width: "100%" }}>
              Giriş Yap
            </Link>
            <Link href="/register" className="btn-glass" style={{ width: "100%", justifyContent: "center" }}>
              Kayıt Ol
            </Link>
          </div>
          <div style={{
            marginTop: 10,
            padding: "14px 16px",
            borderRadius: 14,
            background: "var(--primary-surface)",
            border: "1px solid var(--primary-border)",
            textAlign: "center"
          }}>
            <div style={{ fontSize: 13, fontWeight: 700, color: "var(--primary)", marginBottom: 4 }}>
              <Smartphone size={16} className="icon" aria-hidden="true" /> 5 ücretsiz deneme hakkı mobilde
            </div>
            <div style={{ fontSize: 13, color: "var(--text-muted)", lineHeight: 1.4 }}>
              Kayıt olmadan hemen test etmek için <strong>MARCTOPLU</strong> Android uygulamasını kullanabilirsiniz.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
