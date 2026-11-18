import type { Dispatch, SetStateAction } from "react";
import type { Package, PackageForm } from "./types";

export function PackagesTab({
  packages,
  pkgForm,
  editingPkg,
  savingPkg,
  setPkgForm,
  onSave,
  onCancelEdit,
  onEdit,
  onToggle,
  onDelete,
}: {
  packages: Package[];
  pkgForm: PackageForm;
  editingPkg: string | null;
  savingPkg: boolean;
  setPkgForm: Dispatch<SetStateAction<PackageForm>>;
  onSave: () => void;
  onCancelEdit: () => void;
  onEdit: (pkg: Package) => void;
  onToggle: (id: string) => void;
  onDelete: (id: string) => void;
}) {
  return (
    <div className="admin-section">
      <h2>Token Paket Yönetimi</h2>

      {/* Form */}
      <div className="admin-card">
        <h3>{editingPkg ? "Paketi Düzenle" : "Yeni Paket Ekle"}</h3>
        <div className="admin-form-grid">
          <div>
            <label className="admin-label">Paket Adı</label>
            <input
              value={pkgForm.name}
              onChange={(e) => setPkgForm((p) => ({ ...p, name: e.target.value }))}
              placeholder="500 Token"
            />
          </div>
          <div>
            <label className="admin-label">Token Miktarı</label>
            <input
              type="number"
              value={pkgForm.tokenAmount}
              onChange={(e) => setPkgForm((p) => ({ ...p, tokenAmount: e.target.value }))}
              placeholder="500"
            />
          </div>
          <div>
            <label className="admin-label">Fiyat (₺)</label>
            <input
              type="number"
              step="0.01"
              value={pkgForm.priceTry}
              onChange={(e) => setPkgForm((p) => ({ ...p, priceTry: e.target.value }))}
              placeholder="799.90"
            />
          </div>
          <div className="admin-checkbox-row">
            <label className="admin-label">
              <input
                type="checkbox"
                checked={pkgForm.active}
                onChange={(e) => setPkgForm((p) => ({ ...p, active: e.target.checked }))}
              />
              Aktif Paket
            </label>
          </div>
        </div>
        <div className="admin-form-actions">
          <button onClick={onSave} disabled={savingPkg}>
            {savingPkg ? "Kaydediliyor..." : editingPkg ? "Güncelle" : "Oluştur"}
          </button>
          {editingPkg && (
            <button
              className="ghost"
              onClick={onCancelEdit}
            >
              İptal
            </button>
          )}
        </div>
      </div>

      {/* Paket Listesi */}
      <div className="admin-card">
        <div className="table-wrap">
          {packages.length > 0 ? (
            <table>
              <thead>
                <tr>
                  <th>Ad</th>
                  <th>Token</th>
                  <th>Fiyat (₺)</th>
                  <th>Durum</th>
                  <th>Oluşturma</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {packages.map((pkg) => (
                  <tr key={pkg.id}>
                    <td>
                      <strong>{pkg.name}</strong>
                    </td>
                    <td>{pkg.tokenAmount.toLocaleString("tr-TR")}</td>
                    <td>₺{pkg.priceTry.toLocaleString("tr-TR", { minimumFractionDigits: 2 })}</td>
                    <td>
                      <span
                        className={`admin-badge ${
                          pkg.active ? "badge-active" : "badge-inactive"
                        }`}
                      >
                        {pkg.active ? "Aktif" : "Pasif"}
                      </span>
                    </td>
                    <td className="cell-sub">
                      {new Date(pkg.createdAt).toLocaleDateString("tr-TR")}
                    </td>
                    <td>
                      <div className="admin-row-actions">
                        <button type="button" className="ghost btn-xs" onClick={() => onEdit(pkg)}>
                          Düzenle
                        </button>
                        <button
                          type="button" className="ghost btn-xs"
                          onClick={() => onToggle(pkg.id)}
                        >
                          {pkg.active ? "Pasife Al" : "Aktife Al"}
                        </button>
                        {!pkg.active && (
                          <button
                            type="button" className="danger-ghost btn-xs"
                            onClick={() => onDelete(pkg.id)}
                          >
                            Sil
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <p className="admin-empty">Henüz paket bulunmuyor.</p>
          )}
        </div>
      </div>
    </div>
  );
}
