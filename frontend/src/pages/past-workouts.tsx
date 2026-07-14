import { useState, useEffect } from 'react'
import badgeIcon from '@/assets/badge.png'
import { PageTitle } from '@/components/ui/page-title'
import { Card } from '@/components/ui/card'
import { DatePagination } from '@/components/ui/date-pagination'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { customFetch } from '@/lib/custom-fetch'


type ScheduledEntryDto = {
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
    startedAt?: string
    completedAt?: string
    recordsCount?: number
}

const formatDuration = (start: string, end: string) => {
    const dif = Math.floor((new Date(end).getTime() - new Date(start).getTime()) / 600000);
    if (dif < 1) {
        return '<1 min'
    } else if (dif < 60) {
        return `${dif} min`
    } else {
        const hours = Math.floor(dif / 60);
        const min = dif % 60;
        return `${hours}h ${min}m`
    }

}

export default function PastWorkoutsPage() {
    const [selectedWeek, setSelectedWeek] = useState(() => new Date())
    const [workouts, setWorkouts] = useState<ScheduledEntryDto[]>([])
    const [exerciseImages, setExerciseImages] = useState<{ [key: string]: string }>({})
    const [loading, setLoading] = useState(false)

    useEffect(() => {
        const getImages = async () => {
            try {
                const response = await customFetch(`/api/exercises/allImages`)
                if (response.ok) {
                    const out = await response.json();
                    setExerciseImages(out)
                }
            } catch (e) {
                console.error('Error fetching exercise images:', e)
            }
        }
        void getImages();
    }, [])

    useEffect(() => {
        const fetchWorkouts = async () => {
            setLoading(true)
            try {
                const start = new Date(selectedWeek)
                const end = new Date(start)
                end.setDate(start.getDate() + 6)
                const response = await customFetch(`/api/users/me/schedule?startDate=${start.toISOString()}&endDate=${end.toISOString()}&status=Completed`)
                if (response.ok) {
                    const out = await response.json()
                    setWorkouts(out)
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
                    const allImages = (workout.exercisePreview || [])
                        .map(name => exerciseImages[name])
                        .filter(Boolean);

                    const exerImages = allImages.slice(0, 8);
                    const extraExercises = allImages.length > 8;
                    let workoutDate;
                    if (workout.startedAt) {
                        workoutDate = new Intl.DateTimeFormat('en-US', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(workout.startedAt))
                    } else {
                        workoutDate = "Unknown date"
                    }

                    return (
                        <Card
                            key={workout.id}
                            role="button"
                            tabIndex={0}
                            onClick={() => { /* to go to specific workout page */ }}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') {
                                    e.preventDefault();
                                    /* to go to specific workout page */
                                }
                            }}
                            className="flex flex-col gap-6 p-6 sm:flex-row sm:items-center sm:justify-between border-border cursor-pointer transition-shadow hover:ring-2 hover:ring-brand focus-visible:ring-2 focus-visible:ring-brand"
                        >
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
                            <div className="grid grid-cols-4 gap-6 sm:gap-8 pt-3 sm:pt-0">
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Duration</span>
                                    <span className="text-lg font-bold text-foreground">
                                        {formatDuration(workout.startedAt ?? '', workout.completedAt ?? '')}
                                    </span>
                                </div>
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Volume</span>
                                    <span className="text-lg font-bold text-foreground">
                                        {workout.totalVolume.toLocaleString()} kg
                                    </span>
                                </div>
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Exercises</span>
                                    <span className="text-lg font-bold text-foreground">{workout.exerciseCount}</span>
                                </div>
                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Records</span>
                                    <div className="flex items-center gap-1.5">
                                        <div className="relative h-[24px] w-[24px]">
                                            <img
                                                src={badgeIcon}
                                                alt="Record Badge"
                                                className="h-full w-full object-contain opacity-75 dark:hidden"
                                            />
                                            <span
                                                aria-hidden="true"
                                                className="hidden h-full w-full bg-white/90 dark:block"
                                                style={{
                                                    WebkitMaskImage: `url(${badgeIcon})`,
                                                    WebkitMaskRepeat: 'no-repeat',
                                                    WebkitMaskPosition: 'center',
                                                    WebkitMaskSize: 'contain',
                                                    maskImage: `url(${badgeIcon})`,
                                                    maskRepeat: 'no-repeat',
                                                    maskPosition: 'center',
                                                    maskSize: 'contain',
                                                }}
                                            />
                                        </div>
                                        <span className="text-lg font-bold text-foreground">{workout.recordsCount}</span>
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
        </section>
    )
}