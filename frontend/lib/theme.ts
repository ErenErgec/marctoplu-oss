/** Tema tercihi: "system" işletim sistemini izler, diğerleri <html data-theme> ile zorlanır. */
export type ThemePreference = "system" | "light" | "dark";

export const THEME_STORAGE_KEY = "marc_theme";
const THEME_EVENT = "marc-theme-change";

/**
 * layout.tsx'te <head> içinde satır içi çalışır; React yüklenmeden tema uygulanır
 * ve açık temanın bir an görünüp kaybolması (flash) önlenir.
 */
export const THEME_INIT_SCRIPT = `(function(){try{var t=localStorage.getItem("${THEME_STORAGE_KEY}");if(t==="light"||t==="dark")document.documentElement.setAttribute("data-theme",t);}catch(e){}})();`;

export function readThemePreference(): ThemePreference {
  try {
    const value = window.localStorage.getItem(THEME_STORAGE_KEY);
    return value === "light" || value === "dark" ? value : "system";
  } catch {
    return "system";
  }
}

export function setThemePreference(preference: ThemePreference) {
  const root = document.documentElement;
  if (preference === "system") root.removeAttribute("data-theme");
  else root.setAttribute("data-theme", preference);
  try {
    if (preference === "system") window.localStorage.removeItem(THEME_STORAGE_KEY);
    else window.localStorage.setItem(THEME_STORAGE_KEY, preference);
  } catch {
    // Gizli sekmede depolama kapalı olabilir; tema yine bu oturumda uygulanır
  }
  window.dispatchEvent(new Event(THEME_EVENT));
}

/** useSyncExternalStore için: tercih bu sekmede veya başka sekmede değişince haber verir. */
export function subscribeThemePreference(onChange: () => void) {
  const onStorage = (e: StorageEvent) => {
    if (e.key !== THEME_STORAGE_KEY) return;
    const next = readThemePreference();
    if (next === "system") document.documentElement.removeAttribute("data-theme");
    else document.documentElement.setAttribute("data-theme", next);
    onChange();
  };
  window.addEventListener(THEME_EVENT, onChange);
  window.addEventListener("storage", onStorage);
  return () => {
    window.removeEventListener(THEME_EVENT, onChange);
    window.removeEventListener("storage", onStorage);
  };
}
