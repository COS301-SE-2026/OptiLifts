import { useEffect, useMemo, useState, type ReactNode } from "react"
import { ArrowLeft, Check, ImagePlus, X } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

export type CreateExerciseFormData = Readonly<{
  name: string
  exerciseType: string
  equipment: string
  imageFile: File | null
  imageUrl: string | null
}>

type CreateExerciseInitialValues = Readonly<{
  name?: string
  exerciseType?: string
  equipment?: string
  imageUrl?: string | null
}>

export type ExerciseTypeOption = Readonly<{
  value: string
  label: string
  example: string
  metrics: readonly string[]
}>

type CreateExerciseProps = Readonly<{
  isOpen: boolean
  onCancel: () => void
  onSave: (values: CreateExerciseFormData) => void
  initialValues?: CreateExerciseInitialValues
  exerciseTypes?: readonly string[]
  exerciseTypeOptions?: readonly ExerciseTypeOption[]
  equipmentOptions?: readonly string[]
}>

type CreateExerciseBackdropProps = Readonly<{
  zIndexClassName: string
  backdropClassName: string
  onDismiss: () => void
  children: ReactNode
}>

const DEFAULT_EXERCISE_TYPE_OPTIONS: readonly ExerciseTypeOption[] = [
  {
    value: "weight-reps",
    label: "Weight & Reps",
    example: "Bench Press, Dumbbell Curls",
    metrics: ["REPS", "KG"],
  },
  {
    value: "bodyweight-reps",
    label: "Bodyweight Reps",
    example: "Pullups, Sit ups, Burpees",
    metrics: ["REPS"],
  },
  {
    value: "weighted-bodyweight",
    label: "Weighted Bodyweight",
    example: "Weighted Pull Ups, Weighted Dips",
    metrics: ["REPS", "+KG"],
  },
  {
    value: "assisted-bodyweight",
    label: "Assisted Bodyweight",
    example: "Assisted Pullups, Assisted Dips",
    metrics: ["REPS", "-KG"],
  },
  {
    value: "duration",
    label: "Duration",
    example: "Planks, Yoga, Stretching",
    metrics: ["TIME"],
  },
  {
    value: "duration-weight",
    label: "Duration & Weight",
    example: "Weighted Plank, Wall Sit",
    metrics: ["KG", "TIME"],
  },
  {
    value: "distance-duration",
    label: "Distance & Duration",
    example: "Running, Cycling, Rowing",
    metrics: ["TIME", "KM"],
  },
  {
    value: "weight-distance",
    label: "Weight & Distance",
    example: "Farmers walk, Suitcase Carry",
    metrics: ["KG", "KM"],
  },
] as const

const DEFAULT_EQUIPMENT_OPTIONS = [
  "None",
  "Dumbbell",
  "Barbell",
  "Kettlebell",
  "Machine",
  "Plate",
  "Resistance Band",
  "Suspension Band",
  "Other",
] as const

const ensureOption = (options: readonly string[], value: string): string[] => {
  if (!value || options.includes(value)) {
    return [...options]
  }

  return [value, ...options]
}

const toExerciseTypeOptions = (exerciseTypes: readonly string[]): ExerciseTypeOption[] =>
  exerciseTypes.map((type) => ({ value: type, label: type, example: "Custom exercise type", metrics: ["REPS"] }))

function CreateExerciseBackdrop({ zIndexClassName, backdropClassName, onDismiss, children }: CreateExerciseBackdropProps) {
  return (
    <div
      className={`fixed inset-0 ${zIndexClassName} flex items-start justify-center ${backdropClassName} p-4 pt-[13vh]`}
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onDismiss()
      }}
    >
      {children}
    </div>
  )
}

