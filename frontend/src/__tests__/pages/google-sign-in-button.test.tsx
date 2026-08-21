import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, cleanup, act } from '@testing-library/react'
import { GoogleSignInButton, getEffectiveGoogleTheme } from '@/pages/auth/GoogleSignInButton'

const mockLogin = vi.fn()
const mockNavigate = vi.fn()

vi.mock('@/context/auth-context', () => ({
  useAuth: () => ({
    login: mockLogin,
    isAuthenticated: false,
    isHydrated: true,
  }),
}))

vi.mock('react-router-dom', () => ({
  useNavigate: () => mockNavigate,
}))

describe('GoogleSignInButton', () => {
  let mockInitialize: ReturnType<typeof vi.fn>
  let mockRenderButton: ReturnType<typeof vi.fn>

  beforeEach(() => {
    vi.clearAllMocks()
    mockInitialize = vi.fn()
    mockRenderButton = vi.fn()

    window.google = {
      accounts: {
        id: {
          initialize: mockInitialize as unknown as NonNullable<typeof window.google>['accounts']['id']['initialize'],
          renderButton: mockRenderButton as unknown as NonNullable<typeof window.google>['accounts']['id']['renderButton'],
        },
      },
    }
  })

  afterEach(() => {
    cleanup()
    delete window.google
  })

  it('calculates effective theme correctly from document / storage', () => {
    expect(getEffectiveGoogleTheme('outline')).toBe('outline')
    expect(getEffectiveGoogleTheme('filled_blue')).toBe('filled_blue')
    expect(getEffectiveGoogleTheme('filled_black')).toBe('filled_black')
  })

  it('renders container and initializes GSI once on mount', () => {
    render(<GoogleSignInButton clientId="test-client-id-123" />)

    expect(screen.getByTestId('google-sign-in-button')).toBeDefined()
    expect(mockInitialize).toHaveBeenCalledTimes(1)
    expect(mockInitialize).toHaveBeenCalledWith(
      expect.objectContaining({
        client_id: 'test-client-id-123',
      })
    )
    expect(mockRenderButton).toHaveBeenCalledTimes(1)
  })

  it('does not re-call initialize if mounted multiple times with same clientId', () => {
    const { unmount } = render(<GoogleSignInButton clientId="test-client-id-same" />)
    expect(mockInitialize).toHaveBeenCalledTimes(1)

    unmount()

    render(<GoogleSignInButton clientId="test-client-id-same" />)
    // initialize should NOT be called again for the same client ID on the existing GSI object
    expect(mockInitialize).toHaveBeenCalledTimes(1)
    expect(mockRenderButton).toHaveBeenCalledTimes(2)
  })

  it('invokes onSuccess callback when credential is received', async () => {
    const onSuccess = vi.fn()
    render(<GoogleSignInButton clientId="test-client-id-cb" onSuccess={onSuccess} />)

    expect(mockInitialize).toHaveBeenCalledTimes(1)
    const initCall = mockInitialize.mock.calls[0][0]

    await act(async () => {
      initCall.callback({ credential: 'mock-jwt-token' })
    })

    expect(onSuccess).toHaveBeenCalledWith('mock-jwt-token')
  })
})
