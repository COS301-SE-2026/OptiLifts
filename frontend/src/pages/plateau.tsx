import { useEffect, useState } from 'react'
import { PageTitle } from '@/components/ui/page-title'
import { Card } from '@/components/ui/card'
import { customFetch } from '@/lib/custom-fetch'
import { useOnlineStatus } from '@/lib/use-online-status'
import { OfflineBanner } from '@/components/ui/offline-banner'

type ExerciseDiagnosisDto = {
    exerciseId: string
    exerciseName: string
    status: 'Progressing' | 'Regressing' | 'Plateau'
    slopePctPerWeek: number
    recommendation: string | null
    computedAt: string
}

const STATUS_STYLES: Record<string, string> = {
    Progressing: 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400',
    Plateau: 'bg-amber-500/10 text-amber-600 dark:text-amber-400',
    Regressing: 'bg-red-500/10 text-red-600 dark:text-red-400',
}

export default function PlateauPage() {
    const [exercises, setExercises] = useState<ExerciseDiagnosisDto[]>([])
    const [loading, setLoading] = useState(true)
    const isOnline = useOnlineStatus()

    useEffect(() => {
        const fetchDiagn = async () => {
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
        }

        void fetchDiagn()
    }, [])

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
        </section>
    )
}
