import { useMemo, useState } from 'react'
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
import type { Workout, WorkoutSummary } from '@/types/workout'
import { Plus} from 'lucide-react'

const STUB_WORKOUTS: Workout[] = [
  {
    id: 'w1', name: 'Pull',
    primaryMuscleGroups: ['Lats', 'Biceps'],
    exerciseCount: 5,
    exercisePreview: ['Lat Pulldown', 'High Row', 'Seated Row'],
    createdAt: '2026-05-01'
  },
  {
    id: 'w2',
    name: 'Push',
    primaryMuscleGroups: ['Chest', 'Triceps'],
    exerciseCount: 4,
    exercisePreview: ['Bench Press', 'Incline DB'],
    createdAt: '2026-05-02'
  },
  {
    id: 'w3',
    name: 'Legs',
    primaryMuscleGroups: ['Quadriceps', 'Hamstrings', 'Glutes'],
    exerciseCount: 6,
    exercisePreview: ['Squat', 'Leg Press', 'Romanian Deadlift'],
    createdAt: '2026-05-03'
  },
]

export default function WorkoutsPage() {
  const [workouts] = useState<Workout[]>(STUB_WORKOUTS)
  const [query, setQuery] = useState('')
  const [selectedId, setSelectedId] = useState<string | null>(workouts[0]?.id ?? null)

  const filtered = useMemo(() => {
    if (!query.trim()) return workouts
    const q = query.toLowerCase()
    return workouts.filter((w) => w.name.toLowerCase().includes(q) || w.primaryMuscleGroups.some((m) => m.toLowerCase().includes(q)))
  }, [workouts, query])

  const selectedWorkout = workouts.find((w) => w.id === selectedId) ?? null

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
            <Button variant="icon" size="icon" aria-label="Add">
              <Plus size={20} />
            </Button>
          </div>

          <div className="space-y-4">
            {filtered.map((w) => (
              <Card key={w.id} className={`cursor-pointer ${w.id === selectedId ? 'ring-1 ring-brand' : ''}`} onClick={() => setSelectedId(w.id)}>
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
