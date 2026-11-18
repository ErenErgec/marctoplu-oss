import Link from "next/link";
import { ShieldX } from "lucide-react";

export function AdminUnauthorized({ message }: { message: string }) {
  return (
    <div className="auth-page">
      <div className="auth-card" style={{ textAlign: "center", maxWidth: 460 }}>
        <div className="admin-unauth-icon" aria-hidden="true">
          <ShieldX size={30} className="icon" />
        </div>
        <h2 style={{ fontSize: 22, fontWeight: 800, marginBottom: 8 }}>Yetkisiz erişim</h2>
        <p className="admin-card-desc" style={{ fontSize: 14, marginBottom: 24 }}>
          {message || "Bu sayfayı görüntülemek için yönetici yetkisine sahip bir hesapla oturum açmalısınız."}
        </p>
        <div className="admin-unauth-actions">
          <Link href="/" className="btn-primary">
            Ana panele dön
          </Link>
          <Link href="/login" className="btn-glass">
            Farklı hesapla giriş yap
          </Link>
        </div>
      </div>
    </div>
  );
}
