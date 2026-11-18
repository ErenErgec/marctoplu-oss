export const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:8080";

/** Hata yanıtından { message | errors | title } alanını ya da ham metni okur. */
export async function readApiError(res: Response, fallback: string) {
  const text = await res.text();
  if (!text) return fallback;
  try {
    const data = JSON.parse(text) as { message?: string; title?: string; errors?: Record<string, string[]> };
    // ASP.NET doğrulama hataları (ValidationProblemDetails): ilk alan mesajı Türkçe, title İngilizce
    const firstValidationError = data.errors ? Object.values(data.errors).flat()[0] : undefined;
    return data.message || firstValidationError || data.title || fallback;
  } catch {
    return text;
  }
}

/** Backend'in cihaz başına deneme/suistimal kontrolü için X-Device-Id başlığında gönderilen kalıcı kimlik. */
export function getOrCreateDeviceId(): string {
  if (typeof window === "undefined") return "";
  let id = window.localStorage.getItem("marc_device_id");
  if (!id) {
    id = "web_" + Math.random().toString(36).substring(2, 11) + "_" + Date.now().toString(36);
    window.localStorage.setItem("marc_device_id", id);
  }
  return id;
}
