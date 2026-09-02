import { useCallback, useEffect, useState } from 'react'
import { PageTitle } from '@/components/ui/page-title'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { customFetch } from '@/lib/custom-fetch'
import { useOnlineStatus } from '@/lib/use-online-status'
import { OfflineBanner } from '@/components/ui/offline-banner'
import { ExercisePickerDialog, type CatalogExercise } from '@/components/ui/exercise-picker-dialog'

type WorkoutRefDto = {
    workoutId: string
    workoutName: string
}

type ExerciseDiagnosisDto = {
    exerciseId: string
    exerciseName: string
    muscleGroup: string
    status: 'Progressing' | 'Regressing' | 'Plateau'
    slopePctPerWeek: number
    recommendation: string | null
    canSwapExercise: boolean
    computedAt: string
    workouts: WorkoutRefDto[]
}

type SwapTarg = {
    exerciseId: string
    workoutId: string
    workoutName: string
    muscleGroup: string
}

const STATUS_STYLES: Record<string, string> = {
    Progressing: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400',
    Plateau: 'bg-amber-500/10 text-amber-600 dark:text-amber-400',
    Regressing: 'bg-red-500/10 text-red-600 dark:text-red-400',
}

export default function PlateauPage() {
    const [exercises, setExercises] = useState<ExerciseDiagnosisDto[]>([])
    const [loading, setLoading] = useState(true)
    const [swapTarget, setSwapTarget] = useState<SwapTarg | null>(null)
    const [swapping, setSwapping] = useState(false)
    const isOnline = useOnlineStatus()

    const fetchDiagn = useCallback(async () => {
        setLoading(true)
        try {
            const resp = await customFetch('/api/training/plateau-page')
            if (resp.ok) {
                const out = await resp.json()
                setExercises(out)
            }
        } catch (error) {
            console.error('Error fetching plateau page:', error)
        } finally {
            setLoading(false)
        }
    }, [])

    useEffect(() => {
        void fetchDiagn()
    }, [fetchDiagn])

    const handleSwapExer = async (newExercise: CatalogExercise) => {
        if (!swapTarget) {
            return
        }

        setSwapping(true)
        try {
            const resp = await customFetch(`/api/workouts/${swapTarget.workoutId}/exercises/${swapTarget.exerciseId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ newExerciseId: newExercise.id }),
            })

            if (resp.ok) {
                setSwapTarget(null)
                await fetchDiagn()
            }
        } catch (error) {
            console.error('Error replacing workout exercise:', error)
        } finally {
            setSwapping(false)
        }
    }

    let pageContent

    if (loading) {
        pageContent = (
            <div className="text-center text-muted-foreground py-10">
                Loading progress...
            </div>
        )
    } else if (exercises.length === 0) {
        pageContent = (
            <div className="text-center text-muted-foreground py-10">
                No exercises have enough data yet. Keep logging your workouts and check back soon.
            </div>
        )
    } else {
        pageContent = (
            <div className="space-y-5">
                {exercises.map((exercise) => (
                    <Card key={exercise.exerciseId} className="p-6">
                        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                            <div>
                                <h2 className="text-xl font-bold text-foreground">{exercise.exerciseName}</h2>
                                <p className="mt-1 text-sm text-muted-foreground">
                                    {exercise.slopePctPerWeek >= 0 ? '+' : ''}{exercise.slopePctPerWeek.toFixed(1)}% per week
                                </p>
                            </div>
                            <span
                                className={`inline-flex w-fit items-center rounded-full px-3 py-1 text-xs font-semibold uppercase tracking-wide ${STATUS_STYLES[exercise.status] ?? 'bg-muted text-muted-foreground'}`}
                            >
                                {exercise.status}
                            </span>
                        </div>
                        {exercise.recommendation && (
                            <p className="mt-4 text-sm text-foreground/90 border-t border-border pt-4">
                                {exercise.recommendation}
                            </p>
                        )}
                        {exercise.canSwapExercise && exercise.workouts.length > 0 && (
                            <div className="mt-4 flex flex-wrap gap-2 border-t border-border pt-4">
                                {exercise.workouts.map((workout) => (
                                    <Button
                                        key={workout.workoutId}
                                        variant="outline"
                                        className="h-8 text-xs"
                                        disabled={!isOnline}
                                        onClick={() => setSwapTarget({
                                            exerciseId: exercise.exerciseId,
                                            workoutId: workout.workoutId,
                                            workoutName: workout.workoutName,
                                            muscleGroup: exercise.muscleGroup,
                                        })}

                                    >
                                        Swap in {workout.workoutName}
                                    </Button>
                                ))}
                            </div>
                        )}
                        {exercise.canSwapExercise && exercise.workouts.length === 0 && (
                            <p className="mt-4 text-sm text-muted-foreground border-t border-border pt-4">
                                You have already swapped this exercise to an alternative exercise.
                            </p>
                        )}
                    </Card>
                ))}
            </div>
        )
    }

    return (
        <section className="mx-auto max-w-6xl px-6 py-12">
            <div className="mb-6">
                <PageTitle title="PLATEAU" />
            </div>

            {!isOnline && (
                <OfflineBanner message="You're offline - progress data may be out of date until you reconnect." />
            )}

            {pageContent}

            <ExercisePickerDialog
                isOpen={swapTarget !== null}
                onClose={() => { if (!swapping) setSwapTarget(null) }}
                onSelect={(newExercise) => { void handleSwapExer(newExercise) }}
                title={swapTarget ? `Swap exercise in ${swapTarget.workoutName}` : 'Swap exercise'}
                initialMuscle={swapTarget?.muscleGroup}
            />
        </section>
    )
}
