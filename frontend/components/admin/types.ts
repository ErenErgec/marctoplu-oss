export type Stats = {
  totalUsers: number;
  totalJobs: number;
  completedJobs: number;
  failedJobs: number;
  runningJobs: number;
  queuedJobs: number;
  totalIsbnProcessed: number;
  totalTokensConsumed: number;
  /** Mobil IAP: token_ledger'daki "iap:%" kredileri. */
  iapPurchases: number;
  iapTokensSold: number;
  iapAndroid: IapPlatformStats;
  iapIos: IapPlatformStats;
  /** Fiyatı kayıtlı production satın almalar, para birimi başına (TRY önce). Mağaza komisyonu ve vergiler düşülmemiş. */
  iapIosRevenue: IapRevenue[];
  iapAndroidRevenue: IapRevenue[];
  /** İade edilmiş (ve iadesi geri alınmamış) IAP satın alma sayısı. Gelir toplamlarına dahil değildir. */
  iapRefunds: number;
  /** Askıdaki iyzico web ödemeleri (Payments, status = paid). */
  iyzicoPayments: number;
  iyzicoRevenueTry: number;
};

export type IapPlatformStats = {
  purchases: number;
  tokensSold: number;
};

export type IapRevenue = {
  currency: string;
  purchases: number;
  amount: number;
};

export type Package = {
  id: string;
  name: string;
  tokenAmount: number;
  priceTry: number;
  active: boolean;
  createdAt: string;
};

export type AdminJob = {
  id: string;
  userId: string;
  status: string;
  totalIsbns: number;
  processedIsbns: number;
  consumedTokens: number;
  errorMessage?: string | null;
  createdAt: string;
};

export type UserSummary = {
  userId: string;
  email: string | null;
  name: string | null;
  balance: number;
  totalJobs: number;
  consumedTokens: number;
  createdAt: string;
  updatedAt: string;
};

export type UserTokenResult = {
  success: boolean;
  userId: string;
  email: string | null;
  previousBalance: number;
  addedAmount: number;
  newBalance: number;
  reason: string | null;
};


export type AdminTab = "stats" | "purchases" | "packages" | "jobs" | "users" | "support";

/** token_ledger'daki tek bir IAP kredisi. Fiyat alanları eski satın almalarda ve Orders API yanıt vermediğinde boştur. */
export type Purchase = {
  id: string;
  createdAt: string;
  userId: string;
  email: string | null;
  name: string | null;
  platform: string;
  productId: string | null;
  tokens: number;
  /** iOS: Production/Sandbox, Android: Production/Test/Promo/Rewarded. Eski kayıtlarda null. */
  environment: string | null;
  currency: string | null;
  price: number | null;
  tax: number | null;
  proceeds: number | null;
  /** Android regionCode veya iOS storefront. */
  region: string | null;
  /** Yalnızca Android (GPA.…). */
  orderId: string | null;
  /** Android purchaseToken, iOS transactionId. */
  storeReference: string;
  /** İade işlendiği an; iade yoksa veya geri alındıysa null. */
  refundedAt: string | null;
};

/** Özet, limitten bağımsız olarak filtreye uyan tüm satın almaları kapsar. Gelir iade edilenleri içermez. */
export type PurchaseList = {
  totalCount: number;
  totalTokens: number;
  refundedCount: number;
  revenue: IapRevenue[];
  items: Purchase[];
};

export type PurchaseFilters = {
  platform: "all" | "android" | "ios";
  environment: "all" | "production" | "test";
  /** Son N gün; null = tümü. */
  days: number | null;
  status: "all" | "active" | "refunded";
  search: string;
};

export type SupportTicketFilter = "open" | "resolved" | "all";

export type SupportTicket = {
  id: string;
  userId: string;
  email: string;
  jobId: string | null;
  jobStatus: string | null;
  message: string;
  status: "open" | "resolved";
  createdAt: string;
  resolvedAt: string | null;
};

export type PackageForm = { name: string; tokenAmount: string; priceTry: string; active: boolean };

export type UserTokenForm = { userIdentifier: string; amount: string; reason: string };
