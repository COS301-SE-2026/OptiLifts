import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/context/auth-context'
import { Plus, Dumbbell } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ExerciseCard } from '@/components/ui/exercise-card'
import { PageTitle } from '@/components/ui/page-title'
import { CreateExercise } from '@/components/ui/create-exercise'
import { SearchInput } from '@/components/ui/search-input'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { WorkoutExercise } from '@/types/create-workout'
import type { MuscleName } from '@/types/workout'

const RECOMMENDED_EXERCISES = [
  { name: 'Bicep curl', muscleGroup: 'Biceps' as MuscleName },
  { name: 'Tricep pushdown', muscleGroup: 'Triceps' as MuscleName },
  { name: 'Lat pulldown', muscleGroup: 'Lats' as MuscleName },
] as const

const MUSCLE_OPTIONS = ['All Muscles', 'Biceps', 'Triceps', 'Lats', 'Hamstrings', 'Chest', 'Shoulders'] as const
const EQUIPMENT_OPTIONS = ['All Equipment', 'Dumbbell', 'Barbell', 'Cable', 'Machine', 'Bodyweight'] as const

function MuscleDiagramPlaceholder() 
{
  return (
    <div className="flex h-28 w-48 items-center justify-center rounded-lg border border-border bg-surface-2 text-muted-foreground text-xs text-center leading-tight select-none">
      Muscle<br />Diagram
    </div>
  )
}

let nextExerciseId = 0

