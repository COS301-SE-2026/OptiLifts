import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Dumbbell } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ExerciseCard } from '@/components/ui/exercise-card'
import { PageTitle } from '@/components/ui/page-title'
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
  { name: 'Pull Up', muscleGroup: 'Lats' as MuscleName },
  { name: 'Overhead Press', muscleGroup: 'Shoulders' as MuscleName },
  { name: 'Leg Press', muscleGroup: 'Hamstrings' as MuscleName },
] as const

const ALL_EXERCISES = [
  { name: 'Barbell Bench Press', muscleGroup: 'Chest' as MuscleName, equipment: 'Barbell' },
  { name: 'Back Squat', muscleGroup: 'Hamstrings' as MuscleName, equipment: 'Barbell' },
  { name: 'Dumbbell Shoulder Press', muscleGroup: 'Shoulders' as MuscleName, equipment: 'Dumbbell' },
  { name: 'Lat Pulldown', muscleGroup: 'Lats' as MuscleName, equipment: 'Machine' },
  { name: 'Bicep Curl', muscleGroup: 'Biceps' as MuscleName, equipment: 'Dumbbell' },
  { name: 'Tricep Pushdown', muscleGroup: 'Triceps' as MuscleName, equipment: 'Cable' },
  { name: 'Pull Up', muscleGroup: 'Lats' as MuscleName, equipment: 'Bodyweight' },
  { name: 'Leg Press', muscleGroup: 'Hamstrings' as MuscleName, equipment: 'Machine' },
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
  const [workoutName, setWorkoutName] = useState('')
  const [exercises, setExercises] = useState<WorkoutExercise[]>([])
  const [selectedMuscle, setSelectedMuscle] = useState<(typeof MUSCLE_OPTIONS)[number]>('All Muscles')
  const [selectedEquipment, setSelectedEquipment] = useState<(typeof EQUIPMENT_OPTIONS)[number]>('All Equipment')
  const [searchQuery, setSearchQuery] = useState('')

  const removeExercise = (id: string) =>
    setExercises(prev => prev.filter(e => e.id !== id))

  const updateSets = (id: string, sets: WorkoutExercise['sets']) =>
    setExercises(prev => prev.map(e => e.id === id ? { ...e, sets } : e))

  const addExercise = (name: string, muscle: MuscleName) =>
    setExercises(prev => [...prev, { id: `ex-${nextExerciseId++}`, name, muscle, sets: [] }])

  const filteredRecommended = RECOMMENDED_EXERCISES.filter((ex) => {
    const q = searchQuery.trim().toLowerCase()
    if (q && !ex.name.toLowerCase().includes(q) && !ex.muscleGroup.toLowerCase().includes(q)) return false
    return true
  })

  const filteredExercises = ALL_EXERCISES.filter((ex) => {
    const q = searchQuery.trim().toLowerCase()
    if (q && !ex.name.toLowerCase().includes(q) && !ex.muscleGroup.toLowerCase().includes(q)) return false
    if (selectedMuscle !== 'All Muscles' && ex.muscleGroup !== selectedMuscle) return false
    if (selectedEquipment !== 'All Equipment' && ex.equipment !== selectedEquipment) return false
    return true
  })

  return (
    <section className="w-full px-6 py-6">
      <div className="grid grid-cols-12 gap-6 items-start">
        <div className="col-span-12 lg:col-span-7 flex min-w-0 flex-col gap-6">

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
                <Button variant="default" size="sm" className="self-end h-8" disabled={!workoutName.trim()} onClick={() => navigate('/workouts')}>
                  Save Workout
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

        <div className="col-span-12 lg:col-span-5 min-w-0">
          <div className="flex w-full flex-col gap-4 lg:fixed lg:top-[6.5rem] lg:right-6 lg:z-10 lg:w-[min(28rem,calc(100vw-3rem))] lg:max-h-[calc(100dvh-8.5rem)] lg:overflow-y-auto">

            <Card className="w-full overflow-hidden border-border bg-card">
            <CardHeader className="px-4 py-1">
              <div className="flex items-center justify-between gap-3">
                <CardTitle className="text-base font-bold text-foreground">Recommended</CardTitle>
                <Button type="button" variant="text" className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2">
                  Refresh
                </Button>
              </div>
            </CardHeader>
            <CardContent className="px-0 pb-0">
              <div className="divide-y divide-border/70">
                <div className="max-h-44 overflow-y-auto">
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
              </div>
            </CardContent>
          </Card>

            <Card className="w-full overflow-hidden border-border bg-card">
            <CardHeader className="px-4 py-1">
              <div className="flex items-center justify-between gap-3">
                <CardTitle className="text-base font-bold text-foreground">Exercises</CardTitle>
                <Button type="button" variant="text" className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2">
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

              <div className="[&>div]:max-w-none [&>div]:w-full">
                <SearchInput
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  placeholder="Search"
                  aria-label="Search exercises"
                  className="h-8 w-full"
                />
              </div>

              <div className="divide-y divide-border/70 mt-2">
                <div className="max-h-44 overflow-y-auto">
                {filteredExercises.length > 0 ? filteredExercises.map((ex) => (
                  <div key={ex.name} className="flex items-center gap-3 px-2 py-2.5">
                    <Avatar className="size-9 shrink-0 border border-border">
                      <AvatarFallback className="bg-surface-2">
                        <Dumbbell className="size-4 text-muted-foreground" />
                      </AvatarFallback>
                    </Avatar>
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-sm font-semibold text-foreground">{ex.name}</div>
                      <div className="text-xs text-muted-foreground">{ex.muscleGroup} • {ex.equipment}</div>
                    </div>
                    <Button type="button" variant="icon" size="icon" aria-label={`Add ${ex.name}`} onClick={() => addExercise(ex.name, ex.muscleGroup)} className="size-6 rounded-md border-border bg-surface-2 text-foreground hover:bg-border">
                      <Plus size={12} />
                    </Button>
                  </div>
                )) : (
                  <p className="px-2 py-3 text-sm text-muted-foreground">No exercises match your filters.</p>
                )}
                </div>
              </div>
            </CardContent>
          </Card>

        </div>
        </div>
      </div>
    </section>
  )
}
