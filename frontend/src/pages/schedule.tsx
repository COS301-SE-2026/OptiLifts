import { useEffect, useState, useMemo, useCallback } from 'react'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { PageTitle } from '@/components/ui/page-title'
import { customFetch } from '@/lib/custom-fetch'
import {X, Plus, Loader2, AlertCircle} from 'lucide-react'
import {Button} from '@/components/ui/button'
import {Card, CardTitle} from '@/components/ui/card'
import { SelectWorkoutDialog } from '@/components/ui/select-workout-dialog'
import type { Workout } from '@/types/workout'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { useNavigate } from 'react-router-dom'
import {
  DropdownMenu,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuContent
} from '@/components/ui/dropdown-menu'
import { DatePagination } from '@/components/ui/date-pagination'
import { metricCheck, outputWeight } from '@/lib/weight-utils'
import { cacheScheduleEntries, getCachedScheduleEntries } from '@/lib/offline/workouts-cache'
import { useOnlineStatus } from '@/lib/use-online-status'

//styling constants for same style aspects
const statLABEL = "text-[11px] font-semibold uppercase tracking-wider text-muted-foreground block"
const statVALUE = "text-sm font-bold text-foreground block"
const cardDETAIL = "text-xs text-muted-foreground leading-normal"

const MUSCLECAT_MAP: Record<string, string> = {
    Chest: 'Chest',
    Lats: 'Back',
    'Lower Back': 'Back',
    'Middle Back': 'Back',
    'Upper Back': 'Back',
    Trapezius: 'Back',
    Shoulders: 'Shoulders',
    'Front Deltoid': 'Shoulders',
    'Middle Deltoid': 'Shoulders',
    'Rear Deltoid': 'Shoulders',
    Obliques: 'Core',
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
    readonly secondaryMuscleDistribution?: readonly MuscleDistributionItem[]
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

const isSameDay = (date1: Date, date2: string) => {
        const d1 = new Date(date1)
        const d2 = new Date(date2)
        return d1.getFullYear() === d2.getUTCFullYear() && d1.getMonth() === d2.getUTCMonth() && d1.getDate() === d2.getUTCDate()
}

export default function SchedulePage() {
    const [viewMode, setViewMode] = useState<'week' | 'month'>('week')
    const [currentWeekDate, setCurrentWeekDate] = useState(() => new Date())
    const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null)
    const [scheduleEntries, setScheduleEntries] = useState<ScheduledEntryDto[]>([])
    const [analytics, setAnalytics] = useState<AnalyticsResponse| null>(null)
    const [isLoading, setIsLoading] = useState(true)
    const [isDeleting, setIsDeleting] = useState<string | null>(null)

    const [error, setError] = useState<string | null>(null)
    const [muscleValues, setMuscleValues] = useState<Record<string, number>>({
        Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,})
    const [secondaryMuscleValues, setSecondaryMuscleValues] = useState<Record<string, number>>({
        Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,})
    const [workouts, setWorkouts] = useState<Workout[]>([])
    const [isFetchingWorkouts, setIsFetchingWorkouts] = useState(false)
    const [selectedAddDate, setSelectedAddDate] = useState<Date | null>(null)
    const [isScheduling, setIsScheduling] = useState(false)
    const isOnline = useOnlineStatus()
    const [isOfflineData, setIsOfflineData] = useState(false)

    const navigate = useNavigate()
    const [completedLogs, setCompletedLogs] = useState<Record<string, string>>({})
    const handleWorkoutClick = (session: ScheduledEntryDto) => {
        const isCompleted = session.status === 'Completed' || session.status === '1'
        if (isCompleted) {
            const d2 = new Date(session.scheduled)
            const dateKey = `${d2.getUTCFullYear()}-${String(d2.getUTCMonth() + 1).padStart(2, '0')}-${String(d2.getUTCDate()).padStart(2, '0')}`
            const key = `${session.workoutId}-${dateKey}`
            const logId = completedLogs[key]

            if(logId) {
                navigate(`/workouts/${session.workoutId}/logs/${logId}`)
            } else {
                navigate(`/workouts/${session.workoutId}`)
            }
        } else {
            navigate(`/workouts/${session.workoutId}`)
        }
    }


    //calculate the dates
    const weekDates = useMemo(() => {
        // const now = new Date()
        const currentDay = currentWeekDate.getDay()
        const diffToMonday = currentDay === 0 ? -6 : 1 - currentDay

        const monday = new Date(currentWeekDate)
        monday.setDate(currentWeekDate.getDate() + diffToMonday)
        monday.setHours(0,0,0,0)
        const dates: Date[] = []
        for (let i = 0; i <7; i++){
            const d = new Date(monday)
            d.setDate(monday.getDate() + i)
            dates.push(d)
        }
        return dates
    }, [currentWeekDate])

    const fetchRange = useMemo(() => {
        if (viewMode === 'week'){
            const start = new Date(Date.UTC(weekDates[0].getFullYear(), weekDates[0].getMonth(), weekDates[0].getDate()))
            const end = new Date(Date.UTC(weekDates[6].getFullYear(), weekDates[6].getMonth(), weekDates[6].getDate(), 23,59,59,999))
            return{start,end}
        } else {
            const firstDay = new Date(currentWeekDate.getFullYear(), currentWeekDate.getMonth(), 1)
            const leadingDays = firstDay.getDay() === 0 ? 6: firstDay.getDay() -1
            const firstCell = new Date(currentWeekDate.getFullYear(), currentWeekDate.getMonth(), 1- leadingDays)
            const lastCell = new Date(currentWeekDate.getFullYear(), currentWeekDate.getMonth(), 42 - leadingDays)
            const start = new Date(Date.UTC(firstCell.getFullYear(), firstCell.getMonth(), firstCell.getDate()))
            const end = new Date(Date.UTC(lastCell.getFullYear(), lastCell.getMonth(), lastCell.getDate(), 23,59,59,999))
            return{start,end}
        }
    }, [currentWeekDate, weekDates, viewMode])

    const fetchScheduleAndAnalytics = useCallback(async () => {
        await Promise.resolve()
        setIsLoading(true)
        setError(null)
        try {
            //added ze marking as missed first
            await customFetch('/api/users/me/schedule/missed', {
                method: 'POST'
            }).catch(() => {})


            const {start, end} = fetchRange
            const [scheduleResp, analyticsResp] = await Promise.all([
                customFetch(`/api/users/me/schedule?startDate=${start.toISOString()}&endDate=${end.toISOString()}`),
                customFetch(`/api/users/me/schedule/analytics?startDate=${start.toISOString()}&endDate=${end.toISOString()}`)
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
            setIsOfflineData(false)
            void cacheScheduleEntries(scheduleData)

            try{
                const year = currentWeekDate.getFullYear()
                const month = currentWeekDate.getMonth() + 1
                const calendarRes = await customFetch(`/api/profile/calendar?year=${year}&month=${month}`) //use eddies endpoint from profile
                if (calendarRes.ok){
                    const calendarData = await calendarRes.json()
                    const mapping: Record<string, string> = {}
                    calendarData.entries.forEach((entry: {workoutId:string, date:string, logId:string}) => {
                        mapping[`${entry.workoutId}-${entry.date}`] = entry.logId
                    })
                    setCompletedLogs(mapping)
                }
            } catch(e){
                setError(e instanceof Error ? e.message : 'Error fetching calendar logs')
            }

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

            const secondaryAggre: Record<string, number> = {
                    Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0,
            }
            if (analyticsData.secondaryMuscleDistribution) {
                analyticsData.secondaryMuscleDistribution.forEach((item) => {
                    const mappedCat = MUSCLECAT_MAP[item.muscleGroup]
                    if (mappedCat && mappedCat in secondaryAggre) {
                        secondaryAggre[mappedCat] += item.setCount
                    }
                })
            }
            setSecondaryMuscleValues(secondaryAggre)
        } catch (error) {
            const cached = await getCachedScheduleEntries<ScheduledEntryDto>()

            if (cached && cached.length > 0) {
                setScheduleEntries(cached)
                setIsOfflineData(true)
                return
            }

            setError(error instanceof Error ? error.message : 'Could not load analytics')
        } finally {
            setIsLoading(false)
        }
    }, [fetchRange, currentWeekDate])

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
    }, [fetchScheduleAndAnalytics])

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
        if (!isOnline) {
            return
        }

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

            const bodyPayload: {
                workoutId: string
                scheduledAt: string
                status: number
                repeat?: string
                interval?: number
                until?: string
            } = {
                workoutId,
                scheduledAt,
                status:0
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
                <div className="flex items-center gap-3 sm:gap-4 flex-wrap">
                    <Button variant="outline" size="sm" onClick={() =>setCurrentWeekDate(new Date())}
                    className="bg-surface border border-border text-foreground hover:bg-brand/10 hover:text-brand hover:border-brand/30 font-semibold px-3.5 py-0.5 rounded-xl text-sm transition-all shadow-sm cursor-pointer">
                        Today
                    </Button>
                <DatePagination
                    currentDate={currentWeekDate}
                    onChange={setCurrentWeekDate}
                    type={viewMode}
                />
                
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
            </div>

            {error && (
                <div className="mb-6 rounded-xl border border-destructive/20 bg-destructive/5 px-4 py-3.5 text-sm text-destructive flex items-center gap-2.5 shadow-sm animate-fadeIn" role="alert">
                    <AlertCircle size={18} />
                    <span>{error}</span>
                </div>
            )}

            {isOfflineData && (
                <div className="mb-6 rounded-xl border border-border bg-surface-2 px-4 py-3.5 text-sm text-muted-foreground flex items-center gap-2.5 shadow-sm" role="status">
                    <AlertCircle size={18} />
                    <span>You're offline — showing your saved schedule. Analytics are unavailable until you reconnect.</span>
                </div>
            )}

            {viewMode === 'week' ? (
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
                                                    onClick={handleWorkoutClick}
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
                                <Loader2 className="animate-spin text-muted-foreground/60" size={24} />
                            </div>
                        ) : (
                            <SpiderGraph data={muscleValues} secondaryData={secondaryMuscleValues} />
                        )}
                    </div>

                </div>
                
            </div>
            ) : (
                //month view
                <MonthViewCalendar
                currentDate={currentWeekDate}
                scheduleEntries={scheduleEntries}
                isLoading={isLoading}
                onAddClick={handleAddClick}
                onDeleteSession={(id) => setDeleteTargetId(id)}
                onWorkoutClick={handleWorkoutClick}/>
            )}

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
        readonly onClick: (session: ScheduledEntryDto) => void
    }
    function WorkoutCard({session, isDeleting, onDelete, onClick}: WorkoutCardProps){
        const units = (metricCheck())? 'KG' : 'LB'
        return (
            <div className="flex items-center gap-4 group flex-1">
                <Card
                role="button"
                tabIndex={0}
                onClick={() => onClick(session)}
                onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' '){
                        e.preventDefault()
                        onClick(session)
                    }
                }} 
                className="flex-1 bg-card border border-border rounded-xl p-5 hover:border-brand/40 transition-all shadow-sm cursor-pointer hover:ring-2 hover:ring-brand/45 focus-visible:ring-2 focus-visible:ring-brand/45 outline-none">
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
                                    {session.totalVolume > 0 ? `${outputWeight(session.totalVolume).toLocaleString(undefined, { maximumFractionDigits: 0 })} ${units}` : '-'}
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
    const units = (metricCheck())? 'KG' : 'LB'
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
                        {outputWeight(totalVolume).toLocaleString(undefined, { maximumFractionDigits: 0 })}
                        <span className="text-xs font-sans font-medium text-muted-foreground"> {units}</span>
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

