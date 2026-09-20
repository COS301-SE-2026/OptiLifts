import { Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import type { VisionJobState } from '@/types/optivision'

type ProgressPanelProps = Readonly<{
  state: Extract<VisionJobState, { phase: 'extracting' | 'submitting' | 'polling' }>
  onCancel: () => void
}>

const STATUS_TEXT = {
  extracting: 'Reading your movement',
  submitting: 'Sending to OptiVision',
  polling: 'Analysing your form',
} as const

export function ProgressPanel({ state, onCancel }: ProgressPanelProps) {
  const percent = state.phase === 'extracting' ? state.progress : null

  return (
    <Card className="border-border bg-card">
      <CardHeader className="px-5">
        <div className="flex items-center gap-3">
          <Loader2 className="h-5 w-5 shrink-0 animate-spin text-brand" aria-hidden="true" />
          <CardTitle role="status" className="text-base font-bold text-foreground">
            {STATUS_TEXT[state.phase]}
          </CardTitle>
          {percent !== null && (
            <span aria-hidden="true" className="ml-auto text-sm font-semibold tabular-nums text-muted-foreground">
              {percent}%
            </span>
          )}
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        {percent !== null && (
          <div
            role="progressbar"
            aria-label="Reading video"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={percent}
            className="h-2 overflow-hidden rounded-full bg-surface-2"
          >
            <div className="h-full rounded-full bg-brand transition-all duration-150" style={{ width: `${percent}%` }} />
          </div>
        )}

        <p className="text-xs text-muted-foreground">
          Keep this tab open until it finishes. Reading a longer video can take a little while.
        </p>

        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
      </CardContent>
    </Card>
  )
}
