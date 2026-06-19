import badgeIcon from '../../../../docs/images/badge.png'

type BadgeProps = Readonly<{
  label: string
  value: string
  className?: string
}>

export function Badge({ label, value, className }: BadgeProps) {
  return (
    <section className={[
      'flex aspect-square w-full flex-col items-center justify-center gap-2 rounded-2xl border border-border bg-surface-2 px-3 py-2 text-center shadow-sm',
      className,
    ]
      .filter(Boolean)
      .join(' ')}>
      <div className="space-y-0.5">
        <p className="text-[0.7rem] font-medium text-muted-foreground sm:text-xs">{label}</p>
        <p className="text-base font-bold text-foreground sm:text-lg">{value}</p>
      </div>

      <img
        src={badgeIcon}
        alt="Badge icon"
        className="h-10 w-10 select-none object-contain opacity-70 dark:hidden sm:h-12 sm:w-12"
        draggable={false}
      />

      <span
        aria-hidden="true"
        className="hidden h-10 w-10 bg-white/90 dark:block sm:h-12 sm:w-12"
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
    </section>
  )
}

export default Badge