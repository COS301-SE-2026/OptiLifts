import { useEffect, useMemo, useState } from "react"
import { ArrowLeft, Check, ImagePlus, Search, X } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { useAuth } from "@/context/auth-context"
import { MUSCLE_GROUPS } from "@/constants/muscles"
import { DEFAULT_EXERCISE_TYPE_OPTIONS } from "@/constants/exercise-type-definitions"
import { DEFAULT_EQUIPMENT_OPTIONS } from "@/constants/equipment"
import type { 
  ExerciseTypeDefinition, 
  CreateExerciseProps,
  CreateExerciseBackdropProps
} from "@/types/exercise"

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

const ensureOption = (options: readonly string[], value: string): string[] => {
  if (!value || options.includes(value)) {
    return [...options]
  }

  return [value, ...options]
}

const toExerciseTypeOptions = (exerciseTypes: readonly string[]): ExerciseTypeDefinition[] =>
  exerciseTypes.map((type) => ({ value: type, label: type, example: "Custom exercise type", metrics: ["REPS"] }))

function CreateExerciseBackdrop({ zIndexClassName, backdropClassName, onDismiss, children }: CreateExerciseBackdropProps) {
  return (
    <div className={`fixed inset-0 ${zIndexClassName} flex items-start justify-center p-4 pt-[11vh]`}>
      <button
        type="button"
        className={`absolute inset-0 block w-full outline-none cursor-default ${backdropClassName}`}
        aria-label="Close dialog overlay"
        onClick={onDismiss}
        tabIndex={-1}
      />
      <div className="relative z-10 w-full flex justify-center pointer-events-none [&>*]:pointer-events-auto">
        {children}
      </div>
    </div>
  )
}

export function CreateExercise({
  isOpen,
  onCancel,
  onSave,
  onSaved,
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

  const { token } = useAuth()

  const [name, setName] = useState("")
  const [exerciseType, setExerciseType] = useState<string>(resolvedExerciseTypeOptions[0]?.value ?? "weight-reps")
  const [equipment, setEquipment] = useState<string>(equipmentOptions[0] ?? "None")
  const [selectedImageFile, setSelectedImageFile] = useState<File | null>(null)
  const [selectedImageUrl, setSelectedImageUrl] = useState<string | null>(null)
  const [isTypePickerOpen, setIsTypePickerOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  
  const [primaryMuscle, setPrimaryMuscle] = useState<string | null>(null)
  const [secondaryMuscles, setSecondaryMuscles] = useState<string[]>([])
  const [activeMusclePicker, setActiveMusclePicker] = useState<"primary" | "secondary" | null>(null)
  const [muscleSearchQuery, setMuscleSearchQuery] = useState("")

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

  const filteredMuscles = useMemo(() => {
    if (!muscleSearchQuery.trim()) return MUSCLE_GROUPS
    const query = muscleSearchQuery.toLowerCase()
    return MUSCLE_GROUPS.filter((m) => m.toLowerCase().includes(query))
  }, [muscleSearchQuery])

  const resolveExercisesEndpoint = () => {
    const apiBase = import.meta.env.VITE_API_BASE ?? (import.meta.env.DEV ? "/api" : "http://localhost:5036")
    const normalizedBase = apiBase.replace(/\/$/, "")

    return normalizedBase.endsWith("/api")
      ? `${normalizedBase}/Exercises/custom`
      : `${normalizedBase}/api/Exercises/custom`
  }

  useEffect(() => {
    if (!isOpen) return

    const t = setTimeout(() => {
      setName(initialValues?.name ?? "")
      setExerciseType(initialValues?.exerciseType ?? (effectiveExerciseTypeOptions[0]?.value ?? "weight-reps"))
      setEquipment(initialValues?.equipment ?? (effectiveEquipmentOptions[0] ?? "None"))
      setSelectedImageFile(null)
      setSelectedImageUrl(initialValues?.imageUrl ?? null)
      setPrimaryMuscle(initialValues?.primaryMuscle ?? null)
      setSecondaryMuscles(initialValues?.secondaryMuscles ?? [])
      setIsTypePickerOpen(false)
      setActiveMusclePicker(null)
      setMuscleSearchQuery("")
      setIsSaving(false)
      setSaveError(null)
    }, 0)

    return () => clearTimeout(t)
  }, [effectiveEquipmentOptions, effectiveExerciseTypeOptions, initialValues, isOpen])

  useEffect(() => {
    if (!isOpen) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        if (isTypePickerOpen) setIsTypePickerOpen(false)
        else if (activeMusclePicker) setActiveMusclePicker(null)
        else onCancel()
      }
    }

    document.addEventListener("keydown", onKeyDown)
    return () => document.removeEventListener("keydown", onKeyDown)
  }, [isOpen, onCancel, isTypePickerOpen, activeMusclePicker])

  useEffect(() => {
    const prevOverflow = document.body.style.overflow
    if (isOpen || isTypePickerOpen || activeMusclePicker) {
      document.body.style.overflow = "hidden"
    }

    return () => {
      document.body.style.overflow = prevOverflow
    }
  }, [isOpen, isTypePickerOpen, activeMusclePicker])

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
    event.target.value = ""
  }

  const clearImage = () => {
    setSelectedImageFile(null)
    setSelectedImageUrl(null)
  }

  const toggleSecondaryMuscle = (muscle: string) => {
    setSecondaryMuscles((prev) =>
      prev.includes(muscle) ? prev.filter((m) => m !== muscle) : [...prev, muscle]
    )
  }

  const handleSave = async (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault()

    const values = {
      name: name.trim(),
      exerciseType,
      equipment,
      imageFile: selectedImageFile,
      imageUrl: selectedImageUrl,
      primaryMuscle,
      secondaryMuscles,
    }

    try {
      setIsSaving(true)
      setSaveError(null)

      if (onSave) {
        await onSave(values)
        if (onSaved) {
          await onSaved()
        }
        onCancel()
        return
      }

      const payload = {
        Name: values.name,
        Mechanic: null,
        Equipment: values.equipment || null,
        Category: values.exerciseType || "Custom",
        PrimaryMuscles: values.primaryMuscle ? [values.primaryMuscle] : [],
        SecondaryMuscles: values.secondaryMuscles ?? [],
      }

      const response = await fetch(resolveExercisesEndpoint(), {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
          ...(token ? { Authorization: `Bearer ${token}` } : {}),
        },
        body: JSON.stringify(payload),
      })

      if (!response.ok) {
        const text = await response.text()
        throw new Error(text || `Request failed with status ${response.status}`)
      }

      if (onSaved) {
        await onSaved()
      }

      onCancel()
    } catch (error) {
      const message = error instanceof Error ? error.message : "Failed to create exercise"
      setSaveError(message)
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <>
      {!isTypePickerOpen && !activeMusclePicker && (
        <CreateExerciseBackdrop zIndexClassName="z-40" backdropClassName="bg-foreground/50" onDismiss={onCancel}>
          <form className="w-full max-w-xl rounded-xl border border-border bg-surface p-5 shadow-xl" onSubmit={handleSave} aria-label="Create custom exercise">
            <div className="mb-4 flex items-start justify-between gap-4">
              <div>
                <h2 className="text-2xl leading-none">Create Custom Exercise</h2>
              </div>
              <Button type="button" variant="icon" size="icon" aria-label="Close" onClick={onCancel}>
                <X size={16} />
              </Button>
            </div>

            <div className="grid gap-4">
              <label className="grid gap-1.5">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Exercise name</span>
                <Input value={name} onChange={(event) => setName(event.target.value)} placeholder="e.g. Seated Cable Row" required maxLength={80} />
              </label>

              <div className="grid gap-1.5">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Exercise image</span>
                <div className="rounded-lg border border-dashed border-border p-3">
                  <div className="flex items-center gap-4">
                    <label className="relative flex h-20 w-20 cursor-pointer overflow-hidden rounded-lg border border-border bg-surface-2 transition-colors hover:bg-border items-center justify-center">
                      <input type="file" accept="image/*" className="sr-only" onChange={onImageChange} />
                      {selectedImageUrl ? (
                        <img src={selectedImageUrl} alt="Selected exercise" className="h-full w-full object-cover" />
                      ) : (
                        <div className="text-muted-foreground"><ImagePlus size={20} /></div>
                      )}
                    </label>
                    <div className="flex flex-wrap items-center gap-3">
                      {selectedImageUrl ? <Button type="button" variant="ghost" onClick={clearImage}>Remove</Button> : null}
                    </div>
                  </div>
                </div>
              </div>

              <div className="grid gap-1.5">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Exercise type</span>
                <Button type="button" variant="secondary" className="h-10 w-full justify-between px-3 normal-case tracking-normal" onClick={() => setIsTypePickerOpen(true)}>
                  <span>{selectedExerciseTypeLabel}</span>
                  <span className="text-xs text-muted-foreground">Change</span>
                </Button>
              </div>

              <div className="grid gap-1 border-y border-border py-3">
                <div className="grid gap-1">
                  <div className="flex items-center justify-between gap-4 overflow-hidden">
                    <span className="text-base font-medium whitespace-nowrap shrink-0">Primary Muscle Group</span>
                    <Button type="button" variant="ghost" className="h-auto p-0 pl-2 text-brand hover:bg-transparent min-w-0 flex-1 justify-end overflow-hidden" onClick={() => { setActiveMusclePicker("primary"); setMuscleSearchQuery(""); }}>
                      <span className="truncate uppercase block w-full text-right">{primaryMuscle ?? "Select"}</span>
                    </Button>
                  </div>
                </div>
                <div className="grid gap-1 mt-1">
                  <div className="flex items-center justify-between gap-4 overflow-hidden">
                    <span className="text-base font-medium whitespace-nowrap shrink-0">Other Muscles</span>
                    <Button type="button" variant="ghost" className="h-auto p-0 pl-2 text-brand hover:bg-transparent min-w-0 flex-1 justify-end overflow-hidden" onClick={() => { setActiveMusclePicker("secondary"); setMuscleSearchQuery(""); }}>
                      <span className="truncate uppercase block w-full text-right">{secondaryMuscles.length > 0 ? secondaryMuscles.join(", ") : "Select (optional)"}</span>
                    </Button>
                  </div>
                </div>
              </div>

              <div className="grid gap-1.5">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground">Required equipment</span>
                <DropdownMenu>
                  <DropdownMenuTrigger variant="filter">
                    {equipment}
                  </DropdownMenuTrigger>
                  <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                    {effectiveEquipmentOptions.map((option) => (
                      <DropdownMenuItem key={option} onClick={() => setEquipment(option)}>
                        {option}
                      </DropdownMenuItem>
                    ))}
                  </DropdownMenuContent>
                </DropdownMenu>
              </div>
            </div>

            <div className="mt-6 flex flex-wrap justify-end gap-3">
              <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
              <Button type="submit" disabled={!name.trim() || !primaryMuscle || isSaving}>Save Exercise</Button>
            </div>
            {saveError ? (
              <p className="mt-3 text-sm text-destructive" role="alert">
                {saveError}
              </p>
            ) : null}
          </form>
        </CreateExerciseBackdrop>
      )}

      {isTypePickerOpen ? (
        <CreateExerciseBackdrop zIndexClassName="z-[90]" backdropClassName="bg-foreground/50" onDismiss={() => setIsTypePickerOpen(false)}>
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

      {activeMusclePicker ? (
        <CreateExerciseBackdrop zIndexClassName="z-[90]" backdropClassName="bg-foreground/50" onDismiss={() => setActiveMusclePicker(null)}>
          <div className="flex max-h-[85dvh] w-full max-w-xl flex-col overflow-hidden rounded-xl border border-border bg-surface shadow-xl">
            <div className="relative flex h-14 items-center border-b border-border bg-surface px-4">
              <button type="button" onClick={() => setActiveMusclePicker(null)} className="inline-flex h-8 w-8 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-surface-2 hover:text-foreground" aria-label="Back">
                <ArrowLeft size={18} />
              </button>
              <p className="absolute left-1/2 -translate-x-1/2 text-sm font-semibold uppercase tracking-[0.06em]">
                {activeMusclePicker === "primary" ? "Select Primary Muscle" : "Select Other Muscles"}
              </p>
            </div>

            <div className="px-4 py-3 border-b border-border">
              <div className="relative">
                <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
                <Input
                  value={muscleSearchQuery}
                  onChange={(e) => setMuscleSearchQuery(e.target.value)}
                  placeholder="Search muscle"
                  className="pl-9 h-10 w-full rounded-lg bg-surface-2 border-none"
                />
              </div>
            </div>

            <div className="overflow-y-auto px-4 py-2">
              {filteredMuscles.map((muscle) => {
                const isSelected = activeMusclePicker === "primary" ? primaryMuscle === muscle : secondaryMuscles.includes(muscle)
                return (
                  <button
                    key={muscle}
                    type="button"
                    onClick={() => {
                      if (activeMusclePicker === "primary") {
                        setPrimaryMuscle(muscle)
                        // If it's already in secondary, optionally we might want to remove it, but fine for now
                        setActiveMusclePicker(null)
                      } else {
                        toggleSecondaryMuscle(muscle)
                      }
                    }}
                    className="w-full flex items-center justify-between py-4 border-b border-border/50 last:border-0 hover:bg-surface-2 px-2 rounded-md transition-colors"
                  >
                    <div className="flex items-center gap-4">
                      {/* Placeholder for anatomy image */}
                      <div className="h-10 w-10 overflow-hidden rounded-full bg-surface-2 border border-border shrink-0 flex items-center justify-center">
                        <span className="text-[0.6rem] font-bold text-muted-foreground uppercase">{muscle.slice(0, 2)}</span>
                      </div>
                      <span className="text-[1.03rem] font-medium text-foreground">{muscle}</span>
                    </div>
                    {isSelected ? <Check size={18} className="text-brand" /> : null}
                  </button>
                )
              })}
              {filteredMuscles.length === 0 && (
                <p className="text-center text-sm text-muted-foreground py-8">No muscles found.</p>
              )}
            </div>
          </div>
        </CreateExerciseBackdrop>
      ) : null}
    </>
  )
}
