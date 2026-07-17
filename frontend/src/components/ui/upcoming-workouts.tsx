import { Link } from 'react-router-dom'
import { cn } from '@/lib/utils'

type UpcomingWorkout = Readonly<{
  name: string
  details: string
  highlight?: boolean
}>

type UpcomingWorkoutsCardProps = Readonly<{
  title?: string
  workouts?: UpcomingWorkout[]
  seeAllHref?: string
  className?: string
}>

const DEFAULT_WORKOUTS: UpcomingWorkout[] = [
  { name: 'Push Day B', details: 'Today - 5 exercises', highlight: true },
  { name: 'Leg Day', details: 'Thursday - 8 exercises' },
  { name: 'Pull Day B', details: 'Saturday - 7 exercises' },
]

export function UpcomingWorkoutsCard({
  title = 'Upcoming',
  workouts = DEFAULT_WORKOUTS,
  seeAllHref = '/schedule',
  className,
}: UpcomingWorkoutsCardProps) {
  return (
    <aside className={cn('flex flex-col rounded-xl bg-card p-5 text-card-foreground ring-1 ring-foreground/10 shadow-sm', className)}>
      <h2 className="mb-5 text-center text-xl font-semibold text-foreground">{title}</h2>

      {workouts.length > 0 ? (
        <div className="flex flex-col gap-5 flex-1">
          {workouts.map((workout) => (
            <div key={workout.name} className="border-l-[3px] border-brand pl-3 border-b border-border pb-2">
              <h3 className={cn('text-md font-semibold', workout.highlight ? 'text-black' : 'text-gray-700')}>
                {workout.name}
              </h3>
              <p className="text-xs text-muted-foreground">{workout.details}</p>
            </div>
          ))}
        </div>
      ):(
      <div className="flex flex-1 items-center justify-center rounded-lg border border-dashed border-border px-4 py-8 text-center text-sm text-muted-foreground">
          No upcoming workouts scheduled.
        </div>
      )}

      <Link
        to={seeAllHref}
        className="mt-6 inline-flex w-full justify-center rounded-md border border-border bg-surface-2 py-2 text-sm font-medium text-foreground transition-colors hover:border-brand hover:text-brand">
        See all
      </Link>
    </aside>
  )
}

export default UpcomingWorkoutsCard