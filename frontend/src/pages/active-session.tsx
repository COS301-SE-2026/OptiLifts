import { useEffect, useMemo, useRef, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle, CardAction } from '@/components/ui/card'
import { Input, NumericalUnderscoreInput } from '@/components/ui/input'
import { toast } from '@/components/ui/alert'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { customFetch } from '@/lib/custom-fetch'
import { getColumns } from '@/components/ui/exercise-card'
import { enqueue, flushOutBox, type WorkoutLogPayload, type WorkoutLogSetPayload, type WorkoutLogExercisePayload } from '@/lib/offline/workout-logs'
import { Check, Plus, ChevronDown, MoreHorizontal, ArrowLeft } from 'lucide-react'
import { ExercisePickerDialog, type CatalogExercise } from '@/components/ui/exercise-picker-dialog'

type WorkoutLocationState = Readonly<{
  workout?: Readonly<{
    id?: string
    name: string
    primaryMuscleGroups: string[]
  }>
}>

type SetType = 'Warmup' | 'Normal' | 'DropSet'

type SetData = {
  id: string
  type: SetType
  previous: string
  kg: number | string
  reps: number | string
  rpe: number | string
  duration: number | string
  distance: number | string
  restTime: number
  completed: boolean
  sourceSetId: string | null
}

type ExerciseData = Readonly<{
  id: string
  sourceWorkoutExerciseId: string | null
  name: string
  muscleGroup: string
  sets: SetData[]
  recommendation?: string
  groupId: string | null
  groupType: string | null
  groupRestTime: number | null
  exerciseId: string | null
  exerciseType: string
}>

type WorkoutDetailsResponse = Readonly<{
  id: string
  name: string
  folderId: string | null
  dayIndex: number | null
  createdAt: string
  primaryMuscleGroups: string[]
  exercisePreview: string[]
  exercises: Array<{
    id: string
    exerciseId: string
    name: string
    primaryMuscle: string
    exerciseType: string
    orderIndex: number
    sets: Array<{
      id: string
      type: SetType
      reps: number | null
      weight: number | null
      duration: number | null
      distance: number | null
      orderIndex: number
      restTime: number
    }>
    groupId?: string | null
    groupType?: string | null
    groupRestTime?: number | null
  }>
}>

const SET_TYPE_OPTIONS: readonly SetType[] = ['Warmup', 'Normal', 'DropSet']
const FIELD_TO_SET_KEY = { kg: 'kg', reps: 'reps', time: 'duration', distance: 'distance' } as const

const setTypeLabelMap: Record<SetType, string> = {
  Warmup: 'Warmup',
  Normal: 'Working',
  DropSet: 'Dropset'
}

const getSetLabel = (type: SetType, workingNumber: number): string | number => {
  if (type === 'Warmup') return 'W'
  if (type === 'DropSet') return 'D'
  return workingNumber
}

type SetRowProps = Readonly<{
  set: SetData
  setLabel: string | number
  columns: ReturnType<typeof getColumns>
  gridTemplate: string
  onUpdate: (updater: (current: SetData) => SetData) => void
}>

function SetRow({ set, setLabel, columns, gridTemplate, onUpdate }: SetRowProps) {
  const setField = (key: 'kg' | 'reps' | 'duration' | 'distance' | 'rpe', raw: string) =>
    onUpdate((current) => ({ ...current, [key]: raw === '' ? '' : Number(raw) }))

  return (
    <div className="grid items-center gap-4 rounded-lg bg-surface-2 p-1.5 text-center text-sm font-medium" style={{ gridTemplateColumns: gridTemplate }}>
      <div className="flex items-center">
        <DropdownMenu>
          <DropdownMenuTrigger variant="plain" className="text-muted-foreground hover:text-foreground">
            <ChevronDown className="h-4 w-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            {SET_TYPE_OPTIONS.map((option) => (
              <DropdownMenuItem key={option} onSelect={() => onUpdate((current) => ({ ...current, type: option }))}>
                {setTypeLabelMap[option]}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        <Input readOnly value={setLabel} className="h-8 w-8 border-0 bg-transparent px-0 text-center text-sm font-bold" />
      </div>

      <div className="text-muted-foreground font-normal">{set.previous}</div>

      {columns.map((col) => {
        const key = FIELD_TO_SET_KEY[col.field]
        return (
          <NumericalUnderscoreInput
            key={col.field}
            value={set[key]}
            onChange={(event) => setField(key, event.target.value)}
            className="text-xl text-center mx-auto"
          />
        )
      })}

      <NumericalUnderscoreInput
        value={set.rpe}
        placeholder="RPE"
        onChange={(event) => setField('rpe', event.target.value)}
        className="text-base text-center mx-auto"
      />

      <div className="flex w-full items-center justify-center gap-1">
        <Button
          variant="icon"
          size="icon"
          className={`h-7 w-7 rounded-md border-border transition-colors ${set.completed ? 'bg-brand text-white hover:bg-brand' : 'bg-surface-2 hover:border-brand hover:text-brand'}`}
          onClick={() => onUpdate((current) => ({ ...current, completed: !current.completed }))}
        >
          <Check className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  )
}

