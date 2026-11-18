import type { Dispatch, FormEvent, SetStateAction } from "react";
import { Check, CircleCheck, Coins, Copy, Plus, Search } from "lucide-react";
import type { UserSummary, UserTokenForm, UserTokenResult } from "./types";

const QUICK_AMOUNTS = [25, 50, 100, 250, 500, 1000];

export function UsersTab({
  users,
  loadingUsers,
  userSearch,
  userTokenForm,
  userTokenResult,
  addingUserTokens,
  copiedId,
  setUserSearch,
  setUserTokenForm,
  onSubmitTokens,
  onClearForm,
  onSearch,
  onCopy,
  onSelectUser,
}: {
  users: UserSummary[];
  loadingUsers: boolean;
  userSearch: string;
  userTokenForm: UserTokenForm;
  userTokenResult: UserTokenResult | null;
  addingUserTokens: boolean;
  copiedId: string | null;
  setUserSearch: (value: string) => void;
  setUserTokenForm: Dispatch<SetStateAction<UserTokenForm>>;
  onSubmitTokens: (e?: FormEvent) => void;
  onClearForm: () => void;
  onSearch: (query: string) => void;
  onCopy: (text: string, id: string) => void;
  onSelectUser: (user: UserSummary) => void;
}) {
  return (
    <div className="admin-section">
      <h2>Kullanıcı Yönetimi ve Token Tanımlama</h2>

      {/* Token ekleme / müdahale formu */}
      <div className="admin-card">
        <div className="admin-card-header">
          <div className="admin-card-icon" aria-hidden="true">
            <Coins size={18} className="icon" />
          </div>
          <div>
            <h3>Kullanıcıya token ekle</h3>
            <p className="admin-card-desc">
              Hatalı işlem, destek talebi veya telafi gerektiğinde dilediğiniz kullanıcıya anında token yükleyin.
            </p>
          </div>
        </div>

        <form onSubmit={onSubmitTokens}>
          <div className="admin-form-grid admin-token-form-grid">
            <div>
              <label className="admin-label" htmlFor="token-user">Hedef kullanıcı (e-posta veya ID) *</label>
              <input
                id="token-user"
                type="text"
                placeholder="ornek@kullanici.com veya GUID"
                value={userTokenForm.userIdentifier}
                onChange={(e) => setUserTokenForm((prev) => ({ ...prev, userIdentifier: e.target.value }))}
                required
              />
            </div>

            <div>
              <label className="admin-label" htmlFor="token-amount">Token miktarı *</label>
              <input
                id="token-amount"
                type="number"
                min="1"
                max="1000000"
                placeholder="Örn: 100"
                value={userTokenForm.amount}
                onChange={(e) => setUserTokenForm((prev) => ({ ...prev, amount: e.target.value }))}
                required
              />
            </div>

            <div>
              <label className="admin-label" htmlFor="token-reason">Gerekçe / not</label>
              <input
                id="token-reason"
                type="text"
                placeholder="Örn: ISBN dönüştürme hatası telafisi"
                value={userTokenForm.reason}
                onChange={(e) => setUserTokenForm((prev) => ({ ...prev, reason: e.target.value }))}
              />
            </div>
          </div>

          <div className="admin-chip-row" role="group" aria-label="Hızlı miktar seçimi">
            <span className="admin-chip-label">Hızlı seçim:</span>
            {QUICK_AMOUNTS.map((amt) => {
              const active = userTokenForm.amount === String(amt);
              return (
                <button
                  key={amt}
                  type="button"
                  className={`admin-chip${active ? " is-active" : ""}`}
                  aria-pressed={active}
                  onClick={() => setUserTokenForm((prev) => ({ ...prev, amount: String(amt) }))}
                >
                  +{amt}
                </button>
              );
            })}
          </div>

          <div className="admin-form-actions" style={{ marginTop: 20 }}>
            <button type="submit" disabled={addingUserTokens || !userTokenForm.userIdentifier || !userTokenForm.amount}>
              {addingUserTokens ? "Token tanımlanıyor..." : "Token yükle"}
            </button>
            <button type="button" className="ghost" onClick={onClearForm}>
              Formu temizle
            </button>
          </div>
        </form>

        {userTokenResult && (
          <div className="admin-result" role="status">
            <div className="admin-result-title">
              <CircleCheck size={18} className="icon" aria-hidden="true" /> Token tanımlandı
            </div>
            <div className="admin-result-grid">
              <div>
                <span className="admin-result-label">Kullanıcı</span>
                <strong>{userTokenResult.email || userTokenResult.userId}</strong>
              </div>
              <div>
                <span className="admin-result-label">Önceki bakiye</span>
                <span className="cell-sub">{userTokenResult.previousBalance} Token</span>
              </div>
              <div>
                <span className="admin-result-label">Eklenen</span>
                <strong style={{ color: "var(--status-success)" }}>+{userTokenResult.addedAmount} Token</strong>
              </div>
              <div>
                <span className="admin-result-label">Yeni bakiye</span>
                <strong style={{ color: "var(--primary)" }}>{userTokenResult.newBalance} Token</strong>
              </div>
              {userTokenResult.reason && (
                <div className="admin-result-full">
                  <span className="admin-result-label">Gerekçe</span>
                  <span>{userTokenResult.reason}</span>
                </div>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Kullanıcı listesi ve arama */}
      <div className="admin-card">
        <div className="admin-toolbar">
          <div>
            <h3>Kayıtlı kullanıcılar ({users.length})</h3>
            <p className="admin-card-desc">Cüzdanı veya profili bulunan kullanıcıların bakiyeleri ve iş istatistikleri.</p>
          </div>
          <form
            className="admin-search"
            role="search"
            onSubmit={(e) => {
              e.preventDefault();
              onSearch(userSearch);
            }}
          >
            <input
              type="search"
              aria-label="Kullanıcı ara"
              placeholder="E-posta, isim veya ID ara..."
              value={userSearch}
              onChange={(e) => setUserSearch(e.target.value)}
            />
            <button type="submit" className="ghost btn-xs">
              <Search size={14} className="icon" aria-hidden="true" /> Ara
            </button>
            {userSearch && (
              <button
                type="button"
                className="ghost btn-xs"
                onClick={() => {
                  setUserSearch("");
                  onSearch("");
                }}
              >
                Sıfırla
              </button>
            )}
          </form>
        </div>

        <div className="table-wrap">
          {loadingUsers ? (
            <p className="admin-empty">Kullanıcılar yükleniyor...</p>
          ) : users.length > 0 ? (
            <table>
              <thead>
                <tr>
                  <th>Kullanıcı</th>
                  <th>Kullanıcı ID</th>
                  <th>Bakiye</th>
                  <th>Toplam iş</th>
                  <th>Harcanan token</th>
                  <th>Son işlem</th>
                  <th><span className="sr-only">İşlem</span></th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.userId}>
                    <td>
                      <strong>{u.name || u.email || "İsimsiz kullanıcı"}</strong>
                      {u.email && u.name && <div className="cell-sub">{u.email}</div>}
                    </td>
                    <td>
                      <div className="admin-id-cell">
                        <span className="mono-id" title={u.userId}>
                          {u.userId.slice(0, 8)}…{u.userId.slice(-4)}
                        </span>
                        <button
                          type="button"
                          className="ghost btn-icon-xs"
                          onClick={() => onCopy(u.userId, u.userId)}
                          aria-label="Kullanıcı ID'sini kopyala"
                          title="ID'yi kopyala"
                        >
                          {copiedId === u.userId ? (
                            <Check size={12} className="icon" style={{ color: "var(--status-success)" }} />
                          ) : (
                            <Copy size={12} className="icon" />
                          )}
                        </button>
                      </div>
                    </td>
                    <td>
                      <span className={`balance-pill ${u.balance > 0 ? "is-positive" : "is-empty"}`}>
                        {u.balance.toLocaleString("tr-TR")} Token
                      </span>
                    </td>
                    <td>{u.totalJobs} iş</td>
                    <td>{u.consumedTokens.toLocaleString("tr-TR")}</td>
                    <td className="cell-sub cell-nowrap">
                      {new Date(u.updatedAt || u.createdAt).toLocaleString("tr-TR", {
                        dateStyle: "short",
                        timeStyle: "short",
                      })}
                    </td>
                    <td>
                      <button type="button" className="ghost btn-xs" onClick={() => onSelectUser(u)}>
                        <Plus size={12} className="icon" aria-hidden="true" /> Token ekle
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="admin-empty">
              {userSearch ? "Arama kriterine uygun kullanıcı bulunamadı." : "Henüz kayıtlı kullanıcı bulunmuyor."}
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
