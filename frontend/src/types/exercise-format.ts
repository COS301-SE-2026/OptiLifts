export type KnownExerciseTypeValue =
  | 'WeightReps'
  | 'BodyweightReps'
  | 'WeightedBodyWeight'
  | 'AssistedWeightReps'
  | 'Duration'
  | 'DurationWeight'
  | 'DistanceDuration'
  | 'WeightDistance'


export type PlannedExerciseSet = Readonly<{
  reps: number | null
  weight: number | null
  duration: number | null
  distance: number | null
  restTime: string
}>

export type LoggedExerciseSet = Readonly<{
  type: string
  reps: number
  weight: number
  duration: number | null
  distance: number | null
  rpe: number
}>