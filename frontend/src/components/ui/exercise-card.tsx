import { useState } from 'react'
import { ChevronDown, MoreHorizontal, User, X } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { NumericalUnderscoreInput, Input } from '@/components/ui/input'
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from '@/components/ui/dropdown-menu'
import type { WorkoutExercise, ExerciseSet, SetType } from '@/types/create-workout'
import { metricCheck } from '@/lib/weight-utils'
import { adaptImgUrl } from '@/lib/utils'
import { buildLabels } from '@/lib/exercise-format'

type ExerciseCardProps = Readonly<{
  exercise: WorkoutExercise
  restTime?: number
  onRemove: (id: string) => void
  onSetsChange: (id: string, sets: ExerciseSet[]) => void
  onRestTimeChange?: (id: string, value: number) => void
  onOpenDetails?: (exerciseCatalogId: string) => void
}>

const SET_TYPES: SetType[] = ['W', 'I', 'D']

const setTypeRowClass: Record<SetType, string> = {
  W: 'bg-warning/10 border-l-warning/50',
  I: 'bg-surface-2 border-l-border',
  D: 'bg-brand/10 border-l-brand/50',
}

//add functionality for different types of exercises
type FieldKey = 'kg'|'reps'|'time'|'distance'
type ColumnDef = {
  label: string
  field: FieldKey
}

export function getColumns(exerciseType: string): ColumnDef[] {
  const isMetric = metricCheck()
  const weightUnit = isMetric? 'KG':'LB'
  const plus = isMetric ? '+KG': '+LB'
  const minus = isMetric ? '-KG': '-LB'

const COLUMNSTYPE: Record<string, ColumnDef[]> = {
  'WeightReps': [
    {label: weightUnit, field: 'kg'},
    { label: 'Reps', field: 'reps'},
  ],
  'BodyweightReps': [
    { label: 'Reps', field: 'reps'},
  ],
  'WeightedBodyWeight': [
    {label: plus, field: 'kg'},
    { label: 'Reps', field: 'reps'},
  ],
  'AssistedWeightReps': [
    {label: minus, field: 'kg'},
    { label: 'Reps', field: 'reps'},
  ],
  'Duration': [
    { label: 'Time(s)', field: 'time'},
  ],
  'DurationWeight': [
    {label: weightUnit, field: 'kg'},
    { label: 'Time(s)', field: 'time'},
  ],
  'DistanceDuration': [
    {label: 'KM', field: 'distance'},
    { label: 'Time(s)', field: 'time'},
  ],
  'WeightDistance': [
    {label: weightUnit, field: 'kg'},
    { label: 'KM', field: 'distance'},
  ],
}
return COLUMNSTYPE[exerciseType] ?? COLUMNSTYPE['WeightReps']
}

function SetRow({
  set,
  setLabel,
  columns,
  onChange,
  onRemove,
}: Readonly<{
  set: ExerciseSet
  setLabel: string
  columns: ColumnDef[]
  onChange: (updated: ExerciseSet) => void
  onRemove: () => void
}>) {

  return (
    <div className={`flex items-center rounded-lg border-y border-r border-l-4 border-border px-3 py-2 gap-4 ${setTypeRowClass[set.type]}`}>
      <div className="flex items-center w-20 shrink-0">
        <DropdownMenu>
          <DropdownMenuTrigger variant="plain">
            <ChevronDown className="w-4 h-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-auto min-w-[9rem]">
            {SET_TYPES.map(t => {
              let label = 'Drop'
              if (t === 'W') label = 'Warmup'
              else if (t === 'I') label = 'Working'
              return (
                <DropdownMenuItem key={t} onClick={() => onChange({ ...set, type: t })}>
                  {label}
                </DropdownMenuItem>
              )
            })}
          </DropdownMenuContent>
        </DropdownMenu>
        <Input
          readOnly
          value={setLabel}
          className="w-8 h-8 text-center text-sm font-bold px-0"
        />
      </div>

      {columns.map(col => {
        const val = set[col.field] === undefined || set[col.field] === '' ? '': String(set[col.field])
        return (
          <div key={col.field} className="flex-1 flex justify-center [&>div]:flex [&>div]:justify-center">
            <NumericalUnderscoreInput value={val} onChange={e => {
              const newVal = e.target.value === ''? '': Number(e.target.value)
              onChange({...set, [col.field]: newVal})
            }}
            className="text-xl text-center mx-auto"/>
            </div>
        )
      })}

      <Button variant="icon" size="icon" aria-label="Remove set" onClick={onRemove} className="border-0 bg-transparent w-6 h-6 shrink-0">
        <X className="w-4 h-4 text-muted-foreground" />
      </Button>
    </div>
  )
}

