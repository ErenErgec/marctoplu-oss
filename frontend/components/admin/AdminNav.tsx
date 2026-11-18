import Link from "next/link";
import { ArrowLeft, BarChart3, LifeBuoy, ListChecks, Package, Receipt, Users, type LucideIcon } from "lucide-react";
import type { AdminTab } from "./types";
import { ThemeToggle } from "../ThemeToggle";

const TABS: { id: AdminTab; label: string; icon: LucideIcon }[] = [
  { id: "stats", label: "İstatistikler", icon: BarChart3 },
  { id: "purchases", label: "Satın Almalar", icon: Receipt },
  { id: "packages", label: "Token Paketleri", icon: Package },
  { id: "users", label: "Kullanıcılar & Token", icon: Users },
  { id: "jobs", label: "Son İşler", icon: ListChecks },
  { id: "support", label: "Destek", icon: LifeBuoy },
];

export function AdminNav({
  activeTab,
  onTabChange,
}: {
  activeTab: AdminTab;
  onTabChange: (tab: AdminTab) => void;
}) {
  return (
    <nav className="admin-nav" aria-label="Yönetici menüsü">
      <div className="admin-nav-inner">
        <div className="admin-brand">
          <div className="brand-mark" aria-hidden="true">M</div>
          <strong>Admin Panel</strong>
        </div>
        <div className="admin-nav-links" role="tablist">
          {TABS.map(({ id, label, icon: Icon }) => (
            <button
              key={id}
              type="button"
              role="tab"
              aria-selected={activeTab === id}
              className={activeTab === id ? "active" : ""}
              onClick={() => onTabChange(id)}
            >
              <Icon size={16} className="icon" aria-hidden="true" />
              {label}
            </button>
          ))}
        </div>
        <div className="nav-actions">
          <ThemeToggle />
          <Link href="/" className="admin-back-link">
            <ArrowLeft size={14} className="icon" aria-hidden="true" /> Panel
          </Link>
        </div>
      </div>
    </nav>
  );
}
