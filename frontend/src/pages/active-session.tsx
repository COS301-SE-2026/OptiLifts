import { useEffect, useMemo, useState } from 'react'
import { useLocation } from 'react-router-dom'

type WorkoutLocationState = Readonly<{
  workout?: Readonly<{
    id?: string
    name: string
    primaryMuscleGroups: string[]
  }>
}>



export default function ActiveSessionPage() {
  const location = useLocation()
  const sessionState = location.state as WorkoutLocationState | null
  const workoutName = sessionState?.workout?.name ?? 'PULL'

  return (
    <section className="w-full px-6 py-6 font-sans text-foreground">
      <div className="max-w-3xl w-full">
        <div className="mb-6 flex items-center justify-between w-full">
          <div className="flex items-center gap-3">
            <div className="h-8 w-1.5 rounded-full bg-brand" />
            <h1 className="text-3xl font-bold uppercase tracking-tight">{workoutName}</h1>
          </div>
        </div>
      </div>
    </section>
  )
}