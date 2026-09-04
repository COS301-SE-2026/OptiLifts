import { describe, it, expect, vi, beforeEach } from 'vitest'
import { submitGoogleAuthRequest, mapBackendUserToAuthUser } from '@/pages/auth/auth-request'
import { customFetch } from '@/lib/custom-fetch'
import type { Mock } from 'vitest'

vi.mock('@/lib/custom-fetch', () => ({
  customFetch: vi.fn(),
}))

describe('submitGoogleAuthRequest and auth-request utilities', () => {
  const mockCustomFetch = customFetch as unknown as Mock
  const mockLogin = vi.fn()
  const mockNavigate = vi.fn()
  const mockSetErrorMessage = vi.fn()
  const mockSetIsSubmitting = vi.fn()

  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('maps backend user dto properly and sets local storage', () => {
    const userDto = {
      id: 'user-123',
      displayName: 'Test User',
      email: 'test@example.com',
      metric: true,
      lightTheme: false,
    }

    const authUser = mapBackendUserToAuthUser(userDto)
    expect(authUser.id).toBe('user-123')
    expect(authUser.name).toBe('Test User')
    expect(authUser.email).toBe('test@example.com')
    expect(authUser.metric).toBe(true)
    expect(authUser.lightTheme).toBe(false)
    expect(localStorage.getItem('theme')).toBe('dark')
    expect(localStorage.getItem('units')).toBe('metric')
  })

  it('submits Google ID token to /api/auth/google and logs in on success', async () => {
    const mockUserResponse = {
      id: 'user-google-1',
      displayName: 'Google User',
      email: 'google@example.com',
      metric: true,
      lightTheme: false,
    }

    mockCustomFetch.mockResolvedValue({
      ok: true,
      status: 200,
      json: vi.fn().mockResolvedValue(mockUserResponse),
    })

    await submitGoogleAuthRequest({
      idToken: 'valid-google-id-token',
      login: mockLogin,
      navigate: mockNavigate,
      fromPath: '/dashboard',
      setErrorMessage: mockSetErrorMessage,
      setIsSubmitting: mockSetIsSubmitting,
    })

    expect(mockCustomFetch).toHaveBeenCalledWith(
      '/api/auth/google',
      expect.objectContaining({
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ idToken: 'valid-google-id-token' }),
      })
    )

    expect(mockLogin).toHaveBeenCalledWith({
      user: expect.objectContaining({
        id: 'user-google-1',
        name: 'Google User',
        email: 'google@example.com',
      }),
    })

    expect(mockNavigate).toHaveBeenCalledWith('/dashboard', { replace: true })
  })

  it('handles 401 error response gracefully', async () => {
    mockCustomFetch.mockResolvedValue({
      ok: false,
      status: 401,
      json: vi.fn().mockResolvedValue({ title: 'Invalid Google token' }),
    })

    await submitGoogleAuthRequest({
      idToken: 'invalid-token',
      login: mockLogin,
      navigate: mockNavigate,
      fromPath: '/dashboard',
      setErrorMessage: mockSetErrorMessage,
      setIsSubmitting: mockSetIsSubmitting,
    })

    expect(mockLogin).not.toHaveBeenCalled()
    expect(mockNavigate).not.toHaveBeenCalled()
    expect(mockSetErrorMessage).toHaveBeenCalledWith('Google authentication failed. Please try again.')
  })

  it('handles 429 rate limit response with custom detail message', async () => {
    mockCustomFetch.mockResolvedValue({
      ok: false,
      status: 429,
      json: vi.fn().mockResolvedValue({
        status: 429,
        title: 'Too Many Requests',
        detail: 'Rate limit exceeded. Please wait before trying again.',
      }),
    })

    await submitGoogleAuthRequest({
      idToken: 'some-token',
      login: mockLogin,
      navigate: mockNavigate,
      fromPath: '/dashboard',
      setErrorMessage: mockSetErrorMessage,
      setIsSubmitting: mockSetIsSubmitting,
    })

    expect(mockLogin).not.toHaveBeenCalled()
    expect(mockNavigate).not.toHaveBeenCalled()
    expect(mockSetErrorMessage).toHaveBeenCalledWith('Rate limit exceeded. Please wait before trying again.')
  })
})