let nextSetId = 0

export function ExerciseCard({ exercise, restTime, onRemove, onSetsChange, onRestTimeChange, onOpenDetails }: ExerciseCardProps) {
  const [sets, setSets] = useState<ExerciseSet[]>(exercise.sets)

  const columns = getColumns(exercise.exerciseType ?? 'WeightReps')
  const setLabels = buildLabels(sets)

  const updateSets = (updated: ExerciseSet[]) => {
    setSets(updated)
    onSetsChange(exercise.id, updated)
  }

  const addSet = () => {
    const newSet: ExerciseSet = {
      id: `set-${nextSetId++}`,
      type: 'I',
      kg: '',
      reps: '',
      time: '',
      distance: '',
    }
    updateSets([...sets, newSet])
  }

  const updateSet = (index: number, updated: ExerciseSet) =>
    updateSets(sets.map((s, i) => i === index ? updated : s))

  const removeSet = (index: number) =>
    updateSets(sets.filter((_, i) => i !== index))

  return (
    <div className="rounded-xl border border-border bg-surface overflow-hidden">
      <div className="flex items-center gap-3 px-4 py-3">
        <Avatar size="lg">
          {exercise.imageUrl
            ? <AvatarImage src={adaptImgUrl(exercise.imageUrl)} alt={exercise.name} />
            : null}
          <AvatarFallback className="bg-surface-2">
            <User className="w-5 h-5 text-muted-foreground" />
          </AvatarFallback>
        </Avatar>

        <div className="flex flex-col flex-1 min-w-0">
          <button
            type="button"
            className="block w-fit max-w-full truncate text-left font-sans font-semibold text-sm text-foreground leading-tight cursor-pointer hover:underline disabled:cursor-default disabled:no-underline"
            disabled={!onOpenDetails || !exercise.exerciseCatalogId}
            onClick={() => { if (exercise.exerciseCatalogId) onOpenDetails?.(exercise.exerciseCatalogId) }}
          >
            {exercise.name}
          </button>
          <span className="font-sans text-xs text-muted-foreground">
            {exercise.muscle}
          </span>
        </div>

        {onRestTimeChange && (
          <label className="flex items-center gap-1 text-xs text-muted-foreground whitespace-nowrap">
            <span>Rest (seconds)</span>
            <input 
              type = "number"
              min = {0}
              value = {restTime || ''}
              placeholder="0"
              onChange = {e => onRestTimeChange(exercise.id, Number(e.target.value))}
              className="w-16 rounded-md border border-border bg-surface-2 px-2 py-1 text-center text-foreground [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
            />
          </label>
        )}

        <DropdownMenu>
          <DropdownMenuTrigger variant="plain" className="p-1">
            <MoreHorizontal className="w-4 h-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-auto min-w-[10rem]">
            <DropdownMenuItem variant="destructive" onClick={() => onRemove(exercise.id)}>
              Remove exercise
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className="border-t border-border" />

      <div className="px-4 py-3 flex flex-col gap-2">
        <div className="flex items-center gap-4 text-xs font-bold uppercase tracking-[1px] text-muted-foreground font-sans px-3">
          <div className='flex items-center w-20 shrink-0'>
            <span className="w-4 shrink-0"/>
            <span className="w-8 text-center">Set</span>
          </div>
          {columns.map(col => (
            <span key={col.field} className="flex-1 text-center">{col.label}</span>
          ))}
          <span className="w-6 shrink-0" />
        </div>
        {sets.map((set, i) => (
          <SetRow
            key={set.id}
            set={set}
            setLabel={setLabels[i]}
            columns={columns}
            onChange={updated => updateSet(i, updated)}
            onRemove={() => removeSet(i)}
          />
        ))}

      </div>
        <div className="px-4 py-3">
        <Button variant="outline" size="sm" className="w-full" onClick={addSet}>
          + Add Set
        </Button>
      </div>
      <div className="border-t border-border" />
    </div>
  )
}
