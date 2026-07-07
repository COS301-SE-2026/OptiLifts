import { useMemo, useState } from 'react'
import { cn } from '@/lib/utils'
import { FilterDropdown } from '@/components/ui/filter-dropdown'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Tooltip,
  type ChartOptions,
  type ChartData,
} from 'chart.js'
import { Line } from 'react-chartjs-2'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip)

type VolumeChartPeriod = 'Week' | 'Month' | 'Year'

export type { VolumeChartPeriod }

  return (
    <section className={cn('flex flex-col rounded-xl bg-card p-5 text-card-foreground ring-1 ring-foreground/10 shadow-sm', className)}>
      <div className="flex justify-between w-full mb-8">
        <div className="text-xs font-medium text-muted-foreground pt-2">{unit}</div>   
        <div className="flex-1 text-center pr-12">
          <h2 className="text-3xl font-black uppercase tracking-wider text-foreground">{title}</h2>
        </div>
        {showFilters && (
          <div className="flex flex-col gap-2">
            <FilterDropdown
              value={resolvedPeriod}
              options={PERIOD_OPTIONS}
              onValueChange={(nextValue) => {
                const nextPeriod = nextValue as VolumeChartPeriod
                if (onPeriodChange) {
                  onPeriodChange(nextPeriod)
                  return
                }
                setInternalPeriod(nextPeriod)
              }}
              ariaLabel="Select time period"
              className="bg-surface-2 border border-border rounded-md px-3 py-1.5 text-sm font-medium shadow-sm outline-none focus:ring-1 focus:ring-brand"/>
            {muscleOptions && muscleOptions.length > 0 && (
              <FilterDropdown
                value={resolvedMuscleFilter}
                options={[...muscleOptions]}
                onValueChange={(nextValue) => {
                  if (onMuscleFilterChange) {
                    onMuscleFilterChange(nextValue)
                    return
                  }
                  setInternalMuscleFilter(nextValue)
                }}
                ariaLabel="Select muscle filter"
                className="bg-surface-2 border border-border rounded-md px-3 py-1.5 text-sm font-medium shadow-sm outline-none focus:ring-1 focus:ring-brand"/>
            )}
          </div>
        )}
      </div>

      <div className="relative w-full flex-1 min-h-[220px]">
        <Line data={chartData} options={options} />
      </div>
    </section>
  )
}

export default VolumeChart