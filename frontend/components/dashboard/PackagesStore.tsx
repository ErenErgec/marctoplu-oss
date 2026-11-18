import { Smartphone } from "lucide-react";
import type { TokenPackage } from "../../lib/types";
import { GooglePlayButton } from "../GooglePlayButton";
import { AppStoreButton } from "../AppStoreButton";

export function PackagesStore({
  packages,
  onSelectPackage,
}: {
  packages: TokenPackage[];
  onSelectPackage: (pkg: TokenPackage) => void;
}) {
  return (
    <div id="packages" className="glass-panel">
      <div className="panel-header">
        <div>
          <h2 className="panel-title">Token paketleri</h2>
          <p style={{ fontSize: 13, color: "var(--text-muted)", marginTop: 4 }}>
            Kullanım miktarınıza uygun token paketini seçin.
          </p>
        </div>
      </div>

      {/* Bilgilendirme Kutusu */}
      <div style={{
        padding: "18px 20px",
        borderRadius: 16,
        background: "var(--primary-surface)",
        border: "1px solid var(--primary-border)",
        marginBottom: 24,
        display: "flex",
        flexWrap: "wrap",
        alignItems: "center",
        justifyContent: "space-between",
        gap: 16,
      }}>
        <div style={{ display: "flex", alignItems: "flex-start", gap: 14, flex: "1 1 320px" }}>
          <Smartphone size={26} className="icon" aria-hidden="true" style={{ color: "var(--primary)", marginTop: 2 }} />
          <div>
            <div style={{ fontSize: 14, fontWeight: 700, color: "var(--primary)", marginBottom: 4 }}>
              Token Paket Satın Alımları Google Play ve App Store Üzerinden Yapılmaktadır
            </div>
            <div style={{ fontSize: 13, color: "var(--text-muted)", lineHeight: 1.5 }}>
              Web sitemiz üzerinden ödeme altyapımız yakında eklenecektir. Şu anda paket alımlarınızı <strong>MARCTOPLU Mobil (Google Play & App Store)</strong>  üzerinden tek tıkla güvenle yapabilirsiniz. Mobilden aldığınız tüm token&apos;lar bu web panelinizde de anında geçerlidir.
            </div>
          </div>
        </div>
        <div style={{ display: "flex", gap: 10, flexWrap: "wrap", alignItems: "center" }}>
          <GooglePlayButton size="sm" />
          <AppStoreButton size="sm" />
        </div>
      </div>

      <div className="packages-grid">
        {packages.length === 0 ? (
          <div style={{ color: "var(--text-muted)", fontSize: 14 }}>Paketler yükleniyor...</div>
        ) : (
          packages.map((pkg) => {
            const isPopular = pkg.tokenAmount === 500;
            return (
              <div key={pkg.id} className={`package-card ${isPopular ? "popular" : ""}`}>
                {isPopular && <div className="popular-badge">EN ÇOK TERCİH EDİLEN</div>}
                <div>
                  <h3 style={{ fontSize: 20, marginBottom: 8 }}>{pkg.name}</h3>
                  <p style={{ fontSize: 13, color: "var(--text-muted)", marginBottom: 20 }}>
                    {pkg.tokenAmount.toLocaleString("tr-TR")} bulunan kayıt · Excel, MARC21 ve RIS çıktısı
                  </p>
                  <div style={{ fontFamily: "var(--font-heading)", fontSize: 32, fontWeight: 800, color: "var(--text-main)", marginBottom: 20 }}>
                    ₺{pkg.priceTry.toLocaleString("tr-TR")}
                  </div>
                </div>
                <button
                  className="btn-primary"
                  style={{ width: "100%", display: "flex", alignItems: "center", justifyContent: "center", gap: 8 }}
                  onClick={() => onSelectPackage(pkg)}
                >
                  <Smartphone size={16} className="icon" aria-hidden="true" />
                  <span>Mobilden Satın Al</span>
                </button>
              </div>
            );
          })
        )}
      </div>
    </div>
  );
}
