import { Link } from 'react-router-dom'
import type { WorkoutOverviewProps } from '@/types/workout'
import { PrBadgeIcon } from '@/components/ui/pr-badge-icon'

const MAX_VISIBLE_EXERCISES = 3

export function WorkoutOverview({ href, name, exercises, prs, duration, volume, sets, className }: WorkoutOverviewProps) {
  const visibleExercises = exercises.slice(0, MAX_VISIBLE_EXERCISES)
  const hasMoreExercises = exercises.length > visibleExercises.length
  const exerciseKeyCounts = new Map<string, number>()

  const rootClasses = [
    'flex h-full flex-col justify-between rounded-xl border border-border bg-surface-2 px-3.5 py-3 text-left shadow-sm transition-shadow hover:shadow-md sm:px-4 sm:py-3.5',
    className,
  ]
    .filter(Boolean)
    .join(' ')

  const content = (
    <>
      <div className="flex flex-1 items-start justify-between gap-3 sm:gap-4">
        <div className="min-w-0 flex-1">
          <h3 className="text-base sm:text-lg font-bold leading-tight text-foreground truncate">{name}</h3>
          <div className="mt-1.5 sm:mt-2">
            <p className="text-xs sm:text-sm font-semibold text-foreground/90">Exercises:</p>
            <ul className="mt-1 list-disc space-y-0.5 pl-4 text-xs sm:text-sm leading-snug text-foreground/80">
              {visibleExercises.map((exercise) => {
                const seenCount = exerciseKeyCounts.get(exercise) ?? 0
                exerciseKeyCounts.set(exercise, seenCount + 1)

                return (
                  <li key={`${exercise}-${seenCount}`}>
                    <span className="block max-w-[180px] sm:max-w-[200px] truncate">{exercise}</span>
                  </li>
                )
              })}
              {hasMoreExercises && <li className="list-none pl-0.5 text-xs text-muted-foreground">...</li>}
            </ul>
          </div>
        </div>

        <div className="flex shrink-0 flex-col items-center gap-1 text-center">
          <PrBadgeIcon
            alt="Workout badge"
            sizeClassName="h-11 w-11 sm:h-13 sm:w-13"
            lightClassName="opacity-70"
          />

          <p className="text-xs sm:text-sm font-semibold text-foreground">{prs}</p>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-2 pt-2.5 sm:gap-4 sm:pt-3 border-t border-border/40 mt-2">
        <div className="text-left">
          <p className="text-[0.68rem] sm:text-xs text-muted-foreground uppercase tracking-wider">Duration</p>
          <p className="mt-0.5 text-sm sm:text-base font-bold text-foreground">{duration}</p>
        </div>
        <div className="text-center">
          <p className="text-[0.68rem] sm:text-xs text-muted-foreground uppercase tracking-wider">Volume</p>
          <p className="mt-0.5 text-sm sm:text-base font-bold text-foreground">{volume}</p>
        </div>
        <div className="text-right pr-2 sm:pr-3">
          <p className="text-[0.68rem] sm:text-xs text-muted-foreground uppercase tracking-wider">Sets</p>
          <p className="mt-0.5 text-sm sm:text-base font-bold text-foreground">{sets}</p>
        </div>
      </div>
    </>
  )

  if (href) {
    return (
      <Link to={href} className={rootClasses}>
        {content}
      </Link>
    )
  }

  return <article className={rootClasses}>{content}</article>
}

export default WorkoutOverview