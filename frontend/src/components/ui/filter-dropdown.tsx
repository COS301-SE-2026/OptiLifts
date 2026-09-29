import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'

type FilterDropdownProps = Readonly<{
  value: string
  options: readonly string[]
  onValueChange: (value: string) => void
  className?: string
  ariaLabel: string
  align?: 'start' | 'center' | 'end'
}>

export function FilterDropdown({ value, options, onValueChange, className, ariaLabel, align = 'end' }: FilterDropdownProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger variant="filter" className={className} aria-label={ariaLabel}>
        <span className="min-w-0 truncate">{value}</span>
      </DropdownMenuTrigger>
      <DropdownMenuContent align={align} className="w-[var(--radix-dropdown-menu-trigger-width)]">
        {options.map((option) => (
          <DropdownMenuItem key={option} onSelect={() => onValueChange(option)}>
            {option}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

export default FilterDropdown