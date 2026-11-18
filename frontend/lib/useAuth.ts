import { useState, useEffect, useMemo } from "react";
import { useRouter } from "next/navigation";
import { auth } from "./firebaseClient";
import { onAuthStateChanged, User } from "firebase/auth";
import { API_BASE } from "./api";


export function useAuth() {
  const router = useRouter();
  const [ready, setReady] = useState(false);
  const [token, setToken] = useState("");
  const [user, setUser] = useState<User | null>(null);
  const [profileName, setProfileName] = useState<string>("");

  const userInfo = useMemo(() => {
    if (!user) return { name: "", displayName: null, email: "", initials: "?", isAnonymous: false };
    const isAnonymous = user.isAnonymous ?? false;
    if (isAnonymous) {
      return { name: "Misafir Kullanıcı", displayName: null, email: "Misafir Kullanıcı", initials: "M", isAnonymous: true };
    }
    const email = user.email || "";
    const displayName = (user.displayName?.trim() || profileName.trim()) || "";
    const name = displayName || (email.includes("@") ? email.split("@")[0] : email);

    let initials = "?";
    if (displayName) {
      const parts = displayName.split(/\s+/).filter(Boolean);
      if (parts.length >= 2) {
        initials = (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
      } else if (parts.length === 1 && parts[0]) {
        initials = parts[0][0].toUpperCase();
      }
    } else if (email) {
      const parts = email.split("@")[0].split(/[._-]/).filter(Boolean);
      initials =
        parts.slice(0, 2).map((p: string) => p[0]?.toUpperCase() ?? "").join("") ||
        email[0]?.toUpperCase() ||
        "?";
    }

    return {
      name,
      displayName: displayName || null,
      email,
      initials,
      isAnonymous: false,
    };
  }, [user, profileName]);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(auth, async (currentUser) => {
      setUser(currentUser);
      
      if (currentUser) {
        if (currentUser.displayName) {
          setProfileName(currentUser.displayName);
        }
        // Force refresh the token to ensure we have the latest
        const currentToken = await currentUser.getIdToken(true);
        window.localStorage.setItem("marc_token", currentToken);
        setToken(currentToken);
        setReady(true);

        // If not anonymous, also check/sync backend profile name
        if (!currentUser.isAnonymous) {
          fetch(`${API_BASE}/api/me/profile`, {
            headers: { Authorization: `Bearer ${currentToken}` },
          })
            .then(async (res) => {
              if (res.ok) {
                const data = await res.json();
                const fullName = [data.name, data.surname].filter(Boolean).join(" ").trim();
                if (fullName) {
                  setProfileName(fullName);
                  if (!currentUser.displayName) {
                    const { updateProfile } = await import("firebase/auth");
                    updateProfile(currentUser, { displayName: fullName }).catch(() => {});
                  }
                }
              }
            })
            .catch(() => {});
        }
      } else {
        window.localStorage.removeItem("marc_token");
        setToken("");
        setProfileName("");
        setReady(true); // Still set ready to true, so UI can decide to redirect
        router.replace("/login");
      }
    });

    return () => unsubscribe();
  }, [router]);

  return { ready, token, userInfo };
}

export type AuthUserInfo = ReturnType<typeof useAuth>["userInfo"];
