import { PageTitle } from '@/components/ui/page-title'

export default function PastWorkoutsPage() {
  return (
    <section className="mx-auto max-w-6xl px-6 py-12">
      <div className="mb-6">
        <PageTitle title="Past Workouts" />
      </div>
      
      <div className="rounded-lg border border-border bg-card px-4 py-6 text-sm text-muted-foreground">
        past workouts
      </div>
    </section>
  )
}