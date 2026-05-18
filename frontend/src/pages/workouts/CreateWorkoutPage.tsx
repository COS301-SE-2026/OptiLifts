import { PageTitle } from '@/components/ui/page-title'

export function CreateWorkoutPage() {
  return (
    <div className="flex min-h-[calc(100dvh-4rem)] bg-background">

      {/* Left — main workout builder */}
      <div className="flex flex-1 flex-col gap-6 p-8 overflow-y-auto">
        <PageTitle title="Create Workout" />
        <p className="text-muted-foreground text-sm">Workout builder coming soon…</p>
      </div>

      {/* Right — sidebar */}
      <aside className="w-80 shrink-0 border-l border-border flex flex-col overflow-y-auto bg-surface">
        <p className="p-6 text-muted-foreground text-sm">Sidebar coming soon…</p>
      </aside>

    </div>
  )
}
