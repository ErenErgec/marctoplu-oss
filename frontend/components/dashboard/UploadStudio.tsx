import { useRef, useState } from "react";
import { Download, FileSpreadsheet, Upload, X } from "lucide-react";

const ACCEPT = ".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

function isXlsx(file: File) {
  return file.name.toLowerCase().endsWith(".xlsx");
}

export function UploadStudio({
  file,
  loading,
  onFileChange,
  onUpload,
}: {
  file: File | null;
  loading: boolean;
  onFileChange: (file: File | null) => void;
  onUpload: () => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [rejectMessage, setRejectMessage] = useState("");

  function pickFile(next: File | null | undefined) {
    if (!next) return;
    if (!isXlsx(next)) {
      setRejectMessage(`"${next.name}" desteklenmiyor. Yalnızca Excel (.xlsx) dosyası yükleyebilirsiniz.`);
      return;
    }
    setRejectMessage("");
    onFileChange(next);
  }

  function clearFile() {
    onFileChange(null);
    setRejectMessage("");
    if (inputRef.current) inputRef.current.value = "";
  }

  return (
    <section id="upload" className="glass-panel">
      <div className="panel-header">
        <div>
          <h2 className="panel-title">Yeni kataloglama işi</h2>
          <p className="panel-subtitle">
            ISBN listesi içeren Excel (.xlsx) dosyanızı yükleyin. &quot;ISBN&quot; başlıklı sütun yoksa ilk sütun kullanılır.
          </p>
        </div>
        <a href="/ornek-isbn-listesi.xlsx" download className="btn-glass btn-sm">
          <Download size={14} className="icon" aria-hidden="true" /> Örnek şablon
        </a>
      </div>

      <div
        className={`dropzone${dragging ? " is-dragging" : ""}${file ? " has-file" : ""}`}
        role="button"
        tabIndex={0}
        aria-label="Excel dosyası seçin veya buraya sürükleyin"
        onClick={() => inputRef.current?.click()}
        onKeyDown={(e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            inputRef.current?.click();
          }
        }}
        onDragOver={(e) => {
          e.preventDefault();
          if (!dragging) setDragging(true);
        }}
        onDragLeave={(e) => {
          if (!e.currentTarget.contains(e.relatedTarget as Node | null)) setDragging(false);
        }}
        onDrop={(e) => {
          e.preventDefault();
          setDragging(false);
          pickFile(e.dataTransfer.files?.[0]);
        }}
      >
        <input
          ref={inputRef}
          type="file"
          accept={ACCEPT}
          hidden
          onChange={(e) => pickFile(e.target.files?.[0])}
        />
        <div className="dropzone-icon" aria-hidden="true">
          {file ? <FileSpreadsheet size={26} className="icon" /> : <Upload size={26} className="icon" />}
        </div>
        <h3 className="dropzone-title">
          {file ? file.name : dragging ? "Bırakın, dosya seçilsin" : "Excel dosyanızı buraya sürükleyin"}
        </h3>
        <p className="dropzone-hint">
          {file ? `${(file.size / 1024).toFixed(1)} KB · değiştirmek için tıklayın` : "veya dosya seçmek için tıklayın (.xlsx)"}
        </p>
      </div>

      {rejectMessage && (
        <p className="field-error" role="alert">
          {rejectMessage}
        </p>
      )}

      <div className="upload-actions">
        {file && (
          <button type="button" className="btn-glass" onClick={clearFile} disabled={loading}>
            <X size={16} className="icon" aria-hidden="true" /> Vazgeç
          </button>
        )}
        <button type="button" className="btn-primary" onClick={onUpload} disabled={!file || loading}>
          {loading ? "Yükleniyor..." : "İşlemi başlat"}
        </button>
      </div>
    </section>
  );
}
