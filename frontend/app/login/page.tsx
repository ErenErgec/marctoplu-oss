"use client";

import { Smartphone } from "lucide-react";
import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { signInWithEmailAndPassword, signInWithPopup, GoogleAuthProvider } from "firebase/auth";
import { auth } from "../../lib/firebaseClient";
import { GooglePlayButton } from "../../components/GooglePlayButton";
import { AppStoreButton } from "../../components/AppStoreButton";
import { API_BASE, getOrCreateDeviceId } from "../../lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [checking, setChecking] = useState(false);
  const [message, setMessage] = useState("");

  useEffect(() => {
    const saved = window.localStorage.getItem("marc_token");
    if (saved) router.replace("/");
  }, [router]);

  async function handleAuthSuccess(accessToken: string) {
    const deviceId = getOrCreateDeviceId();
    const verify = await fetch(`${API_BASE}/api/me/wallet`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "X-Device-Id": deviceId,
      },
    });
    if (!verify.ok) {
      throw new Error("Backend token doğrulaması başarısız.");
    }

    window.localStorage.setItem("marc_token", accessToken);
    router.replace("/");
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!email.trim() || !password.trim()) {
      setMessage("E-posta ve şifre zorunludur.");
      return;
    }

    try {
      setChecking(true);
      setMessage("");
      
      const userCredential = await signInWithEmailAndPassword(auth, email.trim(), password);
      const accessToken = await userCredential.user.getIdToken(true);
      await handleAuthSuccess(accessToken);
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Giriş başarısız. Bilgilerinizi kontrol edin.");
    } finally {
      setChecking(false);
    }
  }

  async function onGoogleSignIn() {
    try {
      setChecking(true);
      setMessage("");
      const provider = new GoogleAuthProvider();
      provider.setCustomParameters({ prompt: "select_account" });
      const userCredential = await signInWithPopup(auth, provider);
      const accessToken = await userCredential.user.getIdToken(true);
      await handleAuthSuccess(accessToken);
    } catch (err: unknown) {
      const code = (err as { code?: string })?.code;
      if (code === "auth/popup-closed-by-user" || code === "auth/cancelled-popup-request") {
        return;
      }
      setMessage(err instanceof Error ? err.message : "Google ile giriş başarısız.");
    } finally {
      setChecking(false);
    }
  }

  return (
    <div className="split-layout">
      <div className="split-info-pane">
        <div className="info-content">
          <div className="info-brand">
            <img src="/logo.png" alt="MARCTOPLU Logo" />
            <h1>MARCTOPLU</h1>
          </div>
          
          <div className="feature-list">
            <div className="feature-item">
              <h3>Toplu Kataloglama</h3>
              <p>Yüzlerce ISBN numarasını saniyeler içinde eksiksiz MARC formatına dönüştürün. Hata payını sıfıra indirin.</p>
            </div>
            
            <div className="feature-item">
              <h3>Kredi (Token) Sistemi</h3>
              <p>Yalnızca bulunan kayıtlar için ödeme yapın: bulunamayan ISBN'lerin ve çıktısı oluşmayan işlerin tokeni otomatik iade edilir.</p>
            </div>
            
            <div className="feature-item">
              <h3>Hızlı ve Güvenilir</h3>
              <p>Modern altyapımız sayesinde kesintisiz veri erişimi ve saniyeler süren toplu indirme imkanı.</p>
            </div>
          </div>
        </div>
      </div>

      <div className="split-auth-pane">
        <div className="split-auth-content">
          <div style={{ textAlign: "center", marginBottom: 32 }}>
            <h2 style={{ fontSize: 24, fontWeight: 800, color: "var(--text-main)", letterSpacing: "-0.02em" }}>Hoş Geldiniz</h2>
            <p style={{ color: "var(--text-muted)", fontSize: 14, marginTop: 6 }}>
              Sisteme erişmek için bilgilerinizi giriniz.
            </p>
          </div>

          <button
            type="button"
            onClick={onGoogleSignIn}
            disabled={checking}
            style={{
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              gap: 12,
              width: "100%",
              height: 52,
              borderRadius: 14,
              border: "1.5px solid var(--border-medium)",
              background: "var(--bg-card)",
              color: "var(--text-main)",
              fontSize: 15,
              fontWeight: 600,
              cursor: checking ? "not-allowed" : "pointer",
              transition: "all 0.2s ease",
              boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
              marginBottom: 12,
            }}
          >
            <svg width="22" height="22" viewBox="0 0 24 24">
              <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
              <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
              <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
              <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
            </svg>
            Google ile Giriş Yap
          </button>

          <div style={{ display: "flex", alignItems: "center", margin: "24px 0", color: "var(--text-muted)" }}>
            <div style={{ flex: 1, height: 1, background: "var(--border-light)" }} />
            <span style={{ padding: "0 14px", fontSize: 13, textTransform: "uppercase", letterSpacing: "0.05em", color: "var(--text-secondary)", fontWeight: 500 }}>veya e-posta ile</span>
            <div style={{ flex: 1, height: 1, background: "var(--border-light)" }} />
          </div>

          <form onSubmit={onSubmit} style={{ display: "flex", flexDirection: "column", gap: 18 }}>
            <div>
              <label style={{ fontSize: 13, fontWeight: 700, color: "var(--text-main)", display: "block", marginBottom: 8, letterSpacing: "0.02em" }}>
                E-posta Adresi
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="ornek@eposta.com"
                required
                style={{
                  width: "100%", padding: "14px 16px", borderRadius: 12, border: "1.5px solid var(--border-light)", fontSize: 15, background: "var(--surface-hover)", outline: "none", transition: "all 0.2s ease"
                }}
              />
            </div>

            <div>
              <label style={{ fontSize: 13, fontWeight: 700, color: "var(--text-main)", display: "block", marginBottom: 8, letterSpacing: "0.02em" }}>
                Şifre
              </label>
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                style={{
                  width: "100%", padding: "14px 16px", borderRadius: 12, border: "1.5px solid var(--border-light)", fontSize: 15, background: "var(--surface-hover)", outline: "none", transition: "all 0.2s ease"
                }}
              />
            </div>

            {message && (
              <div style={{ padding: "14px 18px", borderRadius: 12, background: "var(--status-danger-bg)", border: "1px solid var(--status-danger-border)", color: "var(--status-danger)", fontSize: 14, fontWeight: 600 }}>
                {message}
              </div>
            )}

            <button className="btn-primary" type="submit" disabled={checking} style={{ marginTop: 8, height: 52, fontSize: 16, borderRadius: 14 }}>
              {checking ? "Doğrulanıyor..." : "Giriş Yap"}
            </button>
          </form>

          <div style={{
            marginTop: 24,
            padding: "18px 16px",
            borderRadius: 16,
            background: "linear-gradient(180deg, var(--primary-surface) 0%, var(--primary-light) 100%)",
            border: "1px solid var(--primary-border)",
            textAlign: "center",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            gap: 8,
          }}>
            <div style={{ fontSize: 14, fontWeight: 800, color: "var(--primary)", display: "inline-flex", alignItems: "center", gap: 6 }}>
              <Smartphone size={16} className="icon" aria-hidden="true" /> 5 ücretsiz deneme hakkı mobilde
            </div>
            <div style={{ fontSize: 12, color: "var(--primary-deep)", lineHeight: 1.4, marginBottom: 4 }}>
              Kayıt olmadan hemen test etmek için mobil uygulamayı ücretsiz indirebilirsiniz.
            </div>
            <div style={{ display: "flex", gap: 8, flexWrap: "wrap", justifyContent: "center" }}>
              <GooglePlayButton size="sm" />
              <AppStoreButton size="sm" />
            </div>
          </div>

          <p style={{ textAlign: "center", marginTop: 32, fontSize: 15, color: "var(--text-muted)" }}>
            Hesabınız yok mu? <Link href="/register" style={{ fontWeight: 700, color: "var(--primary)" }}>Üye Olun</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
