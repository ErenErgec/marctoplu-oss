import Link from "next/link";
import {
  ArrowRight,
  Check,
  Coins,
  Download,
  FileCode2,
  FileSpreadsheet,
  FileText,
  Library,
  RotateCcw,
  ScanBarcode,
  Search,
  Upload,
} from "lucide-react";
import { AppStoreButton } from "../AppStoreButton";
import { GooglePlayButton } from "../GooglePlayButton";
import { ThemeToggle } from "../ThemeToggle";
import { SiteFooter } from "../dashboard/SiteFooter";

const STEPS = [
  {
    icon: Upload,
    title: "ISBN listenizi verin",
    text: "Excel dosyasını yükleyin ya da mobil uygulamada kitapların barkodunu kamerayla okutun.",
  },
  {
    icon: Search,
    title: "Künyeler hazırlanır",
    text: "Her ISBN'in bibliyografik bilgileri otomatik olarak bulunur ve standart kataloglama kaydına dönüştürülür.",
  },
  {
    icon: Download,
    title: "Çıktınızı indirin",
    text: "Bulunan kayıtlar kütüphane sisteminize aktarılmaya hazır dosyalar olarak gelir.",
  },
];

const FORMATS = [
  { icon: FileCode2, name: "MARC 21", ext: ".mrc", text: "Kütüphane otomasyon sistemlerine doğrudan aktarım için ikili MARC kaydı." },
  { icon: FileCode2, name: "MARCXML", ext: ".xml", text: "Aynı künyenin XML biçimi; dönüştürme ve arşivleme için." },
  { icon: FileSpreadsheet, name: "Excel", ext: ".xlsx", text: "Başlık, yazar, yayınevi ve yıl sütunlarıyla gözden geçirmesi kolay liste." },
  { icon: FileText, name: "RIS", ext: ".ris", text: "Zotero, EndNote ve Mendeley gibi kaynakça yöneticileri için." },
];

const PRICING_POINTS = [
  { icon: Coins, text: "Bulunan her kayıt 1 token" },
  { icon: RotateCcw, text: "Bulunamayan ISBN'lerin tokeni otomatik iade edilir" },
  { icon: ScanBarcode, text: "Kayıt olmadan 5 hediye token ile deneyin" },
];

const PREVIEW_ROWS = [
  { isbn: "978-975-07-1786-1", found: true },
  { isbn: "978-605-09-4720-3", found: true },
  { isbn: "978-0-14-118776-1", found: true },
  { isbn: "978-975-10-2961-5", found: false },
];

/** Backend kapalıyken `/` yerine gösterilen herkese açık tanıtım sayfası (bkz. lib/promo.ts). */
export function PromoLanding() {
  return (
    <div className="app-container promo">
      <header className="navbar promo-nav">
        <Link href="/" className="nav-brand" aria-label="MARCTOPLU ana sayfa">
          <span className="brand-icon">
            <img src="/logo.png" alt="" width={38} height={38} />
          </span>
          MARCTOPLU
        </Link>
        <nav className="promo-nav-links" aria-label="Sayfa bölümleri">
          <a href="#nasil-calisir">Nasıl çalışır</a>
          <a href="#ciktilar">Çıktılar</a>
          <a href="#ucretlendirme">Ücretlendirme</a>
        </nav>
        <div className="nav-actions">
          <ThemeToggle />
          <a href="#indir" className="btn-primary promo-nav-cta">
            Uygulamayı indir
          </a>
        </div>
      </header>

      <main>
        <section className="promo-hero">
          <div className="promo-hero-copy">
            <span className="promo-eyebrow">
              <Library size={14} className="icon" aria-hidden="true" /> Kütüphaneler için toplu kataloglama
            </span>
            <h1 className="promo-title">
              ISBN listenizi verin,
              <br />
              <span className="promo-title-accent">MARC 21 kayıtlarınızı</span> indirin.
            </h1>
            <p className="promo-lead">
              MARCTOPLU yüzlerce ISBN&apos;in künyesini tek seferde bulur ve MARC 21,
              MARCXML, Excel ve RIS olarak hazırlar. Kitap kitap kayıt girmeye son.
            </p>
            <div className="promo-stores">
              <GooglePlayButton size="lg" />
              <AppStoreButton size="lg" />
            </div>
            <p className="promo-note">Web paneli yakında yeniden hizmetinizde.</p>
          </div>

          <div className="promo-preview" aria-hidden="true">
            <div className="promo-preview-head">
              <FileSpreadsheet size={18} className="icon" />
              <span className="promo-preview-file">isbn-listesi.xlsx</span>
              <span className="promo-preview-count">4 ISBN</span>
            </div>
            <ul className="promo-preview-rows">
              {PREVIEW_ROWS.map((row) => (
                <li key={row.isbn}>
                  <span className="promo-preview-isbn">{row.isbn}</span>
                  <span className={row.found ? "status status-success" : "status status-failed"}>
                    {row.found ? "Bulundu" : "İade edildi"}
                  </span>
                </li>
              ))}
            </ul>
            <div className="promo-preview-formats">
              {FORMATS.map((f) => (
                <span key={f.name} className="promo-chip">
                  {f.name}
                </span>
              ))}
            </div>
          </div>
        </section>

        <section id="nasil-calisir" className="promo-section">
          <h2 className="promo-section-title">Nasıl çalışır</h2>
          <ol className="promo-steps">
            {STEPS.map((step, i) => (
              <li key={step.title} className="promo-card">
                <span className="promo-step-num">{i + 1}</span>
                <step.icon size={22} className="icon promo-card-icon" aria-hidden="true" />
                <h3>{step.title}</h3>
                <p>{step.text}</p>
              </li>
            ))}
          </ol>
          <a href="/ornek-isbn-listesi.xlsx" className="promo-link" download>
            Örnek ISBN listesini indirin <ArrowRight size={15} className="icon" aria-hidden="true" />
          </a>
        </section>

        <section id="ciktilar" className="promo-section">
          <h2 className="promo-section-title">Çıktı biçimleri</h2>
          <div className="promo-formats">
            {FORMATS.map((f) => (
              <div key={f.name} className="promo-card">
                <f.icon size={22} className="icon promo-card-icon" aria-hidden="true" />
                <h3>
                  {f.name} <span className="promo-ext">{f.ext}</span>
                </h3>
                <p>{f.text}</p>
              </div>
            ))}
          </div>
        </section>

        <section id="ucretlendirme" className="promo-section promo-pricing">
          <div>
            <h2 className="promo-section-title">Yalnızca bulunan kayıt için ödeyin</h2>
            <p className="promo-lead">
              Token paketleri uygulama içinden alınır. Bulunamayan ISBN&apos;ler ve çıktısı oluşmayan işler
              bakiyenizden düşmez.
            </p>
          </div>
          <ul className="promo-checks">
            {PRICING_POINTS.map((p) => (
              <li key={p.text}>
                <span className="promo-check">
                  <Check size={14} aria-hidden="true" />
                </span>
                <p.icon size={18} className="icon" aria-hidden="true" />
                {p.text}
              </li>
            ))}
          </ul>
        </section>

        <section id="indir" className="promo-cta">
          <h2>Barkodu okutun, künyeniz hazır.</h2>
          <p>Mobil uygulamayı indirin, kayıt olmadan 5 hediye token ile hemen deneyin.</p>
          <div className="promo-stores">
            <GooglePlayButton size="lg" />
            <AppStoreButton size="lg" />
          </div>
        </section>
      </main>

      <SiteFooter />
    </div>
  );
}
