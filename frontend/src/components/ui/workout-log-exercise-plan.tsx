import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { formatLoggedExerciseSetText } from '@/lib/exercise-format'
import { adaptImgUrl } from '@/lib/utils'
import type {WorkoutLogExercisePlanProps } from '@/types/workout-log-exercise-plan'

function getLoggedSetLabel(type: string, workingNumber: number): string | number {
  if (type === 'Warmup') return 'W'
  if (type === 'DropSet') return 'D'
  return workingNumber
}

export function WorkoutLogExercisePlan({
  title = 'Exercises Completed',
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
        <CardContent className="flex flex-col pr-1">
          {normalizedExercises.length > 0 ? (
            <div className="grid auto-rows-max gap-y-4 pr-2 grid-cols-[8px_68px_8px_minmax(0,1.45fr)_8px_max-content_8px]">
              {normalizedExercises.map((exercise) => (
                  <div
                    key={exercise.id}
                    className="col-span-7 grid grid-cols-subgrid items-center rounded-2xl border border-border bg-surface-2 py-2"
                  >
                  <Avatar className="col-start-2 h-[68px] w-[68px] shrink-0 border border-border bg-background">
                    {exercise.imageUrl ? (
                      <AvatarImage src={adaptImgUrl(exercise.imageUrl)} alt={exercise.name} />
                    ) : null}
                    <AvatarFallback className="bg-background text-transparent" />
                  </Avatar>

                  <div className="col-start-4 min-w-0 pr-2">
                    <p className="truncate text-[0.98rem] font-semibold text-foreground">{exercise.name}</p>
                    <p className="mt-1 truncate text-[0.85rem] text-muted-foreground">{exercise.primaryMuscle}</p>
                  </div>

                  <div className="col-start-6 w-full justify-self-end rounded-xl border border-border bg-card px-2 py-2 shadow-sm">
                    <div className="grid gap-y-2 text-[0.84rem] text-foreground">
                      {exercise.sets.map((set, index) => {
                        const workingNumber =
                          exercise.sets
                            .slice(0, index + 1)
                            .filter((currentSet) => currentSet.type !== 'Warmup' && currentSet.type !== 'DropSet')
                            .length || 1

                        return (
                        <div key={set.id} className="grid grid-cols-[1.75rem_minmax(0,1fr)] items-center gap-4">
                          <span className="text-[0.88rem] font-medium text-foreground">{getLoggedSetLabel(set.type, workingNumber)}</span>
                          <span className="justify-self-end whitespace-nowrap text-[0.8rem] text-foreground">
                            {formatLoggedExerciseSetText(exercise.exerciseType, set)}
                          </span>
                        </div>
                        )
                      })}
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
