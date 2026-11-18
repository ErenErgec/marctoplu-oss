"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { FormEvent, useEffect, useMemo, useState } from "react";
import { auth } from "../../../lib/firebaseClient";
import { GooglePlayButton } from "../../../components/GooglePlayButton";
import { AppStoreButton } from "../../../components/AppStoreButton";
import { API_BASE } from "../../../lib/api";
import type { TokenPackage } from "../../../lib/types";

type BuyerForm = {
  name: string;
  surname: string;
  identityNumber: string;
  gsmNumber: string;
  city: string;
  country: string;
  address: string;
  zipCode: string;
};

export default function CheckoutPage() {
  const router = useRouter();
  const params = useParams<{ packageId: string }>();
  const packageId = params.packageId;

  const [ready, setReady] = useState(false);
  const [token, setToken] = useState("");
  const [packages, setPackages] = useState<TokenPackage[]>([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [buyer, setBuyer] = useState<BuyerForm>({
    name: "",
    surname: "",
    identityNumber: "",
    gsmNumber: "",
    city: "",
    country: "Turkey",
    address: "",
    zipCode: "",
  });

  const headers = useMemo(() => ({ Authorization: `Bearer ${token}` }), [token]);
  const selectedPackage = packages.find((x) => x.id === packageId) ?? null;

  useEffect(() => {
    async function bootstrapAuth() {
      const saved = window.localStorage.getItem("marc_token");
      if (saved) {
        setToken(saved);
        setReady(true);
        return;
      }
      if (!auth.currentUser) {
        router.replace("/login");
        return;
      }
      
      const fromSession = await auth.currentUser.getIdToken(true);
      window.localStorage.setItem("marc_token", fromSession);
      setToken(fromSession);
      setReady(true);
    }

    bootstrapAuth();
  }, [router]);

  useEffect(() => {
    async function loadPackages() {
      const res = await fetch(`${API_BASE}/api/token-packages`);
      if (!res.ok) throw new Error("Token paketleri alınamadı.");
      setPackages(await res.json());
    }

    loadPackages().catch((err) => setMessage(err.message));
  }, []);

  async function fillFromProfile() {
    try {
      setLoading(true);
      setMessage("");
      const res = await fetch(`${API_BASE}/api/me/profile`, { headers });
      if (!res.ok) throw new Error("Profil yüklenemedi. Önce profili kaydettiğinizden emin olun.");
      const profile = await res.json();
      setBuyer({
        name: profile.name || "",
        surname: profile.surname || "",
        identityNumber: profile.identityNumber || "",
        gsmNumber: profile.gsmNumber || "",
        city: profile.city || "",
        country: profile.country || "Turkey",
        address: profile.address || "",
        zipCode: profile.zipCode || "",
      });
      setMessage("Bilgiler profilden dolduruldu.");
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Profil yüklenemedi.");
    } finally {
      setLoading(false);
    }
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!packageId) {
      setMessage("Önce bir token paketi seçmelisiniz.");
      return;
    }

    if (!buyer.name || !buyer.surname || !buyer.identityNumber || !buyer.gsmNumber || !buyer.city || !buyer.country || !buyer.address || !buyer.zipCode) {
      setMessage("Ödeme için alıcı/fatura bilgileri zorunludur.");
      return;
    }

    try {
      setLoading(true);
      setMessage("");
      const res = await fetch(`${API_BASE}/api/payments/iyzico/checkout/init`, {
        method: "POST",
        headers: { ...headers, "Content-Type": "application/json" },
        body: JSON.stringify({ packageId, buyer }),
      });
      if (!res.ok) {
        throw new Error("Ödeme başlatılamadı.");
      }
      const data = await res.json();
      window.open(data.checkoutUrl, "_blank", "noopener,noreferrer");
      setMessage("Ödeme sayfası yeni sekmede açıldı.");
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Ödeme başlatılamadı.");
    } finally {
      setLoading(false);
    }
  }

  if (!ready) return null;

  return (
    <div className="app-container">
      {/* Floating Glass Navbar */}
      <header className="navbar">
        <div className="nav-brand">
          <div className="brand-icon">
            <img src="/logo.png" alt="Logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} />
          </div>
          <span>MARCTOPLU</span>
        </div>

        <nav className="nav-menu">
          <Link href="/" className="nav-link">Panel</Link>
          <Link href="/profile" className="nav-link">Profil</Link>
        </nav>

        <Link href="/" className="btn-glass">
          ← Panele Dön
        </Link>
      </header>

      <section style={{ maxWidth: 640, margin: "20px auto", padding: "0" }}>
        <div className="glass-panel checkout-card">
          <div style={{
            width: 64,
            height: 64,
            borderRadius: 20,
            background: "#FFEDD5",
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            fontSize: 32,
            margin: "0 auto 20px",
          }}>
            📱
          </div>

          <h1 style={{ fontSize: 24, fontWeight: 800, marginBottom: 8, color: "var(--text-main)" }}>
            Token Satın Alımı Mobil Uygulamada
          </h1>

          <p style={{ fontSize: 14, color: "var(--text-muted)", marginBottom: 24, lineHeight: 1.6 }}>
            Web üzerinden ödeme altyapımız entegrasyon aşamasındadır.
            Şu anda token alımlarınızı <strong>MARCTOPLU Mobil (Google Play & App Store)</strong> uygulamamız üzerinden In-App Purchase ile güvenle gerçekleştirebilirsiniz.
          </p>

          {selectedPackage && (
            <div style={{
              padding: "18px 22px",
              borderRadius: 18,
              background: "#FFF7ED",
              border: "1px solid #FED7AA",
              marginBottom: 24,
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center"
            }}>
              <div style={{ textAlign: "left" }}>
                <div style={{ fontWeight: 800, fontSize: 17, color: "var(--text-main)" }}>{selectedPackage.name}</div>
                <div style={{ fontSize: 13, color: "var(--text-muted)", fontWeight: 600 }}>{selectedPackage.tokenAmount} MARC Token</div>
              </div>
              <div style={{ fontWeight: 800, fontSize: 24, color: "#C2410C" }}>
                ₺{selectedPackage.priceTry.toLocaleString("tr-TR")}
              </div>
            </div>
          )}

          <div style={{
            textAlign: "left",
            padding: "18px 22px",
            borderRadius: 16,
            background: "#F8FAFC",
            border: "1px solid var(--border-light)",
            marginBottom: 28,
            fontSize: 13,
            color: "var(--text-muted)",
            lineHeight: 1.6
          }}>
            <strong style={{ color: "var(--text-main)", display: "block", marginBottom: 8 }}>Nasıl Satın Alırım?</strong>
            1. Android veya iOS (iPhone/iPad) cihazınızdan <strong>MARCTOPLU</strong> uygulamasını açın.<br />
            2. Bu web panelindeki hesabınızla giriş yapın.<br />
            3. <strong>Token Paketleri</strong> ekranından paketinizi Google Play veya App Store ile tek tıkla satın alın.<br />
            4. Satın aldığınız token&apos;lar bu web panelinize anında yansıyacaktır.
          </div>

          <div style={{ display: "flex", flexDirection: "column", gap: 14, alignItems: "center" }}>
            <div style={{ display: "flex", gap: 12, flexWrap: "wrap", justifyContent: "center" }}>
              <GooglePlayButton size="md" />
              <AppStoreButton size="md" />
            </div>
            <Link href="/" className="btn-glass" style={{ padding: "10px 24px", textDecoration: "none", fontSize: 14 }}>
              Panele Geri Dön
            </Link>
          </div>
        </div>
      </section>
    </div>
  );
}
