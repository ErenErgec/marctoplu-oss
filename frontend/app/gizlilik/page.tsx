import Link from "next/link";
import { Metadata } from "next";

export const metadata: Metadata = {
  title: "Gizlilik Politikası | MARCTOPLU",
  description: "MARCTOPLU mobil ve web uygulaması gizlilik politikası, veri güvenliği ve KVKK aydınlatma metni.",
};

export default function PrivacyPage() {
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
          <Link href="/kullanim-kosullari" className="nav-link">Kullanım Koşulları</Link>
          <Link href="/hesap-silme" className="nav-link">Hesap Silme</Link>
        </nav>

        <Link href="/" className="btn-glass">
          ← Ana Sayfaya Dön
        </Link>
      </header>

      {/* Main Content */}
      <article className="glass-panel" style={{ padding: "36px 32px", lineHeight: 1.7 }}>
        <div style={{ borderBottom: "1px solid var(--border-light)", paddingBottom: 20, marginBottom: 28 }}>
          <div style={{ display: "inline-block", padding: "4px 12px", borderRadius: 20, background: "var(--primary-light)", color: "var(--primary-deep)", fontSize: 12, fontWeight: 700, marginBottom: 12 }}>
            YASAL BİLGİLENDİRME & KVKK
          </div>
          <h1 style={{ fontSize: 28, fontWeight: 800, color: "var(--text-main)", marginBottom: 8 }}>
            Gizlilik Politikası ve Kişisel Verilerin Korunması
          </h1>
          <p style={{ fontSize: 13, color: "var(--text-secondary)" }}>
            Son Güncelleme: 7 Eylül 2026 | Sürüm: 1.2
          </p>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: 24, fontSize: 14.5, color: "var(--text-muted)" }}>
          <section>
            <p>
              MARCTOPLU (&quot;<strong>biz</strong>&quot;, &quot;<strong>bizim</strong>&quot;, &quot;<strong>bize</strong>&quot;), MARCTOPLU mobil uygulaması ve web platformunu (&quot;<strong>Hizmet</strong>&quot;) işletmektedir. Bu Gizlilik Politikası, Hizmetimizi kullandığınızda kişisel verilerinizin nasıl toplandığını, işlendiğini, saklandığını ve korunduğunu açıklamak amacıyla hazırlanmıştır.
            </p>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              1. Toplanan Kişisel Veriler ve Amaçları
            </h2>
            <p style={{ marginBottom: 10 }}>
              Hizmetlerimizi sizlere sunabilmek ve geliştirebilmek amacıyla aşağıdaki verileri toplamaktayız:
            </p>
            <ul style={{ paddingLeft: 24, display: "flex", flexDirection: "column", gap: 6 }}>
              <li>
                <strong>Kimlik ve İletişim Bilgileri:</strong> Kayıt olurken veya hesabınızı kalıcı hale getirirken sağladığınız e-posta adresi, ad ve soyad bilgileri. (Kullanıcı doğrulaması ve hesap güvenliği için).
              </li>
              <li>
                <strong>Cihaz ve Kullanım Verileri:</strong> Cihaz modeli, işletim sistemi sürümü, anonim cihaz tanımlayıcıları (Device ID), IP adresi ve uygulama içi işlem logları. (Hizmet kararlılığı, kötüye kullanımın engellenmesi ve hata teşhisi için).
              </li>
              <li>
                <strong>Katalog ve Token İşlem Verileri:</strong> Sorgulanan ISBN numaraları, oluşturulan MARC işleri ve token harcama kayıtları.
              </li>
            </ul>
          </section>

          <section style={{ background: "var(--primary-surface)", border: "1px solid var(--primary-border)", padding: "18px 20px", borderRadius: 14 }}>
            <h3 style={{ fontSize: 16, fontWeight: 700, color: "var(--primary-deep)", marginBottom: 8, display: "flex", alignItems: "center", gap: 8 }}>
              <span>📷</span> 2. Kamera Erişimi ve İzinler
            </h3>
            <p style={{ color: "var(--primary-deep)", fontSize: 14 }}>
              Mobil uygulamamız, kitap barkodlarını ve ISBN kodlarını anında okuyabilmeniz için cihazınızın kamerasına erişim izni talep eder.
              <strong> Kamera akışı kesinlikle kaydedilmez, fotoğraflar depolanmaz veya üçüncü taraf sunuculara aktarılmaz.</strong>
              Tüm tarama işlemi tamamen cihazınız üzerinde yerel olarak gerçekleştirilir.
            </p>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              3. Üçüncü Taraf Altyapı ve Güvenlik Servisleri
            </h2>
            <p style={{ marginBottom: 10 }}>
              Hizmetimizin güvenli ve kesintisiz çalışması için endüstri standardı üçüncü taraf sağlayıcılarla çalışmaktayız:
            </p>
            <ul style={{ paddingLeft: 24, display: "flex", flexDirection: "column", gap: 6 }}>
              <li>
                <strong>Google Firebase Authentication:</strong> Güvenli oturum açma, şifreleme ve kullanıcı token doğrulama süreçleri.
              </li>
              <li>
                <strong>Firebase Cloud Messaging (FCM):</strong> Katalog işleme süreçleriniz bittiğinde cihazınıza anlık bildirim gönderebilmek için kullanılan cihaz bildirim token&apos;ları.
              </li>
              <li>
                <strong>Google Play Services / Apple App Store:</strong> Uygulama içi satın alımlar, çökme analizleri ve mağaza dağıtım gereksinimleri.
              </li>
            </ul>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              4. Veri Saklama ve Güvenlik
            </h2>
            <p>
              Tüm veri iletişimimiz SSL/TLS (HTTPS) protokolü ile uçtan uca şifrelenmektedir. Parolalarınız tek yönlü kriptografik özetleme algoritmalarıyla saklanmakta olup tarafımızca asla açık metin olarak görüntülenemez.
            </p>
          </section>

          <section style={{ background: "var(--bg-dark)", border: "1px solid var(--border-light)", padding: "20px 22px", borderRadius: 14 }}>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              5. Hesap Silme ve Veri İmha Hakları (KVKK / GDPR Uyumlu)
            </h2>
            <p style={{ marginBottom: 12 }}>
              Kullanıcılarımızın tüm verilerini istedikleri zaman tamamen silme hakkı bulunmaktadır:
            </p>
            <ol style={{ paddingLeft: 24, display: "flex", flexDirection: "column", gap: 8 }}>
              <li>
                <strong>Uygulama İçinden Silme:</strong> Mobil uygulamada <em>Profilim &gt; Hesabımı ve Verilerimi Sil</em> seçeneğine tıklayarak hesabınızı ve tüm kişisel verilerinizi anında silebilirsiniz.
              </li>
              <li>
                <strong>Web Üzerinden Silme:</strong> Uygulama cihazınızda yüklü değilse, <Link href="/hesap-silme" style={{ fontWeight: 600 }}>Hesap Silme Sayfamız</Link> üzerinden veya destek e-postamıza yazarak verilerinizin imhasını talep edebilirsiniz.
              </li>
            </ol>
            <p style={{ marginTop: 12, fontSize: 13, color: "var(--text-secondary)" }}>
              Hesap silme işlemiyle kimlik bilgileriniz, cüzdanınız ve Firebase oturumunuz anında silinir, geçmiş iş kayıtlarınızdaki kişisel bağlantılar anonimleştirilir.
            </p>
          </section>

          <section>
            <h2 style={{ fontSize: 18, fontWeight: 700, color: "var(--text-main)", marginBottom: 10 }}>
              6. İletişim ve Veri Sorumlusu
            </h2>
            <p>
              Gizlilik Politikamız veya kişisel verileriniz ile ilgili her türlü soru, talep veya KVKK kapsamındaki haklarınız için bizimle iletişime geçebilirsiniz:
            </p>
            <div style={{ marginTop: 12, padding: "14px 18px", borderRadius: 12, background: "var(--bg-card)", border: "1px solid var(--border-light)" }}>
              <p><strong>E-posta:</strong> <a href="mailto:info@marctoplu.com">info@marctoplu.com</a></p>
              <p><strong>Web Sitesi:</strong> <a href="https://marctoplu.com">https://marctoplu.com</a></p>
            </div>
          </section>
        </div>
      </article>
    </div>
  );
}
