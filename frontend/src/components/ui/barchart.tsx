type BarChartDatum = Readonly<{
  label: string
  value: number
}>

type BarChartProps = Readonly<{
  title?: string
  data?: readonly BarChartDatum[]
  className?: string
}>

const formatShortDate = (date: Date) =>
  new Intl.DateTimeFormat('en-US', { month: 'short', day: 'numeric' }).format(date)

const createDefaultData = (): BarChartDatum[] => {
  const todayLabel = formatShortDate(new Date())

  return [
    { label: 'Mar 23', value: 1.1 },
    { label: '', value: 0.95 },
    { label: '', value: 0.45 },
    { label: 'Apr 6', value: 1.05 },
    { label: '', value: 1.1 },
    { label: 'Apr 20', value: 2.6 },
    { label: '', value: 0.95 },
    { label: 'May 4', value: 0.7 },
    { label: '', value: 0.65 },
    { label: 'May 18', value: 1.1 },
    { label: '', value: 2.0 },
    { label: todayLabel, value: 1.45 },
  ]
}

export function BarChart({ title = 'Hours this week', data = createDefaultData(), className }: BarChartProps) {
  const maxValue = 3

  return (
    <section className={className}>
      <h2 className="mb-4 text-2xl font-bold tracking-tight text-foreground">{title}</h2>

      <div className="rounded-xl border border-border bg-card px-4 py-4 shadow-sm sm:px-5 sm:py-5">
        <div className="flex gap-3">
          <div className="flex h-[140px] flex-col justify-between pt-1 text-[0.65rem] text-muted-foreground">
            <span>3 hr</span>
            <span>2 hr</span>
            <span>1 hr</span>
            <span>0 hr</span>
          </div>

          <div className="min-w-0 flex-1">
            <div className="relative h-[140px] border-b border-border/40">
              {[0, 1, 2, 3].map((line) => (
                <div
                  key={line}
                  className="absolute left-0 right-0 border-t border-dashed border-border/40"
                  style={{ top: `${(line / 3) * 100}%` }}
                />
              ))}

              <div className="relative z-10 grid h-full grid-cols-12 items-end gap-x-0.5 gap-y-2 sm:gap-x-1">
                {data.map((bar, index) => {
                  const height = Math.max((bar.value / maxValue) * 100, bar.value === 0 ? 0 : 12)

                  return (
                    <div key={`${bar.label || 'bar'}-${index}`} className="flex h-full flex-col items-center justify-end gap-1.5">
                      <div
                        className="w-full max-w-[18px] rounded-[2px] bg-brand-2"
                        style={{ height: `${height}%` }}
                        aria-hidden="true"
                      />
                      <span className="min-h-4 text-[0.7rem] text-muted-foreground">{bar.label}</span>
                    </div>
                  )
                })}
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

export default BarChart