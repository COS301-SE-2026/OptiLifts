import { AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import type { VisionJobState } from '@/types/optivision'

type ResultPanelProps = Readonly<{
  state: Extract<VisionJobState, { phase: 'completed' | 'failed' }>
  onReset: () => void
}>

export function ResultPanel({ state, onReset }: ResultPanelProps) {
  if (state.phase === 'failed') {
    return (
      <Card className="border-border bg-card" role="alert">
        <CardHeader className="px-5">
          <div className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 shrink-0 text-destructive" aria-hidden="true" />
            <CardTitle className="text-base font-bold text-foreground">Something went wrong</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <p className="text-sm text-muted-foreground">{state.message}</p>
          <Button type="button" onClick={onReset}>
            Try again
          </Button>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className="border-border bg-card">
      <CardHeader className="px-5">
        <CardTitle className="text-base font-bold text-foreground">Coach feedback</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <p className="border-l-[3px] border-brand pl-3 text-base leading-relaxed text-foreground">
          {state.coachSummary || 'No feedback was generated for this set.'}
        </p>
        <Button type="button" onClick={onReset}>
          Analyse another set
        </Button>
      </CardContent>
    </Card>
  )
}
