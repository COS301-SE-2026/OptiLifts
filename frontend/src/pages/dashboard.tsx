import { useEffect, useMemo, useState } from 'react'
import { UpcomingWorkoutsCard } from '@/components/ui/upcoming-workouts'
import { VolumeChart } from '@/components/ui/volume-chart'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { Card, CardContent } from '@/components/ui/card'
import streakFlame from '@/assets/streak_flame.png'
import badgeIcon from '@/assets/badge.png'
import { customFetch } from '@/lib/custom-fetch'
import { useAuth } from '@/context/auth-context'
import type { ProfilePageResponse } from '@/types/profile'
import type { WorkoutDetailResponse } from '@/types/workout-detail'
import type { VolumeChartPeriod } from '@/components/ui/volume-chart'

type ScheduleAnalyticsResponse = Readonly<{
    totalWorkouts: number
    totalVolume: number
    totalSets: number
    muscleDistribution: readonly {
        muscleGroup: string
        setCount: number
        percentage: number
    }[]
}>

type ScheduledEntry = Readonly<{
    id: string
    workoutId: string
    workoutName: string
    scheduled: string
    status: string
    primaryMuscleGroups: string[]
    exerciseCount: number
    exercisePreview: string[]
    totalVolume: number
    totalSets: number
    recordCount?: number | null
    startedAt?: string | null
    completedAt?: string | null
}>

type ChartPoint = Readonly<{
    label: string
    value: number
}>

type ChartBucket = Readonly<{
    label: string
    start: Date
    end: Date
}>

const MUSCLE_CATEGORY_MAP: Record<string, 'Chest' | 'Core' | 'Shoulders' | 'Arms' | 'Legs' | 'Back'> = {
    Chest: 'Chest',
    Lats: 'Back',
    'Lower Back': 'Back',
    'Middle Back': 'Back',
    Trapezius: 'Back',
    Shoulders: 'Shoulders',
    Biceps: 'Arms',
    Forearms: 'Arms',
    Triceps: 'Arms',
    Quadriceps: 'Legs',
    Hamstrings: 'Legs',
    Calves: 'Legs',
    Glutes: 'Legs',
    Abductors: 'Legs',
    Adductors: 'Legs',
    Abdominals: 'Core',
}

const MUSCLE_KEYS = ['Chest', 'Core', 'Shoulders', 'Arms', 'Legs', 'Back'] as const
type MuscleFilter = 'All' | (typeof MUSCLE_KEYS)[number]

function startOfDay(date: Date) {
    const next = new Date(date)
    next.setHours(0, 0, 0, 0)
    return next
}

function startOfWeek(date: Date) {
    const day = startOfDay(date)
    const offset = (day.getDay() + 6) % 7
    day.setDate(day.getDate() - offset)
    return day
}

function addDays(date: Date, days: number) {
    const next = new Date(date)
    next.setDate(next.getDate() + days)
    return next
}

function formatDayLabel(date: Date) {
    return date.toLocaleDateString('en-US', { weekday: 'short' })
}

function formatMonthLabel(date: Date) {
    return date.toLocaleDateString('en-US', { month: 'short' })
}

function buildChartBuckets(period: VolumeChartPeriod): ChartBucket[] {
    const currentWeekStart = startOfWeek(new Date())

    if (period === 'Week'){
        return Array.from({ length: 7 }, (_, index) => {
            const day = addDays(currentWeekStart, index)
            return {
                label: formatDayLabel(day),
                start: day,
                end: day,
            }
        })
    }

    if (period === 'Month'){
        return Array.from({ length: 4 }, (_, index) => {
            const start = addDays(currentWeekStart, -(3 - index) * 7)
            return {
                label: `Week ${index + 1}`,
                start,
                end: addDays(start, 6),
            }
        })
    }

    const currentMonthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1)

    return Array.from({ length: 12 }, (_, index) => {
        const start = new Date(currentMonthStart.getFullYear(), currentMonthStart.getMonth() - (11 - index), 1)
        const end = new Date(start.getFullYear(), start.getMonth() + 1, 0)
        return {
            label: formatMonthLabel(start),
            start,
            end,
        }
    })
}

function getEntryDate(entry: ScheduledEntry) {
    return new Date(entry.completedAt ?? entry.startedAt ?? entry.scheduled)
}

function buildVolumeChartData(entries: readonly ScheduledEntry[], period: VolumeChartPeriod, muscleFilter: MuscleFilter): ChartPoint[] {
    const buckets = buildChartBuckets(period)

    return buckets.map((bucket) => {
        const total = entries.reduce((sum, entry) => {
            const entryDate = getEntryDate(entry)
            const withinBucket = entryDate >= bucket.start && entryDate <= bucket.end

            if (!withinBucket) return sum
            if (muscleFilter !== 'All'){
                const entryMuscles = entry.primaryMuscleGroups.map((m) => MUSCLE_CATEGORY_MAP[m] || m)
                if (!entryMuscles.includes(muscleFilter)) return sum
            }

            return sum + entry.totalVolume
        }, 0)

        return {
            label: bucket.label,
            value: total,
        }
    })
}

