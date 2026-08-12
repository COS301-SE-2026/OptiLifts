import { useState, useEffect } from 'react'
import { PageTitle } from '@/components/ui/page-title'
import { Card } from '@/components/ui/card'
import { DatePagination, getWeekStart } from '@/components/ui/date-pagination'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { PrBadgeIcon } from '@/components/ui/pr-badge-icon'
import {
    DropdownMenu,
    DropdownMenuEllipsisContent,
    DropdownMenuEllipsisTrigger,
    DropdownMenuItem,
} from '@/components/ui/dropdown-menu'
import { ConfirmDialog } from '@/components/ui/confirm-dialog'
import { customFetch } from '@/lib/custom-fetch'
import { useNavigate, useLocation, useSearchParams } from 'react-router-dom'
import { metricCheck, outputWeight } from '@/lib/weight-utils'


type ScheduledEntryDto = {
    id: string
    workoutId: string
    workoutName: string
    scheduled: string
    status: string
    primaryMuscleGroups: string[]
    exerciseCount: number
    exercisePreview: string[]
    exercisePreviewIds: string[]
    totalVolume: number
    totalSets: number
    startedAt?: string
    completedAt?: string
    recordCount?: number
    prCount?: number
    logId?: string
}

const formatDuration = (start: string, end: string) => {
    const dif = Math.floor((new Date(end).getTime() - new Date(start).getTime()) / 600000);
    if (dif < 1) {
        return '<1m'
    } else if (dif < 60) {
        return `${dif} min`
    } else {
        const hours = Math.floor(dif / 60);
        const min = dif % 60;
        return `${hours}h ${min}m`
    }

}