const buildPreviousText = (kg: number | null, reps: number | null) => {
  if (kg == null && reps == null) {
    return '-'
  }

  const kgPart = kg == null ? '-' : `${kg}KG`
  const repsPart = reps == null ? '-' : `${reps}`
  return `${kgPart} x ${repsPart}`
}

type SessionSegment =
  | { kind: 'single'; exercise: ExerciseData }
  | { kind: 'group'; groupId: string; groupType: string; groupRestTime: number | null; members: ExerciseData[] }

function findLastMember(exercises: ExerciseData[], start: number, groupId: string): number {
  let last = start

  while (last < exercises.length && exercises[last].groupId === groupId) {
    last++
  }

  return last
}

function toSegs(members: ExerciseData[]): SessionSegment {
  const firstMem = members[0]
  
  if (members.length > 1 && firstMem.groupId) {
    return {
      kind: 'group',
      groupId: firstMem.groupId,
      groupType: firstMem.groupType ?? (members.length === 2 ? 'Superset' : 'Circuit'),
      groupRestTime: firstMem.groupRestTime ?? null,
      members,
    }
  }

  return { kind: 'single', exercise: firstMem }
}

function buildSessionSegs(exercises: ExerciseData[]): SessionSegment[] {
  const segs: SessionSegment[] = []
  let x = 0

  while (x < exercises.length) {
    const curr = exercises[x]
    const end = curr.groupId ? findLastMember(exercises, x, curr.groupId) : x + 1
    
    segs.push(toSegs(exercises.slice(x, end)))
    x = end
  }

  return segs
}

const toSessSet = (set: WorkoutDetailsResponse['exercises'][number]['sets'][number]): SetData => ({
  id: set.id,
  sourceSetId: set.id,
  type: set.type,
  previous: buildPreviousText(set.weight, set.reps),
  kg: set.weight ?? '',
  reps: set.reps ?? '',
  rpe: '',
  duration: set.duration ?? '',
  distance: set.distance ?? '',
  restTime: set.restTime,
  completed: false,
})

const toSessExercise = (exercise: WorkoutDetailsResponse['exercises'][number]): ExerciseData => ({
  id: exercise.id,
  exerciseId: exercise.exerciseId,
  sourceWorkoutExerciseId: exercise.id,
  name: exercise.name,
  muscleGroup: exercise.primaryMuscle,
  groupId: exercise.groupId ?? null,
  groupType: exercise.groupType ?? null,
  groupRestTime: exercise.groupRestTime ?? null,
  exerciseType: exercise.exerciseType,
  sets: [...exercise.sets].sort((a, b) => a.orderIndex - b.orderIndex).map(toSessSet),
})


function groupNumMap(exercises: ExerciseData[]): Map<string, number> {
  const groupNumByExerciseId = new Map<string, number>()
  let counterr = 0

  for (const seg of buildSessionSegs(exercises)) {
    if (seg.kind === 'group') {
      counterr += 1
      for (const member of seg.members) 
      {
        groupNumByExerciseId.set(member.id, counterr)
      }
    } else {
      groupNumByExerciseId.set(seg.exercise.id, 0)
    }
  }
  return groupNumByExerciseId
}

const secureRandomHex = (): string => {
  const res = globalThis.crypto?.getRandomValues?.(new Uint8Array(6))

  return res ? Array.from(res, (b) => b.toString(16).padStart(2, '0')).join('') : Date.now().toString(36)
}

const buildSetPayloads = (exerciseSets: SetData[], groupNumber: number): WorkoutLogSetPayload[] => {
  const sets: WorkoutLogSetPayload[] = []
  let orderIdx = 0

  for (const set of exerciseSets) {
    if (!set.completed) continue

    const reps = set.reps === '' ? 0 : Number(set.reps)
    const weight = set.kg === '' ? 0 : Number(set.kg)
    const rpe = set.rpe === '' ? 0 : Number(set.rpe)

    if (Number.isNaN(reps) || Number.isNaN(weight) || Number.isNaN(rpe)) continue
    
    const dur = set.duration === '' ? null : Number(set.duration)
    const dist = set.distance === '' ? null : Number(set.distance)

    sets.push({ setId: set.sourceSetId, type: set.type, reps, weight, duration: dur, distance: dist, restTime: set.restTime, rpe, orderIndex: orderIdx++, groupNumber, })
  }

  return sets
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

  return `set-${Date.now()}-${secureRandomHex()}`
}

const createClientExerciseId = () => {
  if (globalThis.crypto?.randomUUID) {
    return globalThis.crypto.randomUUID()
  }

  return `exercise-${Date.now()}-${secureRandomHex()}`
}

export default function ActiveSessionPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const sessionState = location.state as WorkoutLocationState | null
  const workoutId = sessionState?.workout?.id

  const [workoutName, setWorkoutName] = useState(sessionState?.workout?.name ?? 'WORKOUT')
  const [isLoading, setIsLoading] = useState(() => Boolean(workoutId))
  const [error, setError] = useState<string | null>(() =>
    workoutId ? null : 'No workout was selected. Start a workout from the workouts page.'
  )
  const logRefId = useRef<string | null>(null)
  logRefId.current ??= globalThis.crypto?.randomUUID?.() ?? `log-${Date.now()}-${secureRandomHex()}`
  const logId = logRefId.current
  const [startedAtMs, setStartedAtMs] = useState<number | null>(null)
  const [nowMs, setNowMs] = useState<number>(0)
  const [isPickerOpen, setPickerOpen] = useState(false)
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

        const mappedExercises: ExerciseData[] = [...data.exercises].sort((a, b) => a.orderIndex - b.orderIndex).map(toSessExercise)

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
    const applySet = (set: SetData): SetData => (set.id === setId ? updater(set) : set)
    const applyExercise = (exercise: ExerciseData): ExerciseData => exercise.id === exerciseId ? { ...exercise, sets: exercise.sets.map(applySet) } : exercise
    setExercises((current) => current.map(applyExercise))
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
              sourceSetId: null,
              type: 'Normal',
              previous: '-',
              kg: '',
              reps: '',
              rpe: '',
              duration: '',
              distance: '',
              restTime: 0,
              completed: false,
            },
          ],
        }
      })
    )
  }
  

  const selectedExercise = (exercise: CatalogExercise) => {
    setExercises((currentExercises) => [
      ...currentExercises,
      {
        id: createClientExerciseId(),
        exerciseId: exercise.id,
        sourceWorkoutExerciseId: null,
        name: exercise.name,
        muscleGroup: exercise.muscleGroup,
        groupId: null,
        groupType: null,
        groupRestTime: null,
        exerciseType: exercise.exerciseType ?? 'weight-reps',
        sets: [
          {
            id: createClientSetId(),
            sourceSetId: null,
            type: 'Normal',
            previous: '-',
            kg: '',
            reps: '',
            rpe: '',
            duration: '',
            distance: '',
            restTime: 0,
            completed: false,
          },
        ],
      },
    ])
    setPickerOpen(false)
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

  const allowedFinish = summary.completedSets > 0

  const buildLogPayload = (): WorkoutLogPayload | null => {
    if (!workoutId) return null

    const groupNums = groupNumMap(exercises)
    const exercisesLog: WorkoutLogExercisePayload[] = []

    for (const exercise of exercises) {
      if (!exercise.exerciseId) continue

      const groupNum = groupNums.get(exercise.id) ?? 0
      const sets = buildSetPayloads(exercise.sets, groupNum)

      if (sets.length === 0) continue

      exercisesLog.push({ exerciseId: exercise.exerciseId, workoutExerciseId: exercise.sourceWorkoutExerciseId,
        orderIndex: exercisesLog.length, groupNumber: groupNum, sets,
      })
    }

    if (exercisesLog.length === 0) return null

    return {
      logId,
      workoutId,
      entryId: null,
      startedAt: new Date(startedAtMs ?? Date.now()).toISOString(),
      completedAt: new Date().toISOString(),
      notes: null,
      exercises: exercisesLog,
    }
  }


  const removeExercise = (exerciseId: string) => {
    setExercises((currentExercises) => currentExercises.filter((exercise) => exercise.id !== exerciseId))
  }


  const finishWorkout = async () => {
    const load = buildLogPayload()
    if (!load) {
      return
    }

    await enqueue(load)
    void flushOutBox()

    if (navigator.onLine) {
      toast.success('Workout saved.', 'Saved')
    } else {
      toast.info("Workout saved but will sync when you're back online.", 'Saved offline')
    }
    navigate('/workouts')
  }

    const renderExerciseCard = (exercise: ExerciseData) => {

    const cols = getColumns(exercise.exerciseType)
    const gridTemp = `4rem 1.5fr ${cols.map(() => '1fr').join(' ')} 0.8fr 5rem`
    
    return (
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
            <DropdownMenu>
              <DropdownMenuTrigger variant="plain" className="p-1 text-muted-foreground hover:text-foreground">
                <MoreHorizontal className="h-5 w-5" />
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem variant="destructive" onSelect={() => removeExercise(exercise.id)}>
                  Remove exercise
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </CardAction>
        </CardHeader>

        <CardContent className="px-5 pb-4">
          <div
            className="mb-2 grid gap-4 px-2 text-center text-xs font-semibold tracking-wide text-muted-foreground"
            style={{ gridTemplateColumns: gridTemp }}>
            <div>SET</div>
            <div>PREVIOUS</div>
            {cols.map((col) => <div key={col.field}>{col.label}</div>)}
            <div>RPE</div>
            <div className="w-full flex justify-center"><Check className="h-4 w-4" /></div>
          </div>

          <div className="space-y-2">
            {exercise.sets.map((set, setIndex) => {
              const workingNumber = exercise.sets.slice(0, setIndex + 1).filter((s) => s.type === 'Normal').length
              return (
                <SetRow
                  key={set.id}
                  set={set}
                  setLabel={getSetLabel(set.type, workingNumber)}
                  columns={cols}
                  gridTemplate={gridTemp}
                  onUpdate={(updater) => updateSet(exercise.id, set.id, updater)}
                />
              )
            })}
          </div>
          <Button
            variant="outline"
            className="mt-3 w-full border-dashed border-border text-muted-foreground hover:text-foreground bg-transparent h-9 text-xs"
            onClick={() => addSet(exercise.id)}>
            <Plus className="mr-2 h-3.5 w-3.5" /> Add Set
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <section className="mx-auto max-w-6xl px-6 py-6 lg:h-[calc(100dvh-5rem)] lg:overflow-hidden">
      <div className="grid grid-cols-12 gap-6 lg:h-full lg:min-h-0">
        <div className="col-span-12 lg:col-span-7 flex min-w-0 flex-col gap-6 lg:h-full lg:min-h-0">
          <div className="flex flex-col gap-2">
            <Button
              variant="text"
              size="sm"
              onClick={() => navigate('/workouts')}
              className="-ml-1 flex items-center gap-1 self-start text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4" />
              <span>Back to Workouts</span>
            </Button>

            <div className="flex items-end justify-between gap-4">
              <div className="flex items-center gap-3">
                <div className="h-8 w-1.5 rounded-full bg-brand" />
                <h1 className="text-3xl font-bold uppercase tracking-tight">{workoutName}</h1>
              </div>

              <div className="flex items-center gap-6 text-center">
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
                <Button variant="default" size="sm" className="h-8" disabled={!allowedFinish} onClick={() => void finishWorkout()}>
                  Finish
                </Button>
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

          <div className="max-h-[calc(100dvh-15rem)] overflow-y-auto pr-1">
            <div className="flex flex-col gap-4">
              {buildSessionSegs(exercises).map((seg) => {
                if (seg.kind === 'single') {
                  return renderExerciseCard(seg.exercise)
                }

                return (
                  <div
                    key={seg.groupId}
                    className="flex flex-col gap-3 rounded-xl border-2 border-brand/60 bg-brand/5 p-3"
                  >
                    <div className="flex items-center justify-between px-1">
                      <span className="text-xs font-bold uppercase tracking-[1px] text-brand">
                        {seg.groupType}
                      </span>
                      {seg.groupRestTime != null && (
                        <span className="text-xs text-muted-foreground">
                          Rest {seg.groupRestTime}s between rounds
                        </span>
                      )}
                    </div>
                    {seg.members.map((member) => renderExerciseCard(member))}
                  </div>
                )
              })}

              {!isLoading && exercises.length === 0 && !error && (
                <p className="text-sm text-muted-foreground">No exercises in this workout yet.</p>
              )}

              <Button
                variant="outline"
                className="w-full border-dashed border-border text-muted-foreground hover:text-foreground bg-transparent h-10 text-xs"
                onClick={() => setPickerOpen(true)}
              >
                <Plus className="mr-2 h-3.5 w-3.5" /> Add Exercise
              </Button>
            </div>
          </div>
        </div>

        <div className="col-span-12 lg:col-span-5 min-w-0">
          <div className="flex w-full flex-col gap-4 lg:max-h-[calc(100dvh-8.5rem)] lg:overflow-y-auto lg:[scrollbar-gutter:stable]">
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
        </div>

      </div>
      <ExercisePickerDialog
        isOpen={isPickerOpen}
        onClose={() => setPickerOpen(false)}
        onSelect={selectedExercise}
      />
    </section>
  )
}