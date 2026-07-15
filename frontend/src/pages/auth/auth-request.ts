import type { Dispatch, SetStateAction } from 'react'
import type { NavigateFunction } from 'react-router-dom'
import type { AuthSession, AuthUser } from '@/context/auth-context'
import { customFetch } from '@/lib/custom-fetch'

type BackendUserDto = Readonly<{
  id: string
  displayName: string
  email: string
  metric: boolean
  lightTheme: boolean
}>

type SubmitAuthRequestArgs = Readonly<{
  endpoint: '/api/auth/login' | '/api/auth/register'
  body: unknown
  login: (session: AuthSession) => void
  navigate: NavigateFunction
  fromPath: string
  setErrorMessage: Dispatch<SetStateAction<string | null>>
  setIsSubmitting: Dispatch<SetStateAction<boolean>>
  fallbackErrorMessage: string
  conflictErrorMessage?: string
  unauthorizedErrorMessage?: string
}>

export function mapBackendUserToAuthUser(user: BackendUserDto): AuthUser {
  const theme = user.lightTheme ? 'light' : 'dark'
  localStorage.setItem('theme', theme)
  if (user.lightTheme) {
    document.documentElement.classList.remove('dark')
  } else {
    document.documentElement.classList.add('dark')
  }

  localStorage.setItem('units', user.metric ? 'metric' : 'imperial')

  return { id: user.id, name: user.displayName, email: user.email, metric: user.metric, lightTheme: user.lightTheme }
}

export async function submitAuthRequest({
  endpoint,
  body,
  login,
  navigate,
  fromPath,
  setErrorMessage,
  setIsSubmitting,
  fallbackErrorMessage,
  conflictErrorMessage,
  unauthorizedErrorMessage,
}: SubmitAuthRequestArgs) {
  setIsSubmitting(true)
  setErrorMessage(null)

  try {
    const res = await customFetch(endpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    })

    if (!res.ok) {
      if (res.status === 401 && unauthorizedErrorMessage) {
        setErrorMessage(unauthorizedErrorMessage)
      } else if (res.status === 409 && conflictErrorMessage) {
        setErrorMessage(conflictErrorMessage)
      } else {
        const payload = await res.json().catch(() => null)
        setErrorMessage(payload?.title ?? fallbackErrorMessage)
      }

      return
    }

    const data = await res.json() as BackendUserDto

    login({
      user: mapBackendUserToAuthUser(data),
    })

    navigate(fromPath, { replace: true })
  } catch (error) {
    setErrorMessage(error instanceof Error ? error.message : 'Network error - please try again.')
  } finally {
    setIsSubmitting(false)
  }
}