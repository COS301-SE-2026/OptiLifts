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
  id?: string
  workoutExerciseId?: string
  exerciseId: string
  name: string
  primaryMuscle?: string
  muscleGroup?: string
  exerciseType?: string
  orderIndex: number
  sets: WorkoutDetailSet[]
  groupId?: string | null
  groupType?: string | null
  groupRestTime?: number | null
  imageUrl?: string | null
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