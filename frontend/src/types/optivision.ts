export type VisionExercise = 'squat' | 'bench' | 'deadlift'

export type VisionLandmark = Readonly<{
  x: number
  y: number
  z: number
}>

export type VisionFrame = Readonly<{
  timestamp: number
  landmarks: readonly VisionLandmark[]
}>

//payload for POST /api/vision/analyze
export type AnalyseReq = Readonly<{
  userId: string
  exercise: VisionExercise
  view: 'side'
  frames: readonly VisionFrame[]
}>

//respone to payload (not in the plan yet - agree with Person 3)
export type AnalyseRes = Readonly<{
  jobId: string
}>

export type VisionJobStatus = 'pending' | 'processing' | 'completed' | 'failed'

//payload GET /api/vision/result/{jobId}
export type ResultRes = Readonly<{
  status: VisionJobStatus
  coach_summary?: string
  score?: number
  issues?: readonly string[]
}>

export type VisionJobState =
  | Readonly<{ phase: 'idle' }>
  | Readonly<{ phase: 'extracting'; progress: number }>
  | Readonly<{ phase: 'submitting' }>
  | Readonly<{ phase: 'polling' }>
  | Readonly<{ phase: 'completed'; coachSummary: string; score: number; issues: readonly string[] }>
  | Readonly<{ phase: 'failed'; message: string }>

export type ExerCheck = Readonly<{
  label: string
  tip: string
}>

export type ExerGuideData = Readonly<{
  name: string
  note: string
  gifUrl: string
  checks: readonly ExerCheck[]
}>
