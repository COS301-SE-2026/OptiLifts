import { useState, useEffect, useCallback, Fragment } from 'react'
import { useAuth } from '@/context/auth-context'
import { useNavigate, useParams } from 'react-router-dom'
import { Plus, Dumbbell, Link2, ArrowLeft, AlertCircle} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ExerciseCard } from '@/components/ui/exercise-card'
import { PageTitle } from '@/components/ui/page-title'
import { CreateExercise } from '@/components/ui/create-exercise'
import { SearchInput } from '@/components/ui/search-input'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { ExerciseDetailsPopup } from '@/components/ui/exercise-details-popup'
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { WorkoutExercise, SetType, ExerciseSet } from '@/types/create-workout'
import type { MuscleName } from '@/types/workout'
import { customFetch } from '@/lib/custom-fetch'
import { inputWeight, outputWeight } from '@/lib/weight-utils'
import { MUSCLE_GROUPS } from '@/constants/muscles'
import { DEFAULT_EQUIPMENT_OPTIONS } from '@/constants/equipment'

type CatalogExercise = {
  id: string
  name: string
  muscleGroup: MuscleName
  equipment?: string
  imageUrl?: string
  exerciseType?: string
}

type ExerciseApiResponse = {
  id: string
  name: string
  primaryMuscles?: string[]
  equipment?: string
  imageUrl?: string
  exerciseType?: string
  category?: string
}

type CreateWorkoutSetPayload = {
  type: string
  reps: number | null
  weight: number | null
  duration: number | null
  distance: number | null
  orderIndex: number
  restTime: number
}

type CreateWorkoutExercisePayload = {
  exerciseId: string
  orderIndex: number
  groupKey: string | null
  sets: CreateWorkoutSetPayload[]
}

type CreateWorkoutGroupPayload = {
  groupKey: string
  type: string
  restTime: number
}

type CreateWorkoutPayload = {
  name: string
  folderId: string | null
  exercises: CreateWorkoutExercisePayload[]
  groups: CreateWorkoutGroupPayload[]
}

type SelectedWorkoutExercise = WorkoutExercise & {
  exerciseCatalogId?: string
  linkedToNext?: boolean
  restTime?: number
}

const DEFAULT_REST = 0

type WorkoutSegment = 
  | { kind: 'single'; exercise: SelectedWorkoutExercise; index: number }
  | { kind: 'group'; anchorId: string; members: Array<{ exercise: SelectedWorkoutExercise; index: number }> } 

// Turns the list of segments into a group of linked exercises
function buildSegs(exercises: SelectedWorkoutExercise[]): WorkoutSegment[] {
  const segments: WorkoutSegment[] = []
  let i = 0
  while (i < exercises.length) {
    let j = i
    while (j < exercises.length - 1 && exercises[j].linkedToNext) j++
    if (j > i) {
      segments.push({
        kind: 'group',
        anchorId: exercises[i].id,
        members: exercises.slice(i, j + 1).map((exercise, k) => ({ exercise, index: i + k})),
      })
    } else {
      segments.push({ kind: 'single', exercise: exercises[i], index: i})
    }
    i = j + 1
  }
  return segments
}

const MUSCLE_OPTIONS = ['All Muscles', ...MUSCLE_GROUPS] as const

function ChainLink({ linked, onClick}: Readonly<{ linked: boolean; onClick: () => void }>) {
  return(
    <div className="flex justify-center">
      <button 
        type="button" 
        onClick={onClick}
        aria-label={linked ? 'Unlink exercises' : 'Link exercises'}
        className={`flex h-7 w-7 cursor-pointer items-center justify-center rounded-full border transition-colors ${
          linked ? 'border-brand bg-brand/10 text-brand' :
            'border-border bg-surface-2 text-muted-foreground hover:text-foreground'
        }`}
      >
        <Link2 className="h-4 w-4" />
      </button>
    </div>
  )
}

let nextExerciseId = 0

const SET_TYPE_MAP: Record<SetType, string> = { W: 'Warmup', I: 'Normal', D: 'Dropset'}

