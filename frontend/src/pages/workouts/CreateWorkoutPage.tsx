import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { PageTitle } from '@/components/ui/page-title'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ExerciseCard } from '@/components/ui/exercise-card'
import type { WorkoutExercise } from '@/types/create-workout'

const DUMMY_EXERCISES: WorkoutExercise[] = [
  { id: '1', name: 'Bicep Curl (Dumbbell)', muscle: 'Biceps', sets: [] },
  { id: '2', name: 'Bench Press (Barbell)', muscle: 'Chest', sets: [] },
]

function MuscleDiagramPlaceholder() {
  return (
    <div className="flex h-28 w-48 items-center justify-center rounded-lg border border-border bg-surface-2 text-muted-foreground text-xs text-center leading-tight select-none">
      Muscle<br />Diagram
    </div>
  )
}

export function CreateWorkoutPage() {
  const navigate = useNavigate()
  const [workoutName, setWorkoutName] = useState('')
  const [exercises, setExercises] = useState<WorkoutExercise[]>(DUMMY_EXERCISES)

  const removeExercise = (id: string) =>
    setExercises(prev => prev.filter(e => e.id !== id))

  const updateSets = (id: string, sets: WorkoutExercise['sets']) =>
    setExercises(prev => prev.map(e => e.id === id ? { ...e, sets } : e))

  return (
    <div className="flex min-h-[calc(100dvh-4rem)] bg-background">

      <div className="flex flex-1 flex-col gap-6 p-8 overflow-y-auto">

        {/* w-[780px] = w-96 input(384) + button(~104) + gap-3(12) + gap-[200px] + w-48 diagram(192) - some overlap */}
        <div className="flex flex-col gap-6 w-[780px]">

          <div className="flex items-center justify-between">
            <div className="flex flex-col gap-2">
              <PageTitle title="Create Workout" />
              <div className="flex items-center gap-3">
                <div className="flex flex-col gap-1 w-96">
                  <label className="text-xs font-semibold uppercase tracking-[1px] text-muted-foreground font-sans">
                    Workout Name
                  </label>
                  <Input
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
          </div>

        </div>

      </div>

    </div>
  )
}
