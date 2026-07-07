export type ExercisePlanSet = Readonly<{
  label: string
  reps: number | null
  weight: number | null
  duration: number | null
  distance: number | null
  restTime: string
}>

export type ExercisePlanItem = Readonly<{
  name: string
  subtitle?: string
  exerciseType?: string
  sets?: ExercisePlanSet[]
  groupId?: string |null
  groupType?: string | null
  groupRestTime? : number | null
}>

export type ExercisePlanProps = Readonly<{
  title?: string
  subtitle?: string
  exercises: Array<string | ExercisePlanItem>
  className?: string
  emptyState?: React.ReactNode
}>