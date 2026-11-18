"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { API_BASE, getOrCreateDeviceId, readApiError } from "../lib/api";
import { DOWNLOAD_FORMATS, type DownloadFormat, type Job, type TokenPackage } from "../lib/types";
import { useAuth } from "../lib/useAuth";
import { DashboardNavbar } from "../components/dashboard/DashboardNavbar";
import { GuestExpiryBanner } from "../components/dashboard/GuestExpiryBanner";
import { GuestLanding } from "../components/dashboard/GuestLanding";
import { HeroWallet } from "../components/dashboard/HeroWallet";
import { JobsTable } from "../components/dashboard/JobsTable";
import { MessageBanner, type DashboardMessage } from "../components/dashboard/MessageBanner";
import { MobilePurchaseModal } from "../components/dashboard/MobilePurchaseModal";
import { PackagesStore } from "../components/dashboard/PackagesStore";
import { SiteFooter } from "../components/dashboard/SiteFooter";
import { UploadStudio } from "../components/dashboard/UploadStudio";
import { SupportModal, type SupportForm } from "../components/dashboard/SupportModal";
import { PromoLanding } from "../components/promo/PromoLanding";
import { PROMO_MODE } from "../lib/promo";

export default function HomePage() {
  // Build zamanı sabiti: tanıtım modunda panel (ve backend çağrıları) hiç render edilmez
  return PROMO_MODE ? <PromoLanding /> : <DashboardPage />;
}

function DashboardPage() {
  const { ready, token, userInfo } = useAuth();
  const [balance, setBalance] = useState<number | null>(null);
  const [packages, setPackages] = useState<TokenPackage[]>([]);
  const [jobs, setJobs] = useState<Job[]>([]);
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [cancelingId, setCancelingId] = useState<string | null>(null);
  const [downloadingState, setDownloadingState] = useState<{ id: string; format: DownloadFormat } | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [message, setMessage] = useState<DashboardMessage | null>(null);
  const [mobileNoticePkg, setMobileNoticePkg] = useState<TokenPackage | null>(null);
  const [showSupportModal, setShowSupportModal] = useState(false);
  const [supportForm, setSupportForm] = useState<SupportForm>({ email: "", jobId: "", message: "" });
  const [supportSent, setSupportSent] = useState(false);
  const [supportSending, setSupportSending] = useState(false);
  const [supportError, setSupportError] = useState("");

  function openSupportModalForJob(job: Job) {
    setSupportForm({
      email: userInfo?.isAnonymous ? "" : (userInfo?.email || ""),
      jobId: job.id,
      message: job.errorMessage ? `Hata detayı: ${job.errorMessage}` : "ISBN işleme sırasında hata oluştu. Token telafisi talep ediyorum.",
    });
    setSupportSent(false);
    setSupportError("");
    setShowSupportModal(true);
  }

  function openGeneralSupportModal() {
    setSupportForm({
      email: userInfo?.isAnonymous ? "" : (userInfo?.email || ""),
      jobId: "",
      message: "",
    });
    setSupportSent(false);
    setSupportError("");
    setShowSupportModal(true);
  }

  async function submitSupportTicket() {
    setSupportSending(true);
    setSupportError("");
    try {
      const res = await fetch(`${API_BASE}/api/support/tickets`, {
        method: "POST",
        headers: { ...headers, "Content-Type": "application/json" },
        body: JSON.stringify({
          email: supportForm.email,
          jobId: supportForm.jobId || null,
          message: supportForm.message,
        }),
      });
      if (!res.ok) {
        throw new Error(await readApiError(res, "Destek talebi gönderilemedi."));
      }
      setSupportSent(true);
    } catch (err: unknown) {
      setSupportError(err instanceof Error ? err.message : "Destek talebi gönderilemedi.");
    } finally {
      setSupportSending(false);
    }
  }

  const headers = useMemo(() => {
    const deviceId = getOrCreateDeviceId();
    return {
      Authorization: `Bearer ${token}`,
      ...(deviceId ? { "X-Device-Id": deviceId } : {}),
    };
  }, [token]);

  async function fetchPackages() {
    const res = await fetch(`${API_BASE}/api/token-packages`);
    if (!res.ok) throw new Error("Token paketleri alınamadı.");
    setPackages(await res.json());
  }

  const fetchWalletAndJobs = useCallback(async () => {
    if (!token) return;
    const timestamp = Date.now();
    const reqHeaders = {
      ...headers,
      "Cache-Control": "no-cache, no-store, must-revalidate",
      Pragma: "no-cache",
    };
    const [walletRes, jobsRes] = await Promise.all([
      fetch(`${API_BASE}/api/me/wallet?_t=${timestamp}`, {
        headers: reqHeaders,
        cache: "no-store",
      }),
      fetch(`${API_BASE}/api/jobs?limit=20&_t=${timestamp}`, {
        headers: reqHeaders,
        cache: "no-store",
      }),
    ]);
    if (!walletRes.ok) throw new Error("Cüzdan bilgisi alınamadı.");
    if (!jobsRes.ok) throw new Error("İş listesi alınamadı.");
    const wallet = await walletRes.json();
    const jobsData: Job[] = await jobsRes.json();
    setBalance(wallet.balance);
    setJobs((prevJobs) => {
      // Check if any job previously running/queued just finished
      const previouslyActive = prevJobs.filter((p) => {
        const s = p.status.toLowerCase();
        return s === "queued" || s === "running" || s === "cancel_requested";
      });
      for (const prev of previouslyActive) {
        const updated = jobsData.find((j) => j.id === prev.id);
        if (updated && updated.status.toLowerCase() === "completed") {
          setMessage({ text: `${updated.totalIsbns} ISBN'lik işiniz tamamlandı. Sonuçları "İşlerim" listesinden indirebilirsiniz.`, tone: "success" });
        }
      }
      return jobsData;
    });
  }, [headers, token]);

  useEffect(() => {
    fetchPackages().catch((err) => setMessage({ text: err.message, tone: "error" }));
  }, []);

  useEffect(() => {
    if (!token) return;
    fetchWalletAndJobs().catch((err) => setMessage({ text: err.message, tone: "error" }));
  }, [fetchWalletAndJobs, token]);

  const hasActiveJob = useMemo(() => {
    return jobs.some((x) => {
      const s = x.status.toLowerCase();
      return s === "queued" || s === "running" || s === "cancel_requested";
    });
  }, [jobs]);

  useEffect(() => {
    if (!token || !hasActiveJob) return;

    let isMounted = true;
    let timerId: ReturnType<typeof setTimeout>;

    const poll = async () => {
      try {
        await fetchWalletAndJobs();
      } catch {
        // ignore polling errors
      }
      if (isMounted) {
        timerId = setTimeout(poll, 2000);
      }
    };

    timerId = setTimeout(poll, 2000);

    return () => {
      isMounted = false;
      clearTimeout(timerId);
    };
  }, [fetchWalletAndJobs, hasActiveJob, token]);


  function proceedPackageCheckout(pkg: TokenPackage) {
    setMobileNoticePkg(pkg);
  }

  function scrollToSection(id: string) {
    const el = document.getElementById(id);
    if (!el) return;
    const yOffset = -96; // Floating navbar offset
    const y = el.getBoundingClientRect().top + window.pageYOffset + yOffset;
    window.scrollTo({ top: y, behavior: "smooth" });

    // Trigger subtle visual pulse highlight on the target section
    el.classList.remove("section-highlight");
    void el.offsetWidth; // Force reflow
    el.classList.add("section-highlight");
    setTimeout(() => {
      el.classList.remove("section-highlight");
    }, 1600);
  }

  async function uploadJob() {
    if (!file) {
      setMessage({ text: "Excel dosyası seçmelisiniz.", tone: "error" });
      return;
    }
    setLoading(true);
    setMessage(null);
    try {
      const form = new FormData();
      form.append("file", file);
      const res = await fetch(`${API_BASE}/api/jobs/upload`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
        },
        body: form,
      });

      if (!res.ok) {
        throw new Error(await readApiError(res, "İş oluşturulamadı."));
      }

      setFile(null);
      await fetchWalletAndJobs();
      setMessage({ text: "İş kuyruğa alındı ve işleniyor.", tone: "info" });
    } catch (err: unknown) {
      setMessage({ text: err instanceof Error ? err.message : "Yükleme sırasında hata oluştu.", tone: "error" });
    } finally {
      setLoading(false);
    }
  }

  async function cancelJob(id: string) {
    setCancelingId(id);
    setMessage(null);
    try {
      const res = await fetch(`${API_BASE}/api/jobs/${id}/cancel`, {
        method: "POST",
        headers,
      });
      if (!res.ok) {
        throw new Error(await readApiError(res, "İptal isteği başarısız oldu."));
      }
      await fetchWalletAndJobs();
      setMessage({ text: "İptal isteği gönderildi.", tone: "info" });
    } catch (err: unknown) {
      setMessage({ text: err instanceof Error ? err.message : "İptal edilirken hata oluştu.", tone: "error" });
    } finally {
      setCancelingId(null);
    }
  }

  async function downloadResult(id: string, format: DownloadFormat = "xlsx") {
    setDownloadingState({ id, format });
    setMessage(null);
    try {
      const res = await fetch(`${API_BASE}/api/jobs/${id}/download?format=${format}`, { headers });
      if (!res.ok) {
        throw new Error(await readApiError(res, "Sonuç dosyası indirilemedi."));
      }

      const foundCountStr = res.headers.get("X-Found-Count");
      const notFoundCountStr = res.headers.get("X-Not-Found-Count");
      const foundCount = foundCountStr ? parseInt(foundCountStr, 10) : null;
      const notFoundCount = notFoundCountStr ? parseInt(notFoundCountStr, 10) : null;

      const blob = await res.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `job_${id}_output.${DOWNLOAD_FORMATS[format].ext}`;
      document.body.appendChild(a);
      a.click();
      a.remove();
      window.URL.revokeObjectURL(url);

      if (format !== "xlsx") {
        // RIS ve MARC çıktılarına yalnızca bulunan kayıtlar girer
        const label = DOWNLOAD_FORMATS[format].label;
        if (notFoundCount && notFoundCount > 0) {
          setMessage({ text: `${label} dosyası indirildi (${foundCount ?? "?"} kayıt). Bulunamayan ${notFoundCount} ISBN bu dosyaya eklenmez; tam liste için Excel çıktısını indirin.`, tone: "success" });
        } else {
          setMessage({ text: `${label} dosyası indirildi (${foundCount ?? "?"} kayıt).`, tone: "success" });
        }
      } else {
        setMessage({ text: "Excel sonuç dosyası indirildi.", tone: "success" });
      }
    } catch (err: unknown) {
      setMessage({ text: err instanceof Error ? err.message : "İndirme sırasında hata oluştu.", tone: "error" });
    } finally {
      setDownloadingState(null);
    }
  }

  async function deleteJob(id: string) {
    setDeletingId(id);
    setMessage(null);
    try {
      const res = await fetch(`${API_BASE}/api/jobs/${id}`, {
        method: "DELETE",
        headers,
      });
      if (!res.ok) {
        throw new Error(await readApiError(res, "İş silinemedi."));
      }
      setMessage({ text: "İş kaydı silindi.", tone: "info" });
      await fetchWalletAndJobs();
    } catch (err: unknown) {
      setMessage({ text: err instanceof Error ? err.message : "Silinirken hata oluştu.", tone: "error" });
    } finally {
      setDeletingId(null);
    }
  }

  if (!ready) {
    return (
      <div className="auth-page">
        <div style={{ color: "var(--primary)", fontSize: "18px", fontWeight: "bold" }}>MARCTOPLU yükleniyor...</div>
      </div>
    );
  }

  if (!token) {
    return <GuestLanding />;
  }

  const userInitial = userInfo?.initials || (userInfo?.name ? userInfo.name[0].toUpperCase() : "U");

  return (
    <div className="app-container">
      <DashboardNavbar userInfo={userInfo} userInitial={userInitial} onOpenSupport={openGeneralSupportModal} />

      {userInfo?.isAnonymous && <GuestExpiryBanner />}

      {message && <MessageBanner message={message} onClose={() => setMessage(null)} />}

      <HeroWallet balance={balance} onScrollTo={scrollToSection} />

      <UploadStudio file={file} loading={loading} onFileChange={setFile} onUpload={uploadJob} />

      <JobsTable
        jobs={jobs}
        cancelingId={cancelingId}
        deletingId={deletingId}
        downloadingState={downloadingState}
        onRefresh={fetchWalletAndJobs}
        onDownload={downloadResult}
        onCancel={cancelJob}
        onDelete={deleteJob}
        onReportIssue={openSupportModalForJob}
      />

      <PackagesStore packages={packages} onSelectPackage={proceedPackageCheckout} />

      <SiteFooter />

      {mobileNoticePkg && (
        <MobilePurchaseModal pkg={mobileNoticePkg} userInfo={userInfo} onClose={() => setMobileNoticePkg(null)} />
      )}

      {showSupportModal && (
        <SupportModal
          form={supportForm}
          sent={supportSent}
          sending={supportSending}
          error={supportError}
          onFormChange={setSupportForm}
          onSubmit={submitSupportTicket}
          onClose={() => setShowSupportModal(false)}
        />
      )}
    </div>
  );
}
