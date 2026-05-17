export type Workout = Readonly<{
  id: string
  name: string
  primaryMuscleGroups: string[]
  exerciseCount: number
  exercisePreview: string[] 
  createdAt?: string
}>

export type MuscleGroup = Readonly<{
  id: string
  name: string
  category?: 'upper' | 'lower' | 'core' //TODO: temporary muscle groups
}>

export type SelectedWorkout = Readonly<{
  workout: Workout | null
  highlightedMuscles: string[]
}>

export type WorkoutSummary = Readonly<{
  workoutName: string
  totalExercises: number
  primaryMuscleGroups: string[]
  estimatedDuration?: number 
}>

export type WorkoutResponse = Readonly<{
  data: Workout | Workout[]
  status: 'success' | 'error'
  message?: string
}>

export type MuscleGroupsResponse = Readonly<{
  data: MuscleGroup[]
  status: 'success' | 'error'
  message?: string
}>

export type WorkoutCardData = Readonly<{
  id: string
  name: string
  primaryMuscleGroups: string[]
  exerciseCount: number
  exercisePreview: string[]
}>

export type WorkoutListItemProps = Readonly<{
  workout: Workout
  isSelected: boolean
  onSelect: (workout: Workout) => void
  onEdit?: (workout: Workout) => void
  onDelete?: (workoutId: string) => void
}>

export type WorkoutSummaryProps = Readonly<{
  summary: WorkoutSummary | null
  highlightedMuscles: string[]
}>

export type MuscleDigramProps = Readonly<{
  highlightedMuscles: string[]
  variant?: 'front' | 'back' | 'both'
}>
