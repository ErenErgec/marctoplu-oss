export type TokenPackage = { id: string; name: string; tokenAmount: number; priceTry: number };

export type Job = {
  id: string;
  status: string;
  totalIsbns: number;
  processedIsbns: number;
  consumedTokens: number;
  refundedTokens: number;
  errorMessage?: string | null;
  canDownload: boolean;
  createdAt: string;
};

export type DownloadFormat = "xlsx" | "ris" | "mrc" | "marcxml";

/** İndirme biçimlerinin dosya uzantısı ve menüde görünen adı */
export const DOWNLOAD_FORMATS: Record<DownloadFormat, { ext: string; label: string }> = {
  xlsx: { ext: "xlsx", label: "Excel" },
  ris: { ext: "ris", label: "RIS" },
  mrc: { ext: "mrc", label: "MARC21" },
  marcxml: { ext: "xml", label: "MARCXML" },
};

