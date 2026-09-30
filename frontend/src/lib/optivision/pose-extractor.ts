import type { PoseLandmarker, PoseLandmarkerResult } from '@mediapipe/tasks-vision'
import { MAX_VIDEO_SECONDS, MIN_DETECTED_RATIO,
  MIN_VIDEO_SECONDS, POSE_LANDMARK_COUNT, POSE_MODEL_PATH,
  SAMPLE_FPS, VIDEO_EVENT_TIMEOUT_MS, WASM_PATH, } from '@/constants/optivision'
import type { VisionFrame, VisionLandmark } from '@/types/optivision'

export class PoseExtractionErr extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'PoseExtractionError'
  }
}

const EMPTY_LANDMARKS: readonly VisionLandmark[] = Array.from(
  { length: POSE_LANDMARK_COUNT },
  () => ({ x: 0, y: 0, z: 0 }),
)

const round4digits = (value: number) => Math.round(value * 10000) / 10000

function waitForEvent(video: HTMLVideoElement, eventName: 'loadeddata' | 'seeked'): Promise<void> {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(
      () => finish(new PoseExtractionErr('The video has timed out. Try a shorter version or different video.')),
      VIDEO_EVENT_TIMEOUT_MS,
    )

    function finish(error?: Error) {
      clearTimeout(timer)
      video.removeEventListener(eventName, onDone)
      video.removeEventListener('error', onError)
      if (error) {
        reject(error)
      } else {
        resolve()
      }
    }

    function onDone() {
      finish()
    }

    function onError() {
      finish(new PoseExtractionErr("We couldn't read your video. Try using an MP4 or MOV."))
    }

    video.addEventListener(eventName, onDone)
    video.addEventListener('error', onError)
  })
}

function createVideoElement(): HTMLVideoElement {
  const vid = document.createElement('video')
  vid.muted = true
  vid.playsInline = true
  vid.preload = 'auto'
  return vid
}

function assertDurationAllowed(video: HTMLVideoElement) {
  const { duration: dur } = video
  if (!Number.isFinite(dur) || dur < MIN_VIDEO_SECONDS) {
    throw new PoseExtractionErr(`Your video must be at least ${MIN_VIDEO_SECONDS} seconds long.`)
  }
  if (dur > MAX_VIDEO_SECONDS) {
    throw new PoseExtractionErr(`Your video must be at most ${MAX_VIDEO_SECONDS} seconds long.`)
  }
}

async function seekTo(video: HTMLVideoElement, seconds: number): Promise<void> {
  if (Math.abs(video.currentTime - seconds) < 0.001) {
    return
  }
  const seeked = waitForEvent(video, 'seeked')
  video.currentTime = seconds
  await seeked
}

async function createLandmarker(): Promise<PoseLandmarker> {
  const mediaPipe = await import('@mediapipe/tasks-vision')
  const file = await mediaPipe.FilesetResolver.forVisionTasks(WASM_PATH)

  const build = (delegate: 'GPU' | 'CPU') =>
    mediaPipe.PoseLandmarker.createFromOptions(file, {
      baseOptions: { modelAssetPath: POSE_MODEL_PATH, delegate },
      runningMode: 'VIDEO',
      numPoses: 1,
      minPoseDetectionConfidence: 0.5,
      minPosePresenceConfidence: 0.5,
      minTrackingConfidence: 0.5,
    })

  try {
    return await build('GPU')
  } 
  catch {
    return build('CPU')
  }
}

function toLandmarks(result: PoseLandmarkerResult): { landmarks: readonly VisionLandmark[]; detected: boolean } {
  if (result.landmarks.length === 0) {
    return { landmarks: EMPTY_LANDMARKS, detected: false }
  }

  const landmrks = result.landmarks[0].map(({ x, y, z }) => ({ x: round4digits(x), y: round4digits(y), z: round4digits(z) }))
  return { landmarks: landmrks, detected: true }
}

function assertEnoughDetects(detectedCount: number, totalFrames: number) {
  if (detectedCount / totalFrames < MIN_DETECTED_RATIO) {
    throw new PoseExtractionErr(
      "We couldn't proper see you. Film from the side with your entire body in frame.",
    )
  }
}

async function readFrames(
  video: HTMLVideoElement,
  landmarker: PoseLandmarker,
  onProgress: (fraction: number) => void,
  signal?: AbortSignal,
): Promise<VisionFrame[]> {
  const totalFrames = Math.floor(video.duration * SAMPLE_FPS)
  const frames: VisionFrame[] = []
  let detectedCount = 0

  for (let i = 0; i < totalFrames; i++) {
    signal?.throwIfAborted()

    const secs = i / SAMPLE_FPS
    await seekTo(video, secs)

    const res = landmarker.detectForVideo(video, Math.round(secs * 1000))
    const { landmarks, detected } = toLandmarks(res)

    if (detected) {
      detectedCount++
    }

    frames.push({ timestamp: round4digits(secs), landmarks })
    onProgress((i + 1) / totalFrames)
  }

  assertEnoughDetects(detectedCount, totalFrames)
  return frames
}

export async function extractLandmarks(
  file: File,
  onProgress: (fraction: number) => void,
  signal?: AbortSignal,
): Promise<VisionFrame[]> {
  const url = URL.createObjectURL(file)
  const video = createVideoElement()
  let landmarker: PoseLandmarker | undefined

  try {
    const getloaded = waitForEvent(video, 'loadeddata')
    video.src = url
    await getloaded

    assertDurationAllowed(video)
    landmarker = await createLandmarker()
    return await readFrames(video, landmarker, onProgress, signal)
  } 
  finally {
    landmarker?.close()
    video.removeAttribute('src')
    video.load()
    URL.revokeObjectURL(url)
  }
}
