import { useState } from 'react'
import { MoreHorizontal, User, X } from 'lucide-react'
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import * as DropdownMenuPrimitive from '@radix-ui/react-dropdown-menu'
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
  index,
  onChange,
  onRemove,
}: {
  set: ExerciseSet
  index: number
  onChange: (updated: ExerciseSet) => void
  onRemove: () => void
}) {
  return (
    <tr className="group">
      <td className="py-3 px-8 text-center">
        <div className="flex items-center justify-center gap-2">
          <span className="font-sans font-bold text-sm text-foreground">{index + 1}</span>
          <DropdownMenu>
            <DropdownMenuPrimitive.Trigger className="flex items-center gap-1 border border-border rounded bg-surface-2 px-1.5 py-0.5 text-xs font-bold font-sans text-foreground cursor-pointer outline-none">
              <span>{set.type}</span>
              <ChevronDown className="w-3 h-3 text-muted-foreground" />
            </DropdownMenuPrimitive.Trigger>
            <DropdownMenuContent>
              {SET_TYPES.map(t => (
                <DropdownMenuItem key={t} onClick={() => onChange({ ...set, type: t })}>
                  {t === 'W' ? 'Warmup' : t === 'I' ? 'Working' : 'Drop'}
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </td>
      <td className="py-3 px-16 text-center">
        <input
          type="number"
          min={0}
          value={set.kg}
          onChange={e => onChange({ ...set, kg: e.target.value === '' ? '' : Number(e.target.value) })}
          className="w-full border-0 border-b-2 border-foreground bg-transparent text-center text-lg font-sans text-foreground outline-none focus:border-brand [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
        />
      </td>
      <td className="py-3 px-16 text-center">
        <input
          type="number"
          min={0}
          value={set.reps}
          onChange={e => onChange({ ...set, reps: e.target.value === '' ? '' : Number(e.target.value) })}
          className="w-full border-0 border-b-2 border-foreground bg-transparent text-center text-lg font-sans text-foreground outline-none focus:border-brand [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
        />
      </td>
      <td className="py-1 pl-2 w-8">
        <button
          onClick={onRemove}
          className="text-muted-foreground hover:text-foreground transition-colors"
          aria-label="Remove set"
        >
          <X className="w-4 h-4" />
        </button>
      </td>
    </tr>
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

  const updateSet = (index: number, updated: ExerciseSet) => {
    const copy = [...sets]
    copy[index] = updated
    updateSets(copy)
  }

  const removeSet = (index: number) =>
    updateSets(sets.filter((_, i) => i !== index))

  return (
    <div className="rounded-xl border border-border bg-surface overflow-hidden">

      {/* Exercise header */}
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
          <DropdownMenuTrigger variant="default" className="border-0 shadow-none bg-transparent p-1 h-auto w-auto">
            <MoreHorizontal className="w-4 h-4 text-muted-foreground" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem variant="destructive" onClick={() => onRemove(exercise.id)}>
              Remove exercise
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      {/* Divider */}
      <div className="border-t border-border" />

      {/* Sets table */}
      <div className="px-4 py-3">
        <table className="w-full table-fixed">
          <thead>
            <tr className="text-xs font-bold uppercase tracking-[1px] text-muted-foreground font-sans">
              <th className="text-center pb-2">Set</th>
              <th className="text-center pb-2">KG</th>
              <th className="text-center pb-2">Reps</th>
              <th className="w-8" />
            </tr>
          </thead>
          <tbody>
            {sets.map((set, i) => (
              <SetRow
                key={set.id}
                set={set}
                index={i}
                onChange={updated => updateSet(i, updated)}
                onRemove={() => removeSet(i)}
              />
            ))}
          </tbody>
        </table>
      </div>

      {/* Divider */}
      <div className="border-t border-border" />

      {/* Add set */}
      <div className="px-4 py-3">
        <Button variant="outline" size="sm" className="w-full" onClick={addSet}>
          + Add Set
        </Button>
      </div>

    </div>
  )
}