export function CreateExercise({
  isOpen,
  onCancel,
  onSave,
  initialValues,
  exerciseTypes,
  exerciseTypeOptions,
  equipmentOptions = DEFAULT_EQUIPMENT_OPTIONS,
}: CreateExerciseProps) {
  const resolvedExerciseTypeOptions = useMemo(() => {
    if (exerciseTypeOptions && exerciseTypeOptions.length > 0) return [...exerciseTypeOptions]
    if (exerciseTypes && exerciseTypes.length > 0) return toExerciseTypeOptions(exerciseTypes)
    return [...DEFAULT_EXERCISE_TYPE_OPTIONS]
  }, [exerciseTypeOptions, exerciseTypes])

  const [name, setName] = useState("")
  const [exerciseType, setExerciseType] = useState<string>(resolvedExerciseTypeOptions[0]?.value ?? "weight-reps")
  const [equipment, setEquipment] = useState<string>(equipmentOptions[0] ?? "None")
  const [selectedImageFile, setSelectedImageFile] = useState<File | null>(null)
  const [selectedImageUrl, setSelectedImageUrl] = useState<string | null>(null)
  const [isTypePickerOpen, setIsTypePickerOpen] = useState(false)

  const effectiveExerciseTypeOptions = useMemo(() => {
    if (!initialValues?.exerciseType) return resolvedExerciseTypeOptions

    const exists = resolvedExerciseTypeOptions.some((o) => o.value === initialValues.exerciseType)
    if (exists) return resolvedExerciseTypeOptions

    return [
      { value: initialValues.exerciseType, label: initialValues.exerciseType, example: "Previously selected type", metrics: ["REPS"] },
      ...resolvedExerciseTypeOptions,
    ]
  }, [initialValues, resolvedExerciseTypeOptions])

  const effectiveEquipmentOptions = useMemo(
    () => ensureOption(equipmentOptions, initialValues?.equipment ?? ""),
    [equipmentOptions, initialValues?.equipment],
  )

  useEffect(() => {
    if (!isOpen) return

    const t = setTimeout(() => {
      setName(initialValues?.name ?? "")
      setExerciseType(initialValues?.exerciseType ?? (effectiveExerciseTypeOptions[0]?.value ?? "weight-reps"))
      setEquipment(initialValues?.equipment ?? (effectiveEquipmentOptions[0] ?? "None"))
      setSelectedImageFile(null)
      setSelectedImageUrl(initialValues?.imageUrl ?? null)
      setIsTypePickerOpen(false)
    }, 0)

    return () => clearTimeout(t)
  }, [effectiveEquipmentOptions, effectiveExerciseTypeOptions, initialValues, isOpen])

  useEffect(() => {
    if (!isOpen) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        if (isTypePickerOpen) setIsTypePickerOpen(false)
        else onCancel()
      }
    }

    document.addEventListener("keydown", onKeyDown)
    return () => document.removeEventListener("keydown", onKeyDown)
  }, [isOpen, onCancel, isTypePickerOpen])

  useEffect(() => {
    const prevOverflow = document.body.style.overflow
    if (isOpen || isTypePickerOpen) {
      document.body.style.overflow = "hidden"
    }

    return () => {
      document.body.style.overflow = prevOverflow
    }
  }, [isOpen, isTypePickerOpen])

  useEffect(() => {
    if (!selectedImageFile) return

    const objectUrl = URL.createObjectURL(selectedImageFile)
    const t = setTimeout(() => setSelectedImageUrl(objectUrl), 0)

    return () => {
      clearTimeout(t)
      URL.revokeObjectURL(objectUrl)
    }
  }, [selectedImageFile])

  if (!isOpen) return null

  const selectedExerciseTypeLabel =
    effectiveExerciseTypeOptions.find((o) => o.value === exerciseType)?.label ?? "Select exercise type"

  const onImageChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const nextFile = event.target.files?.[0] ?? null
    setSelectedImageFile(nextFile)
    if (!nextFile && !selectedImageFile) setSelectedImageUrl(initialValues?.imageUrl ?? null)
  }

  const clearImage = () => {
    setSelectedImageFile(null)
    setSelectedImageUrl(null)
  }

  const handleSave = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    onSave({ name: name.trim(), exerciseType, equipment, imageFile: selectedImageFile, imageUrl: selectedImageUrl })
  }

  return (
    <>
      {!isTypePickerOpen && (
        <CreateExerciseBackdrop zIndexClassName="z-40" backdropClassName="bg-foreground/50" onDismiss={onCancel}>
          <form className="w-full max-w-xl rounded-xl border border-border bg-surface p-6 shadow-xl" onSubmit={handleSave} aria-label="Create custom exercise">
            <div className="mb-6 flex items-start justify-between gap-4">
              <div>
                <h2 className="text-3xl leading-none">Create Custom Exercise</h2>
              </div>
              <Button type="button" variant="icon" size="icon" aria-label="Close" onClick={onCancel}>
                <X size={16} />
              </Button>
            </div>

            <div className="grid gap-5">
              <label className="grid gap-2">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Exercise name</span>
                <Input value={name} onChange={(event) => setName(event.target.value)} placeholder="e.g. Seated Cable Row" required maxLength={80} />
              </label>

              <div className="grid gap-2">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Exercise image</span>
                <div className="rounded-lg border border-dashed border-border p-4">
                  <div className="flex items-center gap-4">
                    <div className="h-20 w-20 overflow-hidden rounded-lg border border-border bg-surface-2">
                      {selectedImageUrl ? <img src={selectedImageUrl} alt="Selected exercise" className="h-full w-full object-cover" /> : <div className="flex h-full w-full items-center justify-center text-muted-foreground"><ImagePlus size={20} /></div>}
                    </div>
                    <div className="flex flex-wrap items-center gap-3">
                      <label className="inline-flex cursor-pointer items-center justify-center rounded-[0.5rem] border border-border bg-surface-2 px-4 py-2 text-sm font-semibold uppercase tracking-[0.05em] transition-colors hover:bg-border">
                        {selectedImageUrl ? "Change image" : "Add image"}
                        <input type="file" accept="image/*" className="sr-only" onChange={onImageChange} />
                      </label>
                      {selectedImageUrl ? <Button type="button" variant="ghost" onClick={clearImage}>Remove</Button> : null}
                    </div>
                  </div>
                </div>
              </div>

              <div className="grid gap-2">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Exercise type</span>
                <Button type="button" variant="secondary" className="h-10 w-full justify-between px-3 normal-case tracking-normal" onClick={() => setIsTypePickerOpen(true)}>
                  <span>{selectedExerciseTypeLabel}</span>
                  <span className="text-xs text-muted-foreground">Change</span>
                </Button>
              </div>

              <label className="grid gap-2">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Required equipment</span>
                <select
                  value={equipment}
                  onChange={(event) => setEquipment(event.target.value)}
                  className="h-10 rounded-lg border border-input bg-surface text-foreground px-3 text-sm outline-none transition-colors focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50"
                >
                  {effectiveEquipmentOptions.map((option) => (
                    <option
                      key={option}
                      value={option}
                      className="bg-surface text-foreground"
                      style={{ backgroundColor: "var(--surface)", color: "var(--foreground)" }}
                    >
                      {option}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            <div className="mt-8 flex flex-wrap justify-end gap-3">
              <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
              <Button type="submit" disabled={!name.trim()}>Save Exercise</Button>
            </div>
          </form>
        </CreateExerciseBackdrop>
      )}

      {isTypePickerOpen ? (
        <CreateExerciseBackdrop zIndexClassName="z-[90]" backdropClassName="bg-foreground/95" onDismiss={() => setIsTypePickerOpen(false)}>
          <div className="flex max-h-[78dvh] w-full max-w-xl flex-col overflow-hidden rounded-xl border border-border bg-surface shadow-xl">
            <div className="relative flex h-14 items-center border-b border-border bg-surface px-4">
              <button type="button" onClick={() => setIsTypePickerOpen(false)} className="inline-flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-surface-2 hover:text-foreground" aria-label="Back">
                <ArrowLeft size={18} />
              </button>
              <p className="absolute left-1/2 -translate-x-1/2 text-sm font-semibold uppercase tracking-[0.06em]">Select Exercise Type</p>
            </div>

            <div className="overflow-y-auto px-4 py-3">
              {effectiveExerciseTypeOptions.map((option) => {
                const isSelected = option.value === exerciseType
                return (
                  <button
                    key={option.value}
                    type="button"
                    onClick={() => {
                      setExerciseType(option.value)
                      setIsTypePickerOpen(false)
                    }}
                    className={`mb-2 w-full rounded-xl border px-4 py-3 text-left transition-colors hover:bg-surface-2 ${
                      isSelected ? "border-brand bg-brand/5 ring-1 ring-brand/30" : "border-border bg-surface"
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-[1.03rem] text-foreground">{option.label}</p>
                        <p className="mt-1 text-sm text-muted-foreground">Example: {option.example}</p>
                      </div>
                      {isSelected ? <Check size={16} className="mt-0.5 text-brand" /> : null}
                    </div>

                    <div className="mt-2 flex flex-wrap gap-2">
                      {option.metrics.map((metric) => (
                        <span key={`${option.value}-${metric}`} className={`rounded-full border px-2.5 py-1 text-[0.66rem] font-semibold tracking-[0.08em] ${isSelected ? "border-brand bg-brand text-white" : "border-border bg-surface-2 text-foreground"}`}>
                          {metric}
                        </span>
                      ))}
                    </div>
                  </button>
                )
              })}
            </div>
          </div>
        </CreateExerciseBackdrop>
      ) : null}
    </>
  )
}
