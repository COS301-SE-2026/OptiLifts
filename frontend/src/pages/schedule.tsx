import { useEffect, useState, useMemo } from 'react'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { PageTitle } from '@/components/ui/page-title'
import { customFetch } from '@/lib/custom-fetch'
import {X, Plus, Loader2, AlertCircle} from 'lucide-react'
import {Button} from '@/components/ui/button'
import {Card, CardTitle} from '@/components/ui/card'
import { SelectWorkoutDialog } from '@/components/ui/select-workout-dialog'
import type { Workout } from '@/types/workout'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import {
  DropdownMenu,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuContent
} from '@/components/ui/dropdown-menu'

//styling constants for same style aspects
const statLABEL = "text-[10px] font-semibold uppercase tracking-wider text-muted-foreground block"
const statVALUE = "text-sm font-bold text-foreground block"
const cardDETAIL = "text-xs text-muted-foreground leading-normal"

const MUSCLECAT_MAP: Record<string, string> = {
    Chest: 'Chest',
    Lats: 'Back',
    'Lower Back': 'Back',
    'Middle Back': 'Back',
    Trapezius: 'Back',
    Shoulders: 'Shoulders',
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
    const [viewMode, setViewMode] = useState<'week' | 'month'>('week')
    const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null)
    const [scheduleEntries, setScheduleEntries] = useState<ScheduledEntryDto[]>([])
    const [analytics, setAnalytics] = useState<AnalyticsResponse| null>(null)
    const [isLoading, setIsLoading] = useState(true)
    const [isDeleting, setIsDeleting] = useState<string | null>(null)

    const [error, setError] = useState<string | null>(null)
    const [muscleValues, setMuscleValues] = useState<Record<string, number>>({
        Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,})
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
        await Promise.resolve()
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

    useEffect(() => { 
        const trigger = async () => {
            await Promise.resolve()
            await fetchScheduleAndAnalytics()
        }
        void trigger()
        const fetchWorkouts = async () =>{
            await Promise.resolve()
            setIsFetchingWorkouts(true)
            try {
                const response = await customFetch('/api/workouts')
                if(response.ok){
                    const data = await response.json()
                    setWorkouts(data)
                } 
            } catch(err) {
                setError(err instanceof Error ? err.message : 'Unexpected error occured while loading workouts.')
            } finally {
                setIsFetchingWorkouts(false)
            }
        }
        void fetchWorkouts()
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
    const handleScheduleWorkout = async(workoutId:string, repeat?:string, interval?: number, until?:string)=> {
        if (!selectedAddDate){
            return
        }
        setIsScheduling(true)
        setError(null)
        try{
            const utcDate = new Date(Date.UTC( //fixing monday not being scheduled
                selectedAddDate.getFullYear(), selectedAddDate.getMonth(), selectedAddDate.getDate()
            ))
            const scheduledAt =utcDate.toISOString()

            const bodyPayload: Record<string, any> ={
                workoutId,
                scheduledAt,
                status: 0
            }
            if(repeat &&interval && until) {
                bodyPayload.repeat = repeat.toLowerCase()
                bodyPayload.interval = interval
                const udate = new Date(until)
                const utcUntil = new Date(Date.UTC(udate.getFullYear(), udate.getMonth(), udate.getDate()))
                bodyPayload.until = utcUntil.toISOString()
            }
            const response = await customFetch('/api/users/me/schedule/sessions',{
                method: 'POST',
                headers: {'Content-Type': 'application/json'},
                body: JSON.stringify(bodyPayload)
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
        return d1.getFullYear() === d2.getUTCFullYear() && d1.getMonth() === d2.getUTCMonth() && d1.getDate() === d2.getUTCDate()
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

    return (
        <section className="mx-auto max-w-6xl px-6 py-12">

            {/* top row */}
            <div className="mb-8 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                <PageTitle title="Scheduler" />
                {/* <div className="relative inline-block self-end sm:self-auto">
                    <select
                        defaultValue="week"
                        className="appearance-none bg-surface border border-border text-foreground px-4 py-2.5 pr-10 rounded-xl text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-brand cursor-pointer shadow-sm transition-all hover:bg-surface-2/40">
                        <option value="week">Week View</option>
                        <option value="month">Month View</option>
                    </select>
                    <div className="pointer-events-none absolute inset-y-0 right-0 flex items-center px-3 text-muted-foreground border-l border-border/40 ml-1">
                        <ChevronDown size={15} />
                    </div>
                </div> */}
                <DropdownMenu>
                    <DropdownMenuTrigger
                    variant="filter"
                    className="bg-surface border border-border text-foreground px-3.5 py-2 rounded-xl text-sm font-semibold hover:bg-surface-2/40 transition-all cursor-pointer flex items-center gap-2 shadow-sm w-fit justify-center">
                        <span>{viewMode === 'week' ? 'Week View' : 'Month View'}</span>
                        {/* <ChevronDown size={15} className="text-muted-foreground ml-1"/> */}
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="bg-surface border border-border rounded-xl p-1 shadow-md">
                        <DropdownMenuItem onClick={() => setViewMode('week')}>Week View</DropdownMenuItem>
                        <DropdownMenuItem onClick={() => setViewMode('month')}>Month View</DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
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
                <div className="col-span-12 lg:col-span-8 space-y-6">
                    {isLoading ? (
                        DAYS.map((day) => (
                            <div key={`skeleton-${day.name}`} className="flex items-center gap-4 animate-pulse">
                                <div className="w-12 h-14 border-r border-border flex flex-col items-center justify-center mr-1"/>    
                                <div className="flex-1 h-28 bg-surface-2 border border-border rounded-xl"/>
                                <div className="w-10 h-10"/>
                            </div>
                        ))
                    ) : (
                        weeklydays.map((day) => {
                            const hasWorkouts = day.sessions.length > 0;
                            const today = new Date();
                            const isToday = day.date.getFullYear() === today.getFullYear() && day.date.getMonth() === today.getMonth() && day.date.getDate() === today.getDate();

                            const todayStart = new Date()
                            todayStart.setHours(0,0,0,0)
                            const dayStart = new Date(day.date)
                            dayStart.setHours(0,0,0,0)
                            const isBeforeToday = dayStart < todayStart

                            return (
                                <div key={day.name} className="flex items-stretch gap-4 min-h-[110px]">
                                    <VerticalDayHeader name={day.name} date={day.formattedDate} isToday={isToday} />

                                    {hasWorkouts ? (
                                        <div className="flex-1 flex flex-col gap-4">
                                            {day.sessions.map((session) => (
                                                <WorkoutCard
                                                    key={session.id}
                                                    session={session}
                                                    isDeleting={isDeleting === session.id}
                                                    onDelete={(id) =>setDeleteTargetId(id)}
                                                />
                                            ))}
                                        </div>
                                    ) : (
                                        <EmptyDayCard
                                            fullName={day.fullName}
                                            onClick={() => handleAddClick(day.date)}
                                            disabled={isBeforeToday}
                                        />
                                    )}
                                </div>
                            )
                        })
                    )}
                </div>

                {/* summary section */}
                <div className="col-span-12 lg:col-span-4 space-y-6 lg:sticky lg:top-24">
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
            key={selectedAddDate ? selectedAddDate.toISOString() : 'closed'}
            isOpen={selectedAddDate !== null}
            onClose={() => setSelectedAddDate(null)}
            workouts={workouts}
            isFetching={isFetchingWorkouts}
            onSchedule={handleScheduleWorkout}
            isScheduling={isScheduling}
            scheduledDate={selectedAddDate}/>

            <ConfirmDialog
            isOpen={deleteTargetId !== null}
            onClose={() => setDeleteTargetId(null)}
            isLoading={isDeleting !== null}
            variant="danger"
            title="Delete Scheduled Workout"
            description="Are you certain you want to delete this scheduled workout session?"
            confirmText="Delete"
            cancelText="Cancel"
            onConfirm={async () => {
            if (deleteTargetId) {
                const id = deleteTargetId
                setDeleteTargetId(null)
                await handleDeletingSession(id)
            }
            }}/>
        </section>
    )
}

//COMPONENTS
    //vertical day headers
    interface VerticalDayHeaderProps{
        readonly name: string
        readonly date: number
        readonly isToday?: boolean
    }
    function VerticalDayHeader({ name, date, isToday}: VerticalDayHeaderProps) {
        return (
            <div className={`font-display tracking-widest [writing-mode:vertical-lr] rotate-180 select-none py-1 flex flex-col items-center justify-center w-12 text-center mr-1 flex-shrink-0
                ${isToday
                    ? 'border-r-2 border-brand text-brand font-bold'
                    : ''}`}>
                <div className="text-xl md:text-2xl font-bold leading-none">{name}</div>
                <div className={`text-xs md:text-sm font-sans font-semibold text-muted-foreground/80 mt-1 leading-none ${isToday ? 'text-brand/90' : 'text-muted-foreground/80'}`}>{date}</div>
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
                            <CardTitle className="text-lg font-bold text-foreground leading-snug">{session.workoutName}</CardTitle>
                            <p className={cardDETAIL}>
                                <span className="font-semibold text-foreground">Primary Muscle Groups: </span>{session.primaryMuscleGroups.join(', ') || 'None'}
                            </p>
                            <p className={cardDETAIL}>
                                <span className="font-semibold text-foreground">Exercises:</span> {session.exercisePreview.join(', ') || 'None'}
                            </p>
                        </div>
                        <div className="md:col-span-5 grid grid-cols-3 gap-2 border-t md:border-t-0 md:border-l border-border pt-3 md:pt-0 md:pl-4 text-center  ">
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
                    tabIndex={0}
                    className="h-10 w-10 text-muted-foreground hover:text-destructive hover:bg-destructive/10 focus-visible:ring-2 focus-visible:ring-destructive focus-visible:outline-none rounded-full transition-all flex-shrink-0"
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
    readonly disabled?:boolean
}
function EmptyDayCard({ fullName, onClick, disabled }: EmptyDayCardProps) {
    return (
        <div className="flex-1 flex items-stretch">
            <button tabIndex={disabled? -1:0}
            disabled={disabled}
            title={disabled ? "You cannot schedule a workout on a day before today" : `Add workout for ${fullName}`}
                className={`flex-1 min-h-[110px] border-2 border-dashed border-border/70 rounded-xl flex items-center justify-center transition-all ${ 
                    disabled
                    ? 'opacity-40 cursor-not-allowed border-border/40 bg-surface/5'
                    : 'hover:border-brand/40 hover:bg-surface-2/20 cursor-pointer group'}`}
                onClick={disabled ? undefined :onClick}
                onKeyDown={(event) => {
                    if (event.key === 'Enter' || event.key === ' ') {
                        event.preventDefault();
                        onClick();
                    }
                }} aria-label={disabled ? `Cannot add workout for ${fullName} before today`: `Add workout for ${fullName}`}>
                <div className={`p-3 bg-surface border border-border rounded-full transition-all shadow-sm ${
                    disabled ? 'text-muted-foreground/40 border-border/40'
                    : 'group-hover:bg-brand/10 group-hover:text-brand group-hover:border-brand/30 text-muted-foreground'
                }`}
                >
                    <Plus size={20} />
                </div>
            </button>
            <div className="w-14 flex-shrink-0" />
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
                <h3 className="font-display text-lg tracking-wider uppercase">Weekly Summary</h3>
            </div>
            <div className="grid grid-cols-3 gap-2 text-center">
                <div className="px-1">
                    <span className={`${statLABEL} mb-1`}>Workouts</span>
                    <span className="text-xl sm:text-2xl font-bold text-foreground font-display">{totalWorkouts}</span>
                </div>
                <div className="px-1">
                    <span className={`${statLABEL} mb-1`}>Total Volume</span>
                    <span className="text-xl sm:text-2xl font-bold text-foreground font-display block truncate">
                        {totalVolume.toLocaleString()}
                        <span className="text-xs font-sans font-medium text-muted-foreground">kg</span>
                    </span>
                </div>
                <div className="px-1">
                    <span className={`${statLABEL} mb-1`}>Total Sets</span>
                    <span className="text-xl sm:text-2xl font-bold text-foreground font-display">{totalSets}</span>
                </div>
            </div>
        </Card>
    )
}