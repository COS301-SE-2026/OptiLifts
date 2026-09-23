import { afterEach, describe, expect, it, vi, type Mock } from 'vitest'
import type { AnalyseReq } from '@/types/optivision'

vi.mock('@/lib/custom-fetch', () => ({ customFetch: vi.fn() }))

async function loadTheApiBud() {
  vi.resetModules()
  vi.stubEnv('VITE_OPTIVISION_MOCK', 'false')
  const api = await import('@/lib/optivision/api')
  const { customFetch } = await import('@/lib/custom-fetch')
  return { ...api, customFetch: customFetch as unknown as Mock }
}

const req: AnalyseReq = {
  userId: 'user-1',
  exercise: 'squat',
  view: 'side',
  frames: [{ timestamp: 0, landmarks: [{ x: 0.1, y: 0.2, z: 0.3 }] }],
}

afterEach(() => {
  vi.unstubAllEnvs()
  vi.clearAllMocks()
})


describe('optivision api', () => {
  it('posts the payload as json and returns the job id', async () => {
    const { submitAnalysis, customFetch } = await loadTheApiBud()
    customFetch.mockResolvedValue({ ok: true, json: async () => ({ jobId: 'job-1' }) })

    expect(await submitAnalysis(req)).toEqual({ jobId: 'job-1' })

    const [url, init] = customFetch.mock.calls[0]
    expect(url).toBe('/api/vision/analyze')
    expect(init.method).toBe('POST')
    expect(JSON.parse(init.body)).toEqual(req)
  })

  it('throws when submitting fails', async () => {
    const { submitAnalysis, customFetch } = await loadTheApiBud()
    customFetch.mockResolvedValue({ ok: false, status: 500 })

    await expect(submitAnalysis(req)).rejects.toThrow('500')
  })

  it('fetches the result for a job id, url-encoded', async () => {
    const { fetchResult, customFetch } = await loadTheApiBud()
    customFetch.mockResolvedValue({ ok: true, json: async () => ({ status: 'processing' }) })

    expect(await fetchResult('a/b')).toEqual({ status: 'processing' })
    expect(customFetch.mock.calls[0][0]).toBe('/api/vision/result/a%2Fb')
  })

  it('throws when the result request fails', async () => {
    const { fetchResult, customFetch } = await loadTheApiBud()
    customFetch.mockResolvedValue({ ok: false, status: 404 })

    await expect(fetchResult('job-1')).rejects.toThrow('404')
  })
})
