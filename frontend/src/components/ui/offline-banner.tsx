import { AlertCircle } from 'lucide-react'

export function OfflineBanner({ message }: { readonly message: string }) {
  return (
    <div
      className="mb-6 flex items-center gap-2.5 rounded-xl border border-border bg-surface-2 px-4 py-3.5 text-sm text-muted-foreground shadow-sm"
      role="status"
    >
      <AlertCircle size={18} />
      <span>{message}</span>
    </div>
  )
}
