import { PoseLandmarker } from '@mediapipe/tasks-vision'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import { SAMPLE_FPS } from '@/constants/optivision'
import { extractLandmarks, PoseExtractionErr } from '@/lib/optivision/pose-extractor'

vi.mock('@mediapipe/tasks-vision', () => ({
  FilesetResolver: { forVisionTasks: vi.fn().mockResolvedValue({}) },
  PoseLandmarker: { createFromOptions: vi.fn() },
}))

class FakeVid extends EventTarget {
  duration = 2
  muted = false
  playsInline = false
  preload = ''
  source = ''
  private time = 0

  get currentTime() {
    return this.time
  }

  set currentTime(value: number) {
    this.time = value
    queueMicrotask(() => this.dispatchEvent(new Event('seeked')))
  }

  set src(value: string) {
    this.source = value
    queueMicrotask(() => this.dispatchEvent(new Event('loadeddata')))
  }

  removeAttribute() {}
  load() {}
}

const pose = Array.from({ length: 33 }, () => ({ x: 0.123456, y: 0.5, z: -0.25, visibility: 1 }))
const file = new File(['x'], 'set.mp4', { type: 'video/mp4' })

describe('extractLandmarks', () => {
  const detectVid = vi.fn()
  const close = vi.fn()

  beforeEach(() => {
    vi.resetAllMocks()
    detectVid.mockReturnValue({ landmarks: [pose] })
    ;(PoseLandmarker.createFromOptions as unknown as Mock).mockResolvedValue({ detectForVideo: detectVid, close })

    const realVidMake = document.createElement.bind(document)
    const video = new FakeVid()
    vi.spyOn(document, 'createElement').mockImplementation(((tag: string) =>
      tag === 'video' ? video : realVidMake(tag)) as typeof document.createElement)
    URL.createObjectURL = vi.fn(() => 'blob:fake')
    URL.revokeObjectURL = vi.fn()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('returns one frame per sample with 33 rounded landmarks', async () => {
    const onProgress = vi.fn()

    const frames = await extractLandmarks(file, onProgress)

    expect(frames).toHaveLength(2 * SAMPLE_FPS)
    expect(frames[0].landmarks).toHaveLength(33)
    expect(frames[0].landmarks[0]).toEqual({ x: 0.1235, y: 0.5, z: -0.25 })
    expect(frames[1].timestamp).toBeGreaterThan(frames[0].timestamp)
    expect(onProgress).toHaveBeenLastCalledWith(1)
  })

  it('fills a frame with no person with zeros so the timeline keeps its length', async () => {
    detectVid.mockReturnValueOnce({ landmarks: [] })

    const frames = await extractLandmarks(file, vi.fn())

    expect(frames[0].landmarks).toHaveLength(33)
    expect(frames[0].landmarks.every((point) => point.x === 0 && point.y === 0 && point.z === 0)).toBe(true)
  })

  it('rejects when the person is found in too few frames, and cleans up', async () => {
    detectVid.mockReturnValue({ landmarks: [] })

    await expect(extractLandmarks(file, vi.fn())).rejects.toBeInstanceOf(PoseExtractionErr)
    expect(close).toHaveBeenCalled()
    expect(URL.revokeObjectURL).toHaveBeenCalled()
  })
})
