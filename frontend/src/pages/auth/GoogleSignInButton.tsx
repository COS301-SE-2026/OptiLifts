import { useEffect, useRef, useState, type Dispatch, type SetStateAction } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/context/auth-context'
import { submitGoogleAuthRequest } from './auth-request'

export type GoogleSignInTheme = 'outline' | 'filled_blue' | 'filled_black'

export type GoogleSignInButtonProps = Readonly<{
  clientId?: string
  text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin'
  theme?: GoogleSignInTheme
  size?: 'large' | 'medium' | 'small'
  shape?: 'rectangular' | 'pill' | 'circle' | 'square'
  width?: number | string
  fromPath?: string
  setErrorMessage?: Dispatch<SetStateAction<string | null>>
  setIsSubmitting?: Dispatch<SetStateAction<boolean>>
  onSuccess?: (idToken: string) => void
  onError?: (error: unknown) => void
  className?: string
}>

declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string
            callback: (response: { credential: string }) => void
            auto_select?: boolean
            cancel_on_tap_outside?: boolean
          }) => void
          renderButton: (
            parent: HTMLElement,
            options: {
              type?: 'standard' | 'icon'
              theme?: GoogleSignInTheme
              size?: 'large' | 'medium' | 'small'
              text?: 'signin_with' | 'signup_with' | 'continue_with' | 'signin'
              shape?: 'rectangular' | 'pill' | 'circle' | 'square'
              logo_alignment?: 'left' | 'center'
              width?: number | string
              locale?: string
            }
          ) => void
          prompt?: () => void
        }
      }
    }
  }
}

export function getEffectiveGoogleTheme(themeProp?: GoogleSignInTheme): GoogleSignInTheme {
  if (themeProp) {
    return themeProp
  }

  if (typeof document !== 'undefined') {
    if (document.documentElement.classList.contains('dark')) {
      return 'filled_black'
    }
    const storedTheme = typeof localStorage !== 'undefined' ? localStorage.getItem('theme') : null
    if (storedTheme === 'dark') {
      return 'filled_black'
    }
    if (storedTheme === 'light') {
      return 'outline'
    }
    if (typeof window !== 'undefined' && window.matchMedia?.('(prefers-color-scheme: dark)').matches) {
      return 'filled_black'
    }
  }

  return 'filled_black'
}

type GoogleCredentialHandler = (response: { credential: string }) => void | Promise<void>

const activeHandlers = new Set<GoogleCredentialHandler>()

function ensureGoogleInitialized(clientId: string) {
  if (!window.google?.accounts?.id || !clientId) return false

  const gsi = window.google.accounts.id as typeof window.google.accounts.id & {
    _optilifts_initialized_client_id?: string
  }

  if (gsi._optilifts_initialized_client_id !== clientId) {
    gsi.initialize({
      client_id: clientId,
      callback: (response: { credential: string }) => {
        for (const handler of activeHandlers) {
          try {
            void handler(response)
          } catch (err) {
            console.error('Error in Google credential handler:', err)
          }
        }
      },
    })
    gsi._optilifts_initialized_client_id = clientId
  }
  return true
}

export function GoogleSignInButton({
  clientId,
  text = 'signin_with',
  theme,
  size = 'large',
  shape = 'rectangular',
  width = 320,
  fromPath = '/dashboard',
  setErrorMessage,
  setIsSubmitting,
  onSuccess,
  className = '',
}: GoogleSignInButtonProps) {
  const { login } = useAuth()
  const navigate = useNavigate()
  const containerRef = useRef<HTMLDivElement>(null)
  const [detectedTheme, setDetectedTheme] = useState<GoogleSignInTheme>(() =>
    getEffectiveGoogleTheme()
  )

  useEffect(() => {
    if (theme) return

    if (typeof MutationObserver === 'undefined' || typeof document === 'undefined') return

    const observer = new MutationObserver(() => {
      setDetectedTheme(getEffectiveGoogleTheme())
    })

    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ['class'],
    })

    return () => observer.disconnect()
  }, [theme])

  const resolvedTheme = theme ?? detectedTheme

  const effectiveClientId =
    clientId ??
    (import.meta.env.VITE_GOOGLE_CLIENT_ID as string | undefined) ??
    (import.meta.env.GOOGLE_CLIENT_ID as string | undefined) ??
    ''

  const handleCredentialResponse = useRef<GoogleCredentialHandler>(() => {})
  handleCredentialResponse.current = async (response: { credential: string }) => {
    if (!response?.credential) {
      setErrorMessage?.('Google Sign-In failed: No credential returned.')
      return
    }

    if (onSuccess) {
      onSuccess(response.credential)
      return
    }

    await submitGoogleAuthRequest({
      idToken: response.credential,
      login,
      navigate,
      fromPath,
      setErrorMessage: setErrorMessage ?? (() => {}),
      setIsSubmitting: setIsSubmitting ?? (() => {}),
    })
  }

  useEffect(() => {
    const handler: GoogleCredentialHandler = (res) => handleCredentialResponse.current(res)
    activeHandlers.add(handler)
    return () => {
      activeHandlers.delete(handler)
    }
  }, [])

  useEffect(() => {
    if (!effectiveClientId) {
      return
    }

    let checkInterval: ReturnType<typeof setInterval> | null = null
    let attempts = 0
    const maxAttempts = 25

    function renderGoogleButton() {
      if (!window.google?.accounts?.id || !containerRef.current || !effectiveClientId) {
        return false
      }

      ensureGoogleInitialized(effectiveClientId)

      containerRef.current.innerHTML = ''
      window.google.accounts.id.renderButton(containerRef.current, {
        type: 'standard',
        theme: resolvedTheme,
        size,
        text,
        shape,
        width,
        logo_alignment: 'left',
      })

      return true
    }

    if (!renderGoogleButton()) {
      checkInterval = setInterval(() => {
        attempts++
        if (renderGoogleButton() || attempts >= maxAttempts) {
          if (checkInterval) {
            clearInterval(checkInterval)
            checkInterval = null
          }
        }
      }, 200)
    }

    return () => {
      if (checkInterval) clearInterval(checkInterval)
    }
  }, [effectiveClientId, resolvedTheme, shape, size, text, width])

  return (
    <div className={`google-signin-wrapper flex justify-center w-full min-h-[44px] ${className}`}>
      <div ref={containerRef} data-testid="google-sign-in-button" className="flex justify-center w-full" />
    </div>
  )
}
