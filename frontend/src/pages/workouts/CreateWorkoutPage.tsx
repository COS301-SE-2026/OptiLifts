import { useState } from 'react'
import { PageTitle } from '@/components/ui/page-title'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

function MuscleDiagramPlaceholder() {
  return (
    <div className="flex h-28 w-48 items-center justify-center rounded-lg border border-border bg-surface-2 text-muted-foreground text-xs text-center leading-tight select-none">
      Muscle<br />Diagram
    </div>
  )
}

export function CreateWorkoutPage() {
  const [workoutName, setWorkoutName] = useState('')

  return (
    <div className="flex min-h-[calc(100dvh-4rem)] bg-background">

      <div className="flex flex-1 flex-col gap-6 p-8 overflow-y-auto">

        <div className="flex items-center gap-[200px]">
          <div className="flex flex-col gap-2">
            <PageTitle title="Create Workout" />
            <div className="flex items-center gap-3">
              <div className="flex flex-col gap-1 w-96">
                <label className="text-xs font-semibold uppercase tracking-[1px] text-muted-foreground font-sans">
                  Workout Name
                </label>
                <Input
                  variant="default"
                  placeholder="e.g. Push Day A"
                  value={workoutName}
                  onChange={e => setWorkoutName(e.target.value)}
                />
              </div>
              <Button variant="default" size="sm" className="self-end h-8" disabled={!workoutName.trim()}>
                Save Workout
              </Button>
            </div>
          </div>
          <MuscleDiagramPlaceholder />
        </div>
      </div>


    </div>
  )
}
