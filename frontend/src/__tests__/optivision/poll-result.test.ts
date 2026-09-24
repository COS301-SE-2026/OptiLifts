import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { POLL_INTERVAL_MS, POLL_TIMEOUT_MS } from '@/constants/optivision'
import { fetchResult } from '@/lib/optivision/api'
import { pollForSumm, VisionJobErr } from '@/lib/optivision/poll-result'

vi.mock('@/lib/optivision/api', () => ({ fetchResult: vi.fn() }))

const mockFetchRes = fetchResult as unknown as Mock

describe('pollForSummary', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('keeps polling until the job completes, then returns the summary', async () => {
    mockFetchRes
      .mockResolvedValueOnce({ status: 'processing' })
      .mockResolvedValueOnce({ status: 'completed', coach_summary: 'Great set' })

    const promise = pollForSumm('job-1', new AbortController().signal)
    await vi.advanceTimersByTimeAsync(POLL_INTERVAL_MS)

    await expect(promise).resolves.toEqual({ coachSummary: 'Great set', score: 100, issues: [] })
    expect(mockFetchRes).toHaveBeenCalledTimes(2)
  })

  it('throws a VisionJobError when the job fails', async () => {
    mockFetchRes.mockResolvedValue({ status: 'failed' })

    await expect(pollForSumm('job-1', new AbortController().signal)).rejects.toBeInstanceOf(VisionJobErr)
  })

  it('gives up after the timeout', async () => {
    mockFetchRes.mockResolvedValue({ status: 'processing' })

    const promise = pollForSumm('job-1', new AbortController().signal)
    const assrt = expect(promise).rejects.toBeInstanceOf(VisionJobErr)
    await vi.advanceTimersByTimeAsync(POLL_TIMEOUT_MS + POLL_INTERVAL_MS)

    await assrt
  })

  it('stops when aborted', async () => {
    mockFetchRes.mockResolvedValue({ status: 'processing' })
    const controller = new AbortController()

    const promise = pollForSumm('job-1', controller.signal)
    const assrt = expect(promise).rejects.toHaveProperty('name', 'AbortError')
    await vi.advanceTimersByTimeAsync(0)
    controller.abort()

    await assrt
  })
})
