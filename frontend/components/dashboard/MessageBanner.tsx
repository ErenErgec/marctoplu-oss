import { CircleAlert, CircleCheck, Info, X } from "lucide-react";

export type MessageTone = "success" | "error" | "info";
export type DashboardMessage = { text: string; tone: MessageTone };

const TONE_ICON = {
  success: CircleCheck,
  error: CircleAlert,
  info: Info,
} as const;

export function MessageBanner({ message, onClose }: { message: DashboardMessage; onClose: () => void }) {
  const Icon = TONE_ICON[message.tone];
  return (
    <div className={`message-banner message-${message.tone}`} role={message.tone === "error" ? "alert" : "status"}>
      <Icon size={18} className="icon" aria-hidden="true" />
      <span className="message-text">{message.text}</span>
      <button type="button" className="message-close" onClick={onClose} aria-label="Mesajı kapat">
        <X size={16} className="icon" />
      </button>
    </div>
  );
}