async function getErrorMessage(res: Response, fallback: string): Promise<string> {
  try {
    if ((res.headers.get('content-type') ?? '').includes('application/json')) {
      const data = await res.json()
      const msg = data?.message ?? data?.detail ?? data?.title    
      if (typeof msg === 'string' && msg.trim()) return msg
    }
  }
  catch {
    // body wasn't JSON or couldn't be read; fall through to status-based message
  }
  switch (res.status) {
    case 400: return "Some workout details are invalid. Please check and try again."
    case 401: return "Your session has expired. Please login again."
    case 403: return "You do not have permission to do that."
    case 404: return "Workout Service not found"
    case 409: return "You already have a workout with these details."
    case 500: return "Something went wrong on our end. Please try again in a few minutes"
    default: return fallback
  }
}

//sonarqube nesting restrictions
function mapApiSetsToExercise(apiSets: Array<{id: string; type:string; weight:number | null; reps: number | null 
  duration?: number | null
  distance?: number | null
}>): ExerciseSet[]{
  return apiSets.map(s => {
    let setType: SetType = 'I'
    if (s.type === 'Warmup'){
      setType = 'W'
    } else if (s.type === 'DropSet' || s.type === 'Dropset'){
      setType = 'D';
    }
    return {
      id: s.id,
      type: setType,
      kg: (s.weight === null ? '' : outputWeight(s.weight)) as number | '',
      reps: (s.reps ?? '') as number | '',
      time: (s.duration ?? '') as number | '',
      distance: (s.distance ?? '') as number | ''
    }
  })
}
function mapApiExercises(
  apiExercises: Array<{
    id: string
    exerciseId: string
    name: string
    primaryMuscle: string
    exerciseType: string
    sets: Array<{
      id: string
      type: string
      weight: number | null
      reps: number | null
      restTime: number
      duration?: number | null
      distance?: number | null
    }>
    groupId?: string | null
    imageUrl?:string | null
  }>
): SelectedWorkoutExercise[] {
  return apiExercises.map((ex,idx,arr) => {
    const linkedToNext = idx < arr.length-1 && !!ex.groupId && ex.groupId === arr[idx+1].groupId
    const sets = mapApiSetsToExercise(ex.sets)
  return {
    id: ex.id,
    name: ex.name,
    muscle: ex.primaryMuscle as MuscleName,
    imageUrl: ex.imageUrl ?? undefined,
    sets,
    exerciseCatalogId: ex.exerciseId,
    linkedToNext,
    restTime: ex.sets[0]?.restTime ?? 60,
    exerciseType: ex.exerciseType
  }
})
}

function MemListOfGroups({
  members,
  onRemove,
  onSetsChange,
  onOpenDetails,
  onToggleLink,
}: Readonly<{
  members: Array<{ exercise: SelectedWorkoutExercise; index: number }>
  onRemove: (id: string) => void
  onSetsChange: (id: string, sets: WorkoutExercise['sets']) => void
  onOpenDetails: (exerciseCatalogId: string) => void
  onToggleLink: (index: number) => void
}>) {
  return (
    <>
      {members.map((m, mi) => (
        <Fragment key={m.exercise.id}>
          <ExerciseCard exercise={m.exercise} onRemove={onRemove} onSetsChange={onSetsChange} onOpenDetails={onOpenDetails} />
          {mi < members.length - 1 && (
            <ChainLink linked onClick={() => onToggleLink(m.index)} />
          )}
        </Fragment>
      ))}
    </>
  )
}


