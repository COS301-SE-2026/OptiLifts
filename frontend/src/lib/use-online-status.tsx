import { useSyncExternalStore } from 'react'
import type { ReactNode } from 'react'

const subscribe = (onStoreChange: () => void) => {
  window.addEventListener('online', onStoreChange)
  window.addEventListener('offline', onStoreChange)

  return () => {
    window.removeEventListener('online', onStoreChange)
    window.removeEventListener('offline', onStoreChange)
  }
}

export function useOnlineStatus(): boolean {
  return useSyncExternalStore(subscribe, () => navigator.onLine, () => true)
}

export const OFFLINE_HINT = 'Unavailable offline'

export function OfflineTooltip({ isOnline, className, children }: { readonly isOnline: boolean; readonly className?: string; readonly children: ReactNode }) {
  if (isOnline) {
    return <>{children}</>
  }

  return <span title={OFFLINE_HINT} className={`inline-flex ${className ?? ''}`}>{children}</span>
}
