import { useMemo, useState } from 'react'
import { cn } from '@/lib/utils'
import { FilterDropdown } from '@/components/ui/filter-dropdown'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Filler,
  Tooltip,
  type ChartOptions,
  type ChartData,
} from 'chart.js'
import { Line } from 'react-chartjs-2'
import { metricCheck, outputWeight } from '@/lib/weight-utils'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Filler, Tooltip)

// Chart.js renders on canvas, so CSS variables need to be read at runtime.
function getcssVariables(name: string, fallback: string): string {
  if (typeof globalThis === 'undefined' || !('window' in globalThis)) return fallback
  const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
  return value || fallback
}

type VolumeChartPeriod = 'Week' | 'Month' | 'Year'

export type { VolumeChartPeriod }

type ChartPoint = Readonly<{
  label: string
  value: number
}>

type VolumeChartProps = Readonly<{
  title?: string
  unit?: string
  data?: readonly ChartPoint[]
  initialPeriod?: VolumeChartPeriod
  period?: VolumeChartPeriod
  onPeriodChange?: (period: VolumeChartPeriod) => void
  initialMuscleFilter?: string
  muscleFilter?: string
  muscleOptions?: readonly string[]
  onMuscleFilterChange?: (muscleFilter: string) => void
  showFilters?: boolean
  className?: string
}>

const PERIOD_OPTIONS: VolumeChartPeriod[] = ['Week', 'Month', 'Year']

export function VolumeChart({
  title = 'Volume',
  unit = (metricCheck())? 'KG' : 'LB',
  data,
  initialPeriod = 'Week',
  period,
  onPeriodChange,
  initialMuscleFilter = 'All',
  muscleFilter,
  muscleOptions,
  onMuscleFilterChange,
  showFilters = true,
  className,
}: VolumeChartProps) {
  const [internalPeriod, setInternalPeriod] = useState<VolumeChartPeriod>(initialPeriod)
  const [internalMuscleFilter, setInternalMuscleFilter] = useState(initialMuscleFilter)
  const resolvedPeriod = period ?? internalPeriod
  const resolvedMuscleFilter = muscleFilter ?? internalMuscleFilter
  const chartPoints = useMemo(() => (data ? [...data] : []), [data])
  const brandColor = getcssVariables('--brand', '#CC0022')
  const brandFill = getcssVariables('--brand-fill', '#CC002226')

  const chartData: ChartData<'line'> = {
    labels: chartPoints.map((p) => p.label),
    datasets: [
      {
        data: chartPoints.map((p) => outputWeight(Math.max(0, p.value))),
        borderColor: brandColor,
        fill: true,
        backgroundColor: brandFill,
        borderWidth: 1.5,
        pointRadius: 0,
        pointHoverRadius: 5,
        pointBackgroundColor: brandColor,
        tension: 0,
      },
    ],
  }

  const options: ChartOptions<'line'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false },
      tooltip: {
        enabled: true,
        displayColors: false,
        backgroundColor: 'white',
        titleColor: '#CC0022',
        bodyColor: '#CC0022',
        borderColor: '#E5E7EB',
        borderWidth: 1,
        padding: 8,
      },
    },
    scales: {
      y: {
        beginAtZero: true,
        min: 0,
        grid: {
          display: false,
        },
        border: {
          display: true,
          color: 'black',
        },
        ticks: {
          color: '#71717A',
          font: { size: 11, family: 'sans-serif' },
          maxTicksLimit: 6,
        },
      },
      x: {
        grid: {
          display: false,
        },
        border: {
          display: true,
          color: 'black',
        },
        ticks: {
          color: '#71717A',
          font: { size: 11, family: 'sans-serif' },
        },
      },
    },
  }

  return (
    <section className={cn('flex flex-col rounded-xl bg-card p-4 sm:p-5 text-card-foreground ring-1 ring-foreground/10 shadow-sm', className)}>
      <div className="flex flex-wrap sm:flex-nowrap justify-between items-end sm:items-start w-full mb-2 gap-y-3 gap-x-2">
        <div className="order-2 sm:order-1 text-xs font-medium text-muted-foreground self-center sm:self-auto sm:pt-10 shrink-0">{unit}</div>   
        <div className={cn("order-1 sm:order-2 w-full sm:w-auto sm:flex-1 text-center min-w-0", showFilters && "sm:pr-12")}>
          <h2 className="text-xl sm:text-3xl font-black uppercase tracking-wider text-foreground">{title}</h2>
        </div>
        {showFilters && (
          <div className="order-3 flex flex-row sm:flex-col gap-2 shrink-0">
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
              align="end"
              className="w-28 min-[380px]:w-32 sm:w-36 h-9 bg-surface-2 border border-border rounded-md px-2.5 sm:px-3 py-0 text-xs sm:text-sm font-medium shadow-sm outline-none focus:ring-1 focus:ring-brand"/>
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
                align="end"
                className="w-28 min-[380px]:w-32 sm:w-36 h-9 bg-surface-2 border border-border rounded-md px-2.5 sm:px-3 py-0 text-xs sm:text-sm font-medium shadow-sm outline-none focus:ring-1 focus:ring-brand"/>
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