export default function CreateWorkoutPage() {
  const navigate = useNavigate()
  const [workoutName, setWorkoutName] = useState('')
  const [exercises, setExercises] = useState<SelectedWorkoutExercise[]>([])
  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [selectedMuscle, setSelectedMuscle] = useState<(typeof MUSCLE_OPTIONS)[number]>('All Muscles')
  const [selectedEquipment, setSelectedEquipment] = useState<string>('All Equipment')
  const [searchQuery, setSearchQuery] = useState('')
  const [detailsExerciseId, setDetailsExerciseId] = useState<string | null>(null)
  const [allExercises, setAllExercises] = useState<CatalogExercise[]>([])
  const [loadingExercises, setLoadingExercises] = useState(true)
  const [exercisesError, setExercisesError] = useState<string | null>(null)
  const { isAuthenticated } = useAuth()
  const [groupSettings, setGroupSettings] = useState<Record<string, { restTime: number }>>({})

  //edit workout
  const {id: workoutId} = useParams<{id: string}>()
  const isEdit = !!workoutId
  const [loadingWorkout, setLoadingWorkout] = useState(isEdit)
  const [loadError, setLoadError] = useState<string | null>(null)
  useEffect(()=>{
    if (!isEdit || !workoutId) return
    const loadWorkoutDetails = async () => {
      setLoadingWorkout(true)
      setLoadError(null)
      try {
        const res = await customFetch(`/api/workouts/${workoutId}`)
        if(!res.ok){
          if (res.status === 404) {
            throw new Error('Workout not found')
          }
          throw new Error(`Failed to load workout details (${res.status})`)
        }
        const workout = (await res.json()) as {
          id: string
          name: string
          exercises: Array<{
            id: string
            exerciseId: string
            name: string
            primaryMuscle: string
            exerciseType: string
            sets: Array<{
              id: string
              type: string
              weight: number | null
              reps: number | null
              restTime: number
            }>
            groupId?:string | null
            groupType?:string | null
            groupRestTime?: number | null
            imageUrl?:string | null
          }>
        }
        setWorkoutName(workout.name)

        const newGroupSettings: Record<string, {restTime: number}> = {}
        workout.exercises.forEach((ex,idx,arr) => {
          if (ex.groupId){
            if (idx === 0 || arr[idx-1].groupId !== ex.groupId){ //the anchor
              newGroupSettings[ex.id] = {restTime: ex.groupRestTime ?? DEFAULT_REST}
            }
          }
        })
        setGroupSettings(newGroupSettings)

        const mappedExercises = mapApiExercises(workout.exercises)
        setExercises(mappedExercises)
      } catch (err){
        setLoadError(err instanceof Error ? err.message : 'Failed to load details of workout')
      } finally {
        setLoadingWorkout(false)
      }
    }
    void loadWorkoutDetails()
  }, [isEdit, workoutId])

  const toggleLink = (index: number) =>
    setExercises(prev => prev.map((e, i) => (i === index ? { ...e, linkedToNext: !e.linkedToNext } : e)))

  const setGroupSetting = (anchorId: string, field: 'restTime', value: number) =>
    setGroupSettings(prev => {
      const current = prev[anchorId] ?? { restTime: DEFAULT_REST }
      return { ...prev, [anchorId]: { ...current, [field]: value } }
    })

  const updateExerciseRestTime = (id: string, value: number) =>
    setExercises(prev => prev.map(e => (e.id === id ? { ...e, restTime: value } : e)))

  const fetchExercises = useCallback(async () => {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' }
    const res = await customFetch('/api/exercises', { headers })

    if (!res.ok) {
      if (res.status === 401 || res.status === 403) throw new Error('Unauthorized - please sign in')
      if (res.status === 404) throw new Error('Endpoint not found (404) - is the API running?')
      throw new Error(`HTTP ${res.status}`)
    }

    const json = (await res.json()) as ExerciseApiResponse[]
    return json.map((ex) => ({
      id: ex.id,
      name: ex.name,
      muscleGroup: (ex.primaryMuscles?.[0] || 'Other') as MuscleName,
      equipment: ex.equipment,
      imageUrl: ex.imageUrl,
      exerciseType: ex.exerciseType ?? ex.category
    })) as CatalogExercise[]
  }, [])

  useEffect(() => {
    let mounted = true
    const loadExercises = async () => {
      setLoadingExercises(true)
      setExercisesError(null)

      try {
        const data = await fetchExercises()
        if (!mounted) return
        setAllExercises(data || [])
      } catch (err) {
        if (!mounted) return
        setExercisesError(err instanceof Error ? err.message : 'Failed to load exercises')
      } finally {
        if (mounted) setLoadingExercises(false)
      }
    }

    void loadExercises()

    return () => {
      mounted = false
    }
  }, [fetchExercises])

  const removeExercise = (id: string) =>
    setExercises(prev => prev.filter(e => e.id !== id))

  const updateSets = (id: string, sets: WorkoutExercise['sets']) =>
    setExercises(prev => prev.map(e => e.id === id ? { ...e, sets } : e))

  const addExercise = (exercise: CatalogExercise) =>
    setExercises(prev => [
      ...prev,
      {
        id: `ex-${nextExerciseId++}`,
        name: exercise.name,
        muscle: exercise.muscleGroup,
        sets: [],
        exerciseCatalogId: exercise.id,
        exerciseType: exercise.exerciseType
      },
    ])

  const handleExerciseSaved = async () => {
    const refreshedExercises = await fetchExercises()
    setAllExercises(refreshedExercises || [])
  }

  const saveWorkout = async () => {
    if (!workoutName.trim() || !isAuthenticated) return
    
    const segments = buildSegs(exercises)

    const groups: CreateWorkoutGroupPayload[] = segments.flatMap(seg => {
      if (seg.kind !== 'group') return []
      const settings = groupSettings[seg.anchorId] ?? { restTime: DEFAULT_REST }
      return [{
        groupKey: seg.anchorId,
        type: seg.members.length === 2 ? 'Superset' : 'Circuit',
        restTime: settings.restTime,
      }]
    })

    const groupKeyByExerciseId = new Map<string, string>()
    for (const seg of segments) {
      if (seg.kind === 'group') {
        for (const m of seg.members) groupKeyByExerciseId.set(m.exercise.id, seg.anchorId)
      }
    }

    const payload: CreateWorkoutPayload = {
      name: workoutName.trim(),
      folderId: null,
      exercises: exercises
        .filter(e => e.exerciseCatalogId)
        .map((e, exerciseIndex) => ({
          exerciseId: e.exerciseCatalogId as string,
          orderIndex: exerciseIndex,
          groupKey: groupKeyByExerciseId.get(e.id) ?? null,
          sets: e.sets.map((s, setIndex) => ({
            type: SET_TYPE_MAP[s.type] ?? 'Normal',
            reps: s.reps === '' ? null : Number(s.reps),
            weight: s.kg === '' ? null : inputWeight(Number(s.kg)),
            duration: s.time === undefined || s.time === '' ? null : Number(s.time),
            distance: s.distance === undefined || s.distance === '' ? null : Number(s.distance),
            orderIndex: setIndex,
            restTime: e.restTime ?? 0,
          })),
        })),
        groups,
    }

    setSaving(true)
    setSaveError(null)
    
    try {
      const url = isEdit ? `/api/workouts/${workoutId}` : '/api/workouts'
      const method = isEdit ? 'PUT' : 'POST'

      const res = await customFetch(url, {
        method,
        headers: { 'Content-Type': 'application/json'},
        body: JSON.stringify(payload),
      })

      if (!res.ok) {
        setSaveError(await getErrorMessage(res, `Failed to create workout (${res.status})`))
        return
      }

      navigate('/workouts')
    } catch (error) {
      setSaveError(error instanceof Error ? error.message : 'Failed to save workout')
    } finally {
      setSaving(false)
    }
  }
  
  const [isCreateExerciseOpen, setIsCreateExerciseOpen] = useState(false)

  const EQUIPMENT_OPTIONS = ['All Equipment', ...DEFAULT_EQUIPMENT_OPTIONS]

  const filteredExercises = allExercises.filter((ex) => {
    const q = searchQuery.trim().toLowerCase()
    if (q && !ex.name.toLowerCase().includes(q) && !ex.muscleGroup.toLowerCase().includes(q)) return false
    if (selectedMuscle !== 'All Muscles' && ex.muscleGroup !== selectedMuscle) return false
    if (selectedEquipment !== 'All Equipment' && ex.equipment?.toLowerCase() !== selectedEquipment.toLowerCase()) return false
    return true
  })

  const exercisesListContent = (() => {
    if (loadingExercises) {
      return <p className="px-2 py-3 text-sm text-muted-foreground">Loading exercises...</p>
    }

    if (exercisesError) {
      return <p className="px-2 py-3 text-sm text-destructive">{exercisesError}</p>
    }

    if (filteredExercises.length === 0) {
      return <p className="px-2 py-3 text-sm text-muted-foreground">No exercises match your filters.</p>
    }

    return filteredExercises.map((ex) => (
      <div key={ex.id} className="flex items-center gap-3 px-2 py-2.5">
        <CircularProfileImage
          src={ex.imageUrl}
          alt={ex.name}
          className="size-9 shrink-0 border-border"
          fallbackIcon={<Dumbbell className="size-4 text-muted-foreground" />}
        />
        <div className="min-w-0 flex-1">
          <button
            type="button"
            className="block w-fit max-w-full truncate text-left text-sm font-semibold text-foreground cursor-pointer hover:underline"
            onClick={() => setDetailsExerciseId(ex.id)}
            aria-label={`View details for ${ex.name}`}
          >
            {ex.name}
          </button>
          <div className="text-xs text-muted-foreground">{ex.muscleGroup} • {ex.equipment}</div>
        </div>
        <Button type="button" variant="icon" size="icon" aria-label={`Add ${ex.name}`} onClick={() => addExercise(ex)} className="size-6 rounded-md border-border bg-surface-2 text-foreground hover:bg-border">
          <Plus size={12} />
        </Button>
      </div>
    ))
  })()

  if (isEdit && loadingWorkout) {//edit page check   
    return (
      <section className="mx-auto flex min-h-[calc(100dvh-4rem)] max-w-5xl items-center justify-center px-6 py-16">
        <p className="text-sm uppercase tracking-[0.2em] text-muted-foreground animate-pulse">Loading workout details</p>
      </section>
    )
  }
  if (!isEdit && loadError){
    return (
      <section className="mx-auto flex min-h-[calc(100dvh-4rem)] max-w-5xl flex-col items-center justify-center px-6 py-16 gap-4">
        <p className="text-sm uppercase tracking-[0.2em] text-destructive font-semibold">Failed to load workout</p>
        <p className="text-muted-foreground">{loadError}</p>
        <Button onClick={() => navigate('/workouts')}>Back to Workouts</Button>
      </section>
    )
  }

  return (
    <section className="mx-auto max-w-6xl px-6 py-6 lg:h-[calc(100dvh-5rem)] lg:overflow-hidden">
      <div className="grid grid-cols-12 gap-6 lg:h-full lg:min-h-0">
        <div className="col-span-12 lg:col-span-7 flex min-w-0 flex-col gap-6 lg:h-full lg:min-h-0">

          <div className="flex items-center justify-between">
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
              <PageTitle title={isEdit ? "Edit Workout" : "Create Workout"}/>
              <div className="flex items-center gap-3">
                <div className="flex flex-col gap-1 w-80">
                  <label htmlFor="workout-name" className="text-xs font-semibold uppercase tracking-[1px] text-muted-foreground font-sans">
                    Workout Name
                  </label>
                  <Input
                    id="workout-name"
                    variant="default"
                    placeholder="e.g. Push Day A"
                    value={workoutName}
                    onChange={e => setWorkoutName(e.target.value)}
                  />
                </div>
                <Button variant="default" size="sm" className="self-end h-8" disabled={!workoutName.trim() || saving} onClick={saveWorkout}>
                  {saving ? 'Saving…' : 'Save Workout'}
                </Button>
              </div>
              {saveError && <p className="text-sm text-destructive">{saveError}</p>}
            </div>
          </div>
          <div className="max-h-[calc(100dvh-15rem)] overflow-y-auto pr-1">
          <div className="flex flex-col gap-3">
            {buildSegs(exercises).map((seg, si, segs) => {
              const lastIndex = seg.kind === 'single' ? seg.index : seg.members.at(-1)!.index
              const chainAfter = si < segs.length - 1
                ? <ChainLink linked={false} onClick={() => toggleLink(lastIndex)} />
                : null

              if (seg.kind === 'single') {
                return (
                  <Fragment key={seg.exercise.id}>
                    <ExerciseCard exercise={seg.exercise} restTime={seg.exercise.restTime} onRemove={removeExercise} 
                    onSetsChange={updateSets} onRestTimeChange={updateExerciseRestTime} onOpenDetails={setDetailsExerciseId} />
                    {chainAfter}
                  </Fragment>
                )
              }

              const settings = groupSettings[seg.anchorId] ?? { restTime: DEFAULT_REST }
              const type = seg.members.length ===  2 ? 'Superset' : 'Circuit'
              const unequalSets = new Set(seg.members.map(m => m.exercise.sets.length)).size > 1

              return(
                <Fragment key={seg.anchorId}>
                  <div className="flex flex-col gap-2 rounded-xl border-2 border-brand/60 bg-brand/5 p-2">
                    <div className="flex items-center justify-between px-2 pt-1">
                      <div className="flex items-center gap-1.5">
                        <span className="text-xs font-bold uppercase tracking-[1px] text-brand">{type}</span>
                        {unequalSets && (
                          <span className="group relative flex cursor-pointer">
                            <AlertCircle className="h-4 w-4" />
                            <span className="pointer-events-none absolute left-0 top-full z-20 mt-1 hidden 
                              w-52 rounded-md border border-border bg-surface px-2 py-1 text-xs font-normal 
                              normal-case leading-snug tracking-normal text-foreground shadow-md group-hover:block"
                              >
                              Exercises in a Superset or Circuit must have equal number of sets
                            </span>
                          </span>
                        )}
                      </div>
                      <div className="flex items-center gap-3 text-xs text-muted-foreground">
                        <label className="flex items-center gap-1">
                          <span>Rest (seconds)</span>
                          <input 
                            type = "number"
                            min = {0}
                            value = {settings.restTime || ''}
                            placeholder="0"
                            onChange = {e => setGroupSetting(seg.anchorId, 'restTime', Number(e.target.value))}
                            className="w-16 rounded-md border border-border bg-surface-2 px-2 py-1 text-center text-foreground [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
                          />
                        </label>
                      </div>
                    </div>
                    <MemListOfGroups
                      members={seg.members}
                      onRemove={removeExercise}
                      onSetsChange={updateSets}
                      onOpenDetails={setDetailsExerciseId}
                      onToggleLink={toggleLink}
                    />
                  </div>
                  {chainAfter}
                </Fragment>
              )
            })}
            {exercises.length === 0 && (
              <p className="text-sm text-muted-foreground">No exercises have been added. Use the panel on the right to add exercises.</p>
            )}
          </div>
          </div>
        </div>

        <div className="col-span-12 lg:col-span-5 min-w-0">
          <div className="flex w-full flex-col gap-4 lg:sticky lg:top-[1.5rem] lg:max-h-[calc(100dvh-6.5rem)] lg:overflow-y-auto lg:[scrollbar-gutter:stable]">
            <Card className="w-full overflow-hidden border-border bg-card">
            <CardHeader className="px-4 py-1">
              <div className="flex items-center justify-between gap-3">
                <CardTitle className="text-base font-bold text-foreground">Exercises</CardTitle>
                <Button type="button" variant="text" className="h-auto p-0 text-xs font-semibold normal-case tracking-normal text-brand hover:text-brand-2" onClick={() => setIsCreateExerciseOpen(true)}>
                  + Create Exercise
                </Button>
              </div>
            </CardHeader>
            <CardContent className="flex min-h-0 flex-col gap-2 px-4 pb-4">
              <DropdownMenu>
                <DropdownMenuTrigger variant="filter" className="w-full shadow-none">
                  <span>{selectedMuscle}</span>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width] max-h-64 overflow-y-auto">
                  {MUSCLE_OPTIONS.map(o => (
                    <DropdownMenuItem key={o} onSelect={() => setSelectedMuscle(o)}>{o}</DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>

              <DropdownMenu>
                <DropdownMenuTrigger variant="filter" className="w-full shadow-none">
                  <span>{selectedEquipment}</span>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-[--radix-dropdown-menu-trigger-width] max-h-64 overflow-y-auto">
                  {EQUIPMENT_OPTIONS.map(o => (
                    <DropdownMenuItem key={o} onSelect={() => setSelectedEquipment(o)}>{o}</DropdownMenuItem>
                  ))}
                </DropdownMenuContent>
              </DropdownMenu>

              <div className="[&>div]:max-w-none [&>div]:w-full">
                <SearchInput
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                  placeholder="Search"
                  aria-label="Search exercises"
                  className="h-8 w-full"
                />
              </div>

              <div className="mt-2 min-h-0 max-h-72 overflow-y-auto pr-1">
                <div className="divide-y divide-border/70">
                {exercisesListContent}
                </div>
              </div>
            </CardContent>
          </Card>

        </div>
        </div>
      </div>
      <ExerciseDetailsPopup
        exerciseId={detailsExerciseId}
        onClose={() => setDetailsExerciseId(null)}
        onChanged={handleExerciseSaved}
      />
      <CreateExercise
        isOpen={isCreateExerciseOpen}
        onCancel={() => setIsCreateExerciseOpen(false)}
        onSaved={handleExerciseSaved}
      />
    </section>
  )
}
