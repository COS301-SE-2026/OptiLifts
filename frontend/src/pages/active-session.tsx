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
          </div>
        </div>
      </div>
    </section>
  )
}