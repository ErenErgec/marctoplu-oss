/**
 * Tanıtım modu: backend kapalıyken (ör. Vercel'de yalnızca vitrin) `/` herkese açık tanıtım
 * sayfasını gösterir; giriş, panel, profil, admin ve ödeme sayfaları next.config.mjs'de `/`'e
 * yönlendirilir. Build sırasında okunur, değiştirince yeniden build gerekir.
 */
export const PROMO_MODE = process.env.NEXT_PUBLIC_PROMO_MODE === "1";
