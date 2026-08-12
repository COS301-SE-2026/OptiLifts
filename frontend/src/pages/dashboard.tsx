import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { UpcomingWorkoutsCard } from '@/components/ui/upcoming-workouts'
import { VolumeChart } from '@/components/ui/volume-chart'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { Card, CardContent, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { PrBadgeIcon } from '@/components/ui/pr-badge-icon'
import streakFlame from '@/assets/streak_flame.png'
import { customFetch } from '@/lib/custom-fetch'
import { WORKOUT_LOG_SYNC_EVENT } from '@/lib/offline/workout-logs'
import { useAuth } from '@/context/auth-context'
import type { ProfilePageResponse } from '@/types/profile'
import type { WorkoutDetailResponse } from '@/types/workout-detail'
import type { VolumeChartPeriod } from '@/components/ui/volume-chart'
import { Dumbbell } from 'lucide-react'

type ScheduleAnalyticsResponse = Readonly<{
    totalWorkouts: number
    totalVolume: number
    totalSets: number
    muscleDistribution: readonly {
        muscleGroup: string
        setCount: number
        percentage: number
    }[]
    secondaryMuscleDistribution?: readonly {
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
    prCount?: number | null
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
    'Upper Back': 'Back',
    Trapezius: 'Back',
    Shoulders: 'Shoulders',
    'Front Deltoid': 'Shoulders',
    'Middle Deltoid': 'Shoulders',
    'Rear Deltoid': 'Shoulders',
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
    Obliques: 'Core',
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

function endOfDay(date: Date) {
    const next = new Date(date)
    next.setHours(23, 59, 59, 999)
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
    return startOfDay(new Date(entry.completedAt ?? entry.startedAt ?? entry.scheduled))
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
    const navigate = useNavigate()
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
    const [refreshToken, setRefreshToken] = useState(0)

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
                const completedRangeEnd = endOfDay(new Date())

                const [profileResponse, scheduleResponse, completedResponse, analyticsResponse] = await Promise.all([
                    customFetch('/api/profile/overview', { headers: { Accept: 'application/json' }}),
                    customFetch(`/api/users/me/schedule?startDate=${today.toISOString()}&endDate=${endDate.toISOString()}` , { headers: { Accept: 'application/json' }}),
                    customFetch(`/api/users/me/schedule?startDate=${completedRangeStart.toISOString()}&endDate=${completedRangeEnd.toISOString()}&status=Completed`, { headers: { Accept: 'application/json' }}),
                    customFetch(`/api/users/me/schedule/analytics?startDate=${completedRangeStart.toISOString()}&endDate=${completedRangeEnd.toISOString()}&status=Completed`, { headers: { Accept: 'application/json' }}),
                ])

                if (!profileResponse.ok){
                    throw new Error(`Failed to load profile (${profileResponse.status})`)
                }
                if (!scheduleResponse.ok){
                    throw new Error(`Failed to load schedule (${scheduleResponse.status})`)
                }
                if (!completedResponse.ok){
                    throw new Error(`Failed to load completed workouts (${completedResponse.status})`)
                }
                if (!analyticsResponse.ok){
                    throw new Error(`Failed to load schedule analytics (${analyticsResponse.status})`)
                }

                const profileJson = (await profileResponse.json()) as ProfilePageResponse
                const upcomingJson = (await scheduleResponse.json()) as ScheduledEntry[]
                const completedJson = (await completedResponse.json()) as ScheduledEntry[]
                const analyticsJson = (await analyticsResponse.json()) as ScheduleAnalyticsResponse

                const workoutIds = [...new Set(completedJson.map((entry) => entry.workoutId))]
                const workoutDetailResponses = await Promise.all(
                    workoutIds.map(async (workoutId) => {
                        const response = await customFetch(`/api/workouts/${workoutId}`, { headers: { Accept: 'application/json' }})
                        if (!response.ok){
                            return null
                        }

                        return (await response.json()) as WorkoutDetailResponse
                    }),
                )

                if (!isActive){
                    return
                }

                setProfileData(profileJson)
                setScheduleEntries(upcomingJson)
                setCompletedEntries(completedJson)
                setCompletedWorkoutDetails(workoutDetailResponses.filter((detail): detail is WorkoutDetailResponse => detail !== null))
                setAnalytics(analyticsJson)
            } catch (loadError){
                if (isActive){
                    setError(loadError instanceof Error ? loadError.message : 'Failed to load dashboard data.')
                }
            } finally{
                if (isActive){
                    setIsFetching(false)
                }
            }
        }

        void loadDashboard()

        const handleWorkoutSync = () => {
            if (isActive) {
                setRefreshToken((current) => current + 1)
            }
        }

        globalThis.addEventListener(WORKOUT_LOG_SYNC_EVENT, handleWorkoutSync)

        return () => {
            isActive = false
            globalThis.removeEventListener(WORKOUT_LOG_SYNC_EVENT, handleWorkoutSync)
        }}, [isAuthenticated, isHydrated, refreshToken])

    const displayProfile = profileData?.profile
    const displayChartTitle = 'Completed Workout Volume'
    const displayChartData = useMemo(() => buildVolumeChartData(completedEntries, volumePeriod, volumeMuscleGroup), [completedEntries, volumePeriod, volumeMuscleGroup])

    const upcomingWorkouts = useMemo(() => {
        const todayStart = startOfDay(new Date())
        return [...scheduleEntries]
            .filter((entry) => entry.status === 'Scheduled' && startOfDay(new Date(entry.scheduled)) >= todayStart)
            .sort((left, right) => new Date(left.scheduled).getTime() - new Date(right.scheduled).getTime())
            .slice(0, 3)
            .map((entry, index) => ({
                id: entry.id,
                workoutId: entry.workoutId,
                name: entry.workoutName,
                details: `${formatUpcomingDate(entry.scheduled)} - ${entry.exerciseCount} exercises`,
                highlight: index === 0,
            }))}, [scheduleEntries])

    const streakDays = useMemo(() => {
        const currentWeekStart = startOfWeek(new Date())
        const currentWeekEnd = addDays(currentWeekStart, 6)

        return [...new Set(
            completedEntries
                .map((entry) => getEntryDate(entry))
                .filter((date) => date >= currentWeekStart && date <= currentWeekEnd)
                .map((date) => date.toISOString().slice(0, 10)),
        )]
            .sort((left, right) => left.localeCompare(right)).map((dateKey) => formatDayLabel(new Date(dateKey)))}, [completedEntries])

    const prsThisWeek = useMemo(() => {
        const currentWeekStart = startOfWeek(new Date())
        const currentWeekEnd = addDays(currentWeekStart, 6)

        return completedEntries
            .filter((entry) => {
                const entryDate = getEntryDate(entry)
                return entryDate >= currentWeekStart && entryDate <= currentWeekEnd
            })
            .reduce((total, entry) => total + (entry.recordCount ?? entry.prCount ?? 0), 0)}, [completedEntries])

    const favoriteExercise = useMemo(() => {
        if (completedWorkoutDetails.length === 0 || completedEntries.length === 0) {
            return { name: 'No workouts yet', count: 0, imageUrl: null as string | null }
        }

        const completionCountsByWorkoutId = completedEntries.reduce((counts, entry) => {
            counts.set(entry.workoutId, (counts.get(entry.workoutId) ?? 0) + 1)
            return counts
        }, new Map<string, number>())

        const workoutById = new Map(completedWorkoutDetails.map((workout) => [workout.id, workout]))
        const exerciseStats = new Map<string, { count: number; imageUrl: string | null }>()

        completionCountsByWorkoutId.forEach((completionCount, workoutId) => {
            const workout = workoutById.get(workoutId)
            if (!workout){
                return
            }

            workout.exercises.forEach((exercise) => {
                const existing = exerciseStats.get(exercise.name)
                if (existing) {
                    existing.count += completionCount
                    if (!existing.imageUrl && exercise.imageUrl) {
                        existing.imageUrl = exercise.imageUrl
                    }
                    return
                }

                exerciseStats.set(exercise.name, {
                    count: completionCount,
                    imageUrl: exercise.imageUrl ?? null,
                })
            })
        })

        const [name, stats] = [...exerciseStats.entries()].sort((left, right) => right[1].count - left[1].count)[0]
            ?? ['No workouts yet', { count: 0, imageUrl: null as string | null }]

        return { name, count: stats.count, imageUrl: stats.imageUrl }
    }, [completedWorkoutDetails, completedEntries])

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

    const secondaryMuscleValues = useMemo(() => {
        const values: Record<(typeof MUSCLE_KEYS)[number], number> = {
            Chest: 0,
            Core: 0,
            Shoulders: 0,
            Arms: 0,
            Legs: 0,
            Back: 0,
        }

        analytics?.secondaryMuscleDistribution?.forEach((item) => {
            const mapped = MUSCLE_CATEGORY_MAP[item.muscleGroup]
            if (mapped) {
                values[mapped] += item.setCount
            }
        })

        return values
    }, [analytics])

    return (
        <section className="mx-auto max-w-6xl px-6 py-12">
            {isFetching && !profileData && (
                <div className="mb-6 rounded-xl border border-border bg-card px-4 py-3 text-sm text-muted-foreground shadow-sm">
                    Loading dashboard data
                </div>
            )}

            {error && (
                <div className="mb-6 rounded-xl border border-destructive/20 bg-destructive/5 px-4 py-3 text-sm text-destructive">
                    {error}
                </div>
            )}


            <div className="mb-8">
                <PageTitle title={`Good Day, ${displayProfile?.name ?? 'Guest'}`} />
                <p className="mt-2 text-lg text-muted-foreground">
                    Upcoming Workout: <span className="font-medium text-foreground">{upcomingWorkouts[0]?.name ?? 'No workout scheduled'}</span>
                </p>

                <div className="mt-2 flex flex-wrap gap-2">
                    <Button
                        disabled={!upcomingWorkouts[0]}
                        onClick={() => {
                            if (upcomingWorkouts[0]){
                                navigate(`/workouts/${upcomingWorkouts[0].workoutId}`)
                            }
                        }}
                        className="h-7 px-5 py-0 text-xs font-bold uppercase tracking-wider"
                    >
                        View Workout
                    </Button>
                    <Button
                        disabled={!upcomingWorkouts[0]}
                        onClick={() => {
                            if (upcomingWorkouts[0]){
                                navigate('/active-session', {
                                    state: {
                                        workout: {
                                            id: upcomingWorkouts[0].workoutId,
                                            name: upcomingWorkouts[0].name,
                                            primaryMuscleGroups: [],
                                        },
                                    },
                                })
                            }
                        }}
                        className="h-7 px-5 py-0 text-xs font-bold uppercase tracking-wider"
                    >
                        Start Session
                    </Button>
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
                        <CardTitle className="text-center text-[16px] font-semibold text-foreground">Favorite exercise</CardTitle>
                        <span className="mt-1 text-s font-medium text-muted-foreground text-center">
                            {favoriteExercise.count > 0 ? `${favoriteExercise.count} completed sessions` : 'No completed workouts yet'}
                        </span>
                        <div className="flex-1 flex items-center justify-center mt-2">
                            <div className="flex items-center justify-center gap-3">
                                <CircularProfileImage
                                    src={favoriteExercise.imageUrl ?? undefined}
                                    alt={favoriteExercise.name}
                                    className="h-13 w-13 shrink-0 border-border"
                                    fallbackIcon={<Dumbbell className="h-4 w-4 text-muted-foreground" />}
                                />
                                <span className="text-lg font-bold leading-tight text-foreground">
                                    {favoriteExercise.name}
                                </span>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/*Exercise streak*/}
                <Card className="flex min-h-[120px] flex-col p-4">
                    <CardContent className="flex h-full w-full flex-col px-0">
                        <CardTitle className="text-center text-[16px] font-semibold text-foreground">Days exercised this week</CardTitle>
                        <div className="flex-1 flex items-center justify-center mt-2">
                            <div className="flex items-center justify-center gap-1">
                                <img
                                    src={streakFlame}
                                    alt="Exercise streak"
                                    className="h-12 w-12 select-none object-contain opacity-85 dark:hidden"
                                    draggable={false}
                                />

                                <span
                                    aria-hidden="true"
                                    className="hidden h-12 w-12 bg-foreground dark:block"
                                    style={{
                                        WebkitMaskImage: `url(${streakFlame})`,
                                        WebkitMaskRepeat: 'no-repeat',
                                        WebkitMaskPosition: 'center',
                                        WebkitMaskSize: 'contain',
                                        maskImage: `url(${streakFlame})`,
                                        maskRepeat: 'no-repeat',
                                        maskPosition: 'center',
                                        maskSize: 'contain',
                                    }}
                                />

                                <span className="text-4xl font-bold text-foreground">{streakDays.length}</span>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/*num PRs*/}
                <Card className="flex min-h-[120px] flex-col p-4">
                    <CardContent className="flex h-full w-full flex-col px-0">
                        <CardTitle className="text-center text-[16px] font-semibold text-foreground">Personal records hit this week</CardTitle>
                        <div className="flex-1 flex items-center justify-center mt-2">
                            <div className="flex items-center justify-center gap-1">
                                <PrBadgeIcon
                                    alt="Personal records badge"
                                    sizeClassName="h-10 w-10"
                                    lightClassName="opacity-85"
                                />

                                <span className="text-4xl font-bold text-foreground">{prsThisWeek}</span>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                {/*Spider graphh*/}  
                <Card className="flex min-h-[120px] flex-col p-4">
                    <CardContent className="flex h-full flex-col px-0">
                        <h3 className="mb-2 w-full text-center text-[20px] font-medium text-foreground">Muscle Balance</h3>
                        <SpiderGraph data={muscleValues} secondaryData={secondaryMuscleValues} className="h-[170px]"/>
                    </CardContent>
                </Card>
            </div>
        </section>
    )
}