export default function PastWorkoutsPage() {
    const location = useLocation()
    const [searchParams] = useSearchParams()
    const navigate = useNavigate()

    const dateParam = (location.state as { date?: string } | null)?.date || searchParams.get('date') || searchParams.get('week')

    const [prevDateParam, setPrevDateParam] = useState(dateParam)
    const [selectedWeek, setSelectedWeek] = useState(() => {
        if (dateParam) {
            const d = new Date(dateParam)
            if (!Number.isNaN(d.getTime())) {
                return getWeekStart(d)
            }
        }
        return getWeekStart(new Date())
    })

    if (dateParam !== prevDateParam) {
        setPrevDateParam(dateParam)
        if (dateParam) {
            const d = new Date(dateParam)
            if (!Number.isNaN(d.getTime())) {
                setSelectedWeek(getWeekStart(d))
            }
        }
    }

    const [workouts, setWorkouts] = useState<ScheduledEntryDto[]>([])
    const [exerciseImages, setExerciseImages] = useState<{ [key: string]: string }>({})
    const [loading, setLoading] = useState(false)
    const [deleteTarget, setDeleteTarget] = useState<{ workoutId: string; logId: string } | null>(null)

    useEffect(() => {
        const fetchWorkouts = async () => {
            setLoading(true)
            try {
                const start = getWeekStart(selectedWeek)
                const end = new Date(start)
                end.setUTCDate(start.getUTCDate() + 6)
                end.setUTCHours(23, 59, 59, 999)
                const response = await customFetch(`/api/users/me/schedule?startDate=${start.toISOString()}&endDate=${end.toISOString()}&status=Completed`)
                if (response.ok) {
                    const out = await response.json()
                    setWorkouts(out)

                    const exercises = Array.from(new Set(
                        out.flatMap((workout: ScheduledEntryDto) => workout.exercisePreviewIds || [])
                    )) as string[];

                    if (exercises.length > 0) {
                        const imgRes = await customFetch('/api/exercises/images', {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ exerciseIds: exercises })
                        });

                        if (imgRes.ok) {
                            const imgDetails = await imgRes.json();
                            setExerciseImages(imgDetails)
                        }
                    }
                }
            } catch (error) {
                console.error('Error fetching workouts:', error)
            } finally {
                setLoading(false)
            }
        }

        void fetchWorkouts()
    }, [selectedWeek])

    let out;

    const handleDelete = async (workoutId: string, logId: string) => {
        setLoading(true)
        try {
            const response = await customFetch(`/api/workouts/${workoutId}/logs/${logId}`, {
                method: 'DELETE',
                headers: {
                    Accept: 'application/json',
                },
            })

            if (!response.ok) {
                throw new Error(`Failed to delete workout log (${response.status})`)
            }

            setWorkouts((current) => current.filter((workout) => workout.logId !== logId))
        } catch (error) {
            console.error('Error deleting workout log:', error)
        } finally {
            setLoading(false)
        }
    }

    if (loading) {
        out = (
            <div className="text-center text-muted-foreground py-10">
                Loading workouts...
            </div>
        );
    } else if (workouts.length === 0) {
        out = (
            <div className="text-center text-muted-foreground py-10">
                You have not completed any workouts this week.
            </div>
        );
    } else {
        out = (
            <div className="space-y-5">
                {workouts.map((workout) => {
                    //map exercise names to images via dictionary
                    const allImages = (workout.exercisePreviewIds || [])
                        .map(id => exerciseImages[id])
                        .filter(Boolean);

                    const exerImages = allImages.slice(0, 8);
                    const extraExercises = allImages.length > 8;
                    let workoutDate;
                    if (workout.startedAt) {
                        workoutDate = new Intl.DateTimeFormat('en-US', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(workout.startedAt))
                    } else {
                        workoutDate = "Unknown date"
                    }

                    const logDate = workout.completedAt || workout.startedAt || workout.scheduled
                    const openLogDetail = () => {
                        navigate(`/workouts/${workout.workoutId}/logs/${workout.logId}`, { state: { date: logDate } })
                    }

                    return (
                        <Card
                            key={workout.id}
                            role="button"
                            tabIndex={0}
                            onClick={(e) => {
                                const target = e.target as HTMLElement
                                if (target.closest('[data-card-menu="true"]')) {
                                    return
                                }
                                openLogDetail()
                            }}
                            onKeyDown={(e) => {
                                const target = e.target as HTMLElement
                                if (target.closest('[data-card-menu="true"]')) {
                                    return
                                }
                                if (e.key === 'Enter' || e.key === ' ') {
                                    e.preventDefault();
                                    openLogDetail();
                                }
                            }}
                            className="relative flex flex-col gap-6 p-6 sm:flex-row sm:items-center sm:justify-between border-border cursor-pointer transition-shadow hover:ring-2 hover:ring-brand focus-visible:ring-2 focus-visible:ring-brand"
                        >
                            <div className="absolute right-4 top-4 z-10" data-card-menu="true">
                                <DropdownMenu>
                                    <DropdownMenuEllipsisTrigger aria-label={`Options for ${workout.workoutName}`} />
                                    <DropdownMenuEllipsisContent align="end">
                                        <DropdownMenuItem
                                            onSelect={(event) => {
                                                event.preventDefault()
                                                if (!workout.logId) {
                                                    return
                                                }
                                                setDeleteTarget({ workoutId: workout.workoutId, logId: workout.logId })
                                            }}
                                            data-variant="destructive"
                                            disabled={!workout.logId}
                                        >
                                            Delete
                                        </DropdownMenuItem>
                                    </DropdownMenuEllipsisContent>
                                </DropdownMenu>
                            </div>
                            {/*left side */}
                            <div className="flex flex-col gap-4">
                                <div>
                                    <h2 className="text-xl font-bold text-foreground truncate max-w-[200px] sm:max-w-[300px]">{workout.workoutName}</h2>
                                    <p className="text-sm font-bold text-foreground/80">{workoutDate}</p>
                                    <p className="mt-1 text-sm text-muted-foreground line-clamp-1 sm:line-clamp-2 max-w-[250px] sm:max-w-[350px]">
                                        Muscles: {(workout.primaryMuscleGroups).join(', ')}
                                    </p>
                                </div>
                                {/*exercise pics */}
                                <div className="flex items-center gap-2 pt-1">
                                    {exerImages.map((imgUrl, idx) => (
                                        <div
                                            key={`${workout.id}-${imgUrl}-${idx}`}
                                            className="h-8 w-8 sm:h-9 sm:w-9 rounded-full bg-background"
                                        >
                                            <CircularProfileImage
                                                src={imgUrl}
                                                alt="Exercise"
                                                className="h-full w-full border border-border"
                                            />
                                        </div>
                                    ))}
                                    {/*more image */}
                                    {extraExercises && (
                                        <div className="flex h-8 w-8 sm:h-9 sm:w-9 items-center justify-center rounded-full border border-border bg-background">
                                            <span className="text-xs font-black tracking-widest text-muted-foreground mb-1">...</span>
                                        </div>
                                    )}
                                </div>
                            </div>
                            {/*right side */}
                            <div className="grid grid-cols-2 sm:grid-cols-4 gap-6 sm:gap-8 pt-5 sm:pt-0 shrink-0 sm:w-[26rem] md:w-[30rem] lg:w-[30rem]">
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Duration</span>
                                    <span className="text-lg font-bold text-foreground">
                                        {formatDuration(workout.startedAt ?? '', workout.completedAt ?? '')}
                                    </span>
                                </div>
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Volume</span>
                                    <span className="text-lg font-bold text-foreground whitespace-nowrap">
                                        {outputWeight(workout.totalVolume).toLocaleString(undefined, { maximumFractionDigits: 0 })} {(metricCheck())? 'KG' : 'LB'}
                                    </span>
                                </div>
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Exercises</span>
                                    <span className="text-lg font-bold text-foreground">{workout.exerciseCount}</span>
                                </div>
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">PRs</span>
                                    <div className="flex items-center gap-1.5">
                                        <PrBadgeIcon
                                            alt="Record Badge"
                                            sizeClassName="h-[24px] w-[24px]"
                                            lightClassName="opacity-75"
                                        />
                                        <span className="text-lg font-bold text-foreground">{workout.recordCount ?? workout.prCount ?? 0}</span>
                                    </div>
                                </div>
                            </div>
                        </Card>
                    )
                })}
            </div>
        )
    }

    return (
        <section className="mx-auto max-w-5xl px-6 py-12">

            <div className="mb-10 flex flex-col gap-6 sm:flex-row sm:items-center sm:justify-between">
                <PageTitle title="COMPLETED WORKOUTS" />
                <DatePagination
                    currentDate={selectedWeek}
                    onChange={setSelectedWeek}
                    type="week"
                />
            </div>

            {/* block above is for out */}
            {out}

            <ConfirmDialog
                isOpen={deleteTarget !== null}
                onClose={() => setDeleteTarget(null)}
                isLoading={loading}
                variant="danger"
                title="Delete Workout Log"
                description="Are you certain you want to permanently delete this workout log?"
                confirmText="Delete"
                cancelText="Cancel"
                onConfirm={async () => {
                    if (deleteTarget) {
                        const target = deleteTarget
                        setDeleteTarget(null)
                        await handleDelete(target.workoutId, target.logId)
                    }
                }}
            />
        </section>
    )
}