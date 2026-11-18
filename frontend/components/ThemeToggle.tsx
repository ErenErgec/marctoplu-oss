"use client";

import { useSyncExternalStore } from "react";
import { Monitor, Moon, Sun } from "lucide-react";
import {
  readThemePreference,
  setThemePreference,
  subscribeThemePreference,
  type ThemePreference,
} from "../lib/theme";

const NEXT: Record<ThemePreference, ThemePreference> = { system: "light", light: "dark", dark: "system" };
const LABEL: Record<ThemePreference, string> = { system: "Sistem teması", light: "Açık tema", dark: "Koyu tema" };
const ICON = { system: Monitor, light: Sun, dark: Moon } as const;

/** Sistem → Açık → Koyu arasında döner. */
export function ThemeToggle() {
  const preference = useSyncExternalStore(subscribeThemePreference, readThemePreference, () => "system" as const);
  const Icon = ICON[preference];

  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={() => setThemePreference(NEXT[preference])}
      aria-label={`${LABEL[preference]} (değiştirmek için tıklayın)`}
      title={`${LABEL[preference]} · değiştir`}
    >
      <Icon size={16} className="icon" aria-hidden="true" />
    </button>
  );
}