export default function CreateWorkoutPage() {
  const navigate = useNavigate()
  const { token } = useAuth()
  const [workoutName, setWorkoutName] = useState('')
  const [exercises, setExercises] = useState<WorkoutExercise[]>([])
  const [saving, setSaving] = useState(false)
  const [selectedMuscle, setSelectedMuscle] = useState<(typeof MUSCLE_OPTIONS)[number]>('All Muscles')
  const [selectedEquipment, setSelectedEquipment] = useState<(typeof EQUIPMENT_OPTIONS)[number]>('All Equipment')
  const [searchQuery, setSearchQuery] = useState('')

  const removeExercise = (id: string) =>
    setExercises(prev => prev.filter(e => e.id !== id))

  const updateSets = (id: string, sets: WorkoutExercise['sets']) =>
    setExercises(prev => prev.map(e => e.id === id ? { ...e, sets } : e))

  const addExercise = (name: string, muscle: MuscleName) =>
    setExercises(prev => [...prev, { id: `ex-${nextExerciseId++}`, name, muscle, sets: [] }])

  const saveWorkout = async () => {
    if (!workoutName.trim() || !token) return
    setSaving(true)
    try {
      const res = await fetch('/api/workouts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
        body: JSON.stringify({ folderId: null, name: workoutName.trim(), dayIndex: null, sets: [] }),
      })
      if (res.ok) navigate('/workouts')
    } finally {
      setSaving(false)
    }
  }
  
  const [isCreateExerciseOpen, setIsCreateExerciseOpen] = useState(false)

  const filteredRecommended = RECOMMENDED_EXERCISES.filter((ex) => {
    const q = searchQuery.trim().toLowerCase()
    if (q && !ex.name.toLowerCase().includes(q) && !ex.muscleGroup.toLowerCase().includes(q)) return false
    if (selectedMuscle !== 'All Muscles' && ex.muscleGroup !== selectedMuscle) return false
    return true
  })

  return (
    <section className="w-full px-6 py-6">
      <div className="grid grid-cols-12 gap-6">
        <div className="col-span-12 lg:col-span-7 flex flex-col gap-6">

          <div className="flex items-center justify-between">
            <div className="flex flex-col gap-2">
              <PageTitle title="Create Workout" />
              <div className="flex items-center gap-3">
                <div className="flex flex-col gap-1 w-80">
                  <label htmlFor="workout-name" className="text-xs font-semibold uppercase tracking-[1px] text-muted-foreground font-sans">
                    Workout Name
                  </label>
                  <Input
                    id="workout-name"
                    variant="default"
                    placeholder="e.g. Push Day A"
                    value={workoutName}
                    onChange={e => setWorkoutName(e.target.value)}
                  />
                </div>
                <Button variant="default" size="sm" className="self-end h-8" disabled={!workoutName.trim() || saving} onClick={saveWorkout}>
                  {saving ? 'Saving…' : 'Save Workout'}
                </Button>
              </div>
            </div>
            <MuscleDiagramPlaceholder />
          </div>

          <div className="flex flex-col gap-3">
            {exercises.map(ex => (
              <ExerciseCard key={ex.id} exercise={ex} onRemove={removeExercise} onSetsChange={updateSets} />
            ))}
            {exercises.length === 0 && (
              <p className="text-sm text-muted-foreground">No exercises added yet. Use the panel on the right to add some.</p>
            )}
          </div>

        </div>

        <div className="col-span-12 lg:col-span-5 flex flex-col gap-4">

          <Card className="overflow-hidden border-border bg-card">
            <CardHeader className="px-4 py-3">
              <div className="flex items-center justify-between gap-3">
                <CardTitle className="text-base font-bold text-foreground">Recommended</CardTitle>
                <Button type="button" variant="text" className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2">
                  Refresh
                </Button>
              </div>
            </CardHeader>
            <CardContent className="px-0 pb-0">
              <div className="divide-y divide-border/70">
                {filteredRecommended.length > 0 ? filteredRecommended.map((ex) => (
                  <div key={ex.name} className="flex items-center gap-3 px-4 py-2.5">
                    <Avatar className="size-9 shrink-0 border border-border">
                      <AvatarFallback className="bg-surface-2">
                        <Dumbbell className="size-4 text-muted-foreground" />
                      </AvatarFallback>
                    </Avatar>
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-sm font-semibold text-foreground">{ex.name}</div>
                      <div className="text-xs text-muted-foreground">{ex.muscleGroup}</div>
                    </div>
                    <Button type="button" variant="icon" size="icon" aria-label={`Add ${ex.name}`} onClick={() => addExercise(ex.name, ex.muscleGroup)} className="size-6 rounded-md border-border bg-surface-2 text-foreground hover:bg-border">
                      <Plus size={12} />
                    </Button>
                  </div>
                )) : (
                  <p className="px-4 py-3 text-sm text-muted-foreground">No recommended exercises match your search.</p>
                )}
              </div>
            </CardContent>
          </Card>

          <Card className="overflow-hidden border-border bg-card">
            <CardHeader className="px-4 py-3">
              <div className="flex items-center justify-between gap-3">
                <CardTitle className="text-base font-bold text-foreground">Exercises</CardTitle>
                <Button type="button" variant="text" className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2" onClick={() => setIsCreateExerciseOpen(true)}>
                  + Create Exercise
                </Button>
              </div>
            </CardHeader>
            <CardContent className="flex flex-col gap-2 px-4 pb-4">
              <DropdownMenu>
                <DropdownMenuTrigger variant="filter" className="w-full shadow-none">
                  <span>{selectedMuscle}</span>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width]">
                  {MUSCLE_OPTIONS.map(o => (
                    <DropdownMenuItem key={o} onSelect={() => setSelectedMuscle(o)}>{o}</DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>

              <DropdownMenu>
                <DropdownMenuTrigger variant="filter" className="w-full shadow-none">
                  <span>{selectedEquipment}</span>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width]">
                  {EQUIPMENT_OPTIONS.map(o => (
                    <DropdownMenuItem key={o} onSelect={() => setSelectedEquipment(o)}>{o}</DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>

              <SearchInput
                value={searchQuery}
                onChange={e => setSearchQuery(e.target.value)}
                placeholder="Search"
                aria-label="Search exercises"
                className="h-8"
              />
            </CardContent>
          </Card>

        </div>
      </div>
      <CreateExercise isOpen={isCreateExerciseOpen} onCancel={() => setIsCreateExerciseOpen(false)} />
    </section>
  )
}
