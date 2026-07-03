import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatPlannedExerciseSetText } from '@/lib/exercise-format'
import type { ExercisePlanItem, ExercisePlanProps, ExercisePlanSet } from '@/types/exercise-plan'

const DEFAULT_EXERCISE_TYPE = 'weight-reps'
const PLANNED_EXERCISE_SUBTITLE = 'Planned exercise'
const DEFAULT_EMPTY_STATE = 'No exercises have been planned for this workout yet.'
const EXERCISE_ROW_CLASS = 'grid grid-cols-[68px_minmax(0,1.25fr)_minmax(160px,12vw)] items-center gap-2 rounded-2xl border border-border bg-surface-2 px-2 py-2'
const SETS_PANEL_CLASS = 'w-full justify-self-end rounded-xl border border-border bg-card px-2 py-2 shadow-sm'
const SET_ROW_CLASS = 'grid grid-cols-[1.75rem_minmax(0,1fr)] items-center gap-4'

const DEFAULT_SETS: ExercisePlanSet[] = [
  { label: 'W', reps: 10, weight: 20, duration: null, distance: null, restTime: '2:30 min rest' },
  { label: '1', reps: 10, weight: 40, duration: null, distance: null, restTime: '2:30 min rest' },
  { label: '2', reps: 10, weight: 40, duration: null, distance: null, restTime: '2:30 min rest' },
]

function normalizeExercise(exercise: string | ExercisePlanItem): Required<ExercisePlanItem> {
  if (typeof exercise === 'string') {
    return {
      name: exercise,
      subtitle: PLANNED_EXERCISE_SUBTITLE,
      exerciseType: DEFAULT_EXERCISE_TYPE,
      sets: DEFAULT_SETS,
    }
  }

  return {
    name: exercise.name,
    subtitle: exercise.subtitle ?? PLANNED_EXERCISE_SUBTITLE,
    exerciseType: exercise.exerciseType ?? DEFAULT_EXERCISE_TYPE,
    sets: exercise.sets && exercise.sets.length > 0 ? exercise.sets : DEFAULT_SETS,
  }
}

function getExerciseRestTime(exercise: Required<ExercisePlanItem>) {
  return exercise.sets[0]?.restTime ?? ''
}

function formatRestTimeLabel(restTime: string) {
  const trimmed = restTime.trimEnd()
  return trimmed.toLowerCase().endsWith('rest') ? trimmed.slice(0, -4).trimEnd() : restTime
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
                  className={EXERCISE_ROW_CLASS}
                >
                  <Avatar className="h-[68px] w-[68px] shrink-0 border border-border bg-background">
                    <AvatarFallback className="bg-background text-transparent" />
                  </Avatar>

                  <div className="min-w-0 pr-2">
                    <p className="truncate text-[0.98rem] font-semibold text-foreground">{exercise.name}</p>
                    <p className="mt-1 truncate text-[0.85rem] text-muted-foreground">{exercise.subtitle}</p>
                  </div>

                  <div className={SETS_PANEL_CLASS}>
                    <div className="grid min-w-[96px] gap-y-2 text-[0.84rem] text-foreground">
                      <p className="text-[0.64rem] font-semibold uppercase tracking-[0.15em] text-muted-foreground">
                        Rest time: {formatRestTimeLabel(getExerciseRestTime(exercise))}
                      </p>
                      {exercise.sets.map((set) => (
                        <div key={`${exercise.name}-${set.label}`} className={SET_ROW_CLASS}>
                          <span className="text-[0.88rem] font-medium text-foreground">{set.label}</span>
                          <span className="justify-self-end whitespace-nowrap text-[0.8rem] text-foreground">
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
              {emptyState ?? DEFAULT_EMPTY_STATE}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

export default ExercisePlan