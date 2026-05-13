import { useEffect, useState } from 'react'
import { Info, CheckCircle2, AlertTriangle, AlertCircle, X } from 'lucide-react'
import s from './alert.module.css'

type Variant = 'info' | 'success' | 'warning' | 'error'

interface ToastItem {
  id: number
  variant: Variant
  title?: string
  message: string
}

const ICONS = {
  info:    Info,
  success: CheckCircle2,
  warning: AlertTriangle,
  error:   AlertCircle,
}

let nextId = 0

// Call anywhere — no provider needed
export const toast = {
  info:    (message: string, title?: string) => emit('info',    message, title),
  success: (message: string, title?: string) => emit('success', message, title),
  warning: (message: string, title?: string) => emit('warning', message, title),
  error:   (message: string, title?: string) => emit('error',   message, title),
}

function emit(variant: Variant, message: string, title?: string) {
  window.dispatchEvent(
    new CustomEvent<ToastItem>('ol-toast', {
      detail: { id: nextId++, variant, message, title },
    })
  )
}

// Drop <Toaster /> once in your app root
export function Toaster() {
  const [toasts, setToasts] = useState<ToastItem[]>([])

  useEffect(() => {
    const handler = (e: Event) => {
      const item = (e as CustomEvent<ToastItem>).detail
      setToasts(prev => [...prev, item])
      setTimeout(() => dismiss(item.id), 5000)
    }
    window.addEventListener('ol-toast', handler)
    return () => window.removeEventListener('ol-toast', handler)
  }, [])

  const dismiss = (id: number) => setToasts(prev => prev.filter(t => t.id !== id))

  return (
    <div className={s.container} aria-live="polite">
      {toasts.map(item => {
        const Icon = ICONS[item.variant]
        return (
          <div key={item.id} className={`${s.toast} ${s[item.variant]}`} role="status">
            <Icon className={`${s.icon} ${s[item.variant]}`} />
            <div className={s.body}>
              {item.title && <p className={s.title}>{item.title}</p>}
              <p className={s.message}>{item.message}</p>
            </div>
            <button className={s.dismiss} onClick={() => dismiss(item.id)} aria-label="Dismiss">
              <X size={16} />
            </button>
          </div>
        )
      })}
    </div>
  )
}
