import { useEffect, useState, useMemo } from 'react'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { PageTitle } from '@/components/ui/page-title'
import { customFetch } from '@/lib/custom-fetch'
import {X, Plus, ChevronDown, Calendar, Loader2, AlertCircle} from 'lucide-react'
import {Button} from '@/components/ui/button'
import {Card, CardContent} from '@/components/ui/card'

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
    readonly totalWorkouts: number
    readonly totalVolume: number
    readonly totalSets: number
    readonly muscleDistribution: readonly MuscleDistributionItem[]
}

interface ScheduledEntryDto {
    readonly id: string
    readonly workoutId: string
    readonly workoutName: string
    readonly scheduled: string
    readonly status: string
    readonly primaryMuscleGroups: string[]
    readonly exerciseCount: number
    readonly exercisePreview: string[]
    readonly totalVolume: number
    readonly totalSets: number
}
const DAYS = [
    {
        name: 'MON',
        label: 'Monday'
    },
    {
        name: 'TUE',
        label: 'Tuesday'
    },
    {
        name: 'WED',
        label: 'Wednesday'
    },
    {
        name: 'THU',
        label: 'Thursday'
    },
    {
        name: 'FRI',
        label: 'Friday'
    },
    {
        name: 'SAT',
        label: 'Saturday'
    },
    {
        name: 'SUN',
        label: 'Sunday'
    },
]

