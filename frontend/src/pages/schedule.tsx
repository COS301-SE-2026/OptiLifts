import { useEffect, useState } from 'react'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { PageTitle } from '@/components/ui/page-title'
import { customFetch } from '@/lib/custom-fetch'

const MUSCLECAT_MAP: Record<string, string> = {
    Chest: 'Chest',
    Lats: 'Back',
    'Lower Back': 'Back',
    'Middle Back': 'Back',
    Trapezius: 'Back',
    Shoulders: 'Arms',
    Biceps: 'Arms',
    Forearms: 'Arms',
    Quadriceps:'Legs',
    Hamstrings:'Legs',
    Calves:'Legs',
    Glutes:'Legs',
    Abductors:'Legs',
    Adductors:'Legs',
    Abdominals:'Core',
}

interface MuscleDistributionItem {
    readonly muscleGroup: string
    readonly setCount: number
    readonly percentage: number
}

interface AnalyticsResponse {
    readonly muscleDistribution: readonly MuscleDistributionItem[]
}

export default function SchedulePage() {
    const [error, setError] = useState<string | null>(null)
    const [muscleValues, setMuscleValues] = useState<Record<string, number>>({
        Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,})

    // const [ setIsLoading] = useState(true) //put isLoading in here, and add a loading icon
    useEffect(() => {
        async function fetchAnalytics() {
            try {
                setError(null)
                const resp = await customFetch('/api/users/me/schedule/analytics')
                if (resp.ok) {
                    const data = (await resp.json()) as AnalyticsResponse
                    const aggre: Record<string, number> = {
                        Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,
                    }
                    if (data.muscleDistribution) {
                        data.muscleDistribution.forEach((item) => {
                            const mappedCat = MUSCLECAT_MAP[item.muscleGroup]
                            if (mappedCat && mappedCat in aggre) {
                                aggre[mappedCat] += item.setCount
                            }
                        })
                    }
                    setMuscleValues(aggre)
                }
            } catch (error) {
                setError(error instanceof Error ? error.message : 'Could not load analytics')
            } finally {
                // setIsLoading(false)
            }
        }
        fetchAnalytics()
    }, [])

    return (
        <section className="mx-auto max-w-6xl px-6 py-12">
            <div className="mb-6">
                <PageTitle title="Scheduler" />
            </div>
            <div className="p-6 max-w-md bg-card border border-border rounded-xl">
                <h2 className="text-xl font-bold mb-4">Muscle Balance Chart</h2>
                {error ? (
                    <div
                    className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive"
                    role="alert"
                >
                    {error}
                </div>
                ) : (
                    <SpiderGraph data={muscleValues} />
                )}                
            </div>
        </section>
    )
}

