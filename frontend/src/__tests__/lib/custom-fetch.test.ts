import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { customFetch } from '@/lib/custom-fetch'

describe('customFetch', () => {
  const originalFetch = globalThis.fetch

  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    globalThis.fetch = originalFetch
  })

  it('dispatches rateLimitExceeded event when response status is 429', async () => {
    const mockHeaders = new Headers()
    mockHeaders.set('Retry-After', '45')

    const mockResponse = new Response(
      JSON.stringify({
        status: 429,
        title: 'Too Many Requests',
        detail: 'Rate limit exceeded. Please wait before trying again.',
      }),
      {
        status: 429,
        headers: mockHeaders,
      }
    )

    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse)
    const eventListener = vi.fn()
    globalThis.addEventListener('rateLimitExceeded', eventListener)

    const response = await customFetch('/api/workouts', { method: 'GET' })

    expect(response.status).toBe(429)
    expect(eventListener).toHaveBeenCalledTimes(1)
    const event = eventListener.mock.calls[0][0] as CustomEvent
    expect(event.detail).toEqual({
      retryAfter: 45,
      url: '/api/workouts',
    })

    globalThis.removeEventListener('rateLimitExceeded', eventListener)
  })

  it('defaults retryAfter to 60 when Retry-After header is missing on 429', async () => {
    const mockResponse = new Response(
      JSON.stringify({ status: 429 }),
      { status: 429 }
    )

    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse)
    const eventListener = vi.fn()
    globalThis.addEventListener('rateLimitExceeded', eventListener)

    const response = await customFetch('/api/exercises')

    expect(response.status).toBe(429)
    expect(eventListener).toHaveBeenCalledTimes(1)
    const event = eventListener.mock.calls[0][0] as CustomEvent
    expect(event.detail.retryAfter).toBe(60)

    globalThis.removeEventListener('rateLimitExceeded', eventListener)
  })

  it('returns normal responses for successful requests without dispatching rateLimitExceeded', async () => {
    const mockResponse = new Response(JSON.stringify({ ok: true }), { status: 200 })
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse)
    const eventListener = vi.fn()
    globalThis.addEventListener('rateLimitExceeded', eventListener)

    const response = await customFetch('/api/workouts')

    expect(response.status).toBe(200)
    expect(eventListener).not.toHaveBeenCalled()

    globalThis.removeEventListener('rateLimitExceeded', eventListener)
  })

  it('does not refresh token when 401 is received on auth endpoints', async () => {
    const mockResponse = new Response(JSON.stringify({ error: 'unauthorized' }), { status: 401 })
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse)

    const response = await customFetch('/api/auth/login')

    expect(response.status).toBe(401)
    expect(globalThis.fetch).toHaveBeenCalledTimes(1)
  })

  it('attempts to refresh token and retries request on 401', async () => {
    const unauthorizedResponse = new Response(JSON.stringify({ error: 'unauthorized' }), { status: 401 })
    const refreshOkResponse = new Response(JSON.stringify({ message: 'refreshed' }), { status: 200 })
    const successResponse = new Response(JSON.stringify({ data: 'success' }), { status: 200 })

    const fetchMock = vi.fn()
      .mockResolvedValueOnce(unauthorizedResponse)
      .mockResolvedValueOnce(refreshOkResponse)
      .mockResolvedValueOnce(successResponse)

    globalThis.fetch = fetchMock

    const response = await customFetch(new URL('https://example.com/api/workouts'))

    expect(response.status).toBe(200)
    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(fetchMock).toHaveBeenNthCalledWith(
      2,
      '/api/auth/refresh',
      expect.objectContaining({ method: 'POST', credentials: 'include' })
    )
  })

  it('dispatches logoutUser and throws error when token refresh fails on 401', async () => {
    const unauthorizedResponse = new Response(JSON.stringify({ error: 'unauthorized' }), { status: 401 })
    const refreshFailResponse = new Response(JSON.stringify({ error: 'failed' }), { status: 401 })

    const fetchMock = vi.fn()
      .mockResolvedValueOnce(unauthorizedResponse)
      .mockResolvedValueOnce(refreshFailResponse)

    globalThis.fetch = fetchMock

    const logoutListener = vi.fn()
    globalThis.addEventListener('logoutUser', logoutListener)

    await expect(customFetch('/api/workouts')).rejects.toThrow('Session expired')
    expect(logoutListener).toHaveBeenCalledTimes(1)

    globalThis.removeEventListener('logoutUser', logoutListener)
  })
})
