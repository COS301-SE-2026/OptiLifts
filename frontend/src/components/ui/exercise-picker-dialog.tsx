import { useEffect, useMemo, useState } from 'react'
import { Dumbbell, Plus, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { SearchInput } from '@/components/ui/search-input'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { CreateExercise } from '@/components/ui/create-exercise'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { MUSCLE_GROUPS } from '@/constants/muscles'
import { DEFAULT_EQUIPMENT_OPTIONS } from '@/constants/equipment'
import { customFetch } from '@/lib/custom-fetch'

export type CatalogExercise = {
  id: string
  name: string
  muscleGroup: string
  equipment?: string
  exerciseType?: string
  imageUrl?: string
}

type ExerciseApiResponse = {
  id: string
  name: string
  primaryMuscles?: string[]
  equipment?: string
  category?: string
  imageUrl?: string
}

type ExercisePickerDialogProps = Readonly<{
  isOpen: boolean
  onClose: () => void
  onSelect: (exercise: CatalogExercise) => void
  title?: string
}>

const MUSCLE_OPTS = ['All Muscles', ...MUSCLE_GROUPS]
const EQUIPMENT_OPTS = ['All Equipment', ...DEFAULT_EQUIPMENT_OPTIONS]

export function ExercisePickerDialog({ isOpen, onClose, onSelect, title = 'Add Exercise' }: ExercisePickerDialogProps) {
  const [allExercises, setAllExercises] = useState<CatalogExercise[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selectedMuscle, setSelectedMuscle] = useState('All Muscles')
  const [selectedEquipment, setSelectedEquipment] = useState('All Equipment')
  const [searchQuery, setSearchQuery] = useState('')
  const [reloadKey, setReloadKey] = useState(0)
  const [isCreateOpen, setIsCreateOpen] = useState(false)

  useEffect(() => {
    if (!isOpen)
    {
      return
    }
    let cancelled = false
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const results = await customFetch('/api/exercises', { headers: { 'Content-Type': 'application/json' } })
        if (!results.ok) throw new Error(`Failed to load exercises (${results.status})`)
        const json = (await results.json()) as ExerciseApiResponse[]
        if (!cancelled) {
          setAllExercises(
            json.map((ex) => ({
              id: ex.id,
              name: ex.name,
              muscleGroup: ex.primaryMuscles?.[0] ?? 'Other',
              equipment: ex.equipment,
              exerciseType: ex.category,
              imageUrl: ex.imageUrl,
            }))
          )
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load exercises')
      } finally {
        setLoading(false)
      }
    }

    void load()
    return () => { cancelled = true }
  }, [isOpen, reloadKey])

  useEffect(() => {
    if (!isOpen) return
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && !isCreateOpen) onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [isOpen, isCreateOpen, onClose])

  const filtered = useMemo(() => {
    const q = searchQuery.trim().toLowerCase()
    return allExercises.filter((ex) => {
      if (q && !ex.name.toLowerCase().includes(q) && !ex.muscleGroup.toLowerCase().includes(q)) return false
      if (selectedMuscle !== 'All Muscles' && ex.muscleGroup !== selectedMuscle) return false
      if (selectedEquipment !== 'All Equipment' && ex.equipment?.toLowerCase() !== selectedEquipment.toLowerCase()) return false
      return true
    })
  }, [allExercises, searchQuery, selectedMuscle, selectedEquipment])

  if (!isOpen) return null

  return (
    <>
    <div className="fixed inset-x-0 bottom-0 top-20 z-30 flex items-center justify-center p-4">
        <button
          type="button"
          className="absolute inset-0 block w-full cursor-default bg-foreground/50 outline-none"
          aria-label="Close exercise picker"
          onClick={onClose}
          tabIndex={-1}
        />
        <div className="relative z-10 flex max-h-[80vh] w-full max-w-lg flex-col overflow-hidden rounded-2xl border border-border bg-card shadow-xl">
          <div className="flex items-center justify-between border-b border-border px-4 py-3">
            <h2 className="text-base font-bold text-foreground">{title}</h2>
            <Button variant="ghost" size="icon" aria-label="Close" onClick={onClose} className="h-8 w-8 text-muted-foreground">
              <X className="h-5 w-5" />
            </Button>
          </div>

          <div className="flex flex-col gap-2 border-b border-border px-4 py-3">
            <div className="flex gap-2">
              <DropdownMenu>
                <DropdownMenuTrigger variant="filter" className="w-full shadow-none">
                  <span>{selectedMuscle}</span>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width] max-h-64 overflow-y-auto">
                  {MUSCLE_OPTS.map((o) => (
                    <DropdownMenuItem key={o} onSelect={() => setSelectedMuscle(o)}>{o}</DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>
              <DropdownMenu>
                <DropdownMenuTrigger variant="filter" className="w-full shadow-none">
                  <span>{selectedEquipment}</span>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width] max-h-64 overflow-y-auto">
                  {EQUIPMENT_OPTS.map((o) => (
                    <DropdownMenuItem key={o} onSelect={() => setSelectedEquipment(o)}>{o}</DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
            <div className="[&>div]:w-full [&>div]:max-w-none">
              <SearchInput
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search exercises"
                aria-label="Search exercises"
                className="h-8 w-full"
              />
            </div>
          </div>

          <div className="min-h-0 flex-1 overflow-y-auto">
            {loading && <p className="px-4 py-3 text-sm text-muted-foreground">Loading exercises...</p>}
            {error && <p className="px-4 py-3 text-sm text-destructive">{error}</p>}
            {!loading && !error && filtered.length === 0 && (
              <p className="px-4 py-3 text-sm text-muted-foreground">No exercises match your filters.</p>
            )}
            <div className="divide-y divide-border/70">
              {filtered.map((ex) => (
                <div key={ex.id} className="flex items-center gap-3 px-4 py-2.5">
                  <CircularProfileImage
                    src={ex.imageUrl}
                    alt={ex.name}
                    className="size-9 shrink-0 border-border"
                    fallbackIcon={<Dumbbell className="size-4 text-muted-foreground" />}
                  />
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-sm font-semibold text-foreground">{ex.name}</div>
                    <div className="text-xs text-muted-foreground">
                      {ex.muscleGroup}{ex.equipment ? ` • ${ex.equipment}` : ''}
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="icon"
                    size="icon"
                    aria-label={`Add ${ex.name}`}
                    onClick={() => onSelect(ex)}
                    className="size-6 rounded-md border-border bg-surface-2 text-foreground hover:bg-border"
                  >
                    <Plus size={12} />
                  </Button>
                </div>
              ))}
            </div>
          </div>

          <div className="border-t border-border px-4 py-2.5">
            <Button
              type="button"
              variant="text"
              className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2"
              onClick={() => setIsCreateOpen(true)}
            >
              + Create Exercise
            </Button>
          </div>
        </div>
      </div>

      <CreateExercise
        isOpen={isCreateOpen}
        onCancel={() => setIsCreateOpen(false)}
        onSaved={() => setReloadKey((k) => k + 1)}
      />
    </>
  )
}
