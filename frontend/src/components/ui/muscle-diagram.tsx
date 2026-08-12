import { MUSCLE_REGION_MAP } from '@/constants/muscles'
import type { MuscleName } from '@/types/workout'

type Props = Readonly<{
  highlightedMuscles: MuscleName[]
  secondaryMuscles?: MuscleName[]
  variant?: 'front' | 'back' | 'both'
}>

export function MuscleDiagram({ highlightedMuscles, secondaryMuscles = [], variant = 'both' }: Props) {
  const primaryMuscles = new Set(highlightedMuscles)
  const secondaryMuscleSet = new Set(secondaryMuscles)

  const getHighlightState = (muscle: MuscleName) => {
    if (primaryMuscles.has(muscle)) {
      return 'primary'
    }

    if (secondaryMuscleSet.has(muscle)) {
      return 'secondary'
    }

    return 'none'
  }

  const getMuscleClassName = (muscle: MuscleName) => {
    const state = getHighlightState(muscle)

    if (state === 'primary') {
      return 'bg-brand/20 border-brand/80'
    }

    if (state === 'secondary') {
      return 'bg-brand/4 border-brand/30'
    }

    return 'bg-surface'
  }

  const showFront = variant === 'front' || variant === 'both'
  const showBack = variant === 'back' || variant === 'both'

  //replace with heatmap svg when implemented
  return (
    <div className="w-full">
      <div className="flex gap-2 items-start">
        {showFront && (
        <div className="flex-1">
          <div className="mb-2 text-sm font-semibold">Front</div>
          <div className="grid grid-cols-2 gap-2">
            {Object.keys(MUSCLE_REGION_MAP).slice(0, 10).map((m) => (
              <div
                key={`front-${m}`}
                className={`rounded-md border p-2 text-xs text-center ${getMuscleClassName(m as MuscleName)}`}>
                {m}
              </div>
            ))}
          </div>
        </div>
        )}

        {showBack && (
        <div className="flex-1">
          <div className="mb-2 text-sm font-semibold">Back</div>
          <div className="grid grid-cols-2 gap-2">
            {Object.keys(MUSCLE_REGION_MAP).slice(10).map((m) => (
              <div
                key={`back-${m}`}
                className={`rounded-md border p-2 text-xs text-center ${getMuscleClassName(m as MuscleName)}`}>
                {m}
              </div>
            ))}
          </div>
        </div>
        )}
      </div>
    </div>
  )
}

export default MuscleDiagram
