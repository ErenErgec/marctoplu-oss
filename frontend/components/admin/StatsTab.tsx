import {
  BookOpen,
  CircleAlert,
  CircleCheck,
  Coins,
  Hourglass,
  Receipt,
  ShoppingBag,
  Smartphone,
  Users,
  Wallet,
  Zap,
} from "lucide-react";
import { RefreshButton } from "./RefreshButton";
import { StatCard } from "./StatCard";
import type { IapRevenue, Stats } from "./types";

const ICON_PROPS = { size: 20, className: "icon", "aria-hidden": true } as const;

const tryAmount = (revenue: IapRevenue[]) =>
  `₺${(revenue.find((r) => r.currency === "TRY")?.amount ?? 0).toLocaleString("tr-TR", { minimumFractionDigits: 2 })}`;

/** Mağaza satış kartının alt satırı: TRY satın alma sayısı ve TL'ye çevrilmeyen diğer para birimleri. */
function revenueSub(revenue: IapRevenue[], store: string) {
  const tryRevenue = revenue.find((r) => r.currency === "TRY");
  const others = revenue.filter((r) => r.currency !== "TRY");
  const parts = [
    tryRevenue
      ? `${tryRevenue.purchases.toLocaleString("tr-TR")} satın alma · iadeler hariç · ${store} komisyonu düşülmedi`
      : "Fiyat kaydı yeni satın almalardan itibaren",
  ];
  if (others.length > 0) {
    parts.push(`+${others.map((r) => r.currency).join(", ")}`);
  }
  return parts.join(" · ");
}

export function StatsTab({
  stats,
  fastTokenAmount,
  addingTokens,
  onRefresh,
  onGoToUsers,
  onFastTokenAmountChange,
  onAddTokensToMe,
}: {
  stats: Stats | null;
  fastTokenAmount: string;
  addingTokens: boolean;
  onRefresh: () => void;
  onGoToUsers: () => void;
  onFastTokenAmountChange: (value: string) => void;
  onAddTokensToMe: () => void;
}) {
  return (
    <div className="admin-section">
      <h2>Platform İstatistikleri</h2>
      {stats ? (
        <>
          <div className="admin-stats-grid">
            <StatCard label="Toplam Kullanıcı" value={stats.totalUsers} icon={<Users {...ICON_PROPS} />} />
            <StatCard
              label="Toplam İş"
              value={stats.totalJobs}
              sub={`${stats.completedJobs} tamamlandı`}
              icon={<CircleCheck {...ICON_PROPS} />}
            />
            <StatCard
              label="Çalışan / Bekleyen"
              value={`${stats.runningJobs} / ${stats.queuedJobs}`}
              icon={<Hourglass {...ICON_PROPS} />}
            />
            <StatCard label="Hatalı İş" value={stats.failedJobs} icon={<CircleAlert {...ICON_PROPS} />} />
            <StatCard
              label="İşlenen ISBN"
              value={stats.totalIsbnProcessed.toLocaleString("tr-TR")}
              icon={<BookOpen {...ICON_PROPS} />}
            />
            <StatCard
              label="Tüketilen Token"
              value={stats.totalTokensConsumed.toLocaleString("tr-TR")}
              icon={<Coins {...ICON_PROPS} />}
            />
            <StatCard
              label="Mobil Satın Alma"
              value={stats.iapPurchases.toLocaleString("tr-TR")}
              sub={`Android ${stats.iapAndroid.purchases.toLocaleString("tr-TR")} · iOS ${stats.iapIos.purchases.toLocaleString("tr-TR")}${
                stats.iapRefunds > 0 ? ` · ${stats.iapRefunds.toLocaleString("tr-TR")} iade` : ""
              }`}
              icon={<Smartphone {...ICON_PROPS} />}
            />
            <StatCard
              label="Satılan Token (IAP)"
              value={stats.iapTokensSold.toLocaleString("tr-TR")}
              sub="Test ve promosyon satın almaları dahil"
              icon={<ShoppingBag {...ICON_PROPS} />}
            />
            <StatCard
              label="App Store Satışı (Brüt)"
              value={tryAmount(stats.iapIosRevenue)}
              sub={revenueSub(stats.iapIosRevenue, "Apple")}
              icon={<Receipt {...ICON_PROPS} />}
            />
            <StatCard
              label="Google Play Satışı (Brüt)"
              value={tryAmount(stats.iapAndroidRevenue)}
              sub={revenueSub(stats.iapAndroidRevenue, "Google")}
              icon={<Receipt {...ICON_PROPS} />}
            />
            {stats.iyzicoPayments > 0 && (
              <StatCard
                label="Web Ödemesi (iyzico)"
                value={`₺${stats.iyzicoRevenueTry.toLocaleString("tr-TR", { minimumFractionDigits: 2 })}`}
                sub={`${stats.iyzicoPayments.toLocaleString("tr-TR")} ödeme · entegrasyon askıda`}
                icon={<Wallet {...ICON_PROPS} />}
              />
            )}
          </div>
          <RefreshButton onClick={onRefresh} />
        </>
      ) : (
        <p className="muted">Yükleniyor...</p>
      )}

      <div className="admin-grid-2">
        <div className="admin-card">
          <div className="admin-card-header">
            <div className="admin-card-icon" aria-hidden="true">
              <Users size={18} className="icon" />
            </div>
            <div>
              <h3>Kullanıcıya token yükle</h3>
              <p className="admin-card-desc">
                Bir kullanıcı sorun bildirdiğinde veya telafi gerektiğinde hesabına anında token tanımlayın.
              </p>
            </div>
          </div>
          <button type="button" className="btn-primary" style={{ width: "100%" }} onClick={onGoToUsers}>
            Kullanıcı token yönetimine git
          </button>
        </div>

        <div className="admin-card">
          <div className="admin-card-header">
            <div className="admin-card-icon" aria-hidden="true">
              <Zap size={18} className="icon" />
            </div>
            <div>
              <h3>Kendi hesabıma token yükle</h3>
              <p className="admin-card-desc">Test veya acil işlemler için açık olan admin hesabınıza token tanımlayın.</p>
            </div>
          </div>
          <div className="admin-inline-form">
            <div>
              <label className="admin-label" htmlFor="fast-token-amount">Token miktarı</label>
              <input
                id="fast-token-amount"
                type="number"
                min="1"
                placeholder="Örn: 500"
                value={fastTokenAmount}
                onChange={(e) => onFastTokenAmountChange(e.target.value)}
              />
            </div>
            <div className="admin-form-actions">
              <button type="button" onClick={onAddTokensToMe} disabled={addingTokens || !fastTokenAmount}>
                {addingTokens ? "Yükleniyor..." : "Yükle"}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
