type CheckboxProps = Readonly<{
  id?: string
  checked: boolean
  onChange: (checked: boolean) => void
  label: string
}>

export function Checkbox({ id, checked, onChange, label }: CheckboxProps) {
  return (
    <label htmlFor={id} className="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer shrink-0">
      <input
        id={id}
        type="checkbox"
        className="h-4 w-4 rounded border-border text-brand focus:ring-brand accent-brand cursor-pointer"
        checked={checked}
        onChange={(e) => onChange(e.target.checked)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault()
            onChange(!checked)
          }
        }}
      />
      <span>{label}</span>
    </label>
  )
}