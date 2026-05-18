import { useState } from 'react'
import { MoreHorizontal, User, X } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { NumericalUnderscoreInput, Input } from '@/components/ui/input'
import { ChevronDown } from 'lucide-react'
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from '@/components/ui/dropdown-menu'
import type { WorkoutExercise, ExerciseSet, SetType } from '@/types/create-workout'

type ExerciseCardProps = Readonly<{
  exercise: WorkoutExercise
  onRemove: (id: string) => void
  onSetsChange: (id: string, sets: ExerciseSet[]) => void
}>

const SET_TYPES: SetType[] = ['W', 'I', 'D']

function SetRow({
  set,
  workingIndex,
  onChange,
  onRemove,
}: {
  set: ExerciseSet
  workingIndex: number
  onChange: (updated: ExerciseSet) => void
  onRemove: () => void
}) {
  return (
    <div className="flex items-center rounded-lg border border-border bg-surface-2 px-3 py-2 gap-4">
      <div className="flex items-center w-20 shrink-0">
        <DropdownMenu>
          <DropdownMenuTrigger variant="plain">
            <ChevronDown className="w-4 h-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            {SET_TYPES.map(t => (
              <DropdownMenuItem key={t} onClick={() => onChange({ ...set, type: t })}>
                {t === 'W' ? 'Warmup' : t === 'I' ? 'Working' : 'Drop'}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        <Input
          readOnly
          value={set.type === 'I' ? workingIndex : set.type}
          className="w-8 h-8 text-center text-sm font-bold px-0"
        />
      </div>

      <div className="flex-1 text-center">
        <NumericalUnderscoreInput
          value={set.kg === '' ? '' : String(set.kg)}
          onChange={e => onChange({ ...set, kg: e.target.value === '' ? '' : Number(e.target.value) })}
          className="text-xl text-center mx-auto"
        />
      </div>

      <div className="flex-1 text-center">
        <NumericalUnderscoreInput
          value={set.reps === '' ? '' : String(set.reps)}
          onChange={e => onChange({ ...set, reps: e.target.value === '' ? '' : Number(e.target.value) })}
          className="text-xl text-center mx-auto"
        />
      </div>

      <Button variant="icon" size="icon" aria-label="Remove set" onClick={onRemove} className="border-0 bg-transparent w-6 h-6 shrink-0">
        <X className="w-4 h-4 text-muted-foreground" />
      </Button>
    </div>
  )
}

let nextSetId = 0

export function ExerciseCard({ exercise, onRemove, onSetsChange }: ExerciseCardProps) {
  const [sets, setSets] = useState<ExerciseSet[]>(exercise.sets)

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
            ? <AvatarImage src={exercise.imageUrl} alt={exercise.name} />
            : null}
          <AvatarFallback className="bg-surface-2">
            <User className="w-5 h-5 text-muted-foreground" />
          </AvatarFallback>
        </Avatar>

        <div className="flex flex-col flex-1 min-w-0">
          <span className="font-sans font-semibold text-sm text-foreground leading-tight truncate">
            {exercise.name}
          </span>
          <span className="font-sans text-xs text-muted-foreground">
            {exercise.muscle}
          </span>
        </div>

        <DropdownMenu>
          <DropdownMenuTrigger variant="plain" className="p-1">
            <MoreHorizontal className="w-4 h-4" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem variant="destructive" onClick={() => onRemove(exercise.id)}>
              Remove exercise
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className="border-t border-border" />

      <div className="px-4 py-3 flex flex-col gap-2">
        <div className="flex items-center gap-4 text-xs font-bold uppercase tracking-[1px] text-muted-foreground font-sans px-3">
          <span className="w-20 shrink-0">Set</span>
          <span className="flex-1 text-center">KG</span>
          <span className="flex-1 text-center">Reps</span>
          <span className="w-6 shrink-0" />
        </div>
        {sets.map((set, i) => {
          const workingIndex = sets.slice(0, i + 1).filter(s => s.type === 'I').length
          return (
            <SetRow
              key={set.id}
              set={set}
              workingIndex={workingIndex}
              onChange={updated => updateSet(i, updated)}
              onRemove={() => removeSet(i)}
            />
          )
        })}
      </div>

      <div className="border-t border-border" />

      <div className="px-4 py-3">
        <Button variant="outline" size="sm" className="w-full" onClick={addSet}>
          + Add Set
        </Button>
      </div>

    </div>
  )
}
