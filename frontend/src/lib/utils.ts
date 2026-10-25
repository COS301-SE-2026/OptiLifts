import { useEffect, useRef } from "react"
import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function adaptImgUrl(url: string | null | undefined): string {
  if (!url) return '';
  
  if (url.includes('http://azurite:10000')) {  // NOSONAR ,aszurite is for local dev so we cannot use https... love sonarqube
    return url.replace('http://azurite:10000', 'http://127.0.0.1:10000');  // NOSONAR
  }
  
  return url;
}

export function useInterceptNavigation(enabled: boolean, onIntercept: (targetPath: string) => void) {
  const onInterceptRef = useRef(onIntercept)
  useEffect(() => {
    onInterceptRef.current = onIntercept
  }, [onIntercept])

  useEffect(() => {
    if (!enabled) {
      return
    }

    const interceptNavbar = (event: MouseEvent) => {
      if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
        return
      }

      const anchor = (event.target as HTMLElement)?.closest('a[href]') as HTMLAnchorElement | null
      if (!anchor || (anchor.target && anchor.target !== '_self')) {
        return
      }

      const url = new URL(anchor.href, window.location.origin)
      if (url.origin !== window.location.origin || url.pathname === window.location.pathname) {
        return
      }

      event.preventDefault()
      onInterceptRef.current(url.pathname + url.search)
    }

    document.addEventListener('click', interceptNavbar, true)
    return () => document.removeEventListener('click', interceptNavbar, true)
  }, [enabled])
}
