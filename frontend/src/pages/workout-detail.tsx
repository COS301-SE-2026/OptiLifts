import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageTitle } from '@/components/ui/page-title'
import ExercisePlan from '@/components/ui/exercise-plan'
import MusclesSummary from '@/components/ui/muscles-summary'
import MuscleDiagram from '@/components/ui/muscle-diagram'
import WorkoutDetailShell from '@/components/ui/workout-detail-shell'
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
    <section className="mx-auto flex h-[calc(100dvh-4rem)] w-full max-w-6xl flex-col gap-8 overflow-hidden px-6 py-12">
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

      <WorkoutDetailShell
        isLoading={isLoading}
        loadingMessage="Loading workout..."
        error={error}
        hasContent={workout !== null}
        notFoundTitle="Workout not found"
        notFoundDescription="The workout you selected could not be found."
        mainContent={
          workout ? (
            <ExercisePlan
              exercises={plannedExercises}
              subtitle={workout.primaryMuscleGroups.join(', ')}
              className="min-h-0"
            />
          ) : null
        }
        summaryContent={
          workout ? (
            <>
              <MuscleDiagram highlightedMuscles={highlightedMuscles} variant="both" />
              <MusclesSummary exercises={workout.exercises} />
            </>
          ) : null
        }
      />
    </section>
  )
}