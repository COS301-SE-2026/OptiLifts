import { useCallback, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { PageTitle } from '@/components/ui/page-title'
import { Button } from '@/components/ui/button'
import { SearchInput } from '@/components/ui/search-input'
import {
  DropdownMenu,
  DropdownMenuEllipsisContent,
  DropdownMenuItem,
  DropdownMenuEllipsisTrigger,
} from '@/components/ui/dropdown-menu'
import { Card, CardAction, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import MuscleDiagram from '@/components/ui/muscle-diagram'
import { useAuth } from '@/context/auth-context'
import { useOnlineStatus, OfflineTooltip } from '@/lib/use-online-status'
import type { Workout, WorkoutSummary, MuscleName } from '@/types/workout'
import type { WorkoutDetailResponse } from '@/types/workout-detail'
import { Plus } from 'lucide-react'
import { customFetch } from '@/lib/custom-fetch'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { cacheWorkoutList, getCachedWorkoutDetail, getCachedWorkoutList, precacheWorkoutDetails } from '@/lib/offline/workouts-cache'
import { OfflineBanner } from '@/components/ui/offline-banner'

export default function WorkoutsPage() {
  const { isAuthenticated, isHydrated } = useAuth()
  const navigate = useNavigate()
  const [workouts, setWorkouts] = useState<Workout[]>([])
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [isFetching, setIsFetching] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null)
  const [selectedWorkoutDetails, setSelectedWorkoutDetails] = useState<WorkoutDetailResponse | null>(null)
  const [isOfflineData, setIsOfflineData] = useState(false)
  const authError = isHydrated && !isAuthenticated ? 'Please log in to view your workouts.' : null
  const isOnline = useOnlineStatus()

  const loadWorkouts = useCallback(async (selectIdAfterLoad?: string) => {
    await Promise.resolve()
    setIsFetching(true)
    setError(null)

    try {
      const response = await customFetch('/api/workouts', {
        headers: {
          Accept: 'application/json',
        },
      })
      if (!response.ok) {
        throw new Error(`Failed to load workouts (${response.status})`)
      }

      const data = (await response.json()) as Workout[]
      setWorkouts(data)
      setIsOfflineData(false)
      void cacheWorkoutList(data)
      void precacheWorkoutDetails(data.map((w) => w.id))

      if (selectIdAfterLoad) {
        setSelectedId(selectIdAfterLoad)
      } 
      else {
        setSelectedId((currentId) => {
          if (data.some((w) => w.id === currentId)) {
            return currentId
          }
          return data[0]?.id ?? null
        })
      }
    } 
    catch (loadError) {
      const cached = await getCachedWorkoutList()

      if (cached && cached.length > 0) {
        setWorkouts(cached)
        setIsOfflineData(true)
        setSelectedId((currentId) => (cached.some((w) => w.id === currentId) ? currentId : cached[0]?.id ?? null))
        return
      }

      setError(loadError instanceof Error ? loadError.message : 'Failed to load workouts.')
    } 
    finally {
      setIsFetching(false)
    }
  }, [])

  useEffect(() => {
    if (!isHydrated || !isAuthenticated) {
      return
    }
    const triggerInitial = async () => {
      await loadWorkouts()
    }
    void triggerInitial()
  }, [isHydrated, isAuthenticated, loadWorkouts])

  useEffect(() => {
    if (!selectedId || !isAuthenticated || !isHydrated) {
      return
    }

    let isActive = true

    const loadSelectedWorkoutDetails = async () => {
      try {
        const response = await customFetch(`/api/workouts/${selectedId}`, {
          headers: {
            Accept: 'application/json',
          },
        })

        if (!response.ok) {
          throw new Error(`Failed to load workout details (${response.status})`)
        }

        const data = (await response.json()) as WorkoutDetailResponse
        if (isActive) {
          setSelectedWorkoutDetails(data)
        }
      } catch {
        const cached = await getCachedWorkoutDetail(selectedId)

        if (isActive) {
          setSelectedWorkoutDetails(cached)
        }
      }
    }

    void loadSelectedWorkoutDetails()

    return () => {
      isActive = false
    }
  }, [selectedId, isAuthenticated, isHydrated])

  //duplication
  const handleDuplicate = async (workoutId: string) => {
    setIsFetching(true)
    setError(null)
    try {
      const response = await customFetch(`/api/workouts/${workoutId}/duplicate`, {
        method: 'POST',
        headers: {
          Accept: 'application/json',
        },
      })
      if (!response.ok) {
        throw new Error(`Failed to duplicate workout (${response.status})`)
      }
      const dupeResult = (await response.json()) as { workoutId: string }
      await loadWorkouts(dupeResult.workoutId)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to duplicate workout.')
      setIsFetching(false)
    }
  }

  //deletions
  const handleDelete = async (workoutId: string) => {
    setIsFetching(true)
    setError(null)
    try {
      const response = await customFetch(`/api/workouts/${workoutId}`, {
        method: 'DELETE',
        headers: {
          Accept: 'application/json',
        },
      })
      if (!response.ok) {
        throw new Error(`Failed to delete workout (${response.status})`)
      }

      setWorkouts((prev) => {
        const update = prev.filter((w) => w.id !== workoutId)
        if (selectedId === workoutId) {
          setSelectedId(update[0]?.id ?? null)
        }
        return update
      })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete workout')
    } finally {
      setIsFetching(false)
    }

  }

  //old
  const visibleWorkouts = useMemo(() => (isAuthenticated ? workouts : []), [isAuthenticated, workouts])
  const isLoading = !isHydrated || isFetching
  const displayError = authError ?? error

  const filtered = useMemo(() => {
    if (!query.trim()) return workouts
    const q = query.toLowerCase()
    return visibleWorkouts.filter((w) => w.name.toLowerCase().includes(q) || w.primaryMuscleGroups.some((m) => m.toLowerCase().includes(q)))
  }, [visibleWorkouts, workouts, query])

  const selectedWorkout = visibleWorkouts.find((w) => w.id === selectedId) ?? null
  const selectedWorkoutSecondaryMuscles = useMemo(
    () =>
      selectedWorkout && selectedWorkoutDetails?.id === selectedWorkout.id
        ? (selectedWorkoutDetails.exercises.flatMap((exercise) => exercise.secondaryMuscles ?? []) ?? []) as MuscleName[]
        : [],
    [selectedWorkout, selectedWorkoutDetails]
  )

  const summary: WorkoutSummary | null = selectedWorkout
    ? {
        workoutName: selectedWorkout.name,
        totalExercises: selectedWorkout.exerciseCount,
        primaryMuscleGroups: selectedWorkout.primaryMuscleGroups,
      }
    : null

  return (
    <section className="mx-auto max-w-6xl px-6 py-12">
      <div className="mb-6">
        <PageTitle title="Workouts" />
      </div>

      <div className="grid grid-cols-12 gap-6">
        <div className="col-span-12 lg:col-span-7">
          <div className="mb-4 flex items-center gap-3">
            <div className="min-w-0 flex-1">
              <SearchInput value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search workouts" />
            </div>
            <OfflineTooltip isOnline={isOnline}>
              <Button variant="icon" size="icon" aria-label="Add" disabled={!isOnline} onClick={() => navigate('/workouts/create')}>
                <Plus size={20} />
              </Button>
            </OfflineTooltip>
          </div>

          {isLoading && (
            <div className="mb-4 rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-muted-foreground">
              Loading workouts...
            </div>
          )}
          {displayError && (
            <div className="mb-4 rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-destructive">
              {displayError}
            </div>
          )}
          {isOfflineData && (
            <OfflineBanner message="You're offline - your workouts will sync when you're back online." />
          )}

          {!isLoading && !error && filtered.length === 0 && (
            <div className="mb-4 rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-muted-foreground">
              No workouts found
            </div>
          )}

          <div className="space-y-4">
            {filtered.map((w, index) => (
              <Card
                key={w.id}
                role="button"
                data-testid={`workout-card-${w.name}`}
                tabIndex={0}
                aria-pressed={w.id === selectedId}
                className={`cursor-pointer transition-shadow focus-visible:ring-2 focus-visible:ring-brand ${w.id === selectedId ? 'ring-1 ring-brand' : ''}`}
                onClick={() => {
                  navigate(`/workouts/${w.id}`)
                }}
                onMouseEnter={() => setSelectedId(w.id)}
                onFocus={() => setSelectedId(w.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault()
                    navigate(`/workouts/${w.id}`)
                  }
                }}
              >
                <CardHeader>
                  <Link to={`/workouts/${w.id}`} className="min-w-0">
                    <CardTitle className="font-bold transition-colors hover:text-brand">
                      {w.name}
                    </CardTitle>
                  </Link>
                  <CardAction onClick={(e) => e.stopPropagation()}>
                    <DropdownMenu>
                      <DropdownMenuEllipsisTrigger aria-label="Options" />
                      <DropdownMenuEllipsisContent>
                        <DropdownMenuItem disabled={!isOnline} onSelect={() => navigate(`/workouts/edit/${w.id}`)}>Edit</DropdownMenuItem>
                        <DropdownMenuItem disabled={!isOnline} onSelect={() => handleDuplicate(w.id)}>Duplicate</DropdownMenuItem>
                        <DropdownMenuItem disabled={!isOnline} onSelect={() => setDeleteTargetId(w.id)} data-variant="destructive">Delete</DropdownMenuItem>
                      </DropdownMenuEllipsisContent>
                    </DropdownMenu>
                  </CardAction>
                </CardHeader>

                <CardContent className="flex items-end justify-between gap-4">
                  <div>
                    <p className="text-sm text-foreground"><span className="font-semibold">Primary Muscle Groups:</span> {w.primaryMuscleGroups.join(', ')}</p>
                    <p className="mt-1 text-sm text-foreground"><span className="font-semibold">Exercises:</span> {w.exercisePreview.join(', ')}</p>
                  </div>
                  <Button id={`start-workout-btn-${index}`} size="sm" onClick={(e) =>  { 
                    e.stopPropagation() 
                    navigate('/active-session', { state: { workout: w } })}}>
                    Start Workout
                  </Button>
                </CardContent>  
              </Card>
            ))}
          </div>
        </div>

        <aside className="col-span-12 lg:col-span-5">
          <MuscleDiagram
            highlightedMuscles={selectedWorkout?.primaryMuscleGroups ?? []}
            secondaryMuscles={selectedWorkoutSecondaryMuscles}
            variant="both"
          />

          <Card className="mt-6">
            <CardHeader>
              <CardTitle className="text-[1.15rem] font-bold">Workout Summary</CardTitle>
            </CardHeader>
            <CardContent>
              {summary ? (
                <div className="space-y-1">
                  <div className="text-sm text-foreground"><span className="font-semibold">Total exercises:</span> {summary.totalExercises}</div>
                  <div className="text-sm text-foreground"><span className="font-semibold">Primary:</span> {summary.primaryMuscleGroups.join(', ')}</div>
                </div>
              ) : (
                <div className="text-sm text-muted-foreground">Select a workout to view summary.</div>
              )}
            </CardContent>
          </Card>
        </aside>
      </div>
      <ConfirmDialog
        isOpen={deleteTargetId !== null}
        onClose={() => setDeleteTargetId(null)}
        isLoading={isFetching}
        variant="danger"
        title="Delete Workout"
        description="Are you certain you want to delete this workout?"
        confirmText="Delete"
        cancelText="Cancel"
        onConfirm={async () => {
          if (deleteTargetId) {
            const id = deleteTargetId
            setDeleteTargetId(null)
            await handleDelete(id)
          }
        }}
        />
    </section>
  )
}
