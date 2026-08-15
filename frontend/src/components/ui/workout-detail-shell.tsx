import type { ReactNode } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

type WorkoutDetailShellProps = Readonly<{
  isLoading: boolean
  loadingMessage: string
  error: string | null
  hasContent: boolean
  notFoundTitle: string
  notFoundDescription: string
  mainContent: ReactNode
  summaryContent: ReactNode
}>

export function WorkoutDetailShell({
  isLoading,
  loadingMessage,
  error,
  hasContent,
  notFoundTitle,
  notFoundDescription,
  mainContent,
  summaryContent,
}: WorkoutDetailShellProps) {
  return (
    <>
      {isLoading && (
        <div className="rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-muted-foreground">
          {loadingMessage}
        </div>
      )}

      {error && (
        <div className="rounded-md border border-border bg-surface-2 px-3 py-2 text-sm text-red-500">
          {error}
        </div>
      )}

      {!isLoading && !error && !hasContent && (
        <Card>
          <CardHeader>
            <CardTitle className="text-xl font-bold">{notFoundTitle}</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">{notFoundDescription}</p>
          </CardContent>
        </Card>
      )}

      {!isLoading && !error && hasContent && (
        <div className="grid gap-6 grid-cols-1 lg:grid-cols-12">
          <div className="col-span-1 lg:col-span-7 flex flex-col gap-4 min-w-0">{mainContent}</div>

          <aside className="col-span-1 lg:col-span-5 min-w-0">
            <Card className="flex flex-col">
              <CardHeader className="pb-2">
                <CardTitle className="text-[1.05rem] font-bold">Summary</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col">
                <div className="flex flex-col gap-4 text-sm text-muted-foreground">
                  {summaryContent}
                </div>
              </CardContent>
            </Card>
          </aside>
        </div>
      )}
    </>
  )
}

export default WorkoutDetailShell