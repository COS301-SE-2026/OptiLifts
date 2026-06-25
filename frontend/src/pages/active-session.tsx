import { useEffect, useMemo, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle, CardAction } from '@/components/ui/card'
import { NumericalUnderscoreInput } from '@/components/ui/input'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { customFetch } from '@/lib/custom-fetch'
import { Check, X, Plus, ChevronDown, MoreHorizontal } from 'lucide-react'

type WorkoutLocationState = Readonly<{
  workout?: Readonly<{
    id?: string
    name: string
    primaryMuscleGroups: string[]
  }>
}>

type SetData = {
  id: string
  type: 'W' | '1' | '2' | '3'
  previous: string
  kg: number | string
  reps: number | string
  rpe: number | string
  completed: boolean
}

type ExerciseData = Readonly<{
  id: string
  name: string
  muscleGroup: string
  sets: SetData[]
  recommendation?: string
}>

type WorkoutDetailsResponse = Readonly<{
  id: string
  name: string
  primaryMuscleGroups: string[]
  exercises: Array<{
    workoutExerciseId: string
    exerciseId: string
    name: string
    muscleGroup: string
    orderIndex: number
    sets: Array<{
      id: string
      type: SetData['type']
      reps: number | null
      weight: number | null
      duration: number | null
      distance: number | null
      orderIndex: number
      restTime: number
    }>
  }>
}>

const SET_TYPE_OPTIONS: ReadonlyArray<SetData['type']> = ['W', '1', '2', '3']

const setTypeLabelMap: Record<SetData['type'], string> = {
  W: 'W',
  1: '1',
  2: '2',
  3: '3'
}

const buildPreviousText = (kg: number | null, reps: number | null) => {
  if (kg == null && reps == null) {
    return '-'
  }

  const kgPart = kg == null ? '-' : `${kg}KG`
  const repsPart = reps == null ? '-' : `${reps}`
  return `${kgPart} x ${repsPart}`
}

const toNumericValue = (value: number | string) => {
  if (typeof value === 'number') {
    return Number.isFinite(value) ? value : 0
  }

  if (!value) {
    return 0
  }

  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : 0
}

const createClientSetId = () => {
  if (globalThis.crypto?.randomUUID) {
    return globalThis.crypto.randomUUID()
  }

  return `set-${Date.now()}-${Math.floor(Math.random() * 10000)}`
}

const createClientExerciseId = () => {
  if (globalThis.crypto?.randomUUID) {
    return globalThis.crypto.randomUUID()
  }

  return `exercise-${Date.now()}-${Math.floor(Math.random() * 10000)}`
}

