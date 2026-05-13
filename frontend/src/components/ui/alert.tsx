import { useEffect, useState } from 'react'
import { Info, CheckCircle2, AlertTriangle, AlertCircle, X } from 'lucide-react'

type Variant = 'info' | 'success' | 'warning' | 'error'

interface ToastItem {
  readonly id: number
  readonly variant: Variant
  readonly title?: string
  readonly message: string
}

const ICONS = {
  info:    Info,
  success: CheckCircle2,
  warning: AlertTriangle,
  error:   AlertCircle,
}

const VARIANT_STYLES: Record<Variant, { border: string; bg: string; icon: string }> = {
  info:    { border: 'var(--brand)',       bg: 'color-mix(in srgb, var(--brand)       8%, var(--surface))', icon: 'var(--brand)'       },
  success: { border: 'var(--success)',     bg: 'color-mix(in srgb, var(--success)     8%, var(--surface))', icon: 'var(--success)'     },
  warning: { border: 'var(--warning)',     bg: 'color-mix(in srgb, var(--warning)     8%, var(--surface))', icon: 'var(--warning)'     },
  error:   { border: 'var(--destructive)', bg: 'color-mix(in srgb, var(--destructive) 8%, var(--surface))', icon: 'var(--destructive)' },
}

let nextId = 0

export const toast = {
  info:    (message: string, title?: string) => emit('info',    message, title),
  success: (message: string, title?: string) => emit('success', message, title),
  warning: (message: string, title?: string) => emit('warning', message, title),
  error:   (message: string, title?: string) => emit('error',   message, title),
}

function emit(variant: Variant, message: string, title?: string) {
  globalThis.dispatchEvent(
    new CustomEvent<ToastItem>('ol-toast', {
      detail: { id: nextId++, variant, message, title },
    })
  )
}

export function Toaster() {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  const dismiss = (id: number) => setToasts(prev => prev.filter(t => t.id !== id))

  useEffect(() => {
    const handler = (e: Event) => {
      const item = (e as CustomEvent<ToastItem>).detail
      setToasts(prev => [...prev, item])
      setTimeout(() => dismiss(item.id), 5000)
    }
    globalThis.addEventListener('ol-toast', handler)
    return () => globalThis.removeEventListener('ol-toast', handler)
  }, [])

  return (
    <div
      className="fixed top-5 left-1/2 -translate-x-1/2 z-[999] flex flex-col gap-2 w-[calc(100%-2rem)] max-w-[420px] pointer-events-none"
      aria-live="polite"
    >
      {toasts.map(item => {
        const Icon = ICONS[item.variant]
        const vs = VARIANT_STYLES[item.variant]
        return (
          <output
            key={item.id}
            className="flex items-start gap-3 px-4 py-[0.875rem] border-l-4 shadow-[0_4px_20px_rgba(0,0,0,0.18)] pointer-events-auto animate-slide-down"
            style={{ borderColor: vs.border, background: vs.bg }}
          >
            <Icon className="flex-shrink-0 mt-[1px] w-4 h-4" style={{ color: vs.icon }} />
            <div className="flex-1 min-w-0">
              {item.title && (
                <p className="font-sans text-xs font-bold uppercase tracking-[1px] text-foreground m-0 mb-[0.2rem]">
                  {item.title}
                </p>
              )}
              <p className="font-sans text-sm text-muted-foreground m-0 leading-[1.4]">
                {item.message}
              </p>
            </div>
            <button
              className="flex-shrink-0 bg-none border-none cursor-pointer p-0 text-muted-foreground flex items-center hover:text-foreground"
              onClick={() => dismiss(item.id)}
              aria-label="Dismiss"
            >
              <X size={16} />
            </button>
          </output>
        )
      })}
    </div>
  )
}
