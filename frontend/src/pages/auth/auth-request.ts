import type { Dispatch, SetStateAction } from 'react'
import type { NavigateFunction } from 'react-router-dom'
import type { AuthSession, AuthUser } from '@/context/auth-context'

type BackendUserDto = Readonly<{
  id: string
  displayName: string
  email: string
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
  return { id: user.id, name: user.displayName, email: user.email }
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
    const res = await fetch(endpoint, {
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

    const data = await res.json()

    login({
      token: data.token,
      user: mapBackendUserToAuthUser(data.user),
    })

    navigate(fromPath, { replace: true })
  } catch (error) {
    setErrorMessage(error instanceof Error ? error.message : 'Network error - please try again.')
  } finally {
    setIsSubmitting(false)
  }
}