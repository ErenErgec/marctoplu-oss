"use client";

import { FormEvent, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useAuth } from "../../lib/useAuth";
import { API_BASE } from "../../lib/api";
import { ThemeToggle } from "../../components/ThemeToggle";

type Profile = {
  email?: string | null;
  name?: string | null;
  surname?: string | null;
  identityNumber?: string | null;
  gsmNumber?: string | null;
  city?: string | null;
  country?: string | null;
  address?: string | null;
  zipCode?: string | null;
  updatedAt?: string | null;
};

export default function ProfilePage() {
  const router = useRouter();
  const { ready, token, userInfo } = useAuth();
  const [profile, setProfile] = useState<Profile>({});
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState("");
  const [isError, setIsError] = useState(false);

  const fetchProfile = useCallback(async () => {
    if (!token) return;
    const res = await fetch(`${API_BASE}/api/me/profile`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) throw new Error("Profil bilgisi alınamadı.");
    setProfile(await res.json());
  }, [token]);

  useEffect(() => {
    if (ready && token && !userInfo?.isAnonymous) {
      fetchProfile().catch(e => { setIsError(true); setMessage(e.message); });
    }
  }, [ready, token, userInfo, fetchProfile]);

  function field(key: keyof Profile) {
    return profile[key] ?? "";
  }

  function setField(key: keyof Profile, value: string) {
    setProfile(p => ({ ...p, [key]: value }));
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault();

    if (userInfo?.isAnonymous) {
      setIsError(true);
      setMessage("Misafir kullanıcılar profil bilgisi kaydedemez. Lütfen önce kalıcı bir hesap oluşturun.");
      return;
    }

    const trimmedName = (profile.name ?? "").trim();
    const trimmedSurname = (profile.surname ?? "").trim();

    if (!trimmedName || trimmedName.length < 2) {
      setIsError(true);
      setMessage("Ad alanı zorunludur ve en az 2 karakter olmalıdır.");
      return;
    }

    if (!trimmedSurname || trimmedSurname.length < 2) {
      setIsError(true);
      setMessage("Soyad alanı zorunludur ve en az 2 karakter olmalıdır.");
      return;
    }

    try {
      setSaving(true);
      setMessage("");
      const res = await fetch(`${API_BASE}/api/me/profile`, {
        method: "PUT",
        headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
        body: JSON.stringify({
          name: trimmedName,
          surname: trimmedSurname,
          identityNumber: profile.identityNumber?.trim() || null,
          gsmNumber: profile.gsmNumber?.trim() || null,
          city: profile.city?.trim() || null,
          country: profile.country?.trim() || null,
          address: profile.address?.trim() || null,
          zipCode: profile.zipCode?.trim() || null,
        }),
      });
      if (!res.ok) {
        const data = await res.json().catch(() => ({}));
        throw new Error(data.title ?? data.message ?? "Kayıt başarısız.");
      }
      setProfile(await res.json());

      try {
        const { auth } = await import("../../lib/firebaseClient");
        if (auth.currentUser) {
          const { updateProfile } = await import("firebase/auth");
          await updateProfile(auth.currentUser, {
            displayName: `${trimmedName} ${trimmedSurname}`,
          });
        }
      } catch {}

      setIsError(false);
      setMessage("Profil bilgileri başarıyla kaydedildi.");
    } catch (err) {
      setIsError(true);
      setMessage(err instanceof Error ? err.message : "Kayıt başarısız.");
    } finally {
      setSaving(false);
    }
  }

  const isGuest = userInfo?.isAnonymous ?? false;

  const initials = isGuest
    ? "M"
    : [field("name"), field("surname")]
        .map(s => String(s).trim()[0])
        .filter(Boolean)
        .join("")
        .toUpperCase() || userInfo?.initials || "?";

  if (!ready) return null;

  return (
    <div className="app-container">
      {/* Floating Glass Navbar */}
      <header className="navbar">
        <div className="nav-brand">
          <div className="brand-icon"><img src="/logo.png" alt="Logo" style={{ width: "100%", height: "100%", objectFit: "cover" }} /></div>
          <span>MARCTOPLU</span>
        </div>

        <nav className="nav-menu">
          <Link href="/" className="nav-link">Panel</Link>
          <Link href="/profile" className="nav-link active">Profil</Link>
        </nav>

        <div className="nav-actions">
          <ThemeToggle />
          <Link href="/" className="btn-glass">
            ← Panele Dön
          </Link>
        </div>
      </header>

      <div className="glass-panel">
        <div className="panel-header">
          <div>
            <h1 style={{ fontSize: 24, fontWeight: 800 }}>Profil Ayarlarım</h1>
            <p style={{ fontSize: 13, color: "var(--text-muted)", marginTop: 4 }}>
              Ad, soyad ve fatura iletişim bilgilerinizi yönetin.
            </p>
          </div>
        </div>

        {isGuest && (
          <div style={{
            marginTop: 20,
            padding: "16px 20px",
            borderRadius: 14,
            background: "var(--status-warning-bg)",
            border: "1px solid var(--status-warning-border)",
            display: "flex",
            alignItems: "center",
            justifyContent: "space-between",
            flexWrap: "wrap",
            gap: 16
          }}>
            <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
              <div style={{
                width: 38,
                height: 38,
                borderRadius: 10,
                background: "var(--status-warning-bg)",
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                fontSize: 18,
                flexShrink: 0
              }}>
                ⚠️
              </div>
              <div>
                <div style={{ fontSize: 14, fontWeight: 700, color: "var(--status-warning)", marginBottom: 2 }}>
                  Misafir Oturumu Aktif
                </div>
                <div style={{ fontSize: 13, color: "var(--text-muted)", lineHeight: 1.4 }}>
                  Misafir oturumundayken profil ve fatura bilgisi kaydedilemez. Bilgilerinizi saklamak için kalıcı hesap açabilirsiniz.
                </div>
              </div>
            </div>
            <Link
              href="/register"
              className="btn-primary"
              style={{
                fontSize: 13,
                padding: "8px 18px",
                textDecoration: "none"
              }}
            >
              Kalıcı Hesap Oluştur →
            </Link>
          </div>
        )}

        <div className="profile-grid">
          {/* User Card */}
          <div style={{ background: "var(--bg-card)", padding: 24, borderRadius: 20, border: "1px solid var(--border-light)", textAlign: "center", height: "fit-content" }}>
            <div className="avatar-circle" style={{ width: 72, height: 72, fontSize: 28, margin: "0 auto 16px" }}>
              {initials}
            </div>
            <h3 style={{ fontSize: 18, fontWeight: 700, marginBottom: 4 }}>
              {isGuest
                ? "Misafir Kullanıcı"
                : [field("name"), field("surname")].filter(Boolean).join(" ") || "—"}
            </h3>
            <p style={{ color: "var(--text-muted)", fontSize: 13 }}>
              {isGuest ? "Geçici Test Oturumu" : (profile.email ?? userInfo?.email ?? "—")}
            </p>
            {isGuest && (
              <div style={{
                display: "inline-block",
                marginTop: 12,
                padding: "4px 12px",
                borderRadius: 20,
                background: "var(--status-warning-bg)",
                border: "1px solid var(--status-warning-border)",
                color: "var(--status-warning)",
                fontSize: 12,
                fontWeight: 700
              }}>
                Misafir Modu
              </div>
            )}
          </div>

          {/* Form */}
          <form onSubmit={onSubmit} style={{ display: "flex", flexDirection: "column", gap: 16 }}>
            <div className="profile-form-grid">
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)", display: "block", marginBottom: 6 }}>
                  AD <span style={{ color: "var(--status-danger)" }}>*</span>
                </label>
                <input
                  value={field("name")}
                  onChange={e => setField("name", e.target.value)}
                  placeholder="Adınız"
                  required
                  minLength={2}
                  maxLength={50}
                  disabled={isGuest || saving}
                  style={isGuest ? { opacity: 0.6, cursor: "not-allowed" } : undefined}
                />
              </div>
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)", display: "block", marginBottom: 6 }}>
                  SOYAD <span style={{ color: "var(--status-danger)" }}>*</span>
                </label>
                <input
                  value={field("surname")}
                  onChange={e => setField("surname", e.target.value)}
                  placeholder="Soyadınız"
                  required
                  minLength={2}
                  maxLength={50}
                  disabled={isGuest || saving}
                  style={isGuest ? { opacity: 0.6, cursor: "not-allowed" } : undefined}
                />
              </div>
            </div>

            <div className="profile-form-grid">
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)", display: "block", marginBottom: 6 }}>TC KİMLİK NO</label>
                <input
                  value={field("identityNumber")}
                  onChange={e => setField("identityNumber", e.target.value)}
                  placeholder="11 haneli TC No"
                  maxLength={11}
                  disabled={isGuest || saving}
                  style={isGuest ? { opacity: 0.6, cursor: "not-allowed" } : undefined}
                />
              </div>
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)", display: "block", marginBottom: 6 }}>TELEFON</label>
                <input
                  value={field("gsmNumber")}
                  onChange={e => setField("gsmNumber", e.target.value)}
                  placeholder="+905XXXXXXXXX"
                  disabled={isGuest || saving}
                  style={isGuest ? { opacity: 0.6, cursor: "not-allowed" } : undefined}
                />
              </div>
            </div>

            <div className="profile-form-grid">
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)", display: "block", marginBottom: 6 }}>ŞEHİR</label>
                <input
                  value={field("city")}
                  onChange={e => setField("city", e.target.value)}
                  placeholder="İstanbul"
                  disabled={isGuest || saving}
                  style={isGuest ? { opacity: 0.6, cursor: "not-allowed" } : undefined}
                />
              </div>
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)", display: "block", marginBottom: 6 }}>ÜLKE</label>
                <input
                  value={field("country")}
                  onChange={e => setField("country", e.target.value)}
                  placeholder="Türkiye"
                  disabled={isGuest || saving}
                  style={isGuest ? { opacity: 0.6, cursor: "not-allowed" } : undefined}
                />
              </div>
            </div>

            <div>
              <label style={{ fontSize: 12, fontWeight: 700, color: "var(--text-muted)", display: "block", marginBottom: 6 }}>ADRES</label>
              <input
                value={field("address")}
                onChange={e => setField("address", e.target.value)}
                placeholder="Fatura adresi"
                disabled={isGuest || saving}
                style={isGuest ? { opacity: 0.6, cursor: "not-allowed" } : undefined}
              />
            </div>

            {message && (
              <div style={{
                padding: "14px 18px",
                borderRadius: 14,
                background: isError ? "var(--status-danger-bg)" : "var(--status-success-bg)",
                border: `1px solid ${isError ? "var(--status-danger-border)" : "var(--status-success-border)"}`,
                color: isError ? "var(--status-danger)" : "var(--status-success)",
                fontSize: 14,
                fontWeight: 700
              }}>
                {message}
              </div>
            )}

            <button
              type="submit"
              className="btn-primary"
              disabled={isGuest || saving}
              style={{
                alignSelf: "flex-end",
                marginTop: 8,
                ...(isGuest ? { opacity: 0.5, cursor: "not-allowed" } : {})
              }}
            >
              {saving ? "Kaydediliyor..." : isGuest ? "Misafir Profil Kaydedemez" : "Değişiklikleri Kaydet"}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
