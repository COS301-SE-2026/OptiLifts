import { useSyncExternalStore } from 'react'

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
