"use client";

import React from "react";

// Uygulama App Store'da yayında değilken boş bırakılır; o zaman buton hiç gösterilmez.
export const APP_STORE_URL = process.env.NEXT_PUBLIC_APP_STORE_URL || null;

type AppStoreButtonProps = {
  className?: string;
  style?: React.CSSProperties;
  size?: "sm" | "md" | "lg";
  href?: string | null;
};

export function AppStoreButton({
  className = "",
  style = {},
  size = "md",
  href = APP_STORE_URL,
}: AppStoreButtonProps) {
  if (!href) return null;

  const height = size === "sm" ? 36 : size === "lg" ? 48 : 42;

  return (
    <a
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={className}
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        textDecoration: "none",
        transition: "transform 0.18s ease, filter 0.18s ease",
        cursor: "pointer",
        lineHeight: 0,
        userSelect: "none",
        ...style,
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.transform = "translateY(-1px) scale(1.02)";
        e.currentTarget.style.filter = "drop-shadow(0 4px 12px rgba(0, 0, 0, 0.35))";
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.transform = "translateY(0) scale(1)";
        e.currentTarget.style.filter = "none";
      }}
      aria-label="App Store'dan İndirin"
    >
      <img
        src="/app-store-badge.png"
        alt="App Store'dan İndirin"
        style={{
          height: `${height}px`,
          width: "auto",
          maxWidth: "100%",
          objectFit: "contain",
          display: "block",
        }}
      />
    </a>
  );
}
