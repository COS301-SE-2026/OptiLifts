import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatPlannedExerciseSetText } from '@/lib/exercise-format'
import type { ExercisePlanItem, ExercisePlanProps, ExercisePlanSet } from '@/types/exercise-plan'

const DEFAULT_SETS: ExercisePlanSet[] = [
  { label: 'W', reps: 10, weight: 20, duration: null, distance: null, restTime: '2:30 min rest' },
  { label: '1', reps: 10, weight: 40, duration: null, distance: null, restTime: '2:30 min rest' },
  { label: '2', reps: 10, weight: 40, duration: null, distance: null, restTime: '2:30 min rest' },
]

function normalizeExercise(exercise: string | ExercisePlanItem): Required<ExercisePlanItem> {
  if (typeof exercise === 'string') {
    return {
      name: exercise,
      subtitle: 'Planned exercise',
      exerciseType: 'weight-reps',
      sets: DEFAULT_SETS,
    }
  }

  return {
    name: exercise.name,
    subtitle: exercise.subtitle ?? 'Planned exercise',
    exerciseType: exercise.exerciseType ?? 'weight-reps',
    sets: exercise.sets && exercise.sets.length > 0 ? exercise.sets : DEFAULT_SETS,
  }
}

function getExerciseRestTime(exercise: Required<ExercisePlanItem>) {
  return exercise.sets[0]?.restTime ?? ''
}

function formatRestTimeLabel(restTime: string) {
  return restTime.replace(/\s*rest$/i, '')
}

export function ExercisePlan({
  title = 'Exercise Plan',
  subtitle,
  exercises,
  className,
  emptyState = 'No exercises have been planned for this workout yet.',
}: ExercisePlanProps) {
  const normalizedExercises = exercises.map((exercise) => {
    const normalized = normalizeExercise(exercise)
    return {
      ...normalized,
      subtitle: normalized.subtitle === 'Planned exercise' && subtitle ? subtitle : normalized.subtitle,
    }
  })

  return (
    <div className={['flex min-h-0 flex-1 flex-col', className].filter(Boolean).join(' ')}>
      <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
        <CardHeader className="pb-2">
          <CardTitle className="text-[1.15rem] font-bold">{title}</CardTitle>
        </CardHeader>
        <CardContent className="flex min-h-0 flex-1 flex-col pr-1">
          {normalizedExercises.length > 0 ? (
            <div className="exercise-summary-scroll min-h-0 flex-1 space-y-4 overflow-y-auto pr-2">
              {normalizedExercises.map((exercise, index) => (
                <div
                  key={`${exercise.name}-${index}`}
                  className="grid grid-cols-[72px_minmax(0,1fr)_minmax(280px,28vw)] items-center gap-5 rounded-2xl border border-border bg-[#d9d9d9] px-4 py-4"
                >
                  <Avatar className="h-[72px] w-[72px] shrink-0 border border-[#7f7f7f] bg-white">
                    <AvatarFallback className="bg-white text-transparent" />
                  </Avatar>

                  <div className="min-w-0 pr-4">
                    <p className="truncate text-[1.05rem] font-semibold text-foreground">{exercise.name}</p>
                    <p className="mt-1 truncate text-sm text-muted-foreground">{exercise.subtitle}</p>
                  </div>

                  <div className="w-full justify-self-end rounded-xl bg-[#f5f5f5] px-5 py-4 shadow-[inset_0_0_0_1px_rgba(0,0,0,0.03)]">
                    <div className="grid min-w-[124px] gap-y-2 text-sm text-foreground">
                      <p className="text-xs font-semibold uppercase tracking-[0.15em] text-muted-foreground">
                        Rest time: {formatRestTimeLabel(getExerciseRestTime(exercise))}
                      </p>
                      {exercise.sets.map((set) => (
                        <div key={`${exercise.name}-${set.label}`} className="grid grid-cols-[1.75rem_minmax(0,1fr)] items-center gap-4">
                          <span className="text-[0.98rem] font-medium text-foreground/90">{set.label}</span>
                          <span className="justify-self-end whitespace-nowrap text-[0.9rem] text-foreground/90">
                            {formatPlannedExerciseSetText(exercise.exerciseType, set, { includeRestTime: false })}
                          </span>
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="rounded-2xl border border-dashed border-border bg-surface-2 px-4 py-8 text-sm text-muted-foreground">
              {emptyState}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export default ExercisePlan