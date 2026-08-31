"use client"

import { useMemo, useState } from "react"
import { ChevronLeft, ChevronRight } from "lucide-react"
import type { CalendarProps } from "@/types/calendar"

const WEEKDAY_LABELS = ["Su", "Mo", "Tu", "We", "Th", "Fr", "Sa"] as const

const pad = (value: number) => String(value).padStart(2, "0")

const toDateKey = (date: Date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`

const startOfMonth = (date: Date) => new Date(date.getFullYear(), date.getMonth(), 1)

const monthLabel = (date: Date) =>
  new Intl.DateTimeFormat("en-US", { month: "long", year: "numeric" }).format(date)

export function Calendar(props: Readonly<CalendarProps>) {
  const { className, highlightedDates = [], month, onMonthChange, onHighlightedDateClick } = props
  const [internalMonth, setInternalMonth] = useState(() => startOfMonth(month ?? new Date()))
  const activeMonth = month === undefined ? internalMonth : startOfMonth(month)

  const highlightedSet = useMemo(() => new Set(highlightedDates), [highlightedDates])

  const gridDays = useMemo(() => {
    const firstDay = new Date(activeMonth.getFullYear(), activeMonth.getMonth(), 1)
    const leadingDays = firstDay.getDay()
    const totalCells = 42

    const cells: Array<{
      date: Date
      dayNumber: number
      dateKey: string
      isHighlighted: boolean
      isToday: boolean
      isOtherMonth: boolean
      cellKey: string
    }> = []

    for (let cellIndex = 0; cellIndex < totalCells; cellIndex += 1) {
      const date = new Date(activeMonth.getFullYear(), activeMonth.getMonth(), cellIndex - leadingDays + 1)
      const dateKey = toDateKey(date)
      const isOtherMonth = date.getMonth() !== activeMonth.getMonth() || date.getFullYear() !== activeMonth.getFullYear()

      cells.push({
        date,
        dayNumber: date.getDate(),
        dateKey,
        isHighlighted: highlightedSet.has(dateKey),
        isToday: dateKey === toDateKey(new Date()),
        isOtherMonth,
        cellKey: dateKey,
      })
    }

    return cells
  }, [activeMonth, highlightedSet])

  const moveMonth = (offset: number) => {
    const next = new Date(activeMonth.getFullYear(), activeMonth.getMonth() + offset, 1)
    if (!month) {
      setInternalMonth(next)
    }
    onMonthChange?.(next)
  }

  return (
    <div className={className}>
      <div className="rounded-xl border border-border bg-surface p-2.5 sm:p-4 shadow-sm">
        <div className="mb-2 sm:mb-3 flex items-center justify-between gap-2 sm:gap-3">
          <button
            type="button"
            aria-label="Previous month"
            className="inline-flex size-6 sm:size-8 items-center justify-center rounded-full border border-brand-2 bg-background text-brand-2 transition-colors hover:bg-surface-2"
            onClick={() => moveMonth(-1)}
          >
            <ChevronLeft size={12} className="sm:hidden" />
            <ChevronLeft size={14} className="hidden sm:block" />
          </button>

          <p className="text-xs sm:text-sm font-semibold uppercase tracking-[0.12em] sm:tracking-[0.16em] text-foreground">
            {monthLabel(activeMonth)}
          </p>

          <button
            type="button"
            aria-label="Next month"
            className="inline-flex size-6 sm:size-8 items-center justify-center rounded-full border border-brand-2 bg-background text-brand-2 transition-colors hover:bg-surface-2"
            onClick={() => moveMonth(1)}
          >
            <ChevronRight size={12} className="sm:hidden" />
            <ChevronRight size={14} className="hidden sm:block" />
          </button>
        </div>

        <div className="grid grid-cols-7 gap-y-0.5 text-center text-[0.6rem] sm:text-[0.72rem] font-semibold uppercase tracking-[0.08em] sm:tracking-[0.18em] text-muted-foreground">
          {WEEKDAY_LABELS.map((label) => (
            <div key={label}>{label}</div>
          ))}
        </div>

        <div className="mt-0.5 sm:mt-2 grid grid-cols-7 justify-items-center gap-x-0.5 sm:gap-x-1 gap-y-0.5 sm:gap-y-2 text-center">
          {gridDays.map((day) => {
            let circleClassName = "text-foreground"
            if (day.isHighlighted) {
              circleClassName = "bg-brand-2 text-background"
            } else if (day.isToday) {
              circleClassName = "border border-brand-2 text-foreground"
            } else if (day.isOtherMonth) {
              circleClassName = "text-muted-foreground/45"
            }

            let hoverClassName = ""
            if (!day.isHighlighted && !day.isToday && !day.isOtherMonth) {
              hoverClassName = "hover:bg-surface-2"
            }

            return (
              <div key={day.cellKey} className="flex justify-center w-full">
                <button
                  type="button"
                  className={[
                    "flex size-7 sm:size-9 items-center justify-center rounded-full text-xs sm:text-sm font-semibold transition-colors",
                    circleClassName,
                    day.isHighlighted && onHighlightedDateClick ? "cursor-pointer hover:brightness-95" : "",
                    hoverClassName,
                  ]
                    .filter(Boolean)
                    .join(" ")}
                  aria-label={`${day.date.toDateString()}${day.isHighlighted ? ' workout completed' : ''}`}
                  disabled={day.isOtherMonth}
                  onClick={() => {
                    if (day.isHighlighted) {
                      onHighlightedDateClick?.(day.dateKey)
                    }
                  }}
                >
                  {day.dayNumber}
                </button>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

export default Calendar