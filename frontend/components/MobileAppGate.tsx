"use client";

import { useEffect, useState, useSyncExternalStore } from "react";
import { usePathname } from "next/navigation";
import { Coins, Monitor, ScanBarcode, Smartphone } from "lucide-react";
import { GooglePlayButton } from "./GooglePlayButton";
import { APP_STORE_URL, AppStoreButton } from "./AppStoreButton";
import { PROMO_MODE } from "../lib/promo";

const DISMISS_KEY = "marc_mobile_gate_dismissed";

// Mağaza incelemesi ve yönetim için web'de açık kalması gereken sayfalar.
// next.config.mjs'deki İngilizce takma adlar da (rewrite) listede olmalı: usePathname
// yeniden yazılmış hedefi değil, adres çubuğundaki yolu döndürür.
const EXCLUDED_PATHS = [
  "/gizlilik",
  "/privacy",
  "/kullanim-kosullari",
  "/terms",
  "/hesap-silme",
  "/delete-account",
  "/admin",
];

function isExcludedPath(pathname: string | null): boolean {
  if (!pathname) return false;
  // Tanıtım sayfası mağaza butonlarını zaten gösteriyor ve mobile uyumlu
  if (PROMO_MODE && pathname === "/") return true;
  return EXCLUDED_PATHS.some((path) => pathname === path || pathname.startsWith(`${path}/`));
}

type Platform = "ios" | "android" | "other";
type GateState = "hidden" | Platform;

function detectGateState(): GateState {
  try {
    if (window.sessionStorage.getItem(DISMISS_KEY) === "true") return "hidden";
  } catch {}

  const ua = navigator.userAgent;
  // iPadOS 13+ masaüstü Safari gibi görünür; dokunmatik noktadan ayırt edilir
  const isIPadOS = /Macintosh/i.test(ua) && navigator.maxTouchPoints > 1;
  // App Store'da yayında değilken iOS kullanıcısına önerilecek uygulama yok
  if (/iPhone|iPad|iPod/i.test(ua) || isIPadOS) return APP_STORE_URL ? "ios" : "hidden";
  if (/Android/i.test(ua)) return "android";
  if (/Mobi|BlackBerry|IEMobile|Opera Mini/i.test(ua)) return "other";
  return "hidden";
}

const noopSubscribe = () => () => {};

/** Mobil tarayıcıdan girenlere uygulamayı indirmeyi öneren tam ekran karşılama. */
export function MobileAppGate() {
  const pathname = usePathname();
  // Sunucuda ve hidrasyon sırasında "hidden"; istemcide gerçek değer okunur
  const detected = useSyncExternalStore(noopSubscribe, detectGateState, () => "hidden" as const);
  const [dismissed, setDismissed] = useState(false);

  const excluded = isExcludedPath(pathname);
  const visible = detected !== "hidden" && !dismissed && !excluded;

  // Karşılama açıkken arkadaki sayfa kaymasın
  useEffect(() => {
    if (!visible) return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.body.style.overflow = previous;
    };
  }, [visible]);

  function handleContinue() {
    setDismissed(true);
    try {
      window.sessionStorage.setItem(DISMISS_KEY, "true");
    } catch {}
  }

  if (!visible) return null;

  const platform = detected as Platform;

  return (
    <div className="mobile-gate" role="dialog" aria-modal="true" aria-labelledby="mobile-gate-title">
      <div className="mobile-gate-inner">
        <img src="/icon.png" alt="" className="mobile-gate-logo" />
        <div className="mobile-gate-brand">MARCTOPLU</div>
        <h1 id="mobile-gate-title" className="mobile-gate-title">
          Telefonda en iyi deneyim uygulamada
        </h1>
        <p className="mobile-gate-lead">
          ISBN&apos;leri tek tek yazmak yerine kitapların barkodunu okutun, künyeleri saniyeler içinde alın.
        </p>

        <ul className="mobile-gate-features">
          <li>
            <ScanBarcode size={20} className="icon" aria-hidden="true" />
            <span>Kamera ile hızlı ISBN barkod tarama</span>
          </li>
          <li>
            <Coins size={20} className="icon" aria-hidden="true" />
            <span>Kayıt olmadan 5 hediye token ile deneyin</span>
          </li>
          <li>
            <Smartphone size={20} className="icon" aria-hidden="true" />
            <span>Token paketlerini uygulama içinden güvenle alın</span>
          </li>
          <li>
            <Monitor size={20} className="icon" aria-hidden="true" />
            <span>Aynı hesapla web panelinden de devam edin</span>
          </li>
        </ul>

        <div className="mobile-gate-stores">
          {platform !== "ios" && <GooglePlayButton size="lg" />}
          {platform !== "android" && <AppStoreButton size="lg" />}
        </div>

        <button type="button" className="mobile-gate-continue" onClick={handleContinue}>
          Web sitesinde devam et
        </button>
      </div>
    </div>
  );
}
