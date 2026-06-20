import badgeIcon from '../../../../docs/images/badge.png'

type BadgeProps = Readonly<{
  name: string
  description: string
  category: string
  earnedAt: string
  iconUrl?: string | null
  className?: string
}>

const formatEarnedAt = (value: string) =>
  new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric', year: 'numeric' }).format(new Date(value))

export function Badge({ name, category, earnedAt, iconUrl, className }: BadgeProps) {
  const resolvedIcon = iconUrl ?? badgeIcon

  return (
    <article
      className={[
        'relative flex h-[180px] min-h-[180px] flex-col justify-between overflow-hidden rounded-2xl border border-border bg-surface-2 p-4 pr-12 shadow-sm sm:h-[180px] sm:p-4 sm:pr-14',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <img
        src={resolvedIcon}
        alt="Badge icon"
        className="absolute right-3 top-3 h-10 w-10 select-none object-contain opacity-85 dark:hidden sm:right-4 sm:top-4"
        draggable={false}
      />

      <span
        aria-hidden="true"
        className="absolute right-3 top-3 hidden h-10 w-10 bg-white/90 dark:block sm:right-4 sm:top-4"
        style={{
          WebkitMaskImage: `url(${resolvedIcon})`,
          WebkitMaskRepeat: 'no-repeat',
          WebkitMaskPosition: 'center',
          WebkitMaskSize: 'contain',
          maskImage: `url(${resolvedIcon})`,
          maskRepeat: 'no-repeat',
          maskPosition: 'center',
          maskSize: 'contain',
        }}
      />

      <div className="min-w-0 space-y-1">
        <p className="text-[0.62rem] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{category}</p>
        <h3 className="max-w-[10ch] text-base font-bold leading-tight text-foreground sm:text-[1.05rem]">{name}</h3>
      </div>

      <p className="max-w-[12ch] text-[0.7rem] font-medium uppercase leading-tight tracking-[0.14em] text-muted-foreground">
        Earned {formatEarnedAt(earnedAt)}
      </p>
    </article>
  )
}

export default Badge