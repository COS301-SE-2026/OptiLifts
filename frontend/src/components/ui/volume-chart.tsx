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
  unit = 'KG',
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

  const chartData: ChartData<'line'> = {
    labels: chartPoints.map((p) => p.label),
    datasets: [
      {
        data: chartPoints.map((p) => p.value),
        borderColor: 'black',
        borderWidth: 1.5,
        pointRadius: 0,
        pointHoverRadius: 5,
        pointBackgroundColor: 'black',
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
        titleColor: '#1A1A1A',
        bodyColor: '#1A1A1A',
        borderColor: '#E5E7EB',
        borderWidth: 1,
        padding: 8,
      },
    },
    scales: {
      y: {
        beginAtZero: false,
        grid: {
          display: false,
        },
        border: {
          display: true,
          color: '#CC0022',
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
          color: '#CC0022',
        },
        ticks: {
          color: '#71717A',
          font: { size: 11, family: 'sans-serif' },
        },
      },
    },
  }

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