import badgeIcon from '../../../../docs/images/badge.png'
import type { WorkoutOverviewProps } from '@/types/workout'

const MAX_VISIBLE_EXERCISES = 10

export function WorkoutOverview({ name, exercises, prs, duration, volume, sets, className }: WorkoutOverviewProps) {
  const visibleExercises = exercises.slice(0, MAX_VISIBLE_EXERCISES)
  const hasMoreExercises = exercises.length > visibleExercises.length

  return (
    <article
      className={[
        'flex h-full flex-col rounded-xl border border-border bg-surface-2 px-4 py-4 shadow-sm sm:px-5 sm:py-5',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <div className="flex flex-1 items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <h3 className="text-2xl font-bold leading-none text-foreground">{name}</h3>
          <div className="mt-3">
            <p className="text-sm font-semibold text-foreground/90">Exercises:</p>
            <ul className="mt-1.5 list-disc space-y-1 pl-5 text-sm leading-snug text-foreground/80">
              {visibleExercises.map((exercise, index) => (
                <li key={index}>
                  <span className="block max-w-[200px] truncate">{exercise}</span>
                </li>
              ))}
              {hasMoreExercises && <li className="list-none pl-1 text-muted-foreground">...</li>}
            </ul>
          </div>
        </div>

        <div className="flex shrink-0 flex-col items-center gap-2 text-center">
          <img
            src={badgeIcon}
            alt="Workout badge"
            className="h-16 w-16 select-none object-contain opacity-70 dark:hidden"
            draggable={false}
          />

          <span
            aria-hidden="true"
            className="hidden h-16 w-16 bg-white/90 dark:block"
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

          <p className="text-sm font-semibold text-foreground">{prs}</p>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4 pt-2 text-center sm:gap-6">
        <div>
          <p className="text-sm text-foreground/80">Duration</p>
          <p className="mt-1 text-base font-bold text-foreground">{duration}</p>
        </div>
        <div>
          <p className="text-sm text-foreground/80">Volume</p>
          <p className="mt-1 text-base font-bold text-foreground">{volume}</p>
        </div>
        <div>
          <p className="text-sm text-foreground/80">Sets</p>
          <p className="mt-1 text-base font-bold text-foreground">{sets}</p>
        </div>
      </div>
    </article>
  )
}

export default WorkoutOverview