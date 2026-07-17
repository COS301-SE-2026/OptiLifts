import type { MuscleName } from './workout'

export type SetType = 'W' | 'I' | 'D'

export type ExerciseSet = Readonly<{
  id: string
  type: SetType
  kg: number | ''
  reps: number | ''
  time?: number | ''
  distance?: number | ''
}>

export type WorkoutExercise = Readonly<{
  id: string
  name: string
  muscle: MuscleName  
  sets: ExerciseSet[]
  imageUrl?: string
  exerciseType?: string
}>

export type ExerciseCatalogItem = Readonly<{
  id: string
  name: string
  muscle: MuscleName
  imageUrl?: string
  equipment?: string
}>
