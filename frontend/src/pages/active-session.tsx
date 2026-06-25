import { useEffect, useMemo, useState } from 'react'
import { useLocation } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle, CardAction } from '@/components/ui/card'
import { NumericalUnderscoreInput } from '@/components/ui/input'
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

export default function ActiveSessionPage() {
  const location = useLocation()
  const sessionState = location.state as WorkoutLocationState | null
  const workoutName = sessionState?.workout?.name ?? 'PULL'
  
  const [secondsElapsed, setSecondsElapsed] = useState(0)

  useEffect(() => {
    const interval = setInterval(() => setSecondsElapsed((s) => s + 1), 1000)
    return () => clearInterval(interval)
  }, [])

  const formatTime = (totalSeconds: number) => {
    const h = Math.floor(totalSeconds / 3600)
    const m = Math.floor((totalSeconds % 3600) / 60)
    if (h > 0) return `${h}h ${m}min`
    return `${m}m`
  }

  const exercises: ExerciseData[] = useMemo(
    () => [
      {
        id: 'bicep-curl-1',
        name: 'Bicep Curl (Dumbbell)',
        muscleGroup: 'Biceps',
        sets: [
          { id: '1', type: 'W', previous: '20KG x 10', kg: 20, reps: 11, rpe: 8.5, completed: true },
          { id: '2', type: '1', previous: '20KG x 8', kg: 20, reps: 8, rpe: 'RPE', completed: true },
          { id: '3', type: '2', previous: '20KG x 7', kg: 20, reps: 7, rpe: 'RPE', completed: true },
        ],
      },
      {
        id: 'bicep-curl-2',
        name: 'Bicep Curl (Dumbbell)',
        muscleGroup: 'Biceps',
        sets: [
          { id: '1', type: 'W', previous: '20KG x 10', kg: 20, reps: 11, rpe: 8.5, completed: true },
          { id: '2', type: '1', previous: '20KG x 8', kg: 20, reps: 8, rpe: 'RPE', completed: true },
          { id: '3', type: '2', previous: '20KG x 7', kg: 20, reps: 7, rpe: 'RPE', completed: false },
        ],
      },
    ],
    []
  )

  const summary = useMemo(() => {
    const completedSets = exercises.flatMap((exercise) => exercise.sets).filter((set) => set.completed)
    const totalVolume = 1500 
    
    return {
      completedSets: completedSets.length,
      totalSets: 12,
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
              <p className="text-sm font-bold">{summary.totalSets}</p>
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-6 w-full">
          
          <Card className="border-border bg-card shadow-sm rounded-xl overflow-hidden pt-4 pb-2">
            <CardHeader className="flex flex-row items-start justify-between pb-4 px-5 pt-0">
              <div className="flex items-center gap-4">
                <div className="h-10 w-10 rounded-full bg-surface-2 border border-border" />
                <div>
                  <CardTitle className="text-base font-bold">{exercises[0].name}</CardTitle>
                  <p className="text-sm text-muted-foreground">{exercises[0].muscleGroup}</p>
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
                {exercises[0].sets.map((set) => (
                  <div key={set.id} className="grid grid-cols-[4rem_1.5fr_1fr_1fr_0.8fr_5rem] items-center gap-4 rounded-lg bg-surface-2 p-1.5 text-center text-sm font-medium">
                    <Button variant="outline" size="sm" className="h-8 w-full justify-between px-2 text-xs bg-surface-2">
                      {set.type} <ChevronDown className="ml-1 h-3 w-3 opacity-50" />
                    </Button>
                    
                    <div className="text-muted-foreground font-normal">{set.previous}</div>
                    
                    <NumericalUnderscoreInput
                      defaultValue={set.kg}
                      className="text-xl text-center mx-auto"
                    />
                    <NumericalUnderscoreInput
                      defaultValue={set.reps}
                      className="text-xl text-center mx-auto"
                    />
                    <div className="flex items-center justify-center border border-border rounded-md h-7 bg-surface-2">
                      <span className="text-xs w-full text-center">{set.rpe}</span>
                    </div>
                    
                    <div className="flex w-full items-center justify-center gap-1">
                      <Button variant="icon" size="icon" className="h-7 w-7 rounded-md bg-surface-2 border-border">
                        <Check className="h-3.5 w-3.5" />
                      </Button>
                      <Button variant="ghost" size="icon" className="h-7 w-7 text-muted-foreground hover:text-destructive">
                        <X className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </div>
                ))}
              </div>

              <Button variant="outline" className="mt-3 w-full border-dashed border-border text-muted-foreground hover:text-foreground bg-transparent h-9 text-xs">
                <Plus className="mr-2 h-3.5 w-3.5" /> Add Set
              </Button>
            </CardContent>
          </Card>

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

          </div>
        </div>
      </div>
    </section>
  )
}