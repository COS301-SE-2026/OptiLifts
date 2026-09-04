import type { ReactNode } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export type PasswordRowProps = Readonly<{
  label: string
  value: string
  onChange: (nextValue: string) => void
  showValue: boolean
  onToggle: () => void
  placeholder: string
  autoComplete?: string
  error?: ReactNode
  disclaimer?: ReactNode
}>

export function PasswordRow({
  label,
  value,
  onChange,
  showValue,
  onToggle,
  placeholder,
  autoComplete = 'current-password',
  error,
  disclaimer,
}: PasswordRowProps) {
  const inputId = `password-row-${label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`

  return (
    <div className="grid gap-1">
      <label htmlFor={inputId} className="text-sm font-semibold uppercase tracking-[0.08em] text-foreground">
        {label}
      </label>
      <div className="relative w-full">
        <Input
          id={inputId}
          required
          type={showValue ? 'text' : 'password'}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          autoComplete={autoComplete}
          placeholder={placeholder}
          className="pr-11"
        />
        <Button
          type="button"
          variant="password"
          size="icon"
          aria-label={showValue ? `Hide ${label.toLowerCase()}` : `Show ${label.toLowerCase()}`}
          onClick={onToggle}
          className="absolute right-1 top-1/2 -translate-y-1/2"
        >
          {showValue ? <Eye size={16} /> : <EyeOff size={16} />}
        </Button>
      </div>
      {disclaimer}
      {error}
    </div>
  )
}
