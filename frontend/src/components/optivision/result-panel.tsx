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

  let phaseOutput = 'No feedback was given for this rep';
  if (state.phase === 'completed' && state.coachSummary) {
    phaseOutput = state.coachSummary;
  }

  let score = 100;
  if (state.phase === 'completed'){
    score = state.score;
  }

  let scoreColour = 'text-green-500';
  if (score < 50) {
    scoreColour = 'text-red-500';
  } else if (score < 80) {
    scoreColour = 'text-yellow-500';
  }

  return (
    <div className="flex flex-col gap-4">
      {/* feedback */}
      <Card className="border-border bg-card">
        <CardHeader className="px-5 py-4">
          <CardTitle className="text-base font-bold text-foreground">Coach feedback</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="border-l-[3px] border-brand pl-4 text-base leading-relaxed text-foreground prose prose-sm dark:prose-invert">
            {phaseOutput}
          </div>
        </CardContent>
      </Card>

      {/* performance section */}
      <Card className="border-border bg-card">
        <CardHeader className="px-5 py-4">
          <CardTitle className="text-base font-bold text-foreground">Performance Stats</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-8">
          <div className="flex items-center gap-10 px-4">
            
            {/* graph for performance*/}
            <div className="relative flex items-center justify-center h-36 w-36 shrink-0">
              <svg className="absolute top-0 left-0 h-full w-full -rotate-90" viewBox="0 0 100 100">
                <circle cx="50" cy="50" r="42" fill="transparent" stroke="currentColor" strokeWidth="8" className="text-surface-2" />
                <circle
                  cx="50" cy="50" r="42"
                  fill="transparent" stroke="currentColor" strokeWidth="8"
                  strokeLinecap="round"
                  className={scoreColour}
                  strokeDasharray={`${(score / 100) * 264} 264`}
                />
              </svg>
              <div className="flex flex-col items-center justify-center pt-1">
                <span className="text-5xl font-black text-foreground leading-none">{score}</span>
                <span className="text-xs uppercase font-bold text-muted-foreground tracking-widest mt-1.5">Score</span>
              </div>
            </div>
            
            {/* the silly goose things */}
            <div className="flex flex-col gap-3 flex-1 min-w-0 pl-2">
              <CardTitle className="text-base font-bold text-foreground">Form Breakdown</CardTitle>
              <ul className="text-base text-muted-foreground space-y-3 mt-1">
                {state.phase === 'completed' && state.issues && state.issues.length > 0 ? (
                  state.issues.map((issue) => (
                    <li key={issue} className="flex items-center gap-3">
                      <div className="h-2 w-2 rounded-full bg-destructive shrink-0" />
                      <span className="truncate leading-tight">{issue}</span>
                    </li>
                  ))
                ) : (
                  <li className="flex items-center gap-3">
                    <div className="h-2 w-2 rounded-full bg-green-500 shrink-0" />
                    <span className="text-foreground font-medium leading-tight">Perfect Form</span>
                  </li>
                )}
              </ul>
            </div>
          </div>

          <Button type="button" onClick={onReset} className="w-full mt-2 h-12 text-base font-bold">
            Analyse Another Set
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
