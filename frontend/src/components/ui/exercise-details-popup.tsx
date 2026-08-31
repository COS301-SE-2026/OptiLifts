import { useEffect, useMemo, useState } from 'react'
import { AlertCircle, Dumbbell, Pencil, Trash2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { CreateExercise } from '@/components/ui/create-exercise'
import { toast } from '@/components/ui/alert'
import { DEFAULT_EQUIPMENT_OPTIONS } from '@/constants/equipment'
import { formatExerciseType } from '@/constants/exercise-type-definitions'
import { customFetch } from '@/lib/custom-fetch'
import type { CreateExerciseFormData, ExerciseDetails } from '@/types/exercise'
import { useOnlineStatus, OfflineTooltip } from '@/lib/use-online-status'
import { adaptImgUrl } from '@/lib/utils'

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
  isDeleted?: boolean
}

type ExerciseDetailsPopupProps = Readonly<{
  exerciseId: string | null
  onClose: () => void
  onChanged?: (exerciseId?: string, oldExerciseId?: string) => void | Promise<void>
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
  isDeleted: dto.isDeleted ?? false,
})

const getExerciseSourceLabel = (details: ExerciseDetails): string => {
  if (details.isDeleted) {
    return 'Deleted custom exercise'
  }
  if (details.isCustom) {
    return 'Custom exercise'
  }
  return 'Exercise library'
}

const capitalizeEquipment = (equipment: string | null | undefined): string | undefined => {
  if (!equipment) {
    return undefined
  }

  const normalized = equipment.trim()
  if (!normalized) {
    return undefined
  }

  const matches = DEFAULT_EQUIPMENT_OPTIONS.find((option) => option.toLowerCase() === normalized.toLowerCase())
  if (matches) {
    return matches
  }

  return normalized
    .split(/\s+/)
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
    .join(' ')
}

const fileFromUrl = async (imageUrl: string, name: string): Promise<File | null> => {
  try {
    const resp = await fetch(adaptImgUrl(imageUrl))

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

const extractErrorMessage = async (resp: Response, defaultMessage: string): Promise<string> => {
    try {
        const errJson = await resp.json()
        if (errJson?.error) return errJson.error
        if (errJson?.message) return errJson.message
    } catch {
        const txt = await resp.text().catch(() => '')
        if (txt) return txt
    }
    return defaultMessage
}

const resolveImageForFork = async (
    values: CreateExerciseFormData,
    existingImageUrl?: string | null
): Promise<File | null | undefined> => {
    if (values.imageFile || !values.imageUrl || values.imageUrl !== existingImageUrl) {
        return values.imageFile
    }

    const file = await fileFromUrl(values.imageUrl, values.name)
    if (!file) {
        toast.info("Couldn't carry the image over", 'Image not copied')
    }
    return file
}

const buildForkExerciseFormData = (values: CreateExerciseFormData, imageFile?: File | null): FormData => {
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
    return data
}

const deleteCustomExercise = async (id: string): Promise<void> => {
    const deleteResp = await customFetch(`/api/exercises/custom/${id}`, { method: 'DELETE' })
    if (!deleteResp.ok) {
        const message = await extractErrorMessage(deleteResp, `Failed to update exercise (${deleteResp.status})`)
        throw new Error(message)
    }
}

const createCustomExercise = async (formData: FormData): Promise<string> => {
    const createResp = await customFetch('/api/exercises/custom', {
        method: 'POST',
        headers: { Accept: 'application/json' },
        body: formData,
    })

    if (!createResp.ok) {
        const message = await extractErrorMessage(createResp, `Request failed with status ${createResp.status}`)
        throw new Error(message)
    }

    const data = (await createResp.json().catch(() => null)) as { id?: string; Id?: string } | null
    return data?.id ?? data?.Id ?? ''
}

export function ExerciseDetailsPopup({ exerciseId, onClose, onChanged }: ExerciseDetailsPopupProps) {
    const [isEditOpen, setIsEditOpen] = useState(false)
    const [isConfirmDeleteOpen, setIsConfirmDeleteOpen] = useState(false)
    const [isDeleting, setIsDeleting] = useState(false)
    const [details, setDetails] = useState<ExerciseDetails | null>(null)
    const [loading, setLoading] = useState(false)
    const [error, setError] = useState<string | null>(null)
    const isOnline = useOnlineStatus()

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
            setDetails(null)

            try {
                const response = await customFetch(`/api/exercises/${exerciseId}`, {
                    headers: { Accept: 'application/json' },
                    cache: 'no-store',
                })
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
            equipment: capitalizeEquipment(details.equipment),
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
            const message = await extractErrorMessage(resp, `Request failed with status ${resp.status}`)
            throw new Error(message)
        }
    }

    const forkNRetire = async (values: CreateExerciseFormData): Promise<string> => {
        if (!details) {
            return ''
        }

        const imageFile = await resolveImageForFork(values, details.imageUrl)
        const data = buildForkExerciseFormData(values, imageFile)

        await deleteCustomExercise(details.id)
        return await createCustomExercise(data)
    }

    const editSaveHandle = async (values: CreateExerciseFormData) => {
        let updatedId: string | undefined = details?.id
        if (structuralDiff(values)) {
            updatedId = await forkNRetire(values)
        } 
        else {
            await saves(values)
        }

        toast.success('Exercise updated.', 'Saved')
        if (onChanged && details) {
            await onChanged(updatedId || details.id, details.id)
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
            setDetails((prev) => (prev ? { ...prev, isDeleted: true } : null))
            setIsConfirmDeleteOpen(false)

            if (onChanged) {
                await onChanged(details.id)
            }
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
        <div className="fixed inset-x-0 bottom-0 top-0 lg:top-20 z-50 flex items-center justify-center p-4">
          <button
            type="button"
            className="absolute inset-0 block w-full cursor-default bg-black/50 backdrop-blur-xs outline-none"
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
                        {getExerciseSourceLabel(details)}
                      </p>
                    </div>
                  </div>

                  {details.isDeleted && (
                    <div className="flex items-center gap-2.5 rounded-xl border border-destructive/30 bg-destructive/10 px-3.5 py-2.5 text-xs text-destructive">
                      <AlertCircle className="size-4 shrink-0" />
                      <span>This exercise has been deleted and cannot be edited or deleted.</span>
                    </div>
                  )}

                  <div className="grid grid-cols-2 gap-3 text-sm">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">Primary Muscle</p>
                      <p className="text-foreground">{details.primaryMuscles[0] ?? '-'}</p>
                    </div>
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.06em] text-muted-foreground">Equipment</p>
                      <p className="text-foreground">{capitalizeEquipment(details.equipment) ?? '-'}</p>
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

            {details?.isCustom && !details.isDeleted && (
              <div className="flex justify-end gap-2 border-t border-border px-4 py-3">
                <OfflineTooltip isOnline={isOnline}>
                  <Button type="button" variant="secondary" disabled={!isOnline} onClick={() => setIsConfirmDeleteOpen(true)}>
                    <Trash2 className="mr-2 h-4 w-4" /> Delete
                  </Button>
                </OfflineTooltip>
                <OfflineTooltip isOnline={isOnline}>
                  <Button type="button" disabled={!isOnline} onClick={() => setIsEditOpen(true)}>
                    <Pencil className="mr-2 h-4 w-4" /> Edit
                  </Button>
                </OfflineTooltip>
              </div>
            )}
          </div>
        </div>
      )}

      {details && isEditOpen &&  (
        <CreateExercise
          isOpen={true} onCancel={() => setIsEditOpen(false)}
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
