import { useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { VISION_EXERCISES } from '@/constants/optivision'
import { EXERCISE_GUIDES } from '@/constants/optivision-guides'
import { cn } from '@/lib/utils'
import type { VisionExercise } from '@/types/optivision'

const LABEL_CLASS = 'text-xs sm:text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground'

type ExerGuidProps = Readonly<{
  exercise: VisionExercise
  onExerciseChange: (exercise: VisionExercise) => void
  disabled: boolean
}>

export function ExerGuide({ exercise, onExerciseChange, disabled }: ExerGuidProps) {
  const guide = EXERCISE_GUIDES[exercise]
  const [failedGif, setFailedGif] = useState<string | null>(null)

  return (
    <Card className="border-border bg-card">
      <CardHeader className="px-5">
        <CardTitle className="text-base font-bold text-foreground">Choose your exercise</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-5">
        <div className="flex gap-2">
          {VISION_EXERCISES.map(({ value, label }) => (
            <button
              key={value}
              type="button"
              disabled={disabled}
              aria-pressed={exercise === value}
              onClick={() => onExerciseChange(value)}
              className={cn(
                'min-h-11 flex-1 cursor-pointer rounded-lg px-2 text-xs font-semibold uppercase tracking-[0.05em] outline-none transition-all focus-visible:ring-2 focus-visible:ring-brand disabled:cursor-not-allowed disabled:opacity-60',
                exercise === value ? 'bg-brand text-white shadow-xs'
                  : 'border border-border bg-surface text-muted-foreground hover:bg-surface-2',
              )}
            >
              {label}
            </button>
          ))}
        </div>

        <div className="flex flex-wrap items-center gap-4">
          {failedGif !== guide.gifUrl && (
            <img
              src={guide.gifUrl}
              alt={`${guide.name} demonstration`}
              width={180}
              height={180}
              onError={() => setFailedGif(guide.gifUrl)}
              className="h-[180px] w-[180px] shrink-0 rounded-lg border border-border bg-white"
            />
          )}
          <div className="min-w-[8rem] flex-1">
            <h3 className="text-base font-bold text-foreground">{guide.name}</h3>
            <p className="mt-1 text-xs text-muted-foreground">{guide.note}</p>
          </div>
        </div>

        <div className="flex flex-col gap-2">
          <span className={LABEL_CLASS}>What we check</span>
          <ul className="flex flex-col gap-3">
            {guide.checks.map(({ label, tip }) => (
              <li key={label} className="border-l-[3px] border-brand pl-3">
                <p className="text-sm font-semibold text-foreground">{label}</p>
                <p className="text-xs text-muted-foreground">{tip}</p>
              </li>
            ))}
          </ul>
        </div>

        <p className="text-[0.7rem] text-muted-foreground">
          Animation ©{' '}
          <a href="https://gymvisual.com/" target="_blank" rel="noopener noreferrer" className="underline hover:text-brand">
            Gym visual — https://gymvisual.com/
          </a>
        </p>
      </CardContent>
    </Card>
  )
}