function getDayPillClass(index: number) {
    const palette = [
        'bg-brand/15 text-brand border-brand/30',
        'bg-foreground/10 text-foreground border-border',
        'bg-surface-2 text-foreground border-border',
    ]

    return palette[index % palette.length]
}

function formatUpcomingDate(dateString: string) {
    const today = startOfDay(new Date())
    const scheduledDate = startOfDay(new Date(dateString))
    const diffDays = Math.round((scheduledDate.getTime() - today.getTime()) / 86400000)

    if (diffDays === 0) return 'Today'
    if (diffDays === 1) return 'Tomorrow'
    if (diffDays < 7) {
        return scheduledDate.toLocaleDateString('en-US', { weekday: 'long' })
    }

    return scheduledDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

export default function DashboardPage() {
    const { isAuthenticated, isHydrated } = useAuth()
    const [volumePeriod, setVolumePeriod] = useState<VolumeChartPeriod>('Week')
    const [volumeMuscleGroup, setVolumeMuscleGroup] = useState<MuscleFilter>('All')
    const [profileData, setProfileData] = useState<ProfilePageResponse | null>(null)
    const [scheduleEntries, setScheduleEntries] = useState<readonly ScheduledEntry[]>([])
    const [completedEntries, setCompletedEntries] = useState<readonly ScheduledEntry[]>([])
    const [completedWorkoutDetails, setCompletedWorkoutDetails] = useState<readonly WorkoutDetailResponse[]>([])
    const [analytics, setAnalytics] = useState<ScheduleAnalyticsResponse | null>(null)
    const [isFetching, setIsFetching] = useState(false)
    const [error, setError] = useState<string | null>(null)

    useEffect(() => {
        if (!isHydrated || !isAuthenticated){
            return
        }

        let isActive = true

        async function loadDashboard() {
            setIsFetching(true)
            setError(null)

            try {
                const today = startOfDay(new Date())
                const endDate = new Date(today)
                endDate.setDate(endDate.getDate() + 30)
                const completedRangeStart = new Date(today)
                completedRangeStart.setFullYear(completedRangeStart.getFullYear() - 1)
                completedRangeStart.setDate(1)
                completedRangeStart.setHours(0, 0, 0, 0)
                const completedRangeEnd = today

                const [profileResponse, scheduleResponse, completedResponse, analyticsResponse] = await Promise.all([
                    customFetch('/api/profile/overview', { headers: { Accept: 'application/json' }}),
                    customFetch(`/api/users/me/schedule?startDate=${today.toISOString()}&endDate=${endDate.toISOString()}` , { headers: { Accept: 'application/json' }}),
                    customFetch(`/api/users/me/schedule?startDate=${completedRangeStart.toISOString()}&endDate=${completedRangeEnd.toISOString()}&status=Completed`, { headers: { Accept: 'application/json' }}),
                    customFetch(`/api/users/me/schedule/analytics?startDate=${completedRangeStart.toISOString()}&endDate=${completedRangeEnd.toISOString()}&status=Completed`, { headers: { Accept: 'application/json' }}),
                ])
        }

    const muscleValues = useMemo(() => {
        const values: Record<(typeof MUSCLE_KEYS)[number], number> = {
            Chest: 0,
            Core: 0,
            Shoulders: 0,
            Arms: 0,
            Legs: 0,
            Back: 0,
        }

        let totalSetsAll = 0

        analytics?.muscleDistribution.forEach((item) => {
            const mapped = MUSCLE_CATEGORY_MAP[item.muscleGroup]
            if (mapped) {
                values[mapped] += item.setCount
            }
            totalSetsAll += item.setCount
        })

        if (totalSetsAll > 0){
            MUSCLE_KEYS.forEach((key) => {
                values[key] = Math.round((values[key] / totalSetsAll) * 100)
            })
        }

        return values
    }, [analytics])

    return (
        <section className="mx-auto max-w-6xl px-6 py-12">
            {isFetching && !profileData && (
                <div className="mb-6 rounded-xl border border-border bg-card px-4 py-3 text-sm text-muted-foreground shadow-sm">
                    Loading dashboard data...
                </div>
            )}

            {error && (
                <div className="mb-6 rounded-xl border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
                    {error}
                </div>
            )}

            <div className="border-l-[5px] border-brand pl-5 py-1 mb-8">
                <h1 className="text-4xl font-extrabold uppercase tracking-tight text-foreground">
                    Good Day, {displayProfile?.name ?? 'Guest'}
                </h1>
                
                <p className="mt-2 text-lg text-muted-foreground">
                    Today&apos;s Workout: <span className="font-medium text-foreground">{upcomingWorkouts[0]?.name ?? 'No workout scheduled'}</span>
                </p>

                <div className="mt-5 flex flex-wrap gap-3">
                    <button 
                        disabled={!upcomingWorkouts[0]}
                        onClick={() => {
                            if (upcomingWorkouts[0]){
                                window.location.href = `/workouts/${upcomingWorkouts[0].workoutId}`
                            }
                        }}
                        className="rounded-md border border-border bg-surface-2 px-5 py-2.5 text-xs font-bold uppercase tracking-wider text-foreground transition-colors hover:border-brand hover:text-brand disabled:opacity-50 disabled:cursor-not-allowed">
                        View Workout
                    </button>
                    <button 
                        disabled={!upcomingWorkouts[0]}
                        onClick={() => {
                            if (upcomingWorkouts[0]){
                                window.location.href = `/session/${upcomingWorkouts[0].id}`
                            }
                        }}
                        className="rounded-md border border-border bg-surface-2 px-5 py-2.5 text-xs font-bold uppercase tracking-wider text-foreground transition-colors hover:border-brand hover:text-brand disabled:opacity-50 disabled:cursor-not-allowed">
                        Start Session
                    </button>
                </div>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">        
                {/*Volume chart*/}
                <div className="md:col-span-2 h-full">
                    <VolumeChart
                        title={displayChartTitle}
                        data={displayChartData}
                        period={volumePeriod}
                        onPeriodChange={setVolumePeriod}
                        muscleFilter={volumeMuscleGroup}
                        muscleOptions={['All', ...MUSCLE_KEYS]}
                        onMuscleFilterChange={(nextValue) => setVolumeMuscleGroup(nextValue as MuscleFilter)}
                        showFilters
                        className="h-full"
                    />
                </div>

                {/*Upcoming workouts panel*/}
                <div className="h-full">
                    <UpcomingWorkoutsCard workouts={upcomingWorkouts} className="h-full" />
                </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4">
                
                {/*Favorite exercise*/}
                <Card className="flex min-h-[120px] flex-col p-4">
                    <CardContent className="flex h-full w-full flex-col px-0">
                        <h3 className="text-[18px] font-medium text-foreground text-center">Favorite exercise</h3>
                        <span className="mt-1 text-xs font-medium text-muted-foreground text-center">
                            {favoriteExercise.count > 0 ? `${favoriteExercise.count} completed sessions` : 'No completed workouts yet'}
                        </span>
                        <div className="flex-1 flex items-center justify-center mt-2">
                            <div className="flex items-center justify-center gap-3">
                                <div className="h-8 w-8 shrink-0 rounded-full border border-border bg-background"></div>
                                <span className="text-md font-bold leading-tight text-foreground">
                                    {favoriteExercise.name}
                                </span>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/*Exercise streak*/}
                <Card className="flex min-h-[120px] flex-col p-4">
                    <CardContent className="flex h-full w-full flex-col px-0">
                        <h3 className="text-[18px] font-medium text-foreground text-center">Days exercised this week</h3>
                        <div className="mt-1 flex flex-wrap justify-center gap-2">
                            {streakDays.length > 0 ? (
                                streakDays.map((day, index) => (
                                    <span
                                        key={day}
                                        className={`inline-flex items-center rounded-full border px-2.5 py-1 text-xs font-semibold ${getDayPillClass(index)}`}
                                    >
                                        {day}
                                    </span>
                                ))
                            ) : (
                                <span className="text-sm text-muted-foreground">No completed workouts this week</span>
                            )}
                        </div>
                        <div className="flex-1 flex items-center justify-center mt-2">
                            <div className="flex items-center justify-center gap-1">
                                <img src={streakFlame} alt="Exercise streak" className="h-12 w-12 object-contain" />
                                <span className="text-4xl font-bold text-foreground">{streakDays.length}</span>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/*num PRs*/}
                <Card className="flex min-h-[120px] flex-col p-4">
                    <CardContent className="flex h-full w-full flex-col px-0">
                        <h3 className="text-[18px] font-medium text-foreground text-center">Personal records hit this week</h3>
                        <div className="flex-1 flex items-center justify-center mt-2">
                            <div className="flex items-center justify-center gap-1">
                                <img src={badgeIcon} alt="Personal records badge" className="h-10 w-10 object-contain" />
                                <span className="text-4xl font-bold text-foreground">{recordsThisWeek}</span>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/*Spider graphh*/}  
                <Card className="flex min-h-[120px] flex-col p-4">
                    <CardContent className="flex h-full flex-col px-0">
                        <h3 className="mb-2 w-full text-center text-[15px] font-medium text-foreground">Muscle Balance</h3>
                        <SpiderGraph data={muscleValues} className="h-[170px]"/>
                    </CardContent>
                </Card>
            </div>
        </section>
    )
}