export type KnownExerciseTypeValue =
  | 'weight-reps'
  | 'bodyweight-reps'
  | 'weighted-bodyweight'
  | 'assisted-bodyweight'
  | 'duration'
  | 'duration-weight'
  | 'distance-duration'
  | 'weight-distance'


export type PlannedExerciseSet = Readonly<{
  reps: number | null
  weight: number | null
  duration: number | null
  distance: number | null
  restTime: string
}>

export type LoggedExerciseSet = Readonly<{
  reps: number
  weight: number
  rpe: number
}>