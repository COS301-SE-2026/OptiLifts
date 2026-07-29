import { ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '@/lib/utils'

type DatePaginationProps = Readonly<{
    currentDate: Date
    onChange: (newDate: Date) => void
    type?: 'week' | 'month'
    className?: string
}>

export const getWeekStart = (date: Date) => {
    const d = new Date(date)
    const year = d.getUTCFullYear()
    const month = d.getUTCMonth()
    const dayOfMonth = d.getUTCDate()
    const dayOfWeek = d.getUTCDay()

    const diffToMonday = dayOfWeek === 0 ? -6 : 1 - dayOfWeek
    return new Date(Date.UTC(year, month, dayOfMonth + diffToMonday))
}

const weekFormat = (date: Date) => {
    const start = getWeekStart(date)
    const end = new Date(start)
    end.setUTCDate(start.getUTCDate() + 6)

    const formatter = new Intl.DateTimeFormat('en-US', { day: 'numeric', month: 'long', timeZone: 'UTC' })
    return `${formatter.format(start)} - ${formatter.format(end)}`
}

const monthFormat = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(date)
}

export function DatePagination({ currentDate, onChange, type = 'week', className }: DatePaginationProps) {
    const getPrev = () => {
        const prev = new Date(currentDate)
        if (type === 'week') {
            prev.setUTCDate(prev.getUTCDate() - 7)
            onChange(getWeekStart(prev))
        } else {
            prev.setUTCMonth(prev.getUTCMonth() - 1)
            prev.setUTCDate(1)
            prev.setUTCHours(0, 0, 0, 0)
            onChange(prev)
        }
    }

    const getNext = () => {
        const next = new Date(currentDate)
        if (type === 'week') {
            next.setUTCDate(next.getUTCDate() + 7)
            onChange(getWeekStart(next))
        } else {
            next.setUTCMonth(next.getUTCMonth() + 1)
            next.setUTCDate(1)
            next.setUTCHours(0, 0, 0, 0)
            onChange(next)
        }
    }

    const displayText = (type === 'week') ? weekFormat(currentDate) : monthFormat(currentDate)
    return (
        <div className={cn("flex items-center gap-3 text-base font-bold", className)}>
            <button
                onClick={getPrev}
                className="text-foreground transition-colors hover:text-brand active:scale-95"
            >
                <ChevronLeft size={22} strokeWidth={2.5} />
            </button>

            <span className="tracking-wide text-foreground select-none min-w-[160px] text-center">
                {displayText}
            </span>

            <button
                onClick={getNext}
                className="text-foreground transition-colors hover:text-brand active:scale-95"
            >
                <ChevronRight size={22} strokeWidth={2.5} />
            </button>
        </div>
    )
}
