export type WorkoutLogDetailSet = Readonly<{
  id: string
  setId: string | null
  type: string
  reps: number
  weight: number
  orderIndex: number
  duration: number | null
  distance: number | null
  restTime: number
  groupNumber: number
  rpe: number
}>

export type WorkoutLogDetailExercise = Readonly<{
  id: string
  exerciseId: string
  name: string
  primaryMuscle: string
  exerciseType: string
  orderIndex: number
  sets: WorkoutLogDetailSet[]
}>

export type WorkoutLogDetailResponse = Readonly<{
  workoutId: string
  logId: string
  name: string
  folderId: string | null
  dayIndex: number | null
  createdAt: string
  completedAt: string | null
  duration: string | null
  primaryMuscleGroups: string[]
  exercisePreview: string[]
  exercises: WorkoutLogDetailExercise[]
}>