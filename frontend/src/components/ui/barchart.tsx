import type { BarChartProps } from '@/types/barchart'

export function BarChart({ title = 'Hours this week', data = [], className }: BarChartProps) {
  const dataMax = data.length > 0 ? Math.max(...data.map((d) => d.value)) : 0
  const maxValue = Math.max(1, Math.ceil(dataMax))

  const yAxisLabels = [
    maxValue,
    (maxValue * 2) / 3,
    (maxValue * 1) / 3,
    0,
  ].map((val) => (Number.isInteger(val) ? val : Number.parseFloat(val.toFixed(1))))

  return (
    <section className={className}>
      <h2 className="mb-4 type-section-title text-foreground">{title}</h2>

      <div className="rounded-xl border border-border bg-card px-4 py-4 shadow-sm sm:px-5 sm:py-5">
        <div className="flex gap-3">
          <div className="relative h-[116px] text-[0.65rem] text-muted-foreground">
            <div className="invisible h-0">
              {yAxisLabels.map((label) => (
                <div key={`ghost-${label}`} className="whitespace-nowrap">{label} hr</div>
              ))}
            </div>
            {yAxisLabels.map((label, i) => (
              <span 
                key={`label-${label}`}
                className="absolute left-0 -translate-y-1/2 whitespace-nowrap"
                style={{ top: `${(i / 3) * 100}%` }}
              >
                {label} hr
              </span>
            ))}
          </div>

          <div className="min-w-0 flex-1">
            <div className="relative h-[116px] border-b border-border/40">
              {[0, 1, 2, 3].map((line) => (
                <div
                  key={line}
                  className="absolute left-0 right-0 border-t border-dashed border-border/40"
                  style={{ top: `${(line / 3) * 100}%` }}
                />
              ))}

              <div className="relative z-10 grid h-full grid-cols-12 items-end gap-x-0.5 sm:gap-x-1">
                {data.map((bar, idx) => {
                  const height = Math.max((bar.value / maxValue) * 100, bar.value === 0 ? 0 : 4)
                  const minutes = bar.value * 60
                  const displayValue = minutes > 0 && minutes < 1 ? '<1m' : `${Math.round(minutes)}m`

                  return (
                    <div key={`bar-${idx}-${bar.value}`} className="flex h-full flex-col items-center justify-end">
                      <div
                        className="w-full max-w-[18px] rounded-[2px] bg-brand-2 transition-opacity hover:opacity-80 cursor-pointer"
                        style={{ height: `${height}%` }}
                        title={displayValue}
                        aria-hidden="true"
                      />
                    </div>
                  )
                })}
              </div>
            </div>
            <div className="grid grid-cols-12 gap-x-0.5 sm:gap-x-1 mt-2">
              {data.map((bar, idx) => (
                <div key={`label-${idx}-${bar.label}`} className="flex justify-center">
                  <span className="text-[0.7rem] text-muted-foreground whitespace-nowrap">{bar.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

export default BarChart