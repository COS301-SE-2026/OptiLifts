import { MUSCLE_GROUPS } from '@/constants/muscles'

export type MuscleName = (typeof MUSCLE_GROUPS)[number]

export type Workout = Readonly<{
  id: string
  name: string
  primaryMuscleGroups: MuscleName[]
  exerciseCount: number
  exercisePreview: string[]
  createdAt?: string
}>

export type MuscleGroup = Readonly<{
  id: string
  name: MuscleName
}>

export type SelectedWorkout = Readonly<{
  workout: Workout | null
  highlightedMuscles: MuscleName[]
}>

export type WorkoutSummary = Readonly<{
  workoutName: string
  totalExercises: number
  primaryMuscleGroups: MuscleName[]
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
  primaryMuscleGroups: MuscleName[]
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
  highlightedMuscles: MuscleName[]
}>

export type MuscleDigramProps = Readonly<{
  highlightedMuscles: MuscleName[]
  variant?: 'front' | 'back' | 'both'
}>
