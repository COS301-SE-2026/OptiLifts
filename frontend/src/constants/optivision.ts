import type { VisionExercise } from '@/types/optivision'

export const VISION_EXERCISES: readonly { value: VisionExercise; label: string }[] = [
  { value: 'squat', label: 'Squat' },
  { value: 'bench', label: 'Bench Press' },
  { value: 'deadlift', label: 'Deadlift' },
]

//pose extraction asse
export const WASM_PATH = '/mediapipe/wasm'
export const POSE_MODEL_PATH = '/mediapipe/models/pose_landmarker_full.task'
export const POSE_LANDMARK_COUNT = 33
export const VIDEO_EVENT_TIMEOUT_MS = 10_000

//pose extraction
export const SAMPLE_FPS = 30
export const MIN_DETECTED_RATIO = 0.6

//upload validation
export const MIN_VIDEO_SECONDS = 2
export const MAX_VIDEO_SECONDS = 60
export const MAX_VIDEO_BYTES = 200 * 1024 * 1024

//polling
export const POLL_INTERVAL_MS = 2000
export const POLL_TIMEOUT_MS = 3 * 60 * 1000
