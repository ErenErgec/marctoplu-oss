import type { TokenPackage } from "../../lib/types";
import type { AuthUserInfo } from "../../lib/useAuth";
import { Lightbulb, Smartphone } from "lucide-react";

export function MobilePurchaseModal({
  pkg,
  userInfo,
  onClose,
}: {
  pkg: TokenPackage;
  userInfo: AuthUserInfo;
  onClose: () => void;
}) {
  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        backgroundColor: "var(--overlay)",
        backdropFilter: "blur(6px)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 9999,
        padding: 20,
      }}
      onClick={onClose}
    >
      <div
        className="modal-card"
        onClick={(e) => e.stopPropagation()}
      >
        <div style={{ display: "flex", alignItems: "center", gap: 12, marginBottom: 16 }}>
          <div
            style={{
              width: 48,
              height: 48,
              borderRadius: 16,
              background: "var(--primary-light)",
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              color: "var(--primary)",
            }}
            aria-hidden="true"
          >
            <Smartphone size={22} className="icon" />
          </div>
          <div>
            <h3 style={{ margin: 0, fontSize: 18, fontWeight: 800, color: "var(--text-main)" }}>
              Token Satın Alımı Mobilde
            </h3>
            <p style={{ margin: "2px 0 0", fontSize: 13, color: "var(--primary)", fontWeight: 700 }}>
              {pkg.name} ({pkg.tokenAmount} Token - ₺{pkg.priceTry})
            </p>
          </div>
        </div>

        <div
          style={{
            padding: "14px 16px",
            borderRadius: 14,
            background: "var(--primary-surface)",
            border: "1px solid var(--primary-border)",
            marginBottom: 20,
            fontSize: 13,
            lineHeight: 1.5,
            color: "var(--text-main)",
          }}
        >
          <strong style={{ color: "var(--primary)" }}>Web üzerinden kartla ödeme yakında!</strong>
          <div style={{ marginTop: 4, color: "var(--text-muted)" }}>
            Web sitemiz için ödeme altyapımız entegrasyon aşamasındadır. Paket alımlarınızı şu anda <strong>MARCTOPLU Mobil Uygulaması (Google Play & App Store)</strong> üzerinden In-App Purchase ile güvenle gerçekleştirebilirsiniz.
          </div>
        </div>

        <div style={{ marginBottom: 24 }}>
          <div
            style={{
              fontSize: 12,
              fontWeight: 700,
              color: "var(--text-muted)",
              textTransform: "uppercase",
              letterSpacing: "0.05em",
              marginBottom: 10,
            }}
          >
            Nasıl Satın Alırım?
          </div>
          <ul
            style={{
              paddingLeft: 20,
              margin: 0,
              fontSize: 13,
              color: "var(--text-muted)",
              display: "flex",
              flexDirection: "column",
              gap: 8,
              lineHeight: 1.4,
            }}
          >
            <li>Android veya iOS (iPhone/iPad) cihazınızdan <strong>MARCTOPLU</strong> uygulamasını açın.</li>
            <li>
              {userInfo.isAnonymous ? (
                <>Kullanıcı hesabınızla (e-posta adresinizle) giriş yapın veya yeni hesap oluşturun.</>
              ) : (
                <>Kayıtlı e-posta adresinizle (<strong>{userInfo.email}</strong>) giriş yapın.</>
              )}
            </li>
            <li>
              <strong>Token Paketleri</strong> bölümünden paketinizi Google Play veya App Store güvencesiyle tek tıkla satın alın.
            </li>
            <li>
              Satın aldığınız token&apos;lar anında hem mobilde hem de bu web panelinizde kullanılabilir olacaktır.
            </li>
          </ul>

          {userInfo.isAnonymous && (
            <div
              style={{
                marginTop: 14,
                padding: "12px 14px",
                borderRadius: 12,
                background: "var(--primary-surface)",
                border: "1px solid var(--primary-border)",
                fontSize: 12,
                color: "var(--primary-deep)",
                lineHeight: 1.4,
              }}
            >
              <Lightbulb size={14} className="icon" aria-hidden="true" /> <strong>İpucu:</strong> Mevcut deneme haklarınızı ve sorgularınızı kaybetmemek için <strong>Profil</strong> sekmesinden hesabınızı e-posta ile kalıcı yapabilirsiniz.
            </div>
          )}
        </div>

        <div>
          <button
            type="button"
            className="btn-glass"
            style={{ width: "100%", height: 44, justifyContent: "center" }}
            onClick={onClose}
          >
            Kapat
          </button>
        </div>
      </div>
    </div>
  );
}
