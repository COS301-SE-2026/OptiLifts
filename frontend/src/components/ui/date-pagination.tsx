import { ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '@/lib/utils'

type DatePaginationProps = Readonly<{
    currentDate: Date
    onChange: (newDate: Date) => void
    type?: 'week' | 'month'
    className?: string
}>

const getWeekStart = (date: Date) => {
    const now = new Date(date);
    const day = now.getDay();
    let diff = now.getDate()-day;
    if (day === 0) {
        diff -= 6; 
    } else {
        diff += 1; 
    }
    return new Date(now.setDate(diff));
}

const weekFormat = (date: Date) => {
    const start = getWeekStart(date)
    const end = new Date(start); 
    end.setDate(start.getDate() + 6); 

    //we have to unfortunately use the US here
    const formatter = new Intl.DateTimeFormat('en-US', { day: 'numeric', month: 'long' })
    return `${formatter.format(start)} - ${formatter.format(end)}`
}

const monthFormat = (date: Date) => {
    return new Intl.DateTimeFormat('en-US', { month: 'long', year: 'numeric' }).format(date)
}
export function DatePagination({ currentDate, onChange, type = 'week', className }: DatePaginationProps) {
    const getPrev = () => {
        const prev = new Date(currentDate)
        if (type === 'week') {
            prev.setDate(prev.getDate() - 7)
            onChange(getWeekStart(prev))
        } else {
            prev.setMonth(prev.getMonth() - 1)
            prev.setDate(1)
            prev.setHours(0, 0, 0, 0)
            onChange(prev)
        }

        onChange(prev)
    }

    const getNext = () => {
        const next = new Date(currentDate)
        if (type === 'week') {
            next.setDate(next.getDate() + 7)
            onChange(getWeekStart(next))
        } else {
            next.setMonth(next.getMonth() + 1)
            next.setDate(1)
            next.setHours(0, 0, 0, 0)
            onChange(next)
        }

        onChange(next)
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



