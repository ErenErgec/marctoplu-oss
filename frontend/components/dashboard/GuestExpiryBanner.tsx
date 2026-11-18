import Link from "next/link";
import { Clock } from "lucide-react";

export function GuestExpiryBanner() {
  return (
    <div
      style={{
        margin: "20px 0",
        padding: "16px 20px",
        borderRadius: 16,
        background: "var(--primary-surface)",
        border: "1px solid var(--primary-border)",
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        flexWrap: "wrap",
        gap: 12,
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
        <Clock size={22} className="icon" aria-hidden="true" style={{ color: "var(--primary)" }} />
        <div>
          <div style={{ fontSize: 14, fontWeight: 700, color: "var(--primary)" }}>
            Misafir Oturumu (3 Günlük Geçici Hesap)
          </div>
          <div style={{ fontSize: 13, color: "var(--text-muted)", marginTop: 2 }}>
            Misafir oturumunuz ve kalan bakiyeniz 3 gün sonra silinir. Bakiyenizi kaybetmemek için hesabınızı kalıcı yapabilirsiniz.
          </div>
        </div>
      </div>
      <Link
        href="/register"
        className="btn-primary"
        style={{ padding: "8px 16px", fontSize: 13, height: "auto", whiteSpace: "nowrap" }}
      >
        Kalıcı Hesap Aç
      </Link>
    </div>
  );
}
