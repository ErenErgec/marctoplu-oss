// Tanıtım modu (lib/promo.ts ile aynı bayrak): backend gerektiren sayfalar ana sayfaya yönlenir
const PROMO_MODE = process.env.NEXT_PUBLIC_PROMO_MODE === "1";
const PROMO_REDIRECTED_PATHS = ["/login", "/register", "/profile", "/admin", "/checkout/:path*"];

/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Docker imajı için; Vercel kendi çıktı biçimini kullanır (VERCEL=1 build sırasında tanımlı)
  output: process.env.VERCEL ? undefined : "standalone",
  async rewrites() {
    return [
      { source: "/privacy", destination: "/gizlilik" },
      { source: "/terms", destination: "/kullanim-kosullari" },
      { source: "/delete-account", destination: "/hesap-silme" },
    ];
  },
  async redirects() {
    if (!PROMO_MODE) return [];
    return PROMO_REDIRECTED_PATHS.map((source) => ({ source, destination: "/", permanent: false }));
  },
};

export default nextConfig;
