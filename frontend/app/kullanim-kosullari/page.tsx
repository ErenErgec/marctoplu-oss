import Link from "next/link";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Kullanım Koşulları | MARCTOPLU",
  description: "MARCTOPLU web ve mobil uygulaması kullanım koşulları, token sistemi ve hizmet şartları.",
};

export default function TermsPage() {
  return (
    <div className="app-container" style={{ maxWidth: 900, margin: "0 auto", padding: "40px 20px" }}>
      {/* Header */}
      <header className="navbar" style={{ marginBottom: 32 }}>
        <div className="nav-brand">
          <div className="brand-icon">
            <img src="/logo.png" alt="Logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
          </div>
          <span>MARCTOPLU</span>
        </div>

        <nav className="nav-menu">
          <Link href="/" className="nav-link">Ana Sayfa</Link>
          <Link href="/gizlilik" className="nav-link">Gizlilik Politikası</Link>
          <Link href="/hesap-silme" className="nav-link">Hesap Silme</Link>
        </nav>

        <Link href="/" className="btn-glass">
          ← Ana Sayfaya Dön
        </Link>
      </header>

      {/* Content */}
      <article className="glass-panel" style={{ padding: "36px 32px", lineHeight: 1.7 }}>
        <div style={{ borderBottom: "1px solid var(--border-light)", paddingBottom: 20, marginBottom: 28 }}>
          <div style={{ display: "inline-block", padding: "4px 12px", borderRadius: 20, background: "var(--primary-light)", color: "var(--primary-deep)", fontSize: 12, fontWeight: 700, marginBottom: 12 }}>
            HİZMET SÖZLEŞMESİ
          </div>
          <h1 style={{ fontSize: 28, fontWeight: 800, color: "var(--text-main)", marginBottom: 8 }}>
            Kullanım Koşulları ve Hizmet Şartları
          </h1>
          <p style={{ fontSize: 13, color: "var(--text-secondary)" }}>
            Son Güncelleme: 7 Eylül 2026 | Sürüm: 1.1
          </p>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 24, fontSize: 14.5, color: "var(--text-muted)" }}>
          <section>
            <p>
              Bu Kullanım Koşulları (&quot;<strong>Koşullar</strong>&quot;), MARCTOPLU web platformu ve mobil uygulamasına (&quot;<strong>Hizmet</strong>&quot;) erişiminizi ve kullanımınızı düzenler. Hizmeti kullanarak bu Koşulları kabul etmiş sayılırsınız.
            </p>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              1. Hizmet Tanımı ve MARC Token Modeli
            </h2>
            <p style={{ marginBottom: 10 }}>
              MARCTOPLU, kütüphaneciler, araştırmacılar ve yayıncılar için ISBN numaralarından uluslararası standartlarda MARC21 ve bibliyografik katalog kayıtları üreten otomatik bir sorgulama sistemidir.
            </p>
            <ul style={{ paddingLeft: 24, display: "flex", flexDirection: "column", gap: 6 }}>
              <li>Kaydı bulunan her ISBN için kullanıcının cüzdanından 1 MARC Token düşülür. Hiçbir kaynakta bulunamayan veya kaynaklara ulaşılamadığı için sorgulanamayan ISBN&apos;ler ücretlendirilmez.</li>
              <li>Toplu kataloglama işlerinde başlangıçta listedeki ISBN sayısı kadar token bloke edilir; iş bitiminde bulunamayan ISBN&apos;lerin token&apos;ları, çıktı dosyası oluşmadan iptal edilen veya başarısız olan işlerde ise bloke edilen token&apos;ların tamamı cüzdana iade edilir.</li>
              <li>Satın alınan token&apos;lar dijital tüketim hakkı olup nakde çevrilemez ve başka kullanıcılara devredilemez.</li>
            </ul>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              2. Hesap Güvenliği ve Kullanıcı Sorumlulukları
            </h2>
            <p>
              Kullanıcılar, hesap bilgilerini ve şifrelerini güvende tutmakla yükümlüdür. Misafir oturumlarında cihaz sıfırlanması veya veri temizlenmesi halinde bakiyenin kaybolmaması için hesabın bir e-posta adresiyle kalıcı hale getirilmesi kullanıcının sorumluluğundadır.
            </p>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              3. Fikri Mülkiyet ve Kullanım Hakları
            </h2>
            <p>
              Sistem tarafından üretilen MARC21 çıktıları, Excel tabloları ve bibliyografik kayıtlar kullanıcının kütüphane otomasyon sistemlerine (Koha, Yordam vb.) yüklenmek üzere serbestçe kullanılabilir. Yazılımın kaynak kodlarının kopyalanması, tersine mühendislik yapılması veya sunucu altyapısına yetkisiz yük bindirilmesi yasaktır.
            </p>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              4. İletişim
            </h2>
            <p>
              Koşullarla ilgili tüm sorularınız için: <a href="mailto:info@marctoplu.com">info@marctoplu.com</a>
            </p>
          </section>
        </div>
      </article>
    </div>
  );
}
