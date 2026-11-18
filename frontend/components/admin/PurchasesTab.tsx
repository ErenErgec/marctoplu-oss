import { Check, Coins, Copy, Receipt, RotateCcw, Search, ShoppingBag, UserRound } from "lucide-react";
import { formatDate } from "../../lib/format";
import { RefreshButton } from "./RefreshButton";
import { StatCard } from "./StatCard";
import type { IapRevenue, Purchase, PurchaseFilters, PurchaseList } from "./types";

const ICON_PROPS = { size: 20, className: "icon", "aria-hidden": true } as const;

const PLATFORM_FILTERS: { value: PurchaseFilters["platform"]; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "android", label: "Google Play" },
  { value: "ios", label: "App Store" },
];

const ENVIRONMENT_FILTERS: { value: PurchaseFilters["environment"]; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "production", label: "Gerçek" },
  { value: "test", label: "Test" },
];

const STATUS_FILTERS: { value: PurchaseFilters["status"]; label: string }[] = [
  { value: "all", label: "Tümü" },
  { value: "active", label: "Geçerli" },
  { value: "refunded", label: "İade edilen" },
];

const PERIOD_FILTERS: { value: number | null; label: string }[] = [
  { value: 7, label: "7 gün" },
  { value: 30, label: "30 gün" },
  { value: 90, label: "90 gün" },
  { value: null, label: "Tümü" },
];

const ENVIRONMENT_LABELS: Record<string, string> = {
  Production: "Gerçek",
  Sandbox: "Sandbox",
  Test: "Lisanslı test",
  Promo: "Promosyon",
  Rewarded: "Ödüllü",
};

export const PURCHASE_PAGE_SIZE = 100;
export const PURCHASE_MAX_LIMIT = 500;

function formatMoney(amount: number, currency: string) {
  try {
    return amount.toLocaleString("tr-TR", { style: "currency", currency });
  } catch {
    return `${amount.toLocaleString("tr-TR", { minimumFractionDigits: 2 })} ${currency}`;
  }
}

function revenueText(revenue: IapRevenue[]) {
  if (revenue.length === 0) return "-";
  return revenue.map((r) => formatMoney(r.amount, r.currency)).join(" + ");
}

function EnvironmentBadge({ environment }: { environment: string | null }) {
  if (!environment) {
    return <span className="admin-badge badge-inactive" title="Ortam bilgisi eklenmeden önceki kayıt">Bilinmiyor</span>;
  }
  return (
    <span className={environment === "Production" ? "status status-success" : "status status-queued"}>
      {ENVIRONMENT_LABELS[environment] ?? environment}
    </span>
  );
}

function StatusBadge({ purchase }: { purchase: Purchase }) {
  if (!purchase.refundedAt) {
    return <span className="status status-success">Geçerli</span>;
  }
  return (
    <>
      <span className="status status-failed">İade edildi</span>
      <div className="cell-sub">{formatDate(purchase.refundedAt)}</div>
    </>
  );
}

function PriceCell({ purchase }: { purchase: Purchase }) {
  if (purchase.price == null || !purchase.currency) {
    return <span className="cell-sub" title="Bu satın alma için fiyat kaydı yok">-</span>;
  }
  const details = [
    purchase.tax != null ? `Vergi ${formatMoney(purchase.tax, purchase.currency)}` : null,
    purchase.proceeds != null ? `Net ${formatMoney(purchase.proceeds, purchase.currency)}` : null,
  ].filter(Boolean);
  return (
    <>
      <strong className={purchase.refundedAt ? "is-refunded" : undefined}>{formatMoney(purchase.price, purchase.currency)}</strong>
      {details.length > 0 && <div className="cell-sub">{details.join(" · ")}</div>}
    </>
  );
}

export function PurchasesTab({
  data,
  loading,
  filters,
  searchDraft,
  copiedId,
  onFiltersChange,
  onSearchDraftChange,
  onLoadMore,
  onRefresh,
  onCopy,
  onOpenUser,
}: {
  data: PurchaseList | null;
  loading: boolean;
  filters: PurchaseFilters;
  searchDraft: string;
  copiedId: string | null;
  onFiltersChange: (filters: PurchaseFilters) => void;
  onSearchDraftChange: (value: string) => void;
  onLoadMore: () => void;
  onRefresh: () => void;
  onCopy: (text: string, id: string) => void;
  onOpenUser: (purchase: Purchase) => void;
}) {
  const items = data?.items ?? [];
  const canLoadMore = data != null && items.length < data.totalCount && items.length < PURCHASE_MAX_LIMIT;

  return (
    <div className="admin-section">
      <h2>Satın Almalar</h2>

      <div className="admin-card">
        <div className="admin-filter-grid">
          <div className="admin-chip-row" role="group" aria-label="Mağaza filtresi">
            <span className="admin-chip-label">Mağaza</span>
            {PLATFORM_FILTERS.map((f) => (
              <button
                key={f.value}
                type="button"
                className={`admin-chip${filters.platform === f.value ? " is-active" : ""}`}
                aria-pressed={filters.platform === f.value}
                onClick={() => onFiltersChange({ ...filters, platform: f.value })}
              >
                {f.label}
              </button>
            ))}
          </div>
          <div className="admin-chip-row" role="group" aria-label="Ortam filtresi">
            <span className="admin-chip-label">Ortam</span>
            {ENVIRONMENT_FILTERS.map((f) => (
              <button
                key={f.value}
                type="button"
                className={`admin-chip${filters.environment === f.value ? " is-active" : ""}`}
                aria-pressed={filters.environment === f.value}
                onClick={() => onFiltersChange({ ...filters, environment: f.value })}
              >
                {f.label}
              </button>
            ))}
          </div>
          <div className="admin-chip-row" role="group" aria-label="Durum filtresi">
            <span className="admin-chip-label">Durum</span>
            {STATUS_FILTERS.map((f) => (
              <button
                key={f.value}
                type="button"
                className={`admin-chip${filters.status === f.value ? " is-active" : ""}`}
                aria-pressed={filters.status === f.value}
                onClick={() => onFiltersChange({ ...filters, status: f.value })}
              >
                {f.label}
              </button>
            ))}
          </div>
          <div className="admin-chip-row" role="group" aria-label="Dönem filtresi">
            <span className="admin-chip-label">Dönem</span>
            {PERIOD_FILTERS.map((f) => (
              <button
                key={f.label}
                type="button"
                className={`admin-chip${filters.days === f.value ? " is-active" : ""}`}
                aria-pressed={filters.days === f.value}
                onClick={() => onFiltersChange({ ...filters, days: f.value })}
              >
                {f.label}
              </button>
            ))}
          </div>
          <form
            className="admin-search"
            role="search"
            onSubmit={(e) => {
              e.preventDefault();
              onFiltersChange({ ...filters, search: searchDraft.trim() });
            }}
          >
            <input
              type="search"
              aria-label="Satın alma ara"
              placeholder="E-posta, kullanıcı ID veya sipariş no..."
              value={searchDraft}
              onChange={(e) => onSearchDraftChange(e.target.value)}
            />
            <button type="submit" className="ghost btn-xs">
              <Search size={14} className="icon" aria-hidden="true" /> Ara
            </button>
            {filters.search && (
              <button
                type="button"
                className="ghost btn-xs"
                onClick={() => {
                  onSearchDraftChange("");
                  onFiltersChange({ ...filters, search: "" });
                }}
              >
                Sıfırla
              </button>
            )}
          </form>
        </div>
      </div>

      {data && (
        <div className="admin-stats-grid">
          <StatCard
            label="Satın Alma"
            value={data.totalCount.toLocaleString("tr-TR")}
            sub="Seçili filtrelere uyan"
            icon={<ShoppingBag {...ICON_PROPS} />}
          />
          <StatCard
            label="Satılan Token"
            value={data.totalTokens.toLocaleString("tr-TR")}
            icon={<Coins {...ICON_PROPS} />}
          />
          <StatCard
            label="Brüt Tutar"
            value={revenueText(data.revenue)}
            sub={
              filters.environment === "production"
                ? "İadeler hariç · vergi ve mağaza komisyonu düşülmedi"
                : "İadeler hariç · test satın almaları dahil olabilir; yalnızca gerçek ödemeler için Ortam: Gerçek seçin"
            }
            icon={<Receipt {...ICON_PROPS} />}
          />
          <StatCard
            label="İade"
            value={data.refundedCount.toLocaleString("tr-TR")}
            sub="Tokenleri kullanıcıdan düşüldü"
            icon={<RotateCcw {...ICON_PROPS} />}
          />
        </div>
      )}

      <div className="admin-card">
        <div className="table-wrap">
          {loading && !data ? (
            <p className="admin-empty">Satın almalar yükleniyor...</p>
          ) : items.length > 0 ? (
            <table>
              <thead>
                <tr>
                  <th>Tarih</th>
                  <th>Kullanıcı</th>
                  <th>Mağaza</th>
                  <th>Paket</th>
                  <th>Tutar</th>
                  <th>Durum</th>
                  <th>Ortam</th>
                  <th>Ülke</th>
                  <th>Sipariş / İşlem No</th>
                  <th><span className="sr-only">İşlem</span></th>
                </tr>
              </thead>
              <tbody>
                {items.map((p) => {
                  const reference = p.orderId ?? p.storeReference;
                  return (
                    <tr key={p.id}>
                      <td className="cell-sub cell-nowrap">{formatDate(p.createdAt)}</td>
                      <td>
                        {p.email ? (
                          <>
                            <a href={`mailto:${p.email}`}>{p.email}</a>
                            {p.name && <div className="cell-sub">{p.name}</div>}
                          </>
                        ) : (
                          <span className="mono-id" title={p.userId}>{p.userId.slice(0, 8)}…</span>
                        )}
                      </td>
                      <td className="cell-nowrap">
                        {p.platform === "android" ? "Google Play" : p.platform === "ios" ? "App Store" : p.platform}
                      </td>
                      <td className="cell-nowrap">
                        <strong>{p.tokens.toLocaleString("tr-TR")} token</strong>
                        {p.productId && <div className="cell-sub">{p.productId}</div>}
                      </td>
                      <td className="cell-nowrap"><PriceCell purchase={p} /></td>
                      <td className="cell-nowrap"><StatusBadge purchase={p} /></td>
                      <td><EnvironmentBadge environment={p.environment} /></td>
                      <td className="cell-sub">{p.region ?? "-"}</td>
                      <td>
                        <div className="admin-row-actions">
                          <span className="mono-id" title={reference}>
                            {reference.length > 24 ? `${reference.slice(0, 24)}…` : reference}
                          </span>
                          <button
                            type="button"
                            className="ghost btn-xs"
                            onClick={() => onCopy(reference, p.id)}
                            aria-label="Sipariş/işlem numarasını kopyala"
                            title="Kopyala"
                          >
                            {copiedId === p.id ? (
                              <Check size={12} className="icon" aria-hidden="true" />
                            ) : (
                              <Copy size={12} className="icon" aria-hidden="true" />
                            )}
                          </button>
                        </div>
                      </td>
                      <td>
                        <button
                          type="button"
                          className="ghost btn-xs"
                          onClick={() => onOpenUser(p)}
                          title="Kullanıcıyı Kullanıcılar & Token sekmesinde aç"
                        >
                          <UserRound size={12} className="icon" aria-hidden="true" /> Kullanıcı
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          ) : (
            <p className="admin-empty">Bu filtrelerde satın alma bulunmuyor.</p>
          )}
        </div>
        {data && items.length > 0 && (
          <div className="admin-table-footer">
            <span className="cell-sub">
              {data.totalCount.toLocaleString("tr-TR")} satın almanın {items.length.toLocaleString("tr-TR")} tanesi gösteriliyor
            </span>
            {canLoadMore && (
              <button type="button" className="ghost btn-xs" onClick={onLoadMore} disabled={loading}>
                {loading ? "Yükleniyor..." : "Daha fazla göster"}
              </button>
            )}
          </div>
        )}
      </div>
      <p className="admin-card-desc">
        İadeler mağaza bildirimleriyle otomatik işlenir: iade edilen satın almanın tokenleri kullanıcıdan düşülür
        (bakiye eksiye inebilir), iade geri alınırsa tokenler geri yüklenir.
      </p>
      <RefreshButton onClick={onRefresh} />
    </div>
  );
}
