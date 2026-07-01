import { useState } from 'react'
import badgeIcon from '@/assets/badge.png'
import { PageTitle } from '@/components/ui/page-title'
import { Card } from '@/components/ui/card'
import { DatePagination } from '@/components/ui/date-pagination' // <-- Imported your new component

type PastWorkout = {
    id: string
    name: string
    date: string
    muscles: string[]
    duration: string
    volume: string
    exercisesCount: number
    recordsCount: number
    exerciseImages: string[]
}

const MOCK_WORKOUTS: PastWorkout[] = [
    {
        id: '1',
        name: 'Push Day A',
        date: '23 June 2026 at 14:30',
        muscles: ['Chest', 'Shoulders', 'Brachioradialis'],
        duration: '1h 20 min',
        volume: '30 000 kg',
        exercisesCount: 8,
        recordsCount: 3,
        exerciseImages: Array(8).fill('')
    },
    {
        id: '2',
        name: 'Push Day A',
        date: '23 June 2026 at 14:30',
        muscles: ['Chest', 'Shoulders', 'Brachioradialis'],
        duration: '1h 20 min',
        volume: '30 000 kg',
        exercisesCount: 12,
        recordsCount: 3,
        exerciseImages: Array(12).fill('')
    },
    {
        id: '3',
        name: 'Push Day A',
        date: '23 June 2026 at 14:30',
        muscles: ['Chest', 'Shoulders', 'Brachioradialis'],
        duration: '1h 20 min',
        volume: '30 000 kg',
        exercisesCount: 8,
        recordsCount: 3,
        exerciseImages: Array(8).fill('')
    }

]

export default function PastWorkoutsPage() {
    const [workouts] = useState<PastWorkout[]>(MOCK_WORKOUTS)

    const [selectedWeek, setSelectedWeek] = useState(() => new Date())

    return (
        <section className="mx-auto max-w-5xl px-6 py-12">

            {/*title and pagination */}
            <div className="mb-10 flex items-center justify-between">
                <PageTitle title="COMPLETED WORKOUTS" />

                <DatePagination
                    currentDate={selectedWeek}
                    onChange={setSelectedWeek}
                    type="week"
                />
            </div>

            {/*entries */}
            <div className="space-y-5">
                {workouts.map((workout) => {
                    const exerImages = workout.exerciseImages.slice(0, 8);
                    const extraExercises = workout.exerciseImages.length > 8;

                    return (
                        <Card
                            key={workout.id}
                            role="button"
                            tabIndex={0}
                            onClick={() => { /* to go to specific workout page */ }}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter' || e.key === ' ') {
                                    e.preventDefault();
                                    /* to go to specific workout page */
                                }
                            }}
                            className="flex flex-col gap-6 p-6 sm:flex-row sm:items-center sm:justify-between border-border cursor-pointer transition-shadow hover:ring-2 hover:ring-brand focus-visible:ring-2 focus-visible:ring-brand"
                        >
                            {/*left side */}
                            <div className="flex flex-col gap-4">
                                <div>
                                    <h2 className="text-xl font-bold text-foreground">{workout.name}</h2>
                                    <p className="text-sm font-bold text-foreground/80">{workout.date}</p>
                                    <p className="mt-1 text-sm text-muted-foreground">
                                        Muscles: {workout.muscles.join(', ')}
                                    </p>
                                </div>

                                {/*exercise pics */}
                                <div className="flex items-center gap-2 pt-1">
                                    {exerImages.map((img, idx) => (
                                        <div
                                            key={idx}
                                            className="h-8 w-8 sm:h-9 sm:w-9 overflow-hidden rounded-full border border-border bg-background"
                                        >
                                            {img && <img src={img} alt="Exercise" className="h-full w-full object-cover" />}
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
                                    <span className="text-lg font-bold text-foreground">{workout.duration}</span>
                                </div>

                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Volume</span>
                                    <span className="text-lg font-bold text-foreground">{workout.volume}</span>
                                </div>

                                <div className="flex flex-col items-center gap-1.5">
                                    <span className="text-sm text-muted-foreground">Exercises</span>
                                    <span className="text-lg font-bold text-foreground">{workout.exercisesCount}</span>
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
        </section>
    )
}