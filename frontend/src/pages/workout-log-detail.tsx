import { useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { ArrowLeft, MoreVertical, Pencil } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { PageTitle } from '@/components/ui/page-title'
import MusclesSummary from '@/components/ui/muscles-summary'
import MuscleDiagram from '@/components/ui/muscle-diagram'
import WorkoutDetailShell from '@/components/ui/workout-detail-shell'
import WorkoutLogExercisePlan from '@/components/ui/workout-log-exercise-plan'
import { DropdownMenu, DropdownMenuEllipsisContent, DropdownMenuItem, DropdownMenuEllipsisTrigger } from '@/components/ui/dropdown-menu'
import { useAuth } from '@/context/auth-context'
import { customFetch } from '@/lib/custom-fetch'
import type { MuscleName } from '@/types/workout'
import type { WorkoutLogDetailResponse } from '@/types/workout-log-detail'
import { metricCheck, outputWeight } from '@/lib/weight-utils'

function formatVolume(totalVolume: number) {
  return `${outputWeight(totalVolume).toLocaleString(undefined, { maximumFractionDigits: 0 })} ${(metricCheck())? 'KG' : 'LB'}`
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
    return minutes === 0 ? '<1m' : `${minutes}m`
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
  const navigate = useNavigate()
  const location = useLocation()
  const { isAuthenticated, isHydrated } = useAuth()
  const [workout, setWorkout] = useState<WorkoutLogDetailResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleBackToPastWorkouts = () => {
    const locationDate = (location.state as { date?: string } | null)?.date
    const date = workout?.completedAt ?? locationDate ?? workout?.createdAt
    if (date) {
      navigate(`/past-workouts?date=${encodeURIComponent(date)}`, { state: { date } })
    } else {
      navigate('/past-workouts')
    }
  }

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
  const secondaryMuscles = useMemo(
    () => (workout?.exercises.flatMap((exercise) => exercise.secondaryMuscles ?? []) ?? []) as MuscleName[],
    [workout]
  )

  return (
    <section className="mx-auto flex min-h-[calc(100dvh-4rem)] w-full max-w-6xl flex-col gap-6 md:gap-8 overflow-y-auto px-4 pt-16 pb-6 sm:px-6 sm:py-10 md:py-12">
      <div className="flex flex-col gap-2">
        <div className="flex items-center gap-2">
          <Button
            variant="text"
            size="sm"
            onClick={handleBackToPastWorkouts}
            className="-ml-2 flex items-center gap-1 self-start text-muted-foreground hover:text-foreground"
          >
            <ArrowLeft className="h-4 w-4" />
            <span>Back to Past Workout</span>
          </Button>
        </div>

        <div className="flex flex-row items-center justify-between gap-3 sm:gap-6">
          <div className="min-w-0 flex-1">
            <p className="mb-1 text-xs font-semibold uppercase tracking-[0.2em] text-brand sm:text-sm">Workout Log</p>
            <PageTitle title={workoutLabel} />
            {workout?.completedAt ? (
              <p className="mt-1 text-xs sm:text-sm text-muted-foreground">Completed {formatCompletedDate(workout.completedAt)}</p>
            ) : null}
          </div>

          <div className="flex items-center gap-3 sm:gap-5 shrink-0">
            <div className="flex items-center gap-3 sm:gap-6 text-right">
              <div>
                <p className="text-[0.66rem] sm:text-[0.7rem] font-semibold uppercase tracking-[0.12em] sm:tracking-[0.16em] text-muted-foreground">Duration</p>
                <p className="text-[1.25rem] sm:text-[1.6rem] type-card-value mt-0.5 text-foreground">{workoutStats.duration}</p>
              </div>
              <div>
                <p className="text-[0.66rem] sm:text-[0.7rem] font-semibold uppercase tracking-[0.12em] sm:tracking-[0.16em] text-muted-foreground">Volume</p>
                <p className="text-[1.25rem] sm:text-[1.6rem] type-card-value mt-0.5 text-foreground">{workoutStats.volume}</p>
              </div>
              <div>
                <p className="text-[0.66rem] sm:text-[0.7rem] font-semibold uppercase tracking-[0.12em] sm:tracking-[0.16em] text-muted-foreground">Sets</p>
                <p className="text-[1.25rem] sm:text-[1.6rem] type-card-value mt-0.5 text-foreground">{workoutStats.sets}</p>
              </div>
            </div>

            {workoutId && logId && (
              <div className="ml-0.5 sm:ml-2">
                <DropdownMenu>
                  <DropdownMenuEllipsisTrigger aria-label="Options">
                    <MoreVertical />
                  </DropdownMenuEllipsisTrigger>
                  <DropdownMenuEllipsisContent align="end">
                    <DropdownMenuItem onSelect={() => navigate(`/workouts/${workoutId}/logs/${logId}/edit`)}>
                      <Pencil className="mr-1.5 h-3.5 w-3.5" />
                      Edit Log
                    </DropdownMenuItem>
                  </DropdownMenuEllipsisContent>
                </DropdownMenu>
              </div>
            )}
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
              <MuscleDiagram highlightedMuscles={highlightedMuscles} secondaryMuscles={secondaryMuscles} variant="both" />
              <MusclesSummary exercises={workout.exercises} />
            </>
          ) : null
        }
      />
    </section>
  )
}