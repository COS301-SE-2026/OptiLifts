export type CalendarProps = Readonly<{
  className?: string
  highlightedDates?: readonly string[]
  month?: Date
  onMonthChange?: (month: Date) => void
  onHighlightedDateClick?: (dateKey: string) => void
}>
