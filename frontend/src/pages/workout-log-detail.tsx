import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageTitle } from '@/components/ui/page-title'
import MusclesSummary from '@/components/ui/muscles-summary'
import MuscleDiagram from '@/components/ui/muscle-diagram'
import WorkoutDetailShell from '@/components/ui/workout-detail-shell'
import WorkoutLogExercisePlan from '@/components/ui/workout-log-exercise-plan'
import { useAuth } from '@/context/auth-context'
import { customFetch } from '@/lib/custom-fetch'
import type { MuscleName } from '@/types/workout'
import type { WorkoutLogDetailResponse } from '@/types/workout-log-detail'

function formatVolume(totalVolume: number) {
  return `${new Intl.NumberFormat('en-US').format(Math.round(totalVolume))} kg`
}

function formatDurationAsHours(duration: string | null) {
  if (!duration) {
    return '--:--'
  }

  const [hoursText, minutesText] = duration.split(':')
  const hours = Number.parseInt(hoursText ?? '0', 10)
  const minutes = Number.parseInt(minutesText ?? '0', 10)

  if (Number.isNaN(hours) || Number.isNaN(minutes)) {
    return duration
  }

  if (hours === 0) {
    return `${minutes}m`
  }

  return minutes === 0 ? `${hours}h` : `${hours}h ${minutes}m`
}

function formatCompletedDate(date: string) {
  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(date))
}

export default function WorkoutLogDetailPage() {
  const { workoutId, logId } = useParams()
  const { isAuthenticated, isHydrated } = useAuth()
  const [workout, setWorkout] = useState<WorkoutLogDetailResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!isHydrated || !isAuthenticated || !workoutId || !logId) {
      return
    }

    let mounted = true

    const loadWorkout = async () => {
      setIsLoading(true)
      setError(null)

      try {
        const response = await customFetch(`/api/workouts/${workoutId}/logs/${logId}`, {
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
          throw new Error(`Failed to load workout log (${response.status})`)
        }

        const data = (await response.json()) as WorkoutLogDetailResponse
        if (mounted) {
          setWorkout(data)
        }
      } catch (loadError) {
        if (mounted) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load workout log.')
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
  }, [isAuthenticated, isHydrated, logId, workoutId])

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

  const workoutLabel = workout?.name ?? 'Workout log'
  const workoutStats = useMemo(() => {
    if (!workout) {
      return { duration: '--:--', volume: '0 kg', sets: '0' }
    }

    const loggedSets = workout.exercises.flatMap((exercise) => exercise.sets)
    const totalVolume = loggedSets.reduce((setTotal, set) => setTotal + set.reps * set.weight, 0)
    const totalSets = loggedSets.length

    return {
      duration: formatDurationAsHours(workout.duration),
      volume: formatVolume(totalVolume),
      sets: `${totalSets}`,
    }
  }, [workout])
  const highlightedMuscles = (workout?.primaryMuscleGroups ?? []) as MuscleName[]

  return (
    <section className="mx-auto flex h-[calc(100dvh-4rem)] w-full max-w-6xl flex-col gap-8 overflow-hidden px-6 py-12">
      <div className="flex flex-none items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="mb-3 text-sm font-semibold uppercase tracking-[0.2em] text-brand">Workout Log</p>
          <PageTitle title={workoutLabel} />
          {workout?.completedAt ? (
            <p className="mt-2 text-sm text-muted-foreground">Completed {formatCompletedDate(workout.completedAt)}</p>
          ) : null}
        </div>

        <div className="flex flex-col items-start gap-4 lg:items-end">
          <div className="grid grid-cols-3 gap-6 justify-items-center text-center">
            <div>
              <p className="text-base text-muted-foreground">Duration</p>
              <p className="mt-1 text-xl font-bold text-foreground">{workoutStats.duration}</p>
            </div>
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
        loadingMessage="Loading workout log..."
        error={error}
        hasContent={workout !== null}
        notFoundTitle="Workout log not found"
        notFoundDescription="The workout log you selected could not be found."
        mainContent={
          workout ? (
            <WorkoutLogExercisePlan
              exercises={workout.exercises}
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