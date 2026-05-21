import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
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
import type { Workout, WorkoutSummary } from '@/types/workout'
import { Plus } from 'lucide-react'

export default function WorkoutsPage() {
  const { token, isHydrated } = useAuth()
  const navigate = useNavigate()
  const [workouts, setWorkouts] = useState<Workout[]>([])
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [isFetching, setIsFetching] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const authError = isHydrated && !token ? 'Please log in to view your workouts.' : null

  useEffect(() => {
    if (!isHydrated || !token) {
      return
    }

    let isActive = true
    async function loadWorkouts() {
      setIsFetching(true)
      setError(null)

      try {
        const response = await fetch('/api/workouts', {
          headers: {
            Authorization: `Bearer ${token}`,
            Accept: 'application/json',
          },
        })
        if (!response.ok) {
          throw new Error(`Failed to load workouts (${response.status})`)
        }

        const data = (await response.json()) as Workout[]

        if (isActive) {
          setWorkouts(data)
          setSelectedId((currentId) => currentId ?? data[0]?.id ?? null)
        }
      } catch (loadError) {
        if (isActive) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load workouts.')
        }
      } finally {
        if (isActive) {
          setIsFetching(false)
        }
      }
    }

    void loadWorkouts()

    return () => {
      isActive = false
    }
  }, [isHydrated, token])

  const visibleWorkouts = useMemo(() => (token ? workouts : []), [token, workouts])
  const isLoading = !isHydrated || isFetching
  const displayError = authError ?? error

  const filtered = useMemo(() => {
    if (!query.trim()) return workouts
    const q = query.toLowerCase()
    return visibleWorkouts.filter((w) => w.name.toLowerCase().includes(q) || w.primaryMuscleGroups.some((m) => m.toLowerCase().includes(q)))
  }, [visibleWorkouts, workouts, query])

  const selectedWorkout = visibleWorkouts.find((w) => w.id === selectedId) ?? null

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
        <div className="col-span-7">
          <div className="mb-4 flex items-center gap-3">
            <div className="min-w-0 flex-1">
              <SearchInput value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search workouts" />
            </div>
            <Button variant="icon" size="icon" aria-label="Add" onClick={() => navigate('/workouts/create')}>
              <Plus size={20} />
            </Button>
          </div>

          {isLoading && (
            <div className="mb-4 rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-muted-foreground">
              Loading workouts...
            </div>
          )}
          {displayError && (
            <div className="mb-4 rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-red-500">
              {displayError}
            </div>
          )}
          {!isLoading && !error && filtered.length === 0 && (
            <div className="mb-4 rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-muted-foreground">
              No workouts found
            </div>
          )}

          <div className="space-y-4">
            {filtered.map((w) => (
              <Card
                key={w.id}
                role="button"
                tabIndex={0}
                aria-pressed={w.id === selectedId}
                className={`cursor-pointer transition-shadow focus-visible:ring-2 focus-visible:ring-brand ${w.id === selectedId ? 'ring-1 ring-brand' : ''}`}
                onClick={() => setSelectedId(w.id)}
                onFocus={() => setSelectedId(w.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault()
                    setSelectedId(w.id)
                  }
                }}
              >
                <CardHeader>
                  <CardTitle className="font-bold">{w.name}</CardTitle>
                  <CardAction>
                    <DropdownMenu>
                      <DropdownMenuEllipsisTrigger aria-label="Options" />
                      <DropdownMenuEllipsisContent>
                        <DropdownMenuItem onSelect={() => {}}>Edit</DropdownMenuItem>
                        <DropdownMenuItem onSelect={() => {}}>Duplicate</DropdownMenuItem>
                        <DropdownMenuItem onSelect={() => {}} data-variant="destructive">Delete</DropdownMenuItem>
                      </DropdownMenuEllipsisContent>
                    </DropdownMenu>
                  </CardAction>
                </CardHeader>

                <CardContent>
                  <p className="text-sm text-foreground"><span className="font-semibold">Primary Muscle Groups:</span> {w.primaryMuscleGroups.join(', ')}</p>
                  <p className="text-sm mt-1 text-foreground"><span className="font-semibold">Exercises:</span> {w.exercisePreview.join(', ')}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </div>

        <aside className="col-span-5">
          <MuscleDiagram highlightedMuscles={selectedWorkout?.primaryMuscleGroups ?? []} variant="both" />

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
    </section>
  )
}
