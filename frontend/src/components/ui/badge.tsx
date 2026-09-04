import badgeIcon from '../../assets/badge.png'
import { PrBadgeIcon } from '@/components/ui/pr-badge-icon'

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
        'relative flex h-[155px] sm:h-[185px] min-h-[155px] sm:min-h-[185px] flex-col justify-between overflow-hidden rounded-xl sm:rounded-2xl border border-border bg-surface-2 p-3 sm:p-4 shadow-sm',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <PrBadgeIcon
        iconUrl={resolvedIcon}
        alt="Badge icon"
        className="absolute right-2 top-2 sm:right-3 sm:top-3"
        sizeClassName="h-9 w-9 sm:h-12 sm:w-12"
        lightClassName="opacity-85"
      />

      <div className="min-w-0 space-y-1 sm:space-y-1.5 pr-8 sm:pr-10">
        <p className="text-[0.65rem] sm:text-[0.72rem] font-semibold uppercase tracking-[0.1em] sm:tracking-[0.18em] text-muted-foreground truncate">{category}</p>
        <h3 className="text-sm sm:text-[1.1rem] font-bold leading-tight text-foreground line-clamp-2">{name}</h3>
      </div>

      <p className="text-[0.65rem] sm:text-[0.75rem] font-medium uppercase leading-tight tracking-[0.06em] sm:tracking-[0.14em] text-muted-foreground">
        Earned<br />
        <span className="whitespace-nowrap">{formatEarnedAt(earnedAt)}</span>
      </p>
    </article>
  )
}

export default Badge