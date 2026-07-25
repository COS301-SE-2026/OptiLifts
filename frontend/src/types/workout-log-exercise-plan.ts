export type WorkoutLogExercisePlanItem = Readonly<{
  id: string
  name: string
  primaryMuscle: string
  exerciseType: string
  orderIndex: number
  sets: ReadonlyArray<{
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
}>

export type WorkoutLogExercisePlanProps = Readonly<{
  title?: string
  subtitle?: string
  exercises: WorkoutLogExercisePlanItem[]
  className?: string
  emptyState?: React.ReactNode
}>