export default function SchedulePage() {
    const [scheduleEntries, setScheduleEntries] = useState<ScheduledEntryDto[]>([])
    const [analytics, setAnalytics] = useState<AnalyticsResponse| null>(null)
    const [isLoading, setIsLoading] = useState(true)
    const [isDeleting, setIsDeleting] = useState<string | null>(null)

    const [error, setError] = useState<string | null>(null)
    const [muscleValues, setMuscleValues] = useState<Record<string, number>>({
        Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,})

    //calculate the dates
    const weekDates = useMemo(() => {
        const now = new Date()
        const currentDay = now.getDay()
        const diffToMonday = currentDay === 0 ? -6 : 1 - currentDay

        const monday = new Date(now)
        monday.setDate(now.getDate() + diffToMonday)
        monday.setHours(0,0,0,0)
        const dates: Date[] = []
        for (let i = 0; i <7; i++){
            const d = new Date(monday)
            d.setDate(monday.getDate() + i)
            dates.push(d)
        }
        return dates
    }, [])

    const fetchScheduleAndAnalytics =async () => {
        setIsLoading(true)
        setError(null)
        try {
            const [scheduleResp, analyticsResp] = await Promise.all([
                customFetch('/api/users/me/schedule'),
                customFetch('/api/users/me/schedule/analytics')
            ])
            if (!scheduleResp.ok) {
                throw new Error(`Failed to load schedules (${scheduleResp.status})`)
            }
            if (!analyticsResp.ok) {
                throw new Error(`Failed to load analytics (${analyticsResp.status})`)
            }
            const scheduleData = (await scheduleResp.json()) as ScheduledEntryDto[]
            const analyticsData = (await analyticsResp.json()) as AnalyticsResponse

            setScheduleEntries(scheduleData)
            setAnalytics(analyticsData)

            const aggre: Record<string, number> = {
                    Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,
            }
            if (analyticsData.muscleDistribution) {
                analyticsData.muscleDistribution.forEach((item) => {
                    const mappedCat = MUSCLECAT_MAP[item.muscleGroup]
                    if (mappedCat && mappedCat in aggre) {
                        aggre[mappedCat] += item.setCount
                    }
                })
            }
            setMuscleValues(aggre)
        } catch (error) {
            setError(error instanceof Error ? error.message : 'Could not load analytics')
        } finally {
            setIsLoading(false)
        }
    }

    // const [ setIsLoading] = useState(true) //put isLoading in here, and add a loading icon
    useEffect(() => {
        fetchScheduleAndAnalytics()
    }, [weekDates])

    const handleDeletingSession = async (sessionId: string) => {
        if (isDeleting) return
        setIsDeleting(sessionId)
        try {
            const response = await customFetch(`/api/users/me/schedule/sessions/${sessionId}`, { method: 'DELETE',})
            if (!response.ok) {
                const errData = await response.json().catch(() => ({})) 
                throw new Error(errData.message || 'Could not delete scheduled workout.')
            }

            await fetchScheduleAndAnalytics()
        } catch(err) {
            setError(err instanceof Error ? err.message : 'Failed to delete workout')
        } finally {
            setIsDeleting(null)
        }
    }

    const handleAddClick = (date: Date) => {
        //placeholder for adding workout implementation (needs a popup)
    }

    const isSameDay = (date1: Date, date2: string) => {
        const d1 = new Date(date1)
        const d2 = new Date(date2)
        return d1.getFullYear() === d2.getFullYear() && d1.getMonth() === d2.getMonth() && d1.getDate() === d2.getDate()
    }
    const weeklydays = weekDates.map((dayDate, index) => {
        const dayM = DAYS[index]
        const sessionsOnDay = scheduleEntries.filter(entry => isSameDay(dayDate, entry.scheduled))
        return {
            date: dayDate,
            name: dayM.name,
            fullName: dayM.label,
            formattedDate: dayDate.getDate(),
            sessions: sessionsOnDay
        }
    })

    //styling constants for same style aspects
    const statLABEL = "text-[10px] font-semibold uppercase tracking-wider text-muted-foreground block"
    const statVALUE = "text-sm font-bold text-foreground block"
    const cardDETAIL = "text-xs text-muted-foreground leading-normal"


    //COMPONENTS
    //vertical day headers
    interface VerticalDayHeaderProps{
        readonly name: string
        readonly date: number
    }
    function VerticalDayHeader({ name, date}: VerticalDayHeaderProps) {
        return (
            <div className="font-display tracking-widest text-muted-foreground [writing-mode:vertical-lr] rotate-180 select-none py-1 border-r border-border/80 flex flex-col items-center justify-center w-12 text-center mr-1 flex-shrink-0">
                <div className="text-xl md:text-2xl font-bold leading-none">{name}</div>
                <div className="text-xs md:text-sm font-sans font-semibold text-muted-foreground/80 mt-1 leading-none">{date}</div>
            </div>
        )
    }

    //workoutcards
    interface WorkoutCardProps{
        readonly session: ScheduledEntryDto
        readonly isDeleting: boolean
        readonly onDelete: (id:string) => void
    }
    function WorkoutCard({session, isDeleting, onDelete}: WorkoutCardProps){
        return 
    }
    interface EmptyDayCardProps{
        readonly fullName: string
        readonly onClick: () => void
    }
    function EmptyDayCard({fullName, onClick}: EmptyDayCardProps){
        return
    }
    //summary for week/month
    interface SummaryCardProps{
        readonly totalWorkouts: number
        readonly totalVolume: number
        readonly totalSets: number
    }
    function SummaryCard({totalWorkouts, totalVolume, totalSets}: SummaryCardProps){
        return
    }

    return (
        <section className="mx-auto max-w-6xl px-6 py-12">
            <div className="mb-6">
                <PageTitle title="Scheduler" />
            </div>

            {/* workout cards */}
            <div className="col-span-12 lg:col-span-7 space-y-6">
                {isLoading ? (
                    Array.from({length:7}).map((_,index) => (
                        <div key={index} className="flex items-center gap-4 animate-pulse">
                            <div className="w-12 h-14 border-r border-border flex flex-col items-center justify-center mr-1"/>    
                            <div className="flex-1 h-28 bg-surface-2 border border-border rounded-xl"/>
                            <div className="w-10 h-10"/>
                        </div>
                    ))
                ) : (
                    weeklydays.map((day) => {
                        const hasWorkouts = day.sessions.length < 0;
                        return (
                            <div key={day.name} className="flex items-stretch gap-4 min-h-[110px]">
                                <VerticalDayHeader name={day.name} date={day.formattedDate} />

                                {hasWorkouts ? (
                                    <div className="flex-1 flex flex-col gap-4">
                                        {day.sessions.map((session) => (
                                            <WorkoutCard
                                                key={session.id}
                                                session={session}
                                                isDeleting={isDeleting === session.id}
                                                onDelete={handleDeletingSession}
                                            />
                                        ))}
                                    </div>
                                ) : (
                                    <EmptyDayCard
                                        fullName={day.fullName}
                                        onClick={() => handleAddClick(day.date)}
                                    />
                                )}
                            </div>
                        )
                    })
                )}
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

