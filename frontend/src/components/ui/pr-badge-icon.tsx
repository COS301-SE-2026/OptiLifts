import badgeIcon from '@/assets/badge.png'
import { cn } from '@/lib/utils'

type PrBadgeIconProps = Readonly<{
  iconUrl?: string | null
  alt?: string
  className?: string
  sizeClassName?: string
  lightClassName?: string
  darkClassName?: string
  darkTintClassName?: string
}>

export function PrBadgeIcon({
  iconUrl,
  alt = 'Personal records badge',
  className,
  sizeClassName = 'h-10 w-10',
  lightClassName,
  darkClassName,
  darkTintClassName = 'bg-white/90',
}: PrBadgeIconProps) {
  const resolvedIcon = iconUrl ?? badgeIcon

  return (
    <span className={cn('relative inline-block shrink-0', sizeClassName, className)}>
      <img
        src={resolvedIcon}
        alt={alt}
        className={cn('h-full w-full select-none object-contain dark:hidden', lightClassName)}
        draggable={false}
      />

      <span
        aria-hidden="true"
        className={cn('hidden h-full w-full dark:block', darkTintClassName, darkClassName)}
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
    </span>
  )
}

export default PrBadgeIcon
