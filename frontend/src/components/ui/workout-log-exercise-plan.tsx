import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatLoggedExerciseSetText, buildLabels } from '@/lib/exercise-format'
import { adaptImgUrl } from '@/lib/utils'
import type {WorkoutLogExercisePlanProps } from '@/types/workout-log-exercise-plan'

export function WorkoutLogExercisePlan({
  title = 'Exercises Completed',
  exercises,
  className,
  emptyState = 'No logged sets have been recorded for this workout yet.',
}: WorkoutLogExercisePlanProps) {
  const normalizedExercises = exercises.map((exercise) => ({
    ...exercise,
    sets: exercise.sets.length > 0 ? exercise.sets : [],
    setLabels: buildLabels(exercise.sets),
  }))

  return (
    <div className={['flex min-h-0 flex-1 flex-col', className].filter(Boolean).join(' ')}>
      <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
        <CardHeader className="pb-2 px-3 sm:px-5">
          <CardTitle className="text-[1.15rem] font-bold">{title}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col px-3 sm:px-5">
          {normalizedExercises.length > 0 ? (
            <div className="grid grid-cols-[1fr_max-content] gap-3 sm:gap-4">
              {normalizedExercises.map((exercise) => (
                <div
                  key={exercise.id}
                  className="col-span-2 grid grid-cols-subgrid items-start rounded-xl sm:rounded-2xl border border-border bg-surface-2 p-2.5 sm:p-3.5"
                >
                  <div className="flex flex-col items-start min-w-0 pr-2 sm:pr-3">
                    <Avatar className="h-12 w-12 sm:h-14 sm:w-14 shrink-0 border border-border bg-background mb-1.5">
                      {exercise.imageUrl ? (
                        <AvatarImage src={adaptImgUrl(exercise.imageUrl)} alt={exercise.name} />
                      ) : null}
                      <AvatarFallback className="bg-background text-transparent" />
                    </Avatar>

                    <p className="w-full truncate text-[0.88rem] sm:text-[0.95rem] font-semibold text-foreground">{exercise.name}</p>
                    <p className="mt-0.5 w-full truncate text-[0.72rem] sm:text-[0.8rem] text-muted-foreground">{exercise.primaryMuscle}</p>
                  </div>

                  <div className="w-full justify-self-end rounded-lg sm:rounded-xl border border-border bg-card px-2 py-1.5 sm:px-2.5 sm:py-2 shadow-xs">
                    <div className="grid min-w-[72px] sm:min-w-[88px] gap-y-1 sm:gap-y-1.5 text-[0.76rem] sm:text-[0.84rem] text-foreground">
                      {exercise.sets.map((set, index) => (
                        <div key={set.id} className="grid grid-cols-[1rem_minmax(0,1fr)] sm:grid-cols-[1.35rem_minmax(0,1fr)] items-center gap-1.5 sm:gap-2.5">
                          <span className="text-[0.72rem] sm:text-[0.84rem] font-medium text-foreground">{exercise.setLabels[index]}</span>
                          <span className="justify-self-end whitespace-nowrap text-[0.7rem] sm:text-[0.78rem] text-foreground">
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
