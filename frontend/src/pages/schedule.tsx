import { useEffect, useState, useMemo } from 'react'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { PageTitle } from '@/components/ui/page-title'
import { customFetch } from '@/lib/custom-fetch'
import {X, Plus, ChevronDown, Calendar, Loader2, AlertCircle} from 'lucide-react'
import {Button} from '@/components/ui/button'
import {Card, CardContent} from '@/components/ui/card'
import { SelectWorkoutDialog } from '@/components/ui/select-workout-dialog'
import type { Workout } from '@/types/workout'

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
    // todo: add new state hooks
    const [workouts, setWorkouts] = useState<Workout[]>([])
    const [isFetchingWorkouts, setIsFetchingWorkouts] = useState(false)
    const [selectedAddDate, setSelectedAddDate] = useState<Date | null>(null)
    const [isScheduling, setIsScheduling] = useState(false)


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
    useEffect(() => { //todo: replace this to also fetch workouts
        fetchScheduleAndAnalytics()
        const fetchWorkouts = async () =>{
            setIsFetchingWorkouts(true)
            try {
                const response = await customFetch('/api/workouts')
                if(response.ok){
                    const data = await response.json()
                    setWorkouts(data)
                } 
            } catch(err) {
                //todo: put nice error here
            } finally {
                setIsFetchingWorkouts(false)
            }
        }
        fetchWorkouts()
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
        setSelectedAddDate(date)
    }
    const handleScheduleWorkout = async(workoutId:string)=> {
        if (!selectedAddDate){
            return
        }
        setIsScheduling(true)
        setError(null)
        try{
            const scheduledAt =selectedAddDate.toISOString()
            const response = await customFetch('/api/users/me/schedule/sessions',{
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify({
                    workoutId, scheduledAt, status: 0
                })
            })
            if (!response.ok){
                const errData = await response.json().catch(() => ({}))
                throw new Error(errData.message || 'Could not schedule the workout')

            }
            setSelectedAddDate(null)
            await fetchScheduleAndAnalytics()
        }catch(err) {
            setError(err instanceof Error ? err.message : 'Failed to schedule the workout')
        } finally {
            setIsScheduling(false)
        }
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
        return (
            <div className="flex items-center gap-4 group flex-1">
                <Card className="flex-1 bg-card border border-border rounded-xl p-5 hover:border-brand/40 transition-all shadow-sm">
                    <div className="grid grid-cols-1 md:grid-cols-12 gap-4 items-center">
                        <div className="md:col-span-7 space-y-2">
                            <h3 className="text-lg font-bold text-foreground leading-snug">{session.workoutName}</h3>
                            <p className={cardDETAIL}>
                                <span className="font-semibold text-foreground">Primary Muscle Groups: </span>{session.primaryMuscleGroups.join(', ') || 'None'}
                            </p>
                            <p className={cardDETAIL}>
                                <span className="font-semibold text-foreground">Exercises:</span> {session.exercisePreview.join(', ') || 'None'}
                            </p>
                        </div>
                        <div className="md:col-span-5 grid grid-cols-3 gap-2 border-t md:border-t-0 md:border-l border-border pt-3 md:pt-0 md:pl-4 text-center md:text-left">
                            <div className="space-y-0.5">
                                <span className={statLABEL}>Volume</span>
                                <span className={`${statVALUE} truncate`}>
                                    {session.totalVolume > 0 ? `${session.totalVolume.toLocaleString()} kg` : '-'}
                                </span>
                            </div>
                            <div className="space-y-0.5">
                                <span className={statLABEL}>Sets</span>
                                <span className={statVALUE}>{session.totalSets > 0 ? session.totalSets : '-'}</span>
                            </div>
                            <div className="space-y-0.5">
                                <span className={statLABEL}>Status</span>
                                <span className={`${statVALUE} ${session.status === 'Completed' || session.status === '1' ? 'text-success' : 'text-brand'
                                    }`}>{session.status}</span>
                            </div>
                        </div>
                    </div>
                </Card >
                {/* deletion butn */}
                <Button
                    variant="ghost"
                    size="icon"
                    className="h-10 w-10 text-muted-foreground hover:text-destructive hover:bg-destructive/10 rounded-full transition-all flex-shrink-0"
                    aria-label={`Delete ${session.workoutName} from schedule`}
                    disabled={isDeleting}
                    onClick={() => onDelete(session.id)}>
                        {isDeleting ? (
                            <Loader2 size={16} className="animate-spin text-destructive"/>
                        ): (
                            <X size={16} />
                        )}
                    </Button>
            </div >
        )
    }
    interface EmptyDayCardProps{
        readonly fullName: string
        readonly onClick: () => void
    }
    function EmptyDayCard({fullName, onClick}: EmptyDayCardProps){
        return (
            <div className="flex-1 flex items-stretch">
                {/* TODO: make accessible */}
                <div className="flex-1 min-h-[110px] border-2 border-dashed border-border/70 rounded-xl flex items-center justify-center hover:border-brand/40 hover:bg-surface-2/20 transition-all cursor-pointer group" onClick={onClick}>
                        <div className="p-3 bg-surface border border-border group-hover:bg-brand/10 group-hover:text-brand group-hover:border-brand/30 text-muted-foreground rounded-full transition-all shadow-sm"
                            aria-label={`Add workout for ${fullName}`}>
                                <Plus size={20} />
                            </div>
                    </div>
                    <div className="w-14 flex-shrink-0"/>
            </div>
        )
    }
    // summary for week/month
    interface SummaryCardProps{
        readonly totalWorkouts: number
        readonly totalVolume: number
        readonly totalSets: number
    }
    function SummaryCard({totalWorkouts, totalVolume, totalSets}: SummaryCardProps){
        return (
            <Card className="bg-card border border-border rounded-2xl p-6 shadow-sm">
                <div className="mb-4">
                    <h3 className="font-display text-lg tracking-wider uppercase text-muted-foreground">Weekly Summary</h3>
                </div>
                <div className="grid grid-cols-3 gap-2 text-center sm:text-left">
                    <div className="border-r border-border/60 last:border-0 pr-2">
                        <span className={`${statLABEL} mb-1`}>Workouts</span>
                        <span className="text-2xl sm:text-3xl font-bold text-foreground font-display">{totalWorkouts}</span>
                    </div>
                    <div className="border-r border-border/60 last:border-0 px-2">
                        <span className={`${statLABEL} mb-1`}>Total Volume</span>
                        <span className="text-2xl sm:text-3xl font-bold text-foreground font-display block truncate">
                            {totalVolume.toLocaleString()}
                            <span className="text-xs font-sans font-medium text-muted-foreground">kg</span>
                        </span>
                    </div>
                    <div className="px-2">
                        <span className={`${statLABEL} mb-1`}>Total Sets</span>
                        <span className="text-2xl sm:text-3xl font-bold text-foreground font-display">{totalSets}</span>
                    </div>
                </div>
            </Card>
        )
    }

    return (
        <section className="mx-auto max-w-6xl px-6 py-12">

            {/* TODO: top row */}
            <div className="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <PageTitle title="Scheduler" />
                <div className="relative inline-block self-end sm:self-auto">
                    <select
                        defaultValue="week"
                        className="appearance-none bg-surface border border-border text-foreground px-4 py-2.5 pr-10 rounded-xl text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-brand cursor-pointer shadow-sm transition-all hover:bg-surface-2/40">
                        <option value="week">Week View</option>
                        <option value="month">Month View</option>
                        </select>
                        <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3 text-muted-foreground border-l border-border/40 ml-1">
                            <ChevronDown size={15}/>
                        </div>
                </div>
            </div>



            {/* error msg */}
            {error && (
                <div className="mb-6 rounded-xl border border-destructive/20 bg-destructive/5 px-4 py-3.5 text-sm text-destructive flex items-center gap-2.5 shadow-sm animate-fadeIn" role="alert">
                    <AlertCircle size={18} />
                    <span>{error}</span>
                </div>
            )}

            <div className="grid grid-cols-12 gap-8 items-start">
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
                            const hasWorkouts = day.sessions.length > 0;
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

                {/* summary section */}
                <div className="col-span-12 lg:col-span-5 space-y-6 lg:sticky lg:top-6">
                    <SummaryCard
                        totalWorkouts={analytics?.totalWorkouts ?? 0}
                        totalVolume={analytics?.totalVolume ?? 0}
                        totalSets={analytics?.totalSets ?? 0}
                    />

                    <div className="p-6 bg-card border border-border rounded-2xl shadow-sm">
                        <h2 className="text-xl font-bold mb-4">Muscle Balance Chart</h2>
                        {isLoading ? (
                            <div className="h-64 flex items-center justify-center">
                                <Loader2 className="animate-spin text-muted-foreground/60" size={24}/>
                            </div>
                        ) : (
                            <SpiderGraph data={muscleValues} />
                        )}
                    </div>

                </div>
                
            </div>
            {/* select workout popup comp */}
            <SelectWorkoutDialog
            isOpen={selectedAddDate !== null}
            onClose={() => setSelectedAddDate(null)}
            workouts={workouts}
            isFetching={isFetchingWorkouts}
            onSchedule={handleScheduleWorkout}
            isScheduling={isScheduling}/>
        </section>
    )
}

