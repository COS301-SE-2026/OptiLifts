import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageTitle } from '@/components/ui/page-title'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import ExercisePlan from '@/components/ui/exercise-plan'
import MuscleDiagram from '@/components/ui/muscle-diagram'
import { useAuth } from '@/context/auth-context'
import { customFetch } from '@/lib/custom-fetch'
import type { ExercisePlanItem } from '@/types/exercise-plan'
import type { MuscleName } from '@/types/workout'
import type { WorkoutDetailExercise, WorkoutDetailResponse } from '@/types/workout-detail'

function formatRestTime(restTimeSeconds: number) {
  const minutes = Math.floor(restTimeSeconds / 60)
  const seconds = restTimeSeconds % 60

  if (seconds === 0) {
    return `${minutes} min rest`
  }

  return `${minutes}:${String(seconds).padStart(2, '0')} min rest`
}

function toExercisePlanItems(exercises: WorkoutDetailExercise[]): ExercisePlanItem[] {
  return exercises.map((exercise) => ({
    name: exercise.name,
    subtitle: exercise.primaryMuscle,
    exerciseType: exercise.exerciseType,
    sets: exercise.sets.map((set) => ({
      label: `${set.orderIndex}`,
      reps: set.reps,
      weight: set.weight,
      duration: set.duration,
      distance: set.distance,
      restTime: formatRestTime(set.restTime),
    })),
  }))
}

function formatVolume(totalVolume: number) {
  return `${new Intl.NumberFormat('en-US').format(Math.round(totalVolume))} kg`
}

export default function WorkoutDetailPage() {
  const { workoutId } = useParams()
  const { isAuthenticated, isHydrated } = useAuth()
  const [workout, setWorkout] = useState<WorkoutDetailResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!isHydrated || !isAuthenticated || !workoutId) {
      return
    }

    let mounted = true

    const loadWorkout = async () => {
      setIsLoading(true)
      setError(null)

      try {
        const response = await customFetch(`/api/workouts/${workoutId}`, {
          headers: {
            Accept: 'application/json',
          },
        })

        if (response.status === 404) {
          if (mounted) {
            setWorkout(null)
          }
          return
        }

        if (!response.ok) {
          throw new Error(`Failed to load workout (${response.status})`)
        }

        const data = (await response.json()) as WorkoutDetailResponse
        if (mounted) {
          setWorkout(data)
        }
      } catch (loadError) {
        if (mounted) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load workout.')
        }
      } finally {
        if (mounted) {
          setIsLoading(false)
        }
      }
    }

    void loadWorkout()

    return () => {
      mounted = false
    }
  }, [isAuthenticated, isHydrated, workoutId])

  useEffect(() => {
    const previousBodyOverflow = document.body.style.overflow
    const previousHtmlOverflow = document.documentElement.style.overflow

    document.body.style.overflow = 'hidden'
    document.documentElement.style.overflow = 'hidden'

    return () => {
      document.body.style.overflow = previousBodyOverflow
      document.documentElement.style.overflow = previousHtmlOverflow
    }
  }, [])

  const workoutLabel = workout?.name ?? 'Workout'
  const plannedExercises = workout ? toExercisePlanItems(workout.exercises) : []
  const highlightedMuscles = (workout?.primaryMuscleGroups ?? []) as MuscleName[]
  const workoutStats = useMemo(() => {
    if (!workout) {
      return { volume: '0 kg', sets: '0' }
    }

    const totalVolume = workout.exercises.reduce((exerciseTotal, exercise) => {
      return (
        exerciseTotal +
        exercise.sets.reduce((setTotal, set) => {
          const reps = set.reps ?? 0
          const weight = set.weight ?? 0
          return setTotal + reps * weight
        }, 0)
      )
    }, 0)

    const totalSets = workout.exercises.reduce((exerciseTotal, exercise) => exerciseTotal + exercise.sets.length, 0)

    return {
      volume: formatVolume(totalVolume),
      sets: `${totalSets}`,
    }
  }, [workout])

  return (
    <section className="mx-auto flex h-[calc(100dvh-4rem)] w-full max-w-none flex-col gap-8 overflow-hidden px-6 py-12">
      <div className="flex flex-none items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="mb-3 text-sm font-semibold uppercase tracking-[0.2em] text-brand">Workout</p>
          <PageTitle title={workoutLabel} />
        </div>

        <div className="flex flex-col items-start gap-4 lg:items-end">
          <div className="grid grid-cols-2 gap-8 text-left lg:text-right">
            <div>
              <p className="text-base text-muted-foreground">Volume</p>
              <p className="mt-1 text-xl font-bold text-foreground">{workoutStats.volume}</p>
            </div>
            <div>
              <p className="text-base text-muted-foreground">Sets</p>
              <p className="mt-1 text-xl font-bold text-foreground">{workoutStats.sets}</p>
            </div>
          </div>
        </div>
      </div>

      {isLoading && (
        <div className="rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-muted-foreground">
          Loading workout...
        </div>
      )}

      {error && (
        <div className="rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-red-500">
          {error}
        </div>
      )}

      {!isLoading && !error && !workout && (
        <Card>
          <CardHeader>
            <CardTitle className="text-xl font-bold">Workout not found</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">The workout you selected could not be found.</p>
          </CardContent>
        </Card>
      )}

      {!isLoading && !error && workout && (
        <div className="grid min-h-0 flex-1 gap-6 lg:grid-cols-[minmax(0,2fr)_minmax(360px,1fr)]">
          <div className="flex min-h-0 flex-col gap-4">
            <ExercisePlan
              exercises={plannedExercises}
              subtitle={workout.primaryMuscleGroups.join(', ')}
              className="min-h-0"
            />
          </div>

          <aside className="min-h-0 space-y-6">
            <Card>
              <CardHeader>
                <CardTitle className="text-[1.05rem] font-bold">Summary</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3 text-sm text-muted-foreground">
                  <MuscleDiagram highlightedMuscles={highlightedMuscles} variant="both" />
                  <div className="rounded-2xl border border-dashed border-border bg-surface-2/40 px-4 py-6">
                    Workout summary placeholder
                  </div>
                </div>
              </CardContent>
            </Card>
          </aside>
        </div>
      )}
    </section>
  )
}