import { MUSCLE_REGION_MAP } from '@/constants/muscles'
import type { MuscleName } from '@/types/workout'

type Props = Readonly<{
  highlightedMuscles: MuscleName[]
  variant?: 'front' | 'back' | 'both'
}>

export function MuscleDiagram({ highlightedMuscles, variant = 'both' }: Props) {
  const isHighlighted = (muscle: MuscleName) => highlightedMuscles.includes(muscle)
  const showFront = variant === 'front' || variant === 'both'
  const showBack = variant === 'back' || variant === 'both'

  //replace with heatmap svg when implemented
  return (
    <div className="w-full">
      <div className="flex gap-4 items-start">
        {showFront && (
        <div className="flex-1">
          <div className="mb-2 text-sm font-semibold">Front</div>
          <div className="grid grid-cols-2 gap-2">
            {Object.keys(MUSCLE_REGION_MAP).slice(0, 8).map((m) => (
              <div
                key={`front-${m}`}
                className={`rounded-md border p-2 text-xs text-center ${isHighlighted(m as MuscleName) ? 'bg-brand/10 border-brand' : 'bg-surface'}`}>
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
            {Object.keys(MUSCLE_REGION_MAP).slice(8).map((m) => (
              <div
                key={`back-${m}`}
                className={`rounded-md border p-2 text-xs text-center ${isHighlighted(m as MuscleName) ? 'bg-brand/10 border-brand' : 'bg-surface'}`}>
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
