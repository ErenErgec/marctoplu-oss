import { LifeBuoy, LogOut } from "lucide-react";
import Link from "next/link";
import { auth } from "../../lib/firebaseClient";
import type { AuthUserInfo } from "../../lib/useAuth";
import { ThemeToggle } from "../ThemeToggle";

export function DashboardNavbar({
  userInfo,
  userInitial,
  onOpenSupport,
}: {
  userInfo: AuthUserInfo;
  userInitial: string;
  onOpenSupport: () => void;
}) {
  return (
    <header className="navbar">
      <div className="nav-brand">
        <div className="brand-icon"><img src="/logo.png" alt="Logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} /></div>
        <span>MARCTOPLU</span>
      </div>

      <nav className="nav-menu">
        <Link href="/" className="nav-link active">Panel</Link>
        <Link href="/profile" className="nav-link">Profil</Link>
        <button type="button" onClick={onOpenSupport} className="nav-link nav-link-button">
          <LifeBuoy size={15} className="icon" aria-hidden="true" /> Destek
        </button>
      </nav>

      <div className="nav-actions">
        <ThemeToggle />
        <div className="nav-user-badge">
          <div className="avatar-circle">{userInitial}</div>
          <span className="nav-user-name">{userInfo.name || userInfo.email}</span>
          <button type="button" className="nav-logout" onClick={() => auth.signOut()}>
            <LogOut size={14} className="icon" aria-hidden="true" /> Çıkış
          </button>
        </div>
      </div>
    </header>
  );
}
