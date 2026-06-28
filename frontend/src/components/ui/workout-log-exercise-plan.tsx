import type { ReactNode } from 'react'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatLoggedExerciseSetText } from '@/lib/exercise-format'
import type { WorkoutLogExercisePlanItem, WorkoutLogExercisePlanProps } from '@/types/workout-log-exercise-plan'

export function WorkoutLogExercisePlan({
  title = 'Exercises Completed',
  subtitle,
  exercises,
  className,
  emptyState = 'No logged sets have been recorded for this workout yet.',
}: WorkoutLogExercisePlanProps) {
  const normalizedExercises = exercises.map((exercise) => ({
    ...exercise,
    sets: exercise.sets.length > 0 ? exercise.sets : [],
  }))

  return (
    <div className={['flex min-h-0 flex-1 flex-col', className].filter(Boolean).join(' ')}>
      <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
        <CardHeader className="pb-2">
          <CardTitle className="text-[1.15rem] font-bold">{title}</CardTitle>
        </CardHeader>
        <CardContent className="flex min-h-0 flex-1 flex-col pr-1">
          {normalizedExercises.length > 0 ? (
            <div className="exercise-summary-scroll min-h-0 flex-1 space-y-4 overflow-y-auto pr-2">
              {normalizedExercises.map((exercise) => (
                <div
                  key={exercise.id}
                  className="grid grid-cols-[72px_minmax(0,1fr)_clamp(280px,18vw,176px)] items-center gap-5 rounded-2xl border border-border bg-[#d9d9d9] px-4 py-4"
                >
                  <Avatar className="h-[72px] w-[72px] shrink-0 border border-[#7f7f7f] bg-white">
                    <AvatarFallback className="bg-white text-transparent" />
                  </Avatar>

                  <div className="min-w-0 pr-4">
                    <p className="truncate text-[1.05rem] font-semibold text-foreground">{exercise.name}</p>
                    <p className="mt-1 truncate text-sm text-muted-foreground">{exercise.primaryMuscle}</p>
                  </div>

                  <div className="w-[284px] justify-self-end rounded-xl bg-[#f5f5f5] px-5 py-4 shadow-[inset_0_0_0_1px_rgba(0,0,0,0.03)]">
                    <div className="grid gap-y-2 text-sm text-foreground">
                      {exercise.sets.map((set) => (
                        <div key={set.id} className="grid grid-cols-[1.75rem_minmax(0,1fr)] items-center gap-4">
                          <span className="text-[0.98rem] font-medium text-foreground/90">{set.orderIndex}</span>
                          <span className="justify-self-end whitespace-nowrap text-[0.9rem] text-foreground/90">
                            {formatLoggedExerciseSetText(exercise.exerciseType, set)}
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

export default WorkoutLogExercisePlan
