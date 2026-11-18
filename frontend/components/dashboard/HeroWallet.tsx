import { Coins, Plus } from "lucide-react";

export function HeroWallet({
  balance,
  onScrollTo,
}: {
  balance: number | null;
  onScrollTo: (sectionId: string) => void;
}) {
  return (
    <section className="hero-wallet-card" aria-label="Token bakiyesi">
      <div className="wallet-summary">
        <div className="wallet-summary-icon" aria-hidden="true">
          <Coins size={26} className="icon" />
        </div>
        <div>
          <div className="balance-title">Kullanılabilir bakiye</div>
          <div className="balance-amount">
            {balance === null ? "…" : balance.toLocaleString("tr-TR")} <span>MARC Token</span>
          </div>
          {balance !== null && (
            <div className="balance-hint">
              Yaklaşık {balance.toLocaleString("tr-TR")} ISBN işleyebilirsiniz.
            </div>
          )}
        </div>
      </div>

      <button type="button" onClick={() => onScrollTo("packages")} className="btn-white">
        <Plus size={16} className="icon" aria-hidden="true" /> Token Yükle
      </button>
    </section>
  );
}
