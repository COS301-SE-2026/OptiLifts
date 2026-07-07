import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'

type FilterDropdownProps = Readonly<{
  value: string
  options: readonly string[]
  onValueChange: (value: string) => void
  className?: string
  ariaLabel: string
}>

export function FilterDropdown({ value, options, onValueChange, className, ariaLabel }: FilterDropdownProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger variant="filter" className={className} aria-label={ariaLabel}>
        <span>{value}</span>
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width]">
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