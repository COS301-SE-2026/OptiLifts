import { MUSCLE_GROUPS } from '@/constants/muscles'
import { cn } from '@/lib/utils'

type MuscleSummaryExercise = Readonly<{
  primaryMuscle: string
  sets: readonly unknown[]
}>

type MuscleSummaryRow = Readonly<{
  muscle: string
  sets: number
}>

type MusclesSummaryProps = Readonly<{
  exercises: readonly MuscleSummaryExercise[]
  className?: string
}>

function formatSetCount(sets: number) {
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: 1,
  }).format(sets)
}

function buildRows(exercises: readonly MuscleSummaryExercise[]): MuscleSummaryRow[] {
  const setsByMuscle = new Map<string, number>(MUSCLE_GROUPS.map((muscle) => [muscle, 0]))

  for (const exercise of exercises) {
    if (!setsByMuscle.has(exercise.primaryMuscle)) {
      continue
    }

    setsByMuscle.set(exercise.primaryMuscle, (setsByMuscle.get(exercise.primaryMuscle) ?? 0) + exercise.sets.length)
  }

  return [...MUSCLE_GROUPS]
    .map((muscle, orderIndex) => ({
      muscle,
      sets: setsByMuscle.get(muscle) ?? 0,
      orderIndex,
    }))
    .sort((left, right) => {
      if (right.sets !== left.sets) {
        return right.sets - left.sets
      }

      return left.orderIndex - right.orderIndex
    })
    .filter(({ sets }) => sets > 0)
    .map(({ muscle, sets }) => ({ muscle, sets }))
}

export default function MusclesSummary({ exercises, className }: MusclesSummaryProps) {
  const rows = buildRows(exercises)
  const maxSets = rows[0]?.sets ?? 0

  return (
    <div className={cn('space-y-4', className)}>
      <div className="flex items-center justify-between text-sm font-semibold text-muted-foreground">
        <span>Muscle</span>
        <span>Sets</span>
      </div>

      <div className="space-y-2">
        {rows.length > 0 ? rows.map(({ muscle, sets }) => {
          let minimumWidth = 0
          if (sets > 0) {
            minimumWidth = 8
          }

          let width = 0
          if (maxSets > 0) {
            width = Math.max((sets / maxSets) * 100, minimumWidth)
          }

          return (
            <div key={muscle} className="grid grid-cols-[minmax(7.5rem,0.85fr)_minmax(0,1.55fr)_3rem] items-center gap-0">
              <span className="whitespace-nowrap text-[0.88rem] text-foreground">{muscle}</span>

              <div className="h-4 rounded-full bg-muted">
                <div
                  className="h-full rounded-full bg-primary"
                  style={{ width: `${width}%` }}
                />
              </div>

              <span className="text-right text-sm text-muted-foreground tabular-nums">{formatSetCount(sets)}</span>
            </div>
          )
        }) : (
          <div className="rounded-2xl border border-dashed border-border bg-muted px-4 py-6 text-sm text-muted-foreground">
            No targeted muscles were recorded for this workout.
          </div>
        )}
      </div>
    </div>
  )
}