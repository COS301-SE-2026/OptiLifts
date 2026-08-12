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
        'relative flex h-[180px] min-h-[180px] flex-col justify-between overflow-hidden rounded-2xl border border-border bg-surface-2 p-4 pr-12 shadow-sm sm:h-[180px] sm:p-4 sm:pr-14',
        className,
      ]
        .filter(Boolean)
        .join(' ')}
    >
      <PrBadgeIcon
        iconUrl={resolvedIcon}
        alt="Badge icon"
        className="absolute right-3 top-3 sm:right-4 sm:top-4"
        sizeClassName="h-10 w-10"
        lightClassName="opacity-85"
      />

      <div className="min-w-0 space-y-1.5 pr-5">
        <p className="text-[0.62rem] font-semibold uppercase tracking-[0.18em] text-muted-foreground">{category}</p>
        <h3 className="max-w-[10ch] text-base font-bold leading-tight text-foreground sm:text-[1.05rem]">{name}</h3>
      </div>

      <p className="text-[0.7rem] font-medium uppercase leading-tight tracking-[0.14em] text-muted-foreground">
        Earned<br />
        <span className="whitespace-nowrap">{formatEarnedAt(earnedAt)}</span>
      </p>
    </article>
  )
}

export default Badge