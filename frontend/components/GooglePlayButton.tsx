"use client";

import React from "react";

export const PLAY_STORE_URL =
  "https://play.google.com/store/apps/details?id=com.marctoplu.app";

type GooglePlayButtonProps = {
  className?: string;
  style?: React.CSSProperties;
  variant?: "dark" | "light" | "outline";
  size?: "sm" | "md" | "lg";
};

export function GooglePlayButton({
  className = "",
  style = {},
  size = "md",
}: GooglePlayButtonProps) {
  const height = size === "sm" ? 36 : size === "lg" ? 48 : 42;

  return (
    <a
      href={PLAY_STORE_URL}
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
      aria-label="Google Play'den İndirin"
    >
      <img
        src="/google-play-badge.png"
        alt="Google Play'den İndirin"
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
