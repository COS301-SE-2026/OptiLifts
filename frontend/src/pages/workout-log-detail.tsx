import { useEffect, useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'
import { PageTitle } from '@/components/ui/page-title'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import MusclesSummary from '@/components/ui/muscles-summary'
import MuscleDiagram from '@/components/ui/muscle-diagram'
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

      {isLoading && (
        <div className="rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-muted-foreground">
          Loading workout log...
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
            <CardTitle className="text-xl font-bold">Workout log not found</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">The workout log you selected could not be found.</p>
          </CardContent>
        </Card>
      )}

      {!isLoading && !error && workout && (
        <div className="grid min-h-0 flex-1 gap-6 lg:grid-cols-[minmax(0,1.75fr)_minmax(360px,1.05fr)]">
          <div className="flex min-h-0 flex-col gap-4">
            <WorkoutLogExercisePlan
              exercises={workout.exercises}
              subtitle={workout.primaryMuscleGroups.join(', ')}
              className="min-h-0"
            />
          </div>

          <aside className="min-h-0">
            <Card className="flex min-h-0 h-full flex-col">
              <CardHeader>
                <CardTitle className="text-[1.05rem] font-bold">Summary</CardTitle>
              </CardHeader>
              <CardContent className="flex min-h-0 flex-1 flex-col">
                <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto pr-2 text-sm text-muted-foreground">
                  <MuscleDiagram highlightedMuscles={highlightedMuscles} variant="both" />
                  <MusclesSummary exercises={workout.exercises} />
                </div>
              </CardContent>
            </Card>
          </aside>
        </div>
      )}
    </section>
  )
}