export default function ActiveSessionPage() {
  const location = useLocation()
  const sessionState = location.state as WorkoutLocationState | null
  const workoutId = sessionState?.workout?.id

  const [workoutName, setWorkoutName] = useState(sessionState?.workout?.name ?? 'WORKOUT')
  const [isLoading, setIsLoading] = useState(() => Boolean(workoutId))
  const [error, setError] = useState<string | null>(() =>
    workoutId ? null : 'No workout was selected. Start a workout from the workouts page.'
  )
  const [startedAtMs, setStartedAtMs] = useState<number | null>(null)
  const [nowMs, setNowMs] = useState<number>(0)
  const [exercises, setExercises] = useState<ExerciseData[]>([])

  useEffect(() => {
    if (!workoutId) {
      return
    }

    let isMounted = true

    const loadWorkout = async () => {
      setIsLoading(true)
      setError(null)

      try {
        const response = await customFetch(`/api/workouts/${workoutId}`, {
          headers: { Accept: 'application/json' },
        })

        if (!response.ok) {
          throw new Error(`Failed to load workout details (${response.status})`)
        }

        const data = (await response.json()) as WorkoutDetailsResponse
        if (!isMounted) {
          return
        }

        setWorkoutName(data.name)
        setStartedAtMs(Date.now())

        const mappedExercises: ExerciseData[] = data.exercises
          .sort((a, b) => a.orderIndex - b.orderIndex)
          .map((exercise) => ({
            id: exercise.workoutExerciseId,
            name: exercise.name,
            muscleGroup: exercise.muscleGroup,
            sets: exercise.sets
              .sort((a, b) => a.orderIndex - b.orderIndex)
              .map((set) => ({
                id: set.id,
                type: set.type,
                previous: buildPreviousText(set.weight, set.reps),
                kg: set.weight ?? '',
                reps: set.reps ?? '',
                rpe: 'RPE',
                completed: false,
              })),
          }))

        setExercises(mappedExercises)
      } catch (loadError) {
        if (!isMounted) {
          return
        }

        setError(loadError instanceof Error ? loadError.message : 'Failed to load workout details.')
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    void loadWorkout()

    return () => {
      isMounted = false
    }
  }, [workoutId])

  useEffect(() => {
    const interval = setInterval(() => setNowMs(Date.now()), 1000)
    return () => clearInterval(interval)
  }, [])

  const secondsElapsed = startedAtMs == null ? 0 : Math.max(0, Math.floor((nowMs - startedAtMs) / 1000))

  const formatTime = (totalSeconds: number) => {
    const h = Math.floor(totalSeconds / 3600)
    const m = Math.floor((totalSeconds % 3600) / 60)
    if (h > 0) return `${h}h ${m}min`
    return `${m}m`
  }

  const updateSet = (
    exerciseId: string,
    setId: string,
    updater: (current: SetData) => SetData
  ) => {
    setExercises((currentExercises) =>
      currentExercises.map((exercise) => {
        if (exercise.id !== exerciseId) {
          return exercise
        }

        return {
          ...exercise,
          sets: exercise.sets.map((set) => (set.id === setId ? updater(set) : set)),
        }
      })
    )
  }

  const addSet = (exerciseId: string) => {
    setExercises((currentExercises) =>
      currentExercises.map((exercise) => {
        if (exercise.id !== exerciseId) {
          return exercise
        }

        return {
          ...exercise,
          sets: [
            ...exercise.sets,
            {
              id: createClientSetId(),
              type: 'W',
              previous: '-',
              kg: '',
              reps: '',
              rpe: 'RPE',
              completed: false,
            },
          ],
        }
      })
    )
  }

  const addExercise = () => {
    setExercises((currentExercises) => [
      ...currentExercises,
      {
        id: createClientExerciseId(),
        name: 'New Exercise',
        muscleGroup: 'Unknown',
        sets: [
          {
            id: createClientSetId(),
            type: 'W',
            previous: '-',
            kg: '',
            reps: '',
            rpe: 'RPE',
            completed: false,
          },
        ],
      },
    ])
  }

  const summary = useMemo(() => {
    const allSets = exercises.flatMap((exercise) => exercise.sets)
    const completedSets = allSets.filter((set) => set.completed)
    const totalVolume = completedSets.reduce((total, set) => {
      return total + toNumericValue(set.kg) * toNumericValue(set.reps)
    }, 0)

    return {
      completedSets: completedSets.length,
      totalSets: allSets.length,
      totalVolume,
    }
  }, [exercises])

  return (
    <section className="w-full px-6 py-6 font-sans text-foreground">
      <div className="max-w-3xl w-full">
        
        <div className="mb-6 flex items-center justify-between w-full">
          <div className="flex items-center gap-3">
            <div className="h-8 w-1.5 rounded-full bg-brand" />
            <h1 className="text-3xl font-bold uppercase tracking-tight">{workoutName}</h1>
          </div>
          <div className="flex items-center gap-8 text-center">
            <div>
              <p className="text-xs font-semibold text-muted-foreground">Duration</p>
              <p className="text-sm font-bold">{formatTime(secondsElapsed)}</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground">Volume</p>
              <p className="text-sm font-bold">{summary.totalVolume.toLocaleString()} kg</p>
            </div>
            <div>
              <p className="text-xs font-semibold text-muted-foreground">Sets</p>
              <p className="text-sm font-bold">{summary.completedSets}/{summary.totalSets}</p>
            </div>
          </div>
        </div>

        {isLoading && (
          <div className="rounded-md border border-border bg-surface-2 px-4 py-3 text-sm text-muted-foreground">
            Loading workout session...
          </div>
        )}
        {error && (
          <div className="rounded-md border border-border bg-surface-2 px-4 py-3 text-sm text-red-500">
            {error}
          </div>
        )}

        <div className="flex flex-col gap-6 w-full">
          {exercises.map((exercise) => (
            <Card key={exercise.id} className="border-border bg-card shadow-sm rounded-xl overflow-hidden pt-4 pb-2">
              <CardHeader className="flex flex-row items-start justify-between pb-4 px-5 pt-0">
                <div className="flex items-center gap-4">
                  <div className="h-10 w-10 rounded-full bg-surface-2 border border-border" />
                  <div>
                    <CardTitle className="text-base font-bold">{exercise.name}</CardTitle>
                    <p className="text-sm text-muted-foreground">{exercise.muscleGroup}</p>
                  </div>
                </div>
                <CardAction>
                  <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground">
                    <MoreHorizontal className="h-5 w-5" />
                  </Button>
                </CardAction>
              </CardHeader>

              <CardContent className="px-5 pb-4">
                <div className="mb-2 grid grid-cols-[4rem_1.5fr_1fr_1fr_1fr_5rem] gap-4 px-2 text-center text-xs font-semibold tracking-wide text-muted-foreground">
                  <div>SET</div>
                  <div>PREVIOUS</div>
                  <div>KG</div>
                  <div>REPS</div>
                  <div>RPE</div>
                  <div className="w-full flex justify-center"><Check className="h-4 w-4" /></div>
                </div>

                <div className="space-y-2">
                  {exercise.sets.map((set) => (
                    <div key={set.id} className="grid grid-cols-[4rem_1.5fr_1fr_1fr_0.8fr_5rem] items-center gap-4 rounded-lg bg-surface-2 p-1.5 text-center text-sm font-medium">
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="outline" size="sm" className="h-8 w-full justify-between px-2 text-xs bg-surface-2">
                            {setTypeLabelMap[set.type]} <ChevronDown className="ml-1 h-3 w-3 opacity-50" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent>
                          {SET_TYPE_OPTIONS.map((option) => (
                            <DropdownMenuItem
                              key={option}
                              onSelect={() => updateSet(exercise.id, set.id, (currentSet) => ({ ...currentSet, type: option }))}
                            >
                              {option}
                            </DropdownMenuItem>
                          ))}
                        </DropdownMenuContent>
                      </DropdownMenu>

                      <div className="text-muted-foreground font-normal">{set.previous}</div>

                      <NumericalUnderscoreInput
                        value={set.kg}
                        onChange={(event) => {
                          const rawValue = event.target.value
                          updateSet(exercise.id, set.id, (currentSet) => ({
                            ...currentSet,
                            kg: rawValue === '' ? '' : Number(rawValue),
                          }))
                        }}
                        className="text-xl text-center mx-auto"
                      />
                      <NumericalUnderscoreInput
                        value={set.reps}
                        onChange={(event) => {
                          const rawValue = event.target.value
                          updateSet(exercise.id, set.id, (currentSet) => ({
                            ...currentSet,
                            reps: rawValue === '' ? '' : Number(rawValue),
                          }))
                        }}
                        className="text-xl text-center mx-auto"
                      />
                      <div className="flex items-center justify-center border border-border rounded-md h-7 bg-surface-2">
                        <span className="text-xs w-full text-center">{set.rpe}</span>
                      </div>

                      <div className="flex w-full items-center justify-center gap-1">
                        <Button
                          variant="icon"
                          size="icon"
                          className={`h-7 w-7 rounded-md border-border ${set.completed ? 'bg-brand text-white' : 'bg-surface-2'}`}
                          onClick={() => updateSet(exercise.id, set.id, (currentSet) => ({ ...currentSet, completed: !currentSet.completed }))}
                        >
                          <Check className="h-3.5 w-3.5" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          className="h-7 w-7 text-muted-foreground hover:text-destructive"
                          onClick={() => updateSet(exercise.id, set.id, (currentSet) => ({ ...currentSet, completed: false }))}
                        >
                          <X className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>

                <Button
                  variant="outline"
                  className="mt-3 w-full border-dashed border-border text-muted-foreground hover:text-foreground bg-transparent h-9 text-xs"
                  onClick={() => addSet(exercise.id)}
                >
                  <Plus className="mr-2 h-3.5 w-3.5" /> Add Set
                </Button>
              </CardContent>
            </Card>
          ))}

          <div className="grid grid-cols-2 gap-4">
            <Card className="border-border bg-card rounded-xl">
              <CardHeader className="pb-2 px-4 pt-4">
                <CardTitle className="text-sm font-bold">Recommended</CardTitle>
              </CardHeader>
              <CardContent className="flex items-center justify-between px-4 pb-4">
                <div className="flex items-center gap-3">
                  <div className="h-8 w-8 rounded-full bg-surface-2 border border-border" />
                  <div>
                    <p className="text-sm font-bold leading-tight">Bicep curl</p>
                    <p className="text-xs text-muted-foreground">Biceps</p>
                  </div>
                </div>
                <Button variant="outline" size="icon" className="h-7 w-7 rounded-md bg-surface-2 border-border">
                  <Plus className="h-3.5 w-3.5" />
                </Button>
              </CardContent>
            </Card>

            <Card className="border-border bg-card rounded-xl">
              <CardHeader className="pb-1 px-4 pt-4">
                <CardTitle className="text-sm font-bold">Why?</CardTitle>
              </CardHeader>
              <CardContent className="px-4 pb-4">
                <p className="text-xs text-muted-foreground leading-relaxed">
                  Based on your RPE, this would be a good alternative for your next exercise.
                </p>
              </CardContent>
            </Card>
          </div>

          <Button
            variant="outline"
            className="w-full border-dashed border-border text-muted-foreground hover:text-foreground bg-transparent h-10 text-xs"
            onClick={addExercise}
          >
            <Plus className="mr-2 h-3.5 w-3.5" /> Add Exercise
          </Button>

        </div>
      </div>
    </section>
  )
}