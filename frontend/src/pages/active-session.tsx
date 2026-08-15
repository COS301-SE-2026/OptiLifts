import { useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { PageTitle } from '@/components/ui/page-title'
import { Card, CardContent, CardHeader, CardTitle, CardAction } from '@/components/ui/card'
import { Input, NumericalUnderscoreInput } from '@/components/ui/input'
import { toast } from '@/components/ui/alert'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'
import { customFetch } from '@/lib/custom-fetch'
import { getColumns } from '@/components/ui/exercise-card'
import { enqueue, flushOutBox, type WorkoutLogPayload, type WorkoutLogSetPayload, type WorkoutLogExercisePayload } from '@/lib/offline/workout-logs'
import { Check, Plus, ChevronDown, MoreHorizontal, ArrowLeft, X, Trophy } from 'lucide-react'
import { ExercisePickerDialog, type CatalogExercise } from '@/components/ui/exercise-picker-dialog'
import { saveDraft, getDraft, clearDraft, getDraftFromStorage } from '@/lib/session-drafts'
import { cacheWorkoutDetail, getCachedWorkoutDetail } from '@/lib/offline/workouts-cache'
import { ExerciseDetailsPopup } from '@/components/ui/exercise-details-popup'
import MusclesSummary from '@/components/ui/muscles-summary'
import MuscleDiagram from '@/components/ui/muscle-diagram'
import { MUSCLE_GROUPS } from '@/constants/muscles'
import type { MuscleName } from '@/types/workout'
import { useOnlineStatus, OfflineTooltip } from '@/lib/use-online-status'
import type { WorkoutLogDetailResponse } from '@/types/workout-log-detail'
import type { WorkoutDetailResponse } from '@/types/workout-detail'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { adaptImgUrl } from '@/lib/utils'
import { buildLabels } from '@/lib/exercise-format'
import confetti from 'canvas-confetti'
import { OfflineBanner } from '@/components/ui/offline-banner'

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
  secondaryMuscles: string[]
  sets: SetData[]
  recommendation?: string
  groupId: string | null
  groupType: string | null
  groupRestTime: number | null
  imageUrl: string | null
  bestWeight: number | null
  bestSetVolume: number | null
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
    secondaryMuscles?: string[]
    exerciseType: string
    orderIndex: number
    imageUrl?: string | null
    bestWeight?: number | null
    bestSetVolume?: number | null
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

type RestTimer = {
  endsAt: number
  totalSeconds: number
  exerciseName: string
}

type SessionDraft = {
  workoutId: string
  workoutName: string
  startedAtMs: number | null
  logId: string
  exercises: ExerciseData[]
  restTimer: RestTimer | null
}

const SET_TYPE_OPTIONS: readonly SetType[] = ['Warmup', 'Normal', 'DropSet']
const FIELD_TO_SET_KEY = { kg: 'kg', reps: 'reps', time: 'duration', distance: 'distance' } as const
const MAX_REST_OVERTIME_MS = 10 * 60 * 1000

const createRestTimer = (seconds: number, exerciseName: string): RestTimer => ({
  endsAt: Date.now() + seconds * 1000,
  totalSeconds: seconds,
  exerciseName,
})

const revivedRestTimer = (timer: RestTimer | null | undefined): RestTimer | null => {
  if (!timer) {
    return null
  }

  return Date.now() - timer.endsAt > MAX_REST_OVERTIME_MS ? null : timer
}

const setTypeLabelMap: Record<SetType, string> = {
  Warmup: 'Warmup',
  Normal: 'Working',
  DropSet: 'Dropset'
}

const setTypeRowClass: Record<SetType, string> = {
  Warmup: 'bg-warning/10 border-l-4 border-warning/50',
  Normal: 'bg-surface-2 border-l-4 border-transparent',
  DropSet: 'bg-brand/10 border-l-4 border-brand/50',
}

type SetRowProps = Readonly<{
  set: SetData
  setLabel: string
  columns: ReturnType<typeof getColumns>
  gridTemplate: string
  gridTemplateMobile: string
  isPR: boolean,
  onUpdate: (updater: (current: SetData) => SetData) => void
  onRemove: () => void
  onRestStart: () => void
}>

function SetRow({ set, setLabel, columns, gridTemplate, gridTemplateMobile, isPR, onUpdate, onRemove, onRestStart }: SetRowProps) {
  const setField = (key: 'kg' | 'reps' | 'duration' | 'distance' | 'rpe', raw: string) =>
    onUpdate((current) => ({ ...current, [key]: raw === '' ? '' : Number(raw) }))

  return (
    <div
      className={`grid items-center gap-2 lg:gap-4 rounded-lg p-1.5 text-center text-sm font-medium [grid-template-columns:var(--set-cols-m)] lg:[grid-template-columns:var(--set-cols)] ${isPR ? 'bg-success/15 border-l-4 border-success' : setTypeRowClass[set.type]}`}
      style={{ ['--set-cols-m' as string]: gridTemplateMobile, ['--set-cols' as string]: gridTemplate }}
    >
      <div className="flex items-center">
        <DropdownMenu>
          <DropdownMenuTrigger variant="plain" className="text-muted-foreground hover:text-foreground">
            <ChevronDown className="h-4 w-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-auto min-w-[9rem]">
            {SET_TYPE_OPTIONS.map((option) => (
              <DropdownMenuItem key={option} onSelect={() => onUpdate((current) => ({ ...current, type: option }))}>
                {setTypeLabelMap[option]}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        <Input readOnly value={setLabel} className="h-8 w-8 border-0 bg-transparent px-0 text-center text-sm font-bold" />
      </div>

      <div className="hidden lg:block text-muted-foreground font-normal">{set.previous}</div>

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

      <div className="hidden lg:block">
        <NumericalUnderscoreInput
          value={set.rpe}
          placeholder="RPE"
          onChange={(event) => setField('rpe', event.target.value)}
          className="text-base text-center mx-auto"
        />
      </div>

      <div className="flex w-full items-center">
        <div className="flex flex-1 items-center justify-center">
          <Button
            variant="icon"
            size="icon"
            className={`relative h-7 w-7 rounded-md border-border transition-colors before:absolute before:-inset-x-2 before:-inset-y-1 before:content-[''] ${set.completed ? 'bg-brand text-primary-foreground hover:bg-brand' : 'bg-surface-2 hover:border-brand hover:text-brand'}`}
            onClick={() => {
              const willComplete = !set.completed
              onUpdate((current) => ({ ...current, completed: !current.completed }))
              if (willComplete) {
                onRestStart()
              }
            }}
          >
            {isPR ? <Trophy className="h-3.5 w-3.5" /> : <Check className="h-3.5 w-3.5" />}
          </Button>
        </div>
        <Button
          variant="icon"
          size="icon"
          aria-label="Remove set"
          className="relative h-7 w-7 rounded-md shrink-0 border-0 bg-transparent text-muted-foreground hover:text-destructive before:absolute before:-inset-x-2 before:-inset-y-1 before:content-['']"
          onClick={onRemove}
        >
          <X className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  )
}

const formatClock = (totalSeconds: number) => {
  const abs = Math.abs(totalSeconds)
  return `${Math.floor(abs / 60)}:${String(abs % 60).padStart(2, '0')}`
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
  secondaryMuscles: exercise.secondaryMuscles ?? [],
  groupId: exercise.groupId ?? null,
  groupType: exercise.groupType ?? null,
  groupRestTime: exercise.groupRestTime ?? null,
  imageUrl: exercise.imageUrl ?? null,
  bestWeight: exercise.bestWeight ?? null,
  bestSetVolume: exercise.bestSetVolume ?? null,
  exerciseType: exercise.exerciseType,
  sets: [...exercise.sets].sort((a, b) => a.orderIndex - b.orderIndex).map(toSessSet),
})

const backfillImage = (exercise: ExerciseData, images: Map<string, string | null>): ExerciseData => {
  if (exercise.imageUrl || !exercise.exerciseId) {
    return exercise
  }

  return { ...exercise, imageUrl: images.get(exercise.exerciseId) ?? null }
}

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

type PrHit = { exerciseName: string; kind: 'weight' | 'volume'; value: number }
type PrKind = 'weight' | 'volume'

const getSetPrKinds = (exercise: ExerciseData, set: SetData): PrKind[] => {
  if (set.type !== 'Normal') {
    return []
  }

  const weight = toNumericValue(set.kg)
  const reps = toNumericValue(set.reps)

  if (weight <= 0 || reps <= 0) {
    return []
  }

  let bestWeight = exercise.bestWeight ?? 0
  let bestVolume = exercise.bestSetVolume ?? 0

  for (const other of exercise.sets) {
    if (other.id === set.id || !other.completed || other.type !== 'Normal') {
      continue
    }

    const otherWeight = toNumericValue(other.kg)
    const otherReps = toNumericValue(other.reps)

    if (otherWeight <= 0 || otherReps <= 0) {
      continue
    }

    bestWeight = Math.max(bestWeight, otherWeight)
    bestVolume = Math.max(bestVolume, otherWeight * otherReps)
  }

  const kinds: PrKind[] = []

  if (weight > bestWeight) {
    kinds.push('weight')
  }

  if (weight * reps > bestVolume) {
    kinds.push('volume')
  }

  return kinds
}

const detectPrs = (exercises: ExerciseData[]): PrHit[] => {
  const hits: PrHit[] = []

  for (const exercise of exercises) {
    let topWeight = 0
    let topVolume = 0

    for (const set of exercise.sets) {
      if (!set.completed || set.type !== 'Normal') {
        continue
      }

      const weight = toNumericValue(set.kg)
      const reps = toNumericValue(set.reps)

      if (weight <= 0 || reps <= 0) {
        continue
      }

      topWeight = Math.max(topWeight, weight)
      topVolume = Math.max(topVolume, weight * reps)
    }

    if (topWeight > (exercise.bestWeight ?? 0)) {
      hits.push({ exerciseName: exercise.name, kind: 'weight', value: topWeight })
    }

    if (topVolume > (exercise.bestSetVolume ?? 0)) {
      hits.push({ exerciseName: exercise.name, kind: 'volume', value: topVolume })
    }
  }

  return hits
}

const buildSetPayloads = (exerciseSets: SetData[], groupNumber: number): WorkoutLogSetPayload[] => {
  const sets: WorkoutLogSetPayload[] = []
  let orderIdx = 1

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

function hasBlankReqFields(set: SetData, cols: ReturnType<typeof getColumns>): boolean {
  return set.completed && cols.some((col) => set[FIELD_TO_SET_KEY[col.field]] === '')
}

function exerciseGotBlanks(exercise: ExerciseData): boolean {
  if (!exercise.exerciseId) {
    return false
  }

  const cols = getColumns(exercise.exerciseType)
  return exercise.sets.some((set) => hasBlankReqFields(set, cols))
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

const formattedTime = (totalSecs: number) => {
  const h = Math.floor(totalSecs / 3600)
  const m = Math.floor((totalSecs % 3600) / 60)

  if (h > 0) {
    return `${h}h ${m}min`
  }

  return `${m}m`
}

const formatPastDurationText = (pastDurationText: string): string => {
  const [hoursText, minutesText] = pastDurationText.split(':')
  const hours = Number.parseInt(hoursText ?? '0', 10)
  const minutes = Number.parseInt(minutesText ?? '0', 10)

  if (Number.isNaN(hours) || Number.isNaN(minutes)) {
    return pastDurationText
  }

  if (hours === 0) {
    return minutes === 0 ? '<1m' : `${minutes}m`
  }

  return minutes === 0 ? `${hours}h` : `${hours}h ${minutes}m`
}

type ActiveSessionProps = Readonly<{
  mode?: 'active' | 'edit'
}>

const initSessError = (workoutId: string | undefined, isEditMode: boolean, logId: string | undefined) => {
  if (!workoutId) {
    return 'No workout was selected. Start a workout from the workouts page.'
  }

  if (isEditMode && !logId) {
    return 'No workout log was selected to edit.'
  }

  return null
}

const makeSessLogId = (isEditMode: boolean, paramLogId: string | undefined) => {
  if (isEditMode && paramLogId) {
    return paramLogId
  }

  return globalThis.crypto?.randomUUID?.() ?? `log-${Date.now()}-${secureRandomHex()}`
}

export default function ActiveSessionPage({ mode = 'active' }: ActiveSessionProps) {
  const navigate = useNavigate()
  const location = useLocation()
  const params = useParams<{ workoutId?: string; logId?: string }>()
  const sessionState = location.state as WorkoutLocationState | null

  const isEditMode = mode === 'edit' || Boolean(params.workoutId && params.logId)
  const workoutId = isEditMode ? params.workoutId : sessionState?.workout?.id

  const [workoutName, setWorkoutName] = useState(sessionState?.workout?.name ?? 'WORKOUT')
  const [isLoading, setIsLoading] = useState(() => Boolean(workoutId && (!isEditMode || params.logId)))
  const [error, setError] = useState<string | null>(() => initSessError(workoutId, isEditMode, params.logId))
  const [logId] = useState(() => makeSessLogId(isEditMode, params.logId))
  const [startedAtIso, setStartedAtIso] = useState<string | null>(null)
  const [completedAtIso, setCompletedAtIso] = useState<string | null>(null)
  const [pastDurationText, setPastDurationText] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [startedAtMs, setStartedAtMs] = useState<number | null>(null)
  const [nowMs, setNowMs] = useState<number>(() => Date.now())
  const [isPickerOpen, setPickerOpen] = useState(false)
  const [exitOpen, setExitOpen] = useState(false)
  const [pendingNavTo, setPendingNavTo] = useState<string | null>(null)
  const [exercises, setExercises] = useState<ExerciseData[]>([])
  const [primaryMuscleGroups, setPrimaryMuscleGroups] = useState<string[]>(sessionState?.workout?.primaryMuscleGroups ?? [])
  const [detailsExerciseId, setDetailsExerciseId] = useState<string | null>(null)
  const [conflictDraft, setConflictDraft] = useState<{ workoutId: string; workoutName: string } | null>(null)
  const [startKey, setStartKey] = useState(0)
  const [restTimer, setRestTimer] = useState<RestTimer | null>(null)
  const [prSetIds, setPrSetIds] = useState<string[]>([])
  const [isOfflineData, setIsOfflineData] = useState(false)
  const isOnline = useOnlineStatus()

  useEffect(() => {
    if (!workoutId) {
      return
    }

    let isMounted = true

    if (isEditMode) {
      if (!logId) {
        return
      }

      const fetchLogDetail = async () => {
        setIsLoading(true)
        setError(null)
        try {
          const resp = await customFetch(`/api/workouts/${workoutId}/logs/${logId}`, {
            headers: { Accept: 'application/json' },
          })

          if (!resp.ok) {
            throw new Error(`Failed to load workout log (${resp.status})`)
          }

          const data = (await resp.json()) as WorkoutLogDetailResponse
          if (!isMounted) return

          setWorkoutName(data.name)
          setPrimaryMuscleGroups(data.primaryMuscleGroups ?? [])
          setStartedAtIso(data.startedAt ?? null)
          setCompletedAtIso(data.completedAt ?? null)
          setPastDurationText(data.duration ?? null)

          const mappedExers: ExerciseData[] = (data.exercises ?? []).map((ex) => ({
            id: ex.id,
            exerciseId: ex.exerciseId,
            sourceWorkoutExerciseId: ex.id,
            name: ex.name,
            muscleGroup: ex.primaryMuscle,
            secondaryMuscles: ex.secondaryMuscles ?? [],
            bestWeight: null,
            bestSetVolume: null,
            groupId: null,
            groupType: null,
            groupRestTime: null,
            imageUrl: ex.imageUrl ?? null,
            exerciseType: ex.exerciseType,
            sets: (ex.sets ?? []).map((s) => ({
              id: s.id,
              sourceSetId: s.setId ?? s.id,
              type: (s.type as SetType) ?? 'Normal',
              previous: buildPreviousText(s.weight, s.reps),
              kg: s.weight ?? '',
              reps: s.reps ?? '',
              rpe: s.rpe ?? '',
              duration: s.duration ?? '',
              distance: s.distance ?? '',
              restTime: s.restTime ?? 0,
              completed: true,
            })),
          }))

          setExercises(mappedExers)
        } catch (loadError) {
          if (isMounted) {
            setError(loadError instanceof Error ? loadError.message : 'Failed to load workout log.')
          }
        } finally {
          if (isMounted) {
            setIsLoading(false)
          }
        }
      }

      void fetchLogDetail()
      return () => {
        isMounted = false
      }
    }

    const restoreDraft = (draft: SessionDraft) => {
      setWorkoutName(draft.workoutName)
      setStartedAtMs(draft.startedAtMs)
      setExercises(draft.exercises)
      setRestTimer(revivedRestTimer(draft.restTimer))
      setIsLoading(false)
    }


    // drafts don't carry images or muscle groups - backfill from the workout
    const backfillDraft = async () => {
      try {
        const draftResp = await customFetch(`/api/workouts/${workoutId}`, {
          headers: { Accept: 'application/json' },
        })

        if (!draftResp.ok || !isMounted) {
          return
        }

        const draftData = (await draftResp.json()) as WorkoutDetailsResponse

        if (!isMounted) {
          return
        }

        setPrimaryMuscleGroups(draftData.primaryMuscleGroups ?? [])

        const imageByExerciseId = new Map(draftData.exercises.map((ex) => [ex.exerciseId, ex.imageUrl ?? null]))

        setExercises((current) => current.map((ex) => backfillImage(ex, imageByExerciseId)))
      }
      catch {
        // offline or request failed - the draft still works as-is
      }
    }

    const fetchWorkout = async () => {
      try {
        const resp = await customFetch(`/api/workouts/${workoutId}`, {
          headers: { Accept: 'application/json' },
        })

        if (!resp.ok) {
          throw new Error(`Failed to load this workout's details (${resp.status})`)
        }

        const data = (await resp.json()) as WorkoutDetailsResponse
        if (!isMounted) {
          return
        }

        setWorkoutName(data.name)
        setPrimaryMuscleGroups(data.primaryMuscleGroups ?? [])
        setStartedAtMs(Date.now())

        const mappedExers: ExerciseData[] = [...data.exercises].sort((a, b) => a.orderIndex - b.orderIndex).map(toSessExercise)

        setExercises(mappedExers)
        setIsOfflineData(false)
        void cacheWorkoutDetail(data as WorkoutDetailResponse)
      }
      catch (loadError) {
        if (!isMounted) {
          return
        }

        const cached = (await getCachedWorkoutDetail(workoutId)) as WorkoutDetailsResponse | null

        if (cached) {
          setWorkoutName(cached.name)
          setPrimaryMuscleGroups(cached.primaryMuscleGroups ?? [])
          setStartedAtMs(Date.now())
          setExercises([...cached.exercises].sort((a, b) => a.orderIndex - b.orderIndex).map(toSessExercise))
          setIsOfflineData(true)
          return
        }

        setError(loadError instanceof Error ? loadError.message : 'Failed to load workout details.')
      }
      finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    const loadWorkout = async () => {
      setIsLoading(true)
      setError(null)

      const sessdraft = getDraft<SessionDraft>(workoutId)
      if (sessdraft) {
        restoreDraft(sessdraft)
        await backfillDraft()
        return
      }

      const otherDraft = getDraftFromStorage()
      if (otherDraft && otherDraft.workoutId !== workoutId) {
        setConflictDraft(otherDraft)
        setIsLoading(false)
        return
      }

      await fetchWorkout()
    }

    void loadWorkout()

    return () => {
      isMounted = false
    }
  }, [isEditMode, workoutId, logId, startKey])

  useEffect(() => {
    if (isEditMode) {
      return
    }
    const interval = setInterval(() => setNowMs(Date.now()), 1000)
    return () => clearInterval(interval)
  }, [isEditMode])

  //autosave on any exercise change
  useEffect(() => {
    if (isEditMode) {
      return
    }
    if (workoutId && exercises.length > 0) {
      saveDraft<SessionDraft>(workoutId, { workoutId, workoutName, startedAtMs, logId, exercises, restTimer })
    }
  }, [isEditMode, workoutId, workoutName, startedAtMs, logId, exercises, restTimer])

  //listener on whole doc for any sort of clicks that link to other pages
  //prompts the keep/discard dialog 
  useEffect(() => {
    if (isEditMode || !workoutId || exercises.length === 0) {
      return
    }

    const interceptNavbar = (event: MouseEvent) => {
      if (event.defaultPrevented || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
        return
      }

      const anchor = (event.target as HTMLElement)?.closest('a[href]') as HTMLAnchorElement | null
      if (!anchor || (anchor.target && anchor.target !== '_self')) {
        return
      }

      const url = new URL(anchor.href, window.location.origin)
      if (url.origin !== window.location.origin || url.pathname === window.location.pathname) {
        return
      }

      event.preventDefault()

      setPendingNavTo(url.pathname + url.search)
      setExitOpen(true)
    }

    document.addEventListener('click', interceptNavbar, true)
    return () => document.removeEventListener('click', interceptNavbar, true)

  }, [isEditMode, workoutId, exercises.length])

  const secElaps = startedAtMs == null ? 0 : Math.max(0, Math.floor((nowMs - startedAtMs) / 1000))
  const restRem = restTimer ? Math.round((restTimer.endsAt - nowMs) / 1000) : null
  const restOT = restRem !== null && restRem < 0
  const restProg = restTimer && restRem !== null
    ? Math.min(100, Math.max(0, ((restTimer.totalSeconds - restRem) / restTimer.totalSeconds) * 100)) : 0

  const durationDisplay = useMemo(() => {
    if (!isEditMode) {
      return formattedTime(secElaps)
    }

    if (startedAtIso && completedAtIso) {
      const durSecs = Math.max(0, Math.round((new Date(completedAtIso).getTime() - new Date(startedAtIso).getTime()) / 1000))
      return formattedTime(durSecs)
    }

    if (pastDurationText) {
      return formatPastDurationText(pastDurationText)
    }

    return '--:--'
  }, [isEditMode, secElaps, startedAtIso, completedAtIso, pastDurationText])

  const handleSetCompleted = (exercise: ExerciseData, set: SetData) => {
    startRest(exercise, set)

    if (isEditMode || getSetPrKinds(exercise, set).length === 0) {
      return
    }

    setPrSetIds((current) => (current.includes(set.id) ? current : [...current, set.id]))
    void confetti({ particleCount: 120, spread: 70, origin: { y: 0.7 }, disableForReducedMotion: true })
  }

  const startRest = (exercise: ExerciseData, set: SetData) => {
    if (isEditMode) {
      return
    }

    const sec = exercise.groupId ? (exercise.groupRestTime ?? set.restTime) : set.restTime

    if (!sec || sec <= 0) {
      setRestTimer(null)
      return
    }

    setRestTimer(createRestTimer(sec, exercise.name))
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

  const removeSet = (exerciseId: string, setId: string) => {
    setExercises((currentExercises) =>
      currentExercises.map((exercise) => {
        if (exercise.id !== exerciseId) {
          return exercise
        }

        return {
          ...exercise,
          sets: exercise.sets.filter((set) => set.id !== setId),
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
        secondaryMuscles: [],
        bestWeight: null,
        bestSetVolume: null,
        groupId: null,
        groupType: null,
        groupRestTime: null,
        imageUrl: exercise.imageUrl ?? null,
        exerciseType: exercise.exerciseType ?? 'WeightReps',
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

  const highlightedMuscles = useMemo(
    () => primaryMuscleGroups.filter((muscle): muscle is MuscleName => MUSCLE_GROUPS.includes(muscle as MuscleName)),
    [primaryMuscleGroups]
  )

  const secondaryMuscles = useMemo(
    () =>
      exercises
        .flatMap((exercise) => exercise.secondaryMuscles ?? [])
        .filter((muscle): muscle is MuscleName => MUSCLE_GROUPS.includes(muscle as MuscleName)),
    [exercises]
  )


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
        orderIndex: exercisesLog.length + 1, groupNumber: groupNum, sets,
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

    const prs = detectPrs(exercises)

    if (prs.length === 1) {
      const [pr] = prs
      toast.success(
        pr.kind === 'weight' ? `${pr.exerciseName} - ${pr.value}kg` : `${pr.exerciseName} - ${pr.value.toLocaleString()}kg set volume`, 'New PR'
      )
    }
    else if (prs.length > 1) {
      toast.success(`${prs.length} new personal records this session.`, 'New PRs')
    }

    if (navigator.onLine) {
      await flushOutBox()
    }

    if (navigator.onLine) {
      toast.success('Workout saved.', 'Saved')
    } 
    else {
      toast.warning("Workout saved but will sync when you're back online.", 'Saved offline')
    }

    if (workoutId) {
      clearDraft(workoutId)
    }

    navigate('/workouts')
  }

    const keep = () => {
    if (workoutId) {
      saveDraft<SessionDraft>(workoutId, { workoutId, workoutName, startedAtMs, logId, exercises, restTimer })
    }

    navigate(pendingNavTo ?? '/workouts')
    setPendingNavTo(null)
  }

  const discard = () => {
    if (workoutId) {
      clearDraft(workoutId)
    }

    navigate(pendingNavTo ?? '/workouts')
    setPendingNavTo(null)
  }

  const savePastWorkoutEdits = async () => {
    if (!workoutId || !logId) {
      return
    }

    const groupNums = groupNumMap(exercises)
    const exercisesLog: WorkoutLogExercisePayload[] = []

    for (const exercise of exercises) {
      if (!exercise.exerciseId) continue

      const groupNum = groupNums.get(exercise.id) ?? 0
      const sets = buildSetPayloads(exercise.sets, groupNum)

      if (sets.length === 0) continue

      exercisesLog.push({
        exerciseId: exercise.exerciseId,
        workoutExerciseId: exercise.sourceWorkoutExerciseId,
        orderIndex: exercisesLog.length + 1,
        groupNumber: groupNum,
        sets,
      })
    }

    if (exercisesLog.length === 0) {
      toast.error('Workout log cannot be empty. Please complete at least one set.')
      return
    }

    const payload = {
      notes: null,
      startedAt: startedAtIso,
      completedAt: completedAtIso,
      exercises: exercisesLog,
    }

    setIsSaving(true)
    try {
      const resp = await customFetch(`/api/workouts/${workoutId}/logs/${logId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })

      if (!resp.ok) {
        throw new Error(`Failed to update workout log (${resp.status})`)
      }

      toast.success('Workout log updated successfully.', 'Saved')
      navigate(`/workouts/${workoutId}/logs/${logId}`)
    } catch (saveError) {
      toast.error(saveError instanceof Error ? saveError.message : 'Failed to update workout log.')
    } finally {
      setIsSaving(false)
    }
  }

  const blanks = () => exercises.some(exerciseGotBlanks)

  const allowedFinish = summary.completedSets > 0 && !blanks()

  const renderExerCard = (exercise: ExerciseData) => {

    const cols = getColumns(exercise.exerciseType)
    const gridTempMobile = `2.75rem ${cols.map(() => 'minmax(0, 1fr)').join(' ')} 4.5rem`
    const gridTemp = `3.5rem minmax(0, 1.5fr) ${cols.map(() => 'minmax(0, 1fr)').join(' ')} minmax(0, 0.8fr) 4rem`
    const setLabels = buildLabels(exercise.sets)

    return (
      <Card key={exercise.id} className="border-border bg-card shadow-sm rounded-xl overflow-hidden pt-4 pb-2">
        <CardHeader className="flex flex-row items-start justify-between pb-4 px-5 pt-0">
          <div className="flex items-center gap-4">
            <Avatar size="lg" className="shrink-0 bg-surface-2">
              {exercise.imageUrl ? <AvatarImage src={adaptImgUrl(exercise.imageUrl)} alt={exercise.name} /> : null}
              <AvatarFallback className="bg-surface-2 text-transparent" />
            </Avatar>
            <div>
              <button
                type="button"
                className="block text-left text-base font-bold leading-snug text-foreground cursor-pointer hover:underline disabled:cursor-default disabled:no-underline"
                disabled={!exercise.exerciseId || !isOnline}
                onClick={() => { if (exercise.exerciseId) setDetailsExerciseId(exercise.exerciseId) }}
              >
                {exercise.name}
              </button>
              <p className="text-sm text-muted-foreground">{exercise.muscleGroup}</p>
            </div>
          </div>
          <CardAction>
            <DropdownMenu>
              <DropdownMenuTrigger variant="plain" className="p-1 text-muted-foreground hover:text-foreground">
                <MoreHorizontal className="h-5 w-5" />
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-auto min-w-[10rem]">
                <DropdownMenuItem variant="destructive" onSelect={() => removeExercise(exercise.id)}>
                  Remove exercise
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </CardAction>
        </CardHeader>

        <CardContent className="px-5 pb-4">
          <div
            className="mb-2 grid gap-2 lg:gap-4 border-l-4 border-transparent p-1.5 text-center text-xs font-semibold tracking-wide text-muted-foreground [grid-template-columns:var(--set-cols-m)] lg:[grid-template-columns:var(--set-cols)]"
            style={{ ['--set-cols-m' as string]: gridTempMobile, ['--set-cols' as string]: gridTemp }}>
            <div>SET</div>
            <div className="hidden lg:block">PREVIOUS</div>
            {cols.map((col) => <div key={col.field}>{col.label}</div>)}
            <div className="hidden lg:block">RPE</div>
            <div />
          </div>

          <div className="space-y-2">
            {exercise.sets.map((set, setIndex) => (
              <SetRow
                key={set.id}
                set={set}
                setLabel={setLabels[setIndex]}
                columns={cols}
                gridTemplate={gridTemp}
                gridTemplateMobile={gridTempMobile}
                onUpdate={(updater) => updateSet(exercise.id, set.id, updater)}
                onRemove={() => removeSet(exercise.id, set.id)}
                isPR={prSetIds.includes(set.id)}
                onRestStart={() => handleSetCompleted(exercise, set)}
              />
            ))}
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
    <section className="mx-auto max-w-6xl px-6 pb-6 pt-20 lg:pt-6 lg:h-[calc(100dvh-5rem)] lg:overflow-hidden">
      <div className="grid grid-cols-12 gap-6 lg:h-full lg:min-h-0">
        <div className="col-span-12 lg:col-span-7 flex min-w-0 flex-col gap-6 lg:h-full lg:min-h-0">
          <div className="flex flex-col gap-2">
            <Button
              variant="text"
              size="sm"
              onClick={() => {
                if (isEditMode) {
                  navigate(`/workouts/${workoutId}/logs/${logId}`)
                } else {
                  setExitOpen(true)
                }
              }}
              className="flex items-center gap-1 self-start p-0 text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4" />
              <span>{isEditMode ? 'Back to Workout Log' : 'Back to Workouts'}</span>
            </Button>

            <div className="flex flex-col gap-3 lg:flex-row lg:items-end lg:justify-between lg:gap-4">
              <div className="flex items-center gap-3">
                <PageTitle title={workoutName} />
                {isEditMode && (
                  <span className="rounded bg-brand/10 px-2 py-0.5 text-xs font-semibold uppercase tracking-wider text-brand">
                    Editing Past Workout
                  </span>
                )}
              </div>

              <div className="flex items-center justify-between gap-4 text-center lg:justify-end lg:gap-6">
                <div>
                  <p className="text-xs font-semibold text-muted-foreground">Duration</p>
                  <p className="text-sm font-bold">{durationDisplay}</p>
                </div>
                <div>
                  <p className="text-xs font-semibold text-muted-foreground">Volume</p>
                  <p className="text-sm font-bold">{summary.totalVolume.toLocaleString()} kg</p>
                </div>
                <div>
                  <p className="text-xs font-semibold text-muted-foreground">Sets</p>
                  <p className="text-sm font-bold">{summary.completedSets}/{summary.totalSets}</p>
                </div>
                {isEditMode ? (
                  <OfflineTooltip isOnline={isOnline}>
                    <Button
                      variant="default"
                      size="sm"
                      className="h-8"
                      disabled={!allowedFinish || isSaving || !isOnline}
                      onClick={() => void savePastWorkoutEdits()}
                    >
                      {isSaving ? 'Saving...' : 'Save Changes'}
                    </Button>
                  </OfflineTooltip>
                ) : (
                  <Button variant="default" size="sm" className="h-8" disabled={!allowedFinish} onClick={() => void finishWorkout()}>
                    Finish
                  </Button>
                )}
              </div>
            </div>
          </div>

          {isLoading && (
            <div className="rounded-md border border-border bg-surface-2 px-4 py-3 text-sm text-muted-foreground">
              Loading workout session...
            </div>
          )}
          {error && (
            <div className="rounded-md border border-border bg-surface-2 px-4 py-3 text-sm text-destructive">
              {error}
            </div>
          )}

          {isOfflineData && (
            <OfflineBanner message="You're offline - this session is saved on your device and will sync when you reconnect." />
          )}

          <div className="lg:max-h-[calc(100dvh-15rem)] lg:overflow-y-auto lg:pr-1">
            <div className="flex flex-col gap-4">
              {buildSessionSegs(exercises).map((seg) => {
                if (seg.kind === 'single') {
                  return renderExerCard(seg.exercise)
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
                    {seg.members.map((member) => renderExerCard(member))}
                  </div>
                )
              })}

              {!isLoading && exercises.length === 0 && !error && (
                <p className="text-sm text-muted-foreground">No exercises in this workout yet.</p>
              )}

              <OfflineTooltip isOnline={isOnline} className="w-full">
                <Button
                  variant="outline"
                  className="w-full border-dashed border-border text-muted-foreground hover:text-foreground bg-transparent h-10 text-xs"
                  disabled={!isOnline}
                  onClick={() => setPickerOpen(true)}
                >
                  <Plus className="mr-2 h-3.5 w-3.5" /> Add Exercise
                </Button>
              </OfflineTooltip>
              {restTimer && <div aria-hidden className="h-24" />}
            </div>
          </div>
        </div>

        <div className="col-span-12 min-w-0 lg:col-span-5 lg:min-h-0">
          <Card className="flex h-full min-h-0 flex-col rounded-xl border-border bg-card">
            <CardHeader>
              <CardTitle className="text-[1.05rem] font-bold">Summary</CardTitle>
            </CardHeader>
            <CardContent className="flex min-h-0 flex-1 flex-col">
              <div className="exercise-summary-scroll flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto pr-2 text-sm text-muted-foreground">
                <MuscleDiagram highlightedMuscles={highlightedMuscles} secondaryMuscles={secondaryMuscles} variant="both" />
                <MusclesSummary exercises={exercises} />
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
            <ExerciseDetailsPopup
        exerciseId={detailsExerciseId}
        onClose={() => setDetailsExerciseId(null)}
      />
      <ExercisePickerDialog
        isOpen={isPickerOpen}
        onClose={() => setPickerOpen(false)}
        onSelect={selectedExercise}
      />
      {exitOpen && (
        <div className="fixed inset-x-0 bottom-0 top-0 lg:top-20 z-40 flex items-center justify-center p-4">
          <button type="button" aria-label="Stay" className="absolute inset-0 bg-foreground/50" onClick={() => setExitOpen(false)} />
          <div className="relative z-10 w-full max-w-sm rounded-2xl border border-border bg-card p-6 shadow-xl">
            <div className="flex items-start justify-between gap-4">
              <h2 className="text-lg font-bold text-foreground">Leave session?</h2>
              <button type="button" aria-label="Stay" onClick={() => setExitOpen(false)} className="text-muted-foreground hover:text-foreground">
                <X className="h-5 w-5" />
              </button>
            </div>
            <p className="mt-2 text-sm text-muted-foreground">
              Do you want to keep this session or discard it permanently?
            </p>
            <div className="mt-6 flex gap-3">
              <Button variant="secondary" className="flex-1" onClick={discard}>Discard</Button>
              <Button variant="default" className="flex-1" onClick={keep}>Keep</Button>
            </div>
          </div>
        </div>
      )}
      {conflictDraft && (
        <div className="fixed inset-x-0 bottom-0 top-20 z-50 flex items-center justify-center p-4">
          <button type="button" aria-label="Go back" className="absolute inset-0 bg-foreground/50" onClick={() => navigate(-1)} />
          <div className="relative z-10 w-full max-w-sm rounded-2xl border border-border bg-card p-6 shadow-xl">
            <div className="flex items-start justify-between gap-4">
              <h2 className="text-lg font-bold text-foreground">Session is already in progress</h2>
              <button type="button" aria-label="Go back" onClick={() => navigate(-1)} className="text-muted-foreground hover:text-foreground">
                <X className="h-5 w-5" />
              </button>
            </div>
            <p className="mt-2 text-sm text-muted-foreground">
              You already have an active session for{' '}
              <span className="font-semibold text-foreground">{conflictDraft.workoutName}</span>.
              You can resume it, or discard it and start {workoutName}?
            </p>
            <div className="mt-6 flex gap-3">
              <Button
                variant="secondary"
                className="flex-1"
                onClick={() => {
                  clearDraft(conflictDraft.workoutId)
                  setConflictDraft(null)
                  setStartKey((key) => key + 1)
                }}
              >
                Discard &amp; Start
              </Button>
              <Button
                variant="default"
                className="flex-1"
                onClick={() => {
                  const resume = conflictDraft
                  setConflictDraft(null)
                  navigate('/active-session', {
                    state: { workout: { id: resume.workoutId, name: resume.workoutName } },
                    replace: true,
                  })
                }}
              >
                Resume
              </Button>
            </div>
          </div>
        </div>
      )}
      {restTimer && restRem !== null && (
        <div className="fixed inset-x-0 bottom-0 z-[80] border-t-2 border-brand bg-background/95 backdrop-blur">
          <div className="h-1 w-full bg-surface-2">
            <div
              className={`h-full transition-[width] duration-1000 ease-linear ${restOT ? 'bg-warning' : 'bg-brand'}`}
              style={{ width: `${restProg}%` }}
            />
          </div>

          <div className="mx-auto flex max-w-6xl items-center gap-4 px-4 py-3 sm:px-6">
            <span className={`font-display text-[32px] leading-none tracking-[1px] tabular-nums ${restOT ? 'text-warning' : 'text-brand'}`}>
              {restOT ? '+' : ''}{formatClock(restRem)}
            </span>

            <div className="min-w-0 flex-1">
              <p className="text-xs font-semibold uppercase tracking-[1.5px] text-muted-foreground">
                {restOT ? 'Rest over' : 'Resting'}
              </p>
              <p className="truncate text-sm font-semibold text-foreground">{restTimer.exerciseName}</p>
            </div>

            <Button
              variant="icon"
              size="icon"
              aria-label="Dismiss rest timer"
              className="h-11 w-11 shrink-0 border-0 bg-transparent text-muted-foreground hover:text-foreground"
              onClick={() => setRestTimer(null)}
            >
              <X className="h-5 w-5" />
            </Button>
          </div>
        </div>
      )}
    </section>
  )
}