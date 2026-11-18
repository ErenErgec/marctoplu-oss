import type { Metadata, Viewport } from "next";
import { Inter, Outfit } from "next/font/google";
import "./globals.css";
import { MobileAppGate } from "../components/MobileAppGate";
import { THEME_INIT_SCRIPT } from "../lib/theme";

// Fontlar build sırasında indirilip kendi sunucumuzdan servis edilir (render-blocking @import yok)
const inter = Inter({ subsets: ["latin", "latin-ext"], variable: "--font-inter", display: "swap" });
const outfit = Outfit({ subsets: ["latin", "latin-ext"], variable: "--font-outfit", display: "swap" });

export const metadata: Metadata = {
  // Paylaşım önizlemesinde (og:image) mutlak URL üretmek için; görsel app/opengraph-image.png
  metadataBase: new URL("https://marctoplu.com"),
  title: "MARCTOPLU | Toplu ISBN & Kataloglama Altyapısı",
  description: "Bulunan kayıt başına token ile toplu ISBN sorgulama ve MARC21 kataloglama platformu",
  openGraph: {
    type: "website",
    siteName: "MARCTOPLU",
    locale: "tr_TR",
    url: "/",
    title: "MARCTOPLU | Toplu ISBN & Kataloglama Altyapısı",
    description: "ISBN listenizi yükleyin; MARC 21, MARCXML, Excel ve RIS çıktılarınızı indirin.",
  },
  twitter: { card: "summary_large_image" },
  icons: {
    icon: [
      { url: "/favicon.ico", sizes: "any" },
      { url: "/icon.png", type: "image/png", sizes: "192x192" },
      { url: "/favicon-32x32.png", type: "image/png", sizes: "32x32" },
    ],
    apple: [
      { url: "/apple-icon.png", sizes: "180x180", type: "image/png" },
    ],
  },
};

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#F8FAFC" },
    { media: "(prefers-color-scheme: dark)", color: "#0B1120" },
  ],
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    // data-theme, React yüklenmeden THEME_INIT_SCRIPT tarafından yazıldığı için hidrasyon uyarısı bastırılır
    <html lang="tr" className={`${inter.variable} ${outfit.variable}`} data-scroll-behavior="smooth" suppressHydrationWarning>
      <head>
        {/* HTML ayrıştırılırken senkron çalışır; elle seçilen tema ilk çizimden önce uygulanır (flash yok).
            next/script beforeInteractive JS paketleri yüklendikten sonra çalıştığı için yetmiyor.
            React 19 geliştirme modunda "Encountered a script tag" uyarısı verir; production'da etkisi yoktur. */}
        <script dangerouslySetInnerHTML={{ __html: THEME_INIT_SCRIPT }} />
      </head>
      <body>
        <MobileAppGate />
        {children}
      </body>
    </html>
  );
}
