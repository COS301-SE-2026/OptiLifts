export type WorkoutDetailSet = Readonly<{
  id: string
  type: string
  reps: number | null
  weight: number | null
  duration: number | null
  distance: number | null
  orderIndex: number
  restTime: number
}>

export type WorkoutDetailExercise = Readonly<{
  id: string
  exerciseId: string
  name: string
  primaryMuscle: string
  exerciseType: string
  orderIndex: number
  sets: WorkoutDetailSet[]
}>

export type WorkoutDetailResponse = Readonly<{
  id: string
  name: string
  folderId: string | null
  dayIndex: number | null
  createdAt: string
  primaryMuscleGroups: string[]
  exercisePreview: string[]
  exercises: WorkoutDetailExercise[]
}>