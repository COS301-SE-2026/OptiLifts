import { useCallback, useEffect, useState } from 'react'
import { PageTitle } from '@/components/ui/page-title'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { SearchInput } from '@/components/ui/search-input'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { customFetch } from '@/lib/custom-fetch'
import { useOnlineStatus } from '@/lib/use-online-status'
import { OfflineBanner } from '@/components/ui/offline-banner'
import { ExercisePickerDialog, type CatalogExercise } from '@/components/ui/exercise-picker-dialog'
import { AlertTriangle } from 'lucide-react'

type WorkoutRefDto = {
    workoutId: string
    workoutName: string
}

type TrendStatus = 'Progressing' | 'Regressing' | 'Plateau'

type ExerciseDiagnosisDto = {
    exerciseId: string
    exerciseName: string
    muscleGroup: string
    status: TrendStatus
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

const BAR_COLORS: Record<TrendStatus, string> = {
    Progressing: 'bg-emerald-500',
    Plateau: 'bg-amber-500',
    Regressing: 'bg-red-500',
}

const STATUS_FILTER_OPTIONS = ['All Statuses', 'Plateau', 'Regressing', 'Progressing'] as const
type StatusFilter = (typeof STATUS_FILTER_OPTIONS)[number]

export default function ProgressionPage() {
    const [exercises, setExercises] = useState<ExerciseDiagnosisDto[]>([])
    const [loading, setLoading] = useState(true)
    const [swapTarget, setSwapTarget] = useState<SwapTarg | null>(null)
    const [swapping, setSwapping] = useState(false)
    const isOnline = useOnlineStatus()
    const [searchQuery, setSearchQuery] = useState('')
    const [statusFilter, setStatusFilter] = useState<StatusFilter>('All Statuses')

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
        // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch-on-mount, not a state-adjustment effect
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

    const amountOfStatuses: Record<TrendStatus, number> = {
        Plateau: exercises.filter((e) => e.status === 'Plateau').length,
        Regressing: exercises.filter((e) => e.status === 'Regressing').length,
        Progressing: exercises.filter((e) => e.status === 'Progressing').length,
    }
    const maxCount = Math.max(1, amountOfStatuses.Plateau, amountOfStatuses.Regressing, amountOfStatuses.Progressing)

    const filtered = exercises.filter((e) => {
        const matchesSearch = e.exerciseName.toLowerCase().includes(searchQuery.toLowerCase())
        const matchesStatus = statusFilter === 'All Statuses' || e.status === statusFilter
        return matchesSearch && matchesStatus
    })

    let listOfCont

    if (loading) {
        listOfCont = (
            <p className="text-center text-muted-foreground py-10">Loading progress...</p>
        )
    } else if (exercises.length === 0) {
        listOfCont = (
            <p className="text-center text-muted-foreground py-10">
                No exercises have enough data yet. Keep logging your workouts and check back soon.
            </p>
        )
    } else if (filtered.length === 0) {
        listOfCont = (
            <p className="text-center text-muted-foreground py-10">No exercises match your filters.</p>
        )
    } else {
        listOfCont = (
            <div className="flex flex-col gap-3">
                {filtered.map((exercise) => (
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
                        {exercise.recommendation && !(exercise.canSwapExercise && exercise.workouts.length === 0) && (
                            <p className="mb-2 text-sm text-foreground/90 border-t border-border pt-4">
                                {exercise.recommendation}
                            </p>
                        )}
                        {exercise.canSwapExercise && exercise.workouts.length > 0 && (
                            <div className="mb-2 flex flex-wrap gap-2 border-t border-border pt-4">
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
                            <p className="text-sm text-muted-foreground border-t border-border pt-4">
                                You have already swapped this exercise to an alternative exercise.
                            </p>
                        )}
                    </Card>
                ))}
            </div>
        )
    }

    return (
        <section className="mx-auto max-w-6xl px-6 py-6 lg:h-[calc(100dvh-5rem)] lg:overflow-hidden">
            <div className="mb-6">
                <PageTitle title="PROGRESSION" />
            </div>

            {!isOnline && (
                <OfflineBanner message="You're offline - progress data may be out of date until you reconnect." />
            )}

            <div className="grid grid-cols-12 gap-6 lg:h-full lg:min-h-0">
                <div className="col-span-12 lg:col-span-7 flex min-w-0 flex-col gap-6 lg:h-full lg:min-h-0">
                    <div className="max-h-[calc(100dvh-15rem)] overflow-y-auto pr-1">
                        {listOfCont}
                    </div>
                </div>

                <div className="col-span-12 lg:col-span-5 min-w-0">
                    <div className="flex w-full flex-col gap-4 lg:sticky lg:top-[1.5rem] lg:max-h-[calc(100dvh-6.5rem)] lg:overflow-y-auto lg:[scrollbar-gutter:stable]">
                        <Card className="w-full overflow-hidden border-border bg-card">
                            <CardHeader className="px-4 py-1">
                                <CardTitle className="text-base font-bold text-foreground">Filter</CardTitle>
                            </CardHeader>
                            <CardContent className="flex min-h-0 flex-col gap-2 px-4 pb-4">
                                <SearchInput
                                    value={searchQuery}
                                    onChange={(e) => setSearchQuery(e.target.value)}
                                    placeholder="Search exercises"
                                    aria-label="Search exercises"
                                    className="h-8 w-full"
                                />

                                <DropdownMenu>
                                    <DropdownMenuTrigger variant="filter" className="w-full shadow-none">
                                        <span>{statusFilter}</span>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                                        {STATUS_FILTER_OPTIONS.map((o) => (
                                            <DropdownMenuItem key={o} onSelect={() => setStatusFilter(o)}>{o}</DropdownMenuItem>
                                        ))}
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            </CardContent>
                        </Card>

                        <Card className="w-full overflow-hidden border-border bg-card">
                            <CardHeader className="px-4 py-1">
                                <CardTitle className="text-base font-bold text-foreground">Overview</CardTitle>
                            </CardHeader>
                            <CardContent className="flex flex-col gap-3 px-4 pb-4">
                                {(['Plateau', 'Regressing', 'Progressing'] as const).map((status) => (
                                    <div key={status} className="flex items-center gap-2 text-xs">
                                        <span className="w-20 shrink-0 font-semibold text-muted-foreground">{status}</span>
                                        <div className="h-2 flex-1 overflow-hidden rounded-full bg-surface-2">
                                            <div
                                                className={`h-full rounded-full ${BAR_COLORS[status]}`}
                                                style={{ width: `${(amountOfStatuses[status] / maxCount) * 100}%` }}
                                            />
                                        </div>
                                        <span className="w-4 shrink-0 text-right font-semibold text-foreground">{amountOfStatuses[status]}</span>
                                    </div>
                                ))}
                                {amountOfStatuses.Plateau === 0 && amountOfStatuses.Regressing === 0 && (
                                    <p className="flex items-center gap-2 text-xs text-muted-foreground">
                                        <AlertTriangle className="h-3.5 w-3.5 shrink-0" />
                                        No plateaus or regressions right now.
                                    </p>
                                )}
                            </CardContent>
                        </Card>
                    </div>
                </div>
            </div>

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
