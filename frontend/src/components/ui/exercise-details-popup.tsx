import { useEffect, useMemo, useState } from 'react'
import { Dumbbell, Pencil, Trash2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { CreateExercise } from '@/components/ui/create-exercise'
import { toast } from '@/components/ui/alert'
import { DEFAULT_EQUIPMENT_OPTIONS } from '@/constants/equipment'
import { formatExerciseType } from '@/constants/exercise-type-definitions'
import { customFetch } from '@/lib/custom-fetch'
import type { CreateExerciseFormData, ExerciseDetails } from '@/types/exercise'

type ExerciseDetsResponse = {
  id: string
  name: string
  mechanic?: string | null
  equipment?: string | null
  category: string
  primaryMuscles: string[]
  secondaryMuscles: string[]
  isCustom: boolean
  imageUrl?: string | null
}

type ExerciseDetailsPopupProps = Readonly<{
  exerciseId: string | null
  onClose: () => void
  onChanged?: () => void | Promise<void>
}>

const formatMechanic = (mechanic: string | null | undefined): string | null => {
  if (!mechanic) {
    return null
  }

  return mechanic.charAt(0).toUpperCase() + mechanic.slice(1)
}

const toDetails = (dto: ExerciseDetsResponse): ExerciseDetails => ({
  id: dto.id,
  name: dto.name,
  mechanic: formatMechanic(dto.mechanic),
  equipment: dto.equipment ?? null,
  exerciseType: dto.category,
  primaryMuscles: dto.primaryMuscles,
  secondaryMuscles: dto.secondaryMuscles,
  isCustom: dto.isCustom,
  imageUrl: dto.imageUrl ?? null,
})

const normalizeEquipment = (equipment: string | null): string | undefined => {
  if (!equipment) {
    return undefined
  }

  const matches = DEFAULT_EQUIPMENT_OPTIONS.find((option) => option.toLowerCase() === equipment.toLowerCase())
  
  return matches ?? equipment
}

const fileFromUrl = async (imageUrl: string, name: string): Promise<File | null> => {
  try {
    const resp = await fetch(imageUrl)

    if (!resp.ok) {
        return null
    }
    const bloblStorage = await resp.blob()
    const ext = bloblStorage.type.split('/')[1] ?? 'jpg'

    return new File([bloblStorage], `${name || 'exercise'}.${ext}`, { type: bloblStorage.type || 'image/jpeg' })
  } 
  catch {
    return null
  }
}

const sameEquip = (a: string | null | undefined, b: string | null | undefined) => (a ?? '').toLowerCase() === (b ?? '').toLowerCase()

export function ExerciseDetailsPopup({ exerciseId, onClose, onChanged }: ExerciseDetailsPopupProps) {
    const [isEditOpen, setIsEditOpen] = useState(false)
    const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false)
    const [isDeleting, setIsDeleting] = useState(false)
    const [details, setDetails] = useState<ExerciseDetails | null>(null)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        if (!exerciseId) {
            return
        }

        const key = (e: KeyboardEvent) => {

            if (e.key === 'Escape' && !isEditOpen && !isConfirmDeleteOpen) {
                onClose()
            }
        }

        window.addEventListener('keydown', key)
        return () => window.removeEventListener('keydown', key)
    }, [exerciseId, isEditOpen, isConfirmDeleteOpen, onClose])

    useEffect(() => {
        if (!exerciseId) {
            return
        }

        let canclled = false

        const loading = async () => {
            setLoading(true)
            setError(null)

            try {
                const response = await customFetch(`/api/exercises/${exerciseId}`, { headers: { Accept: 'application/json' } })
                if (!response.ok) throw new Error(`Failure to load exercise (${response.status})`)

                const dto = (await response.json()) as ExerciseDetsResponse

                if (!canclled) setDetails(toDetails(dto))
                } 
                catch (err) {
                    if (!canclled) setError(err instanceof Error ? err.message : 'Failed to load exercise')
                } 
                finally {
                    if (!canclled) setLoading(false)
            }
        }

        void loading()
        return () => { canclled = true }
    }, [exerciseId])

    const initEditVals = useMemo(() => {
        if (!details) {
            return undefined
        }

        return {
            name: details.name,
            exerciseType: details.exerciseType,
            equipment: normalizeEquipment(details.equipment),
            imageUrl: details.imageUrl,
            primaryMuscle: details.primaryMuscles[0] ?? null,
            secondaryMuscles: details.secondaryMuscles,
        }
    }, [details])

    if (!exerciseId) {
        return null
    }

    const structuralDiff = (values: CreateExerciseFormData) =>
        values.exerciseType !== details?.exerciseType ||
        !sameEquip(values.equipment, details?.equipment) ||
        values.primaryMuscle !== (details?.primaryMuscles[0] ?? null) ||
        values.secondaryMuscles.length !== details?.secondaryMuscles.length ||
        values.secondaryMuscles.some((m) => !details?.secondaryMuscles.includes(m))

    const saves = async (values: CreateExerciseFormData) => {
        if (!details) {
            return
        }

        const data = new FormData()

        data.append('Name', values.name)
        if (values.imageFile) {
            data.append('Image', values.imageFile)
        } 
        else if (!values.imageUrl && details.imageUrl) {
            data.append('RemoveImage', 'true')
        }

        const resp = await customFetch(`/api/exercises/custom/${details.id}`, {
            method: 'PUT',
            headers: { Accept: 'application/json' },
            body: data,
        })

        if (!resp.ok) {
            const txt = await resp.text()
            throw new Error(txt || `Request failed with status ${resp.status}`)
        }
    }

    const forkNRetire = async (values: CreateExerciseFormData) => {
        if (!details) {
            return
        }

        let imageFile = values.imageFile

        if (!imageFile && values.imageUrl && values.imageUrl === details.imageUrl) {
            imageFile = await fileFromUrl(values.imageUrl, values.name)
            if (!imageFile) {
                toast.info("Couldn't carry the image over", 'Image not copied')
            }
        }

        const data = new FormData()
        data.append('Name', values.name)

        if (values.equipment) {
            data.append('Equipment', values.equipment)
        }
        data.append('Category', values.exerciseType)
        if (values.primaryMuscle) {
            data.append('PrimaryMuscles', values.primaryMuscle)
        }
        values.secondaryMuscles.forEach((m) => data.append('SecondaryMuscles', m))
        if (imageFile) {
            data.append('Image', imageFile)
        }

        const createResp = await customFetch('/api/exercises/custom', {
            method: 'POST',
            headers: { Accept: 'application/json' },
            body: data,
        })

        if (!createResp.ok) {
            const text = await createResp.text()
            throw new Error(text || `Request failed with status ${createResp.status}`)
        }

        const deleteResp = await customFetch(`/api/exercises/custom/${details.id}`, { method: 'DELETE' })
        if (!deleteResp.ok) {
            toast.info('The update was successful', 'Cleanup needed')
        }
    }

    const editSaveHandle = async (values: CreateExerciseFormData) => {
        if (structuralDiff(values)) {
            await forkNRetire(values)
        } 
        else {
            await saves(values)
        }

        toast.success('Exercise updated.', 'Saved')
        if (onChanged) {
            await onChanged()
        }

        onClose()
    }

    const deleteHandle = async () => {
        if (!details) {
            return
        }

        setIsDeleting(true)

        try {
            const resp = await customFetch(`/api/exercises/custom/${details.id}`, { method: 'DELETE' })
            if (!resp.ok) {
                const body = (await resp.json().catch(() => null)) as { error?: string } | null
                throw new Error(body?.error ?? `Request failed with status ${resp.status}`)
            }

            toast.success('Exercise deleted.', 'Deleted')
            if (onChanged) {
                await onChanged()
            }

            setIsConfirmDeleteOpen(false)
            onClose()
        } 
        catch (err) {
            toast.error(err instanceof Error ? err.message : 'Failed to delete exercise', 'Error')
        } 
        finally {
            setIsDeleting(false)
        }
    }

  return (
    <>
      {!isEditOpen && (
        <div className="fixed inset-x-0 bottom-0 top-0 lg:top-20 z-40 flex items-center justify-center p-4">
          <button
            type="button"
            className="absolute inset-0 block w-full cursor-default bg-foreground/50 outline-none"
            aria-label="Close exercise details" onClick={onClose} tabIndex={-1}
          />
          <div className="relative z-10 flex max-h-[80vh] w-full max-w-lg flex-col overflow-hidden rounded-2xl border border-border bg-card shadow-xl">
            <div className="flex items-center justify-between border-b border-border px-4 py-3">
              <h2 className="text-base font-bold text-foreground">Exercise Details</h2>
              <Button variant="ghost" size="icon" aria-label="Close" onClick={onClose} className="h-8 w-8 text-muted-foreground">
                <X className="h-5 w-5" />
              </Button>
            </div>

            <div className="min-h-0 flex-1 overflow-y-auto px-4 py-4">
              {loading && <p className="text-sm text-muted-foreground">Loading...</p>}
              {error && <p className="text-sm text-destructive">{error}</p>}

              {details && !loading && !error && (
                <div className="flex flex-col gap-4">
                  <div className="flex items-center gap-4">
                    <CircularProfileImage
                      src={details.imageUrl ?? undefined} alt={details.name}
                      className="size-16 shrink-0 border-border"
                      fallbackIcon={<Dumbbell className="size-6 text-muted-foreground" />}
                    />
                    <div className="min-w-0">
                      <p className="truncate text-lg font-bold text-foreground">{details.name}</p>
                      <p className="text-xs text-muted-foreground">
                        {details.isCustom ? 'Custom exercise' : 'Exercise library'}
                      </p>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">Primary Muscle</p>
                      <p className="text-foreground">{details.primaryMuscles[0] ?? '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">Equipment</p>
                      <p className="text-foreground">{details.equipment ?? '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">Type</p>
                      <p className="text-foreground">{formatExerciseType(details.exerciseType)}</p>
                    </div>
                    {details.mechanic && (
                      <div>
                        <p className="text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">Mechanic</p>
                        <p className="text-foreground capitalize">{details.mechanic}</p>
                      </div>
                    )}
                  </div>

                  {details.secondaryMuscles.length > 0 && (
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">Other Muscles</p>
                      <div className="mt-1 flex flex-wrap gap-1.5">
                        {details.secondaryMuscles.map((muscle) => (
                          <span key={muscle} className="rounded-full border border-border bg-surface-2 px-2.5 py-1 text-xs font-medium text-foreground">
                            {muscle}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}
            </div>

            {details?.isCustom && (
              <div className="flex justify-end gap-2 border-t border-border px-4 py-3">
                <Button type="button" variant="secondary" onClick={() => setIsConfirmDeleteOpen(true)}>
                  <Trash2 className="mr-2 h-4 w-4" /> Delete
                </Button>
                <Button type="button" onClick={() => setIsEditOpen(true)}>
                  <Pencil className="mr-2 h-4 w-4" /> Edit
                </Button>
              </div>
            )}
          </div>
        </div>
      )}

      {details && (
        <CreateExercise
          isOpen={isEditOpen} onCancel={() => setIsEditOpen(false)}
          onSave={editSaveHandle} initialValues={initEditVals}
        />
      )}

      <ConfirmDialog
        isOpen={isConfirmDeleteOpen} onClose={() => setIsConfirmDeleteOpen(false)}
        onConfirm={deleteHandle} isLoading={isDeleting}
        variant="danger" title="Delete Exercise"
        description="This will remove this exercise from your list permanently. Existing workouts and logs that used it are unaffected."
        confirmText="Delete" cancelText="Cancel"
      />
    </>
  )
}