// month view calendar comp
interface MonthViewCalendarProps{
    readonly currentDate: Date
    readonly scheduleEntries: readonly ScheduledEntryDto[]
    readonly isLoading: boolean
    readonly onAddClick: (date: Date) => void
    readonly onDeleteSession: (id: string) => void
    readonly onWorkoutClick: (session: ScheduledEntryDto) => void
}
function MonthViewCalendar({
    currentDate, 
    scheduleEntries,
    isLoading,
    onAddClick,
    onDeleteSession,
    onWorkoutClick
}: MonthViewCalendarProps){
    const WEEKDAYS=["MON","TUE", "WED", "THU","FRI","SAT", "SUN"]
    const gridDays = useMemo(() => {
        const first = new Date(currentDate.getFullYear(), currentDate.getMonth(),1)
        const leadingDays = first.getDay() ===0 ? 6 : first.getDay() -1
        const totalCells = 42
        const cells: Array<{
            date: Date
            dayNumber: number
            isToday: boolean
            isOtherMonth: boolean //visually different
            key: string
        }> = []

        const today = new Date()
        const todayStart = new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime()

        for(let cellIndex = 0; cellIndex < totalCells; cellIndex += 1){
            const date = new Date(currentDate.getFullYear(), currentDate.getMonth(), cellIndex - leadingDays + 1)
            const cellDateStart = new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime()
            const isOtherMonth = date.getMonth() !== currentDate.getMonth() || date.getFullYear() !== currentDate.getFullYear()
            cells.push({
                date, dayNumber: date.getDate(), isToday: cellDateStart === todayStart, isOtherMonth, key: `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`,
                
            })
        }
        return cells

    }, [currentDate])

    //frontend
    return (
        <div className="relative bg-card border border-border rounded-2xl shadow-sm overflow-hidden animate-fadeIn w-full">
            {isLoading && (
                <div className="absolute inset-0 bg-background/40 backdrop-blur-[1px] flex items-center justify-center z-10 rounded-2xl">
                    <Loader2 className="animate-spin text-brand" size={32}/> 
                </div>                   
            )}
            {/* weekdays */}
            <div className="grid grid-cols-7 text-center text-xs font-extrabold uppercase tracking-wider text-foreground py-3 bg-surface/40 border-b border-border">
                {WEEKDAYS.map((day) => (
                    <div key={day}>{day}</div>
                ))}
            </div>

            <div className="grid grid-cols-7 gap-px bg-border">
                {gridDays.map((cell) => {
                    const sessionsOnDay = scheduleEntries.filter((entry) => isSameDay(cell.date, entry.scheduled))
                    const isCompleted = sessionsOnDay.some((s) => s.status === 'Completed' || s.status === '1')
                    const isMissed = sessionsOnDay.some((s) => s.status === 'Missed')
                    const isScheduled = sessionsOnDay.length > 0 && !isCompleted && !isMissed

                    const today = new Date()
                    today.setHours(0,0,0,0)
                    const cellDateStart = new Date(cell.date)
                    cellDateStart.setHours(0,0,0,0)
                    const isBeforeToday = cellDateStart < today //ensure cannot schedule

                    let circleClass = "flex size-9 items-center justify-center rounded-full text-sm font-bold transition-all"
                    if (isCompleted) {
                        circleClass += " bg-brand text-background shadow-md shadow-brand/20"
                    } else if (isScheduled){
                        circleClass += " border-2 border-brand text-brand bg-transparent"
                    } else if (isMissed){
                        circleClass += " border-2 border-dotted border-brand text-brand bg-transparent"
                    } else if (cell.isToday){
                        circleClass += " bg-surface-2 border border-border text-foreground font-extrabold"
                    } else if (cell.isOtherMonth){
                        circleClass += " text-muted-foreground/35"
                    } else {
                        circleClass += " text-foreground hover:bg-surface-2"
                    }

                    return (
                        <div key={cell.key}
                        className={`min-h-[120px] p-2.5 flex flex-col items-center justify-between bg-card transition-all ${cell.isOtherMonth ? '!bg-surface-2/10 opacity-60' : ''}`}>
                            <div className="flex justify-center w-full mb-1">
                                <span className={circleClass}>
                                    {cell.dayNumber}
                                </span>
                            </div>

                            <div className="flex-1 flex flex-col gap-1.5 items-stretch justify-center w-full min-h-[48px]">
                                {sessionsOnDay.map((session) => (
                                    <div key={session.id}
                                    className="group/item relative px-2.5 py-1.5 bg-surface border border-border rounded-lg flex items-center justify-between gap-1.5 text-xs font-bold text-foreground transition-all hover:border-brand/40 hover:ring-2 hover:ring-brand/45 focus-visible-within:ring-2 focus-visible-within:ring-brand/45 shadow-sm">
                                        <button type="button"
                                        onClick={() => onWorkoutClick(session)}
                                        className="truncate flex-1 text-left outline-none cursor-pointer focus-visible:underline" 
                                        title={session.workoutName}>
                                            {session.workoutName}
                                        </button>                                            
                                        <button 
                                        type="button" 
                                        className="opacity-0 group-hover/item:opacity-100 size-4 flex items-center justify-center rounded-full hover:bg-destructive/10 text-muted-foreground hover:text-destructive transition-all flex-shrink-0 cursor-pointer outline-none focus-visible:opacity-100 focus-visible:ring-1 focus-visible:ring-destructive"
                                        onClick={(e) => {
                                            e.stopPropagation()
                                            onDeleteSession(session.id)
                                        }}
                                        aria-label={`Delete ${session.workoutName}`}>
                                            <X size={10} />
                                        </button>
                                    </div>

                    ))}
                    {sessionsOnDay.length === 0 && (
                        <div className="flex justify-center py-1">
                            <button type="button" 
                            tabIndex={isBeforeToday ? -1 : 0}
                            disabled={isBeforeToday}
                            onClick={() => onAddClick(cell.date)}
                            className="flex items-center justify-center w-full py-1.5 border border-dashed border-border/70 hover:border-brand/50 hover:bg-brand/5 rounded-lg transition-all text-muted-foreground hover:text-brand cursor-pointer transition-all text-muted-foreground hover:text-brand cursor-pointer disabled:opacity-30 disabled:cursor-not-allowed"
                            title={isBeforeToday ? "Cannot schedule in the past" : "Add workout"}>
                                <Plus size={14} />
                            </button>
                        </div>
                    )}
            </div>
        </div>
    )
})} </div> </div>)
}