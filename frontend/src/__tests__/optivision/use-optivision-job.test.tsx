import { act, cleanup, renderHook } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { useAuth } from '@/context/auth-context'
import { submitAnalysis } from '@/lib/optivision/api'
import { pollForSumm } from '@/lib/optivision/poll-result'
import { extractLandmarks, PoseExtractionErr } from '@/lib/optivision/pose-extractor'
import { useOptiVisJob } from '@/lib/optivision/use-optivision-job'
import type { VisionFrame } from '@/types/optivision'

vi.mock('@/context/auth-context', () => ({ useAuth: vi.fn() }))
vi.mock('@/lib/optivision/api', () => ({ submitAnalysis: vi.fn() }))
vi.mock('@/lib/optivision/pose-extractor', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/lib/optivision/pose-extractor')>()),
  extractLandmarks: vi.fn(),
}))
vi.mock('@/lib/optivision/poll-result', async (importOriginal) => ({
  ...(await importOriginal<typeof import('@/lib/optivision/poll-result')>()),
  pollForSumm: vi.fn(),
}))

const mockAuth = useAuth as unknown as Mock
const mockExtrct = extractLandmarks as unknown as Mock
const mockSubmt = submitAnalysis as unknown as Mock
const mockPoll = pollForSumm as unknown as Mock

const file = new File(['x'], 'set.mp4', { type: 'video/mp4' })
const frames: VisionFrame[] = [{ timestamp: 0, landmarks: [] }]

describe('useOptivisionJob', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    mockAuth.mockReturnValue({ user: { id: 'user-1' } })
  })

  afterEach(() => {
    cleanup()
  })

  it('runs the whole flow and completes with the coach summary', async () => {
    mockExtrct.mockResolvedValue(frames)
    mockSubmt.mockResolvedValue({ jobId: 'job-1' })
    mockPoll.mockResolvedValue({ coachSummary: 'Great depth', score: 100 })
    const { result } = renderHook(() => useOptiVisJob())

    await act(async () => {
      await result.current.start(file, 'squat')
    })

    expect(result.current.state).toEqual({ phase: 'completed', coachSummary: 'Great depth', score: 100 })
    expect(mockSubmt).toHaveBeenCalledWith(
      { userId: 'user-1', exercise: 'squat', view: 'side', frames },
      expect.any(AbortSignal),
    )
  })

  it('shows the message when the video cannot be read, and does not submit', async () => {
    mockExtrct.mockRejectedValue(new PoseExtractionErr('The video must be at least 2 seconds long.'))
    const { result } = renderHook(() => useOptiVisJob())

    await act(async () => {
      await result.current.start(file, 'squat')
    })

    expect(result.current.state).toEqual({
      phase: 'failed',
      message: 'The video must be at least 2 seconds long.',
    })
    expect(mockSubmt).not.toHaveBeenCalled()
  })

  it('fails when nobody is logged in', async () => {
    mockAuth.mockReturnValue({ user: null })
    const { result } = renderHook(() => useOptiVisJob())

    await act(async () => {
      await result.current.start(file, 'squat')
    })

    expect(result.current.state).toEqual({ phase: 'failed', message: 'Please log in again.' })
    expect(mockExtrct).not.toHaveBeenCalled()
  })

  it('aborts the running job and goes back to idle on reset', () => {
    let signal!: AbortSignal
    mockExtrct.mockImplementation((_file: File, _onProgress: unknown, abortSignal: AbortSignal) => {
      signal = abortSignal
      return new Promise(() => {})
    })
    const { result } = renderHook(() => useOptiVisJob())

    act(() => {
      void result.current.start(file, 'squat')
    })
    act(() => result.current.reset())

    expect(signal.aborted).toBe(true)
    expect(result.current.state).toEqual({ phase: 'idle' })
  })
})
