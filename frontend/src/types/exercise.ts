import type { ReactNode } from "react"

export type ExerciseTypeDefinition = Readonly<{
  value: string
  label: string
  example: string
  metrics: readonly string[]
}>

export type CreateExerciseFormData = Readonly<{
  name: string
  exerciseType: string
  equipment: string
  imageFile: File | null
  imageUrl: string | null
  primaryMuscle: string | null
  secondaryMuscles: string[]
}>

export type CreateExerciseInitialValues = Readonly<{
  name?: string
  exerciseType?: string
  equipment?: string
  imageUrl?: string | null
  primaryMuscle?: string | null
  secondaryMuscles?: string[]
}>

export type CreateExerciseProps = Readonly<{
  isOpen: boolean
  onCancel: () => void
  onSave: (values: CreateExerciseFormData) => void
  initialValues?: CreateExerciseInitialValues
  exerciseTypes?: readonly string[]
  exerciseTypeOptions?: readonly ExerciseTypeDefinition[]
  equipmentOptions?: readonly string[]
}>

export type CreateExerciseBackdropProps = Readonly<{
  zIndexClassName: string
  backdropClassName: string
  onDismiss: () => void
  children: ReactNode
}>

