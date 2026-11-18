import { initializeApp, getApps, getApp } from "firebase/app";
import {
  browserLocalPersistence,
  browserPopupRedirectResolver,
  getAuth,
  indexedDBLocalPersistence,
  initializeAuth,
  type Auth,
} from "firebase/auth";

const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN,
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
  storageBucket: process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID,
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID
};

const app = !getApps().length ? initializeApp(firebaseConfig) : getApp();

/**
 * Oturum önce localStorage'da tutulur. Safari, Google açılır penceresi açıkken arka plandaki
 * sekmenin IndexedDB bağlantısını kapatıyor ve getAuth'un varsayılanı (IndexedDB) girişi
 * "Database is closing/hidden" hatasıyla düşürüyordu. IndexedDB listede ikinci sırada kalır:
 * Firebase orada kayıtlı eski oturumu localStorage'a taşır, kullanıcı çıkış yapmış olmaz.
 */
function createAuth(): Auth {
  if (typeof window === "undefined") return getAuth(app);
  try {
    return initializeAuth(app, {
      persistence: [browserLocalPersistence, indexedDBLocalPersistence],
      popupRedirectResolver: browserPopupRedirectResolver,
    });
  } catch {
    // Geliştirmede modül yeniden yüklenince Auth zaten başlatılmış olur
    return getAuth(app);
  }
}

export const auth = createAuth();
