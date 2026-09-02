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
})
