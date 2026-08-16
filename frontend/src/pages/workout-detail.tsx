import { useCallback, useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { PageTitle } from '@/components/ui/page-title'
import { Button } from '@/components/ui/button'
import ExercisePlan from '@/components/ui/exercise-plan'
import MusclesSummary from '@/components/ui/muscles-summary'
import MuscleDiagram from '@/components/ui/muscle-diagram'
import WorkoutDetailShell from '@/components/ui/workout-detail-shell'
import { useAuth } from '@/context/auth-context'
import { customFetch } from '@/lib/custom-fetch'
import type { ExercisePlanItem } from '@/types/exercise-plan'
import type { MuscleName } from '@/types/workout'
import { ExerciseDetailsPopup } from '@/components/ui/exercise-details-popup'
import type { WorkoutDetailExercise, WorkoutDetailResponse } from '@/types/workout-detail'
import { metricCheck, outputWeight } from '@/lib/weight-utils'
import { MoreVertical } from 'lucide-react'
import { DropdownMenu, DropdownMenuEllipsisContent, DropdownMenuItem, DropdownMenuEllipsisTrigger } from '@/components/ui/dropdown-menu'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { buildLabels } from '@/lib/exercise-format'
import { getCachedWorkoutDetail } from '@/lib/offline/workouts-cache'
import { useOnlineStatus } from '@/lib/use-online-status'
import { OfflineBanner } from '@/components/ui/offline-banner'

function formatRestTime(restTimeSeconds: number) {
  const minutes = Math.floor(restTimeSeconds / 60)
  const seconds = restTimeSeconds % 60

  if (seconds === 0) {
    return `${minutes} min rest`
  }

  return `${minutes}:${String(seconds).padStart(2, '0')} min rest`
}

function toExercisePlanItems(exercises: WorkoutDetailExercise[]): ExercisePlanItem[] {
  return exercises.map((exercise) => {
    const orderedSets = [...(exercise.sets ?? [])].sort((a, b) => a.orderIndex - b.orderIndex)
    const labels = buildLabels(orderedSets)

    return {
      name: exercise.name,
      subtitle: exercise.primaryMuscle ?? exercise.muscleGroup,
      exerciseType: exercise.exerciseType ?? 'WeightReps',
      exerciseId: exercise.exerciseId ?? exercise.id ?? exercise.workoutExerciseId,
      imageUrl: exercise.imageUrl,
      sets: orderedSets.map((set, setIndex) => ({
        label: labels[setIndex],
        reps: set.reps,
        weight: set.weight,
        duration: set.duration,
        distance: set.distance,
        restTime: formatRestTime(set.restTime),
      })),
      groupId: exercise.groupId,
      groupType: exercise.groupType,
      groupRestTime: exercise.groupRestTime,
    }
  })
}

function formatVolume(totalVolume: number) {
  return `${outputWeight(totalVolume).toLocaleString(undefined, { maximumFractionDigits: 0 })} ${(metricCheck())? 'KG' : 'LB'}`
}

export default function WorkoutDetailPage() {
  const { workoutId } = useParams()
  const navigate = useNavigate()
  const { isAuthenticated, isHydrated } = useAuth()
  const [workout, setWorkout] = useState<WorkoutDetailResponse | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [detailsExerciseId, setDetailsExerciseId] = useState<string | null>(null)
  const [refreshKey, setRefreshKey] = useState(0)
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null)
  const [isOfflineData, setIsOfflineData] = useState(false)
  const isOnline = useOnlineStatus()

  const handleWorkoutChanged = useCallback(() => {
    setRefreshKey((prev) => prev + 1)
  }, [])

  const handleDelete = async (targetId: string) => {
    setIsLoading(true)
    setError(null)
    try {
      const response = await customFetch(`/api/workouts/${targetId}`, {
        method: 'DELETE',
        headers: {
          Accept: 'application/json',
        },
      })
      if (!response.ok) {
        throw new Error(`Failed to delete workout (${response.status})`)
      }
      navigate('/workouts')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete workout')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    if (!isHydrated || !isAuthenticated || !workoutId) {
      return
    }

    let mounted = true

    const fetchWorkout = async () => {
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
          setIsOfflineData(false)
        }
      } catch (loadError) {
        const cached = await getCachedWorkoutDetail(workoutId)

        if (mounted && cached) {
          setWorkout(cached)
          setIsOfflineData(true)
          return
        }


        if (mounted) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load workout.')
        }
      } finally {
        if (mounted) {
          setIsLoading(false)
        }
      }
    }

    void fetchWorkout()

    return () => {
      mounted = false
    }
  }, [isAuthenticated, isHydrated, workoutId, refreshKey])

  const workoutLabel = workout?.name ?? 'Workout Detail'
  const plannedExercises = useMemo(
    () => (workout ? toExercisePlanItems(workout.exercises) : []),
    [workout]
  )

  const totalSets = useMemo(
    () => workout?.exercises.reduce((sum, item) => sum + item.sets.length, 0) ?? 0,
    [workout]
  )

  const totalVolume = useMemo(() => {
    if (!workout) {
      return 0
    }

    return workout.exercises.reduce((exerciseSum, item) => {
      const exerciseVolume = item.sets.reduce((setSum, set) => {
        const weight = set.weight ?? 0
        const reps = set.reps ?? 0
        return setSum + weight * reps
      }, 0)

      return exerciseSum + exerciseVolume
    }, 0)
  }, [workout])

  const workoutStats = useMemo(() => {
    return {
      volume: formatVolume(totalVolume),
      sets: `${totalSets}`,
    }
  }, [totalVolume, totalSets])

  const highlightedMuscles = useMemo(
    () => (workout?.primaryMuscleGroups ?? []) as MuscleName[],
    [workout]
  )
  const secondaryMuscles = useMemo(
    () => (workout?.exercises.flatMap((exercise) => exercise.secondaryMuscles ?? []) ?? []) as MuscleName[],
    [workout]
  )

  return (
    <section className="mx-auto flex min-h-[calc(100dvh-4rem)] w-full max-w-6xl flex-col gap-6 md:gap-8 overflow-y-auto px-4 pt-16 pb-6 sm:px-6 sm:py-10 md:py-12">
      {isOfflineData && (
        <OfflineBanner message="You're offline - showing a saved copy of this workout." />
      )}
      <div className="flex flex-row items-center justify-between gap-3 sm:gap-6">
        <div className="min-w-0 flex-1">
          <p className="mb-1 text-xs font-semibold uppercase tracking-[0.2em] text-brand sm:text-sm">Workout</p>
          <PageTitle title={workoutLabel} />
        </div>
        <div className="flex flex-col items-end gap-2 sm:gap-3 shrink-0">
          <div className="flex items-center gap-3 sm:gap-6 text-right">
            <div>
              <p className="text-[0.66rem] sm:text-[0.7rem] font-semibold uppercase tracking-[0.12em] sm:tracking-[0.16em] text-muted-foreground">Volume</p>
              <p className="text-[1.25rem] sm:text-[1.6rem] type-card-value mt-0.5 text-foreground">{workoutStats.volume}</p>
            </div>
            <div>
              <p className="text-[0.66rem] sm:text-[0.7rem] font-semibold uppercase tracking-[0.12em] sm:tracking-[0.16em] text-muted-foreground">Sets</p>
              <p className="text-[1.25rem] sm:text-[1.6rem] type-card-value mt-0.5 text-foreground">{workoutStats.sets}</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            <Button
              id="start-workout-btn"
              size="sm"
              disabled={!workout || isLoading}
              onClick={() => {
                if (workout) {
                  navigate('/active-session', { state: { workout } })
                }
              }}
            >
              Start Workout
            </Button>
            {workout && (
              <DropdownMenu>
                <DropdownMenuEllipsisTrigger aria-label="Options">
                  <MoreVertical />
                </DropdownMenuEllipsisTrigger>
                <DropdownMenuEllipsisContent align="end">
                  <DropdownMenuItem disabled={!isOnline} onSelect={() => navigate(`/workouts/edit/${workout.id}`)}>Edit</DropdownMenuItem>
                  <DropdownMenuItem disabled={!isOnline} onSelect={() => setDeleteTargetId(workout.id)} data-variant="destructive">
                    Delete
                  </DropdownMenuItem>
                </DropdownMenuEllipsisContent>
              </DropdownMenu>
            )}
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
              onOpenDetails={setDetailsExerciseId}
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
      <ExerciseDetailsPopup
        exerciseId={detailsExerciseId}
        onClose={() => setDetailsExerciseId(null)}
        onChanged={handleWorkoutChanged}
      />
      <ConfirmDialog
        isOpen={deleteTargetId !== null}
        onClose={() => setDeleteTargetId(null)}
        isLoading={isLoading}
        variant="danger"
        title="Delete Workout"
        description="Are you certain you want to delete this workout?"
        confirmText="Delete"
        cancelText="Cancel"
        onConfirm={async () => {
          if (deleteTargetId) {
            const id = deleteTargetId
            setDeleteTargetId(null)
            await handleDelete(id)
          }
        }}
      />
    </section>
  )
}