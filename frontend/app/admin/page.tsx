"use client";

import { useCallback, useEffect, useState } from "react";
import { CircleAlert, CircleCheck, Loader2, X } from "lucide-react";
import { useAuth } from "../../lib/useAuth";
import { API_BASE, readApiError } from "../../lib/api";
import { AdminNav } from "../../components/admin/AdminNav";
import { AdminUnauthorized } from "../../components/admin/AdminUnauthorized";
import { JobsTab } from "../../components/admin/JobsTab";
import { PackagesTab } from "../../components/admin/PackagesTab";
import { PURCHASE_MAX_LIMIT, PURCHASE_PAGE_SIZE, PurchasesTab } from "../../components/admin/PurchasesTab";
import { StatsTab } from "../../components/admin/StatsTab";
import { SupportTab } from "../../components/admin/SupportTab";
import { UsersTab } from "../../components/admin/UsersTab";
import type {
  AdminJob,
  AdminTab,
  Package,
  PackageForm,
  Purchase,
  PurchaseFilters,
  PurchaseList,
  Stats,
  SupportTicket,
  SupportTicketFilter,
  UserSummary,
  UserTokenForm,
  UserTokenResult,
} from "../../components/admin/types";

export default function AdminPage() {
  const { ready, token, userInfo } = useAuth();
  const [authStatus, setAuthStatus] = useState<"checking" | "authorized" | "unauthorized">("checking");
  const [authErrorMsg, setAuthErrorMsg] = useState("");
  const [stats, setStats] = useState<Stats | null>(null);
  const [packages, setPackages] = useState<Package[]>([]);
  const [jobs, setJobs] = useState<AdminJob[]>([]);
  const [users, setUsers] = useState<UserSummary[]>([]);
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [userSearch, setUserSearch] = useState("");
  const [activeTab, setActiveTab] = useState<AdminTab>("stats");
  const [tickets, setTickets] = useState<SupportTicket[]>([]);
  const [ticketFilter, setTicketFilter] = useState<SupportTicketFilter>("open");
  const [updatingTicketId, setUpdatingTicketId] = useState<string | null>(null);
  const [purchases, setPurchases] = useState<PurchaseList | null>(null);
  const [loadingPurchases, setLoadingPurchases] = useState(false);
  const [purchaseFilters, setPurchaseFilters] = useState<PurchaseFilters>({
    platform: "all",
    environment: "all",
    days: 30,
    status: "all",
    search: "",
  });
  const [purchaseSearchDraft, setPurchaseSearchDraft] = useState("");
  const [purchaseLimit, setPurchaseLimit] = useState(PURCHASE_PAGE_SIZE);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  // Paket formu
  const [pkgForm, setPkgForm] = useState<PackageForm>({ name: "", tokenAmount: "", priceTry: "", active: true });
  const [editingPkg, setEditingPkg] = useState<string | null>(null);
  const [savingPkg, setSavingPkg] = useState(false);

  // Hızlı Token (Kendime)
  const [fastTokenAmount, setFastTokenAmount] = useState("");
  const [addingTokens, setAddingTokens] = useState(false);

  // Kullanıcıya Token Ekleme Formu
  const [userTokenForm, setUserTokenForm] = useState<UserTokenForm>({
    userIdentifier: "",
    amount: "100",
    reason: "Problem telafisi / Müşteri desteği",
  });
  const [addingUserTokens, setAddingUserTokens] = useState(false);
  const [userTokenResult, setUserTokenResult] = useState<UserTokenResult | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);

  // Toast'lar 6 sn sonra kendiliğinden kapanır; hata mesajları biraz daha uzun kalır
  useEffect(() => {
    if (!success) return;
    const id = setTimeout(() => setSuccess(""), 6000);
    return () => clearTimeout(id);
  }, [success]);

  useEffect(() => {
    if (!error) return;
    const id = setTimeout(() => setError(""), 9000);
    return () => clearTimeout(id);
  }, [error]);

  const headers = useCallback(
    () => ({ Authorization: `Bearer ${token}`, "Content-Type": "application/json" }),
    [token]
  );

  const fetchStats = useCallback(async () => {
    if (!token) return;
    const res = await fetch(`${API_BASE}/api/admin/stats`, { headers: headers() });
    if (res.status === 403) {
      setError("Admin yetkisi yok.");
      return;
    }
    if (res.status === 503) {
      setError("Admin paneli devre dışı. App:AdminEmails ayarını yapılandırın.");
      return;
    }
    if (!res.ok) throw new Error("İstatistikler alınamadı.");
    setStats(await res.json());
  }, [token, headers]);

  const fetchPackages = useCallback(async () => {
    if (!token) return;
    const res = await fetch(`${API_BASE}/api/admin/packages`, { headers: headers() });
    if (!res.ok) throw new Error("Paketler alınamadı.");
    setPackages(await res.json());
  }, [token, headers]);

  const fetchJobs = useCallback(async () => {
    if (!token) return;
    const res = await fetch(`${API_BASE}/api/admin/jobs?limit=50`, { headers: headers() });
    if (!res.ok) throw new Error("İş listesi alınamadı.");
    setJobs(await res.json());
  }, [token, headers]);

  const fetchTickets = useCallback(async () => {
    if (!token) return;
    const res = await fetch(`${API_BASE}/api/admin/support-tickets?status=${ticketFilter}&limit=200`, { headers: headers() });
    if (!res.ok) throw new Error("Destek talepleri alınamadı.");
    setTickets(await res.json());
  }, [token, headers, ticketFilter]);

  const fetchPurchases = useCallback(async () => {
    if (!token) return;
    setLoadingPurchases(true);
    try {
      const params = new URLSearchParams({
        platform: purchaseFilters.platform,
        environment: purchaseFilters.environment,
        status: purchaseFilters.status,
        limit: String(purchaseLimit),
      });
      if (purchaseFilters.days != null) params.set("days", String(purchaseFilters.days));
      if (purchaseFilters.search) params.set("search", purchaseFilters.search);
      const res = await fetch(`${API_BASE}/api/admin/purchases?${params}`, { headers: headers() });
      if (!res.ok) throw new Error(await readApiError(res, "Satın almalar alınamadı."));
      setPurchases(await res.json());
    } finally {
      setLoadingPurchases(false);
    }
  }, [token, headers, purchaseFilters, purchaseLimit]);

  const fetchUsers = useCallback(
    async (searchQuery?: string) => {
      if (!token) return;
      try {
        setLoadingUsers(true);
        const url = `${API_BASE}/api/admin/users?limit=100${
          searchQuery ? `&search=${encodeURIComponent(searchQuery)}` : ""
        }`;
        const res = await fetch(url, { headers: headers() });
        if (!res.ok) throw new Error("Kullanıcı listesi alınamadı.");
        setUsers(await res.json());
      } catch (e) {
        setError(e instanceof Error ? e.message : "Kullanıcılar yüklenemedi.");
      } finally {
        setLoadingUsers(false);
      }
    },
    [token, headers]
  );

  useEffect(() => {
    if (!ready) return;

    if (!token || userInfo?.isAnonymous) {
      setAuthStatus("unauthorized");
      setAuthErrorMsg(
        userInfo?.isAnonymous
          ? "Misafir kullanıcıların yönetici paneline erişim yetkisi yoktur. Lütfen yetkili yönetici hesabınızla giriş yapınız."
          : "Yönetici paneline erişmek için giriş yapmalısınız."
      );
      return;
    }

    async function verifyAdmin() {
      try {
        const res = await fetch(`${API_BASE}/api/admin/check`, {
          headers: { Authorization: `Bearer ${token}` },
        });

        if (res.ok) {
          setAuthStatus("authorized");
          fetchStats().catch((e) => setError(e.message));
          fetchPackages().catch((e) => setError(e.message));
        } else if (res.status === 403) {
          setAuthStatus("unauthorized");
          setAuthErrorMsg(`Giriş yaptığınız hesap (${userInfo?.email || "bu e-posta"}) yönetici yetkisine sahip değildir.`);
        } else if (res.status === 503) {
          setAuthStatus("unauthorized");
          setAuthErrorMsg("Yönetici paneli sunucuda henüz aktif edilmemiş (App:AdminEmails tanımlı değil).");
        } else {
          setAuthStatus("unauthorized");
          setAuthErrorMsg("Yetki doğrulaması başarısız oldu.");
        }
      } catch {
        setAuthStatus("unauthorized");
        setAuthErrorMsg("Sunucuya bağlanırken bir iletişim hatası oluştu.");
      }
    }

    verifyAdmin();
  }, [ready, token, userInfo, fetchStats, fetchPackages]);

  useEffect(() => {
    if (activeTab === "jobs" && token) {
      fetchJobs().catch((e) => setError(e.message));
    } else if (activeTab === "users" && token) {
      fetchUsers(userSearch);
    } else if (activeTab === "support" && token) {
      fetchTickets().catch((e) => setError(e.message));
    } else if (activeTab === "purchases" && token) {
      fetchPurchases().catch((e) => setError(e.message));
    }
  }, [activeTab, token, fetchJobs, fetchUsers, fetchTickets, fetchPurchases, userSearch]);

  async function handleAddTokensToUser(e?: React.FormEvent) {
    if (e) e.preventDefault();
    try {
      const amount = parseInt(userTokenForm.amount, 10);
      if (!userTokenForm.userIdentifier.trim()) {
        setError("Lütfen hedef kullanıcının e-posta adresini veya Kullanıcı ID'sini girin.");
        return;
      }
      if (!amount || amount <= 0) {
        setError("Geçerli bir token miktarı girin (en az 1).");
        return;
      }

      setAddingUserTokens(true);
      setError("");
      setSuccess("");
      setUserTokenResult(null);

      const res = await fetch(`${API_BASE}/api/admin/users/add-tokens`, {
        method: "POST",
        headers: headers(),
        body: JSON.stringify({
          userIdentifier: userTokenForm.userIdentifier.trim(),
          amount,
          reason: userTokenForm.reason.trim(),
        }),
      });

      if (!res.ok) {
        throw new Error(await readApiError(res, "Token eklenemedi."));
      }
      const data = await res.json();

      const calculatedNewBalance =
        typeof data.newBalance === "number" && data.newBalance > data.previousBalance
          ? data.newBalance
          : (data.previousBalance ?? 0) + amount;

      setUserTokenResult({
        ...data,
        newBalance: calculatedNewBalance,
      });
      setSuccess(`Kullanıcıya başarıyla ${amount} token tanımlandı.`);
      fetchUsers(userSearch);
      fetchStats();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Bir hata oluştu.");
    } finally {
      setAddingUserTokens(false);
    }
  }

  function handleSelectUser(u: UserSummary) {
    setUserTokenForm((prev) => ({
      ...prev,
      userIdentifier: u.email || u.userId,
    }));
    setUserTokenResult(null);
    setActiveTab("users");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleCompensateJob(job: AdminJob) {
    const isFailed = job.status.toLowerCase() === "failed" || job.status.toLowerCase().includes("cancel");
    const suggestedAmount = job.consumedTokens > 0 ? String(job.consumedTokens) : "50";
    setUserTokenForm({
      userIdentifier: job.userId,
      amount: suggestedAmount,
      reason: `İş #${job.id.slice(0, 8)} ${isFailed ? "hata telafisi" : "müdahalesi"}`,
    });
    setUserTokenResult(null);
    setActiveTab("users");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handlePurchaseFiltersChange(filters: PurchaseFilters) {
    setPurchaseFilters(filters);
    setPurchaseLimit(PURCHASE_PAGE_SIZE);
  }

  function handleOpenPurchaseUser(purchase: Purchase) {
    setUserSearch(purchase.email || purchase.userId);
    setUserTokenResult(null);
    setActiveTab("users");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function handleCompensateTicket(ticket: SupportTicket) {
    setUserTokenForm({
      userIdentifier: ticket.userId,
      amount: "50",
      reason: `Destek talebi #${ticket.id.slice(0, 8)} telafisi`,
    });
    setUserTokenResult(null);
    setActiveTab("users");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  async function setTicketStatus(ticket: SupportTicket, status: "open" | "resolved") {
    setUpdatingTicketId(ticket.id);
    try {
      const res = await fetch(`${API_BASE}/api/admin/support-tickets/${ticket.id}/status?status=${status}`, {
        method: "PATCH",
        headers: headers(),
      });
      if (!res.ok) throw new Error(await readApiError(res, "Talep durumu güncellenemedi."));
      setSuccess(status === "resolved" ? "Talep çözüldü olarak işaretlendi." : "Talep yeniden açıldı.");
      await fetchTickets();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Talep durumu güncellenemedi.");
    } finally {
      setUpdatingTicketId(null);
    }
  }

  function copyToClipboard(text: string, id: string) {
    navigator.clipboard.writeText(text);
    setCopiedId(id);
    setTimeout(() => setCopiedId(null), 2000);
  }

  async function savePackage() {
    try {
      setSavingPkg(true);
      setError("");
      const body = {
        name: pkgForm.name.trim(),
        tokenAmount: parseInt(pkgForm.tokenAmount),
        priceTry: parseFloat(pkgForm.priceTry),
        active: pkgForm.active,
      };

      const url = editingPkg
        ? `${API_BASE}/api/admin/packages/${editingPkg}`
        : `${API_BASE}/api/admin/packages`;
      const method = editingPkg ? "PUT" : "POST";

      const res = await fetch(url, { method, headers: headers(), body: JSON.stringify(body) });
      if (!res.ok) {
        throw new Error(await readApiError(res, "Kayıt başarısız."));
      }

      setSuccess(editingPkg ? "Paket güncellendi." : "Yeni paket oluşturuldu.");
      setPkgForm({ name: "", tokenAmount: "", priceTry: "", active: true });
      setEditingPkg(null);
      await fetchPackages();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Hata oluştu.");
    } finally {
      setSavingPkg(false);
    }
  }

  async function togglePackage(id: string) {
    try {
      const res = await fetch(`${API_BASE}/api/admin/packages/${id}/toggle`, {
        method: "PATCH",
        headers: headers(),
      });
      if (!res.ok) throw new Error("Durum değiştirilemedi.");
      await fetchPackages();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Hata.");
    }
  }

  async function deletePackage(id: string) {
    if (!confirm("Bu paketi silmek istediğinizden emin misiniz?")) return;
    try {
      const res = await fetch(`${API_BASE}/api/admin/packages/${id}`, { method: "DELETE", headers: headers() });
      if (!res.ok) {
        throw new Error(await readApiError(res, "Silinemedi."));
      }
      setSuccess("Paket silindi.");
      await fetchPackages();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Hata.");
    }
  }

  async function handleAddTokensToMe() {
    try {
      const amount = parseInt(fastTokenAmount, 10);
      if (!amount || amount <= 0) {
        setError("Geçerli bir miktar girin.");
        return;
      }
      setAddingTokens(true);
      setError("");
      setSuccess("");

      const res = await fetch(`${API_BASE}/api/admin/me/add-tokens?amount=${amount}`, {
        method: "POST",
        headers: headers(),
      });

      if (!res.ok) {
        throw new Error(await readApiError(res, "Token yüklenemedi."));
      }

      setSuccess(`Hesabınıza başarıyla ${amount} token eklendi.`);
      setFastTokenAmount("");
      fetchStats();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Hata.");
    } finally {
      setAddingTokens(false);
    }
  }

  function startEdit(pkg: Package) {
    setEditingPkg(pkg.id);
    setPkgForm({
      name: pkg.name,
      tokenAmount: String(pkg.tokenAmount),
      priceTry: String(pkg.priceTry),
      active: pkg.active,
    });
  }

  if (!ready || authStatus === "checking") {
    return (
      <div className="auth-page">
        <div className="admin-loading" role="status">
          <Loader2 size={18} className="icon admin-spin" aria-hidden="true" />
          Yönetici yetkisi doğrulanıyor...
        </div>
      </div>
    );
  }

  if (authStatus === "unauthorized") {
    return <AdminUnauthorized message={authErrorMsg} />;
  }

  return (
    <main className="admin-layout">
      <AdminNav activeTab={activeTab} onTabChange={setActiveTab} />

      <div className="admin-content">
        {(error || success) && (
          <div className="toast-container">
            {error && (
              <div className="toast toast-error" role="alert">
                <CircleAlert size={18} className="icon" aria-hidden="true" />
                <span className="toast-message">{error}</span>
                <button type="button" className="toast-close" onClick={() => setError("")} aria-label="Kapat">
                  <X size={16} className="icon" />
                </button>
              </div>
            )}
            {success && (
              <div className="toast toast-success" role="status">
                <CircleCheck size={18} className="icon" aria-hidden="true" />
                <span className="toast-message">{success}</span>
                <button type="button" className="toast-close" onClick={() => setSuccess("")} aria-label="Kapat">
                  <X size={16} className="icon" />
                </button>
              </div>
            )}
          </div>
        )}

        {activeTab === "stats" && (
          <StatsTab
            stats={stats}
            fastTokenAmount={fastTokenAmount}
            addingTokens={addingTokens}
            onRefresh={() => fetchStats().catch((e) => setError(e.message))}
            onGoToUsers={() => setActiveTab("users")}
            onFastTokenAmountChange={setFastTokenAmount}
            onAddTokensToMe={handleAddTokensToMe}
          />
        )}

        {activeTab === "purchases" && (
          <PurchasesTab
            data={purchases}
            loading={loadingPurchases}
            filters={purchaseFilters}
            searchDraft={purchaseSearchDraft}
            copiedId={copiedId}
            onFiltersChange={handlePurchaseFiltersChange}
            onSearchDraftChange={setPurchaseSearchDraft}
            onLoadMore={() => setPurchaseLimit((l) => Math.min(l + PURCHASE_PAGE_SIZE, PURCHASE_MAX_LIMIT))}
            onRefresh={() => fetchPurchases().catch((e) => setError(e.message))}
            onCopy={copyToClipboard}
            onOpenUser={handleOpenPurchaseUser}
          />
        )}

        {activeTab === "packages" && (
          <PackagesTab
            packages={packages}
            pkgForm={pkgForm}
            editingPkg={editingPkg}
            savingPkg={savingPkg}
            setPkgForm={setPkgForm}
            onSave={savePackage}
            onCancelEdit={() => {
              setEditingPkg(null);
              setPkgForm({ name: "", tokenAmount: "", priceTry: "", active: true });
            }}
            onEdit={startEdit}
            onToggle={togglePackage}
            onDelete={deletePackage}
          />
        )}

        {activeTab === "jobs" && (
          <JobsTab
            jobs={jobs}
            onRefresh={() => fetchJobs().catch((e) => setError(e.message))}
            onCompensate={handleCompensateJob}
          />
        )}

        {activeTab === "support" && (
          <SupportTab
            tickets={tickets}
            filter={ticketFilter}
            updatingId={updatingTicketId}
            onFilterChange={setTicketFilter}
            onRefresh={() => fetchTickets().catch((e) => setError(e.message))}
            onSetStatus={setTicketStatus}
            onCompensate={handleCompensateTicket}
          />
        )}

        {activeTab === "users" && (
          <UsersTab
            users={users}
            loadingUsers={loadingUsers}
            userSearch={userSearch}
            userTokenForm={userTokenForm}
            userTokenResult={userTokenResult}
            addingUserTokens={addingUserTokens}
            copiedId={copiedId}
            setUserSearch={setUserSearch}
            setUserTokenForm={setUserTokenForm}
            onSubmitTokens={handleAddTokensToUser}
            onClearForm={() => {
              setUserTokenForm({ userIdentifier: "", amount: "100", reason: "" });
              setUserTokenResult(null);
            }}
            onSearch={fetchUsers}
            onCopy={copyToClipboard}
            onSelectUser={handleSelectUser}
          />
        )}
      </div>
    </main>
  );
}
