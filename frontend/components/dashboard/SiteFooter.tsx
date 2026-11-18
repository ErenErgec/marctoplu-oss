import Link from "next/link";

export function SiteFooter() {
  return (
    <footer style={{
      marginTop: 48,
      paddingTop: 24,
      paddingBottom: 32,
      borderTop: "1px solid var(--border-light)",
      display: "flex",
      flexWrap: "wrap",
      alignItems: "center",
      justifyContent: "space-between",
      gap: 16,
      fontSize: 13,
      color: "var(--text-secondary)",
    }}>
      <div>
        © 2026 MARCTOPLU. Tüm hakları saklıdır.
      </div>
      <div style={{ display: "flex", gap: 20, flexWrap: "wrap" }}>
        <Link href="/gizlilik" style={{ color: "var(--text-muted)", textDecoration: "none" }}>
          Gizlilik Politikası (KVKK)
        </Link>
        <Link href="/kullanim-kosullari" style={{ color: "var(--text-muted)", textDecoration: "none" }}>
          Kullanım Koşulları
        </Link>
        <Link href="/hesap-silme" style={{ color: "var(--text-muted)", textDecoration: "none" }}>
          Hesap ve Veri Silme
        </Link>
        <a href="mailto:info@marctoplu.com" style={{ color: "var(--text-muted)", textDecoration: "none" }}>
          info@marctoplu.com
        </a>
      </div>
    </footer>
  );
}
