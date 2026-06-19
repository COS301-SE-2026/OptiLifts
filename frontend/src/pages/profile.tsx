import { Badge } from '@/components/ui/badge'
import { BarChart } from '@/components/ui/barchart'
import { Calendar } from '@/components/ui/calendar'
import { ProfileOverview } from '@/components/ui/profile-overview'
import { WorkoutOverview } from '@/components/ui/workout-overview'

const WORKOUT_DAYS = ['2026-06-02', '2026-06-05', '2026-06-11', '2026-06-14', '2026-06-18'] as const

export default function ProfilePage() {
  return (
    <section className="mx-auto max-w-6xl px-6 py-8">
      <div className="mb-8 w-full max-w-[1144px]">
        <ProfileOverview
          name="Alex"
          email="gymgoer@gmail.com"
          bio="Loves to gym every day all day. This is their favourite app ever."
        />
      </div>

      <div className="mb-8 grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,600px)_520px] lg:items-start">
        <BarChart className="w-full max-w-[600px]" />

        <div className="grid grid-cols-3 gap-4 self-start sm:gap-5 lg:mt-12">
          <Badge label="Streak" value="5 weeks" className="aspect-square w-full min-h-[180px]" />
          <Badge label="Workouts" value="51" className="aspect-square w-full min-h-[180px]" />
          <Badge label="Records" value="10 PRs" className="aspect-square w-full min-h-[180px]" />
        </div>
      </div>

      <div className="mb-8">
        <h2 className="mb-3 text-2xl font-bold tracking-tight text-foreground">Recent Workouts</h2>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1.65fr)_minmax(320px,0.9fr)] lg:items-stretch">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:h-full lg:items-stretch">
            <WorkoutOverview
              name="Pull"
              exercises="Lat Pulldown, Iso-Lateral High Row, Vbar Low Row..."
              prs="10 PRs"
              duration="1h 5min"
              volume="4 500 kg"
              sets="23"
              className="h-full"
            />
            <WorkoutOverview
              name="Push"
              exercises="Lat Pulldown, Iso-Lateral High Row, Vbar Low Row..."
              prs="10 PRs"
              duration="1h 5min"
              volume="3 000 kg"
              sets="16"
              className="h-full"
            />
          </div>

          <div className="rounded-lg border border-border bg-card p-3 sm:p-4">
            <Calendar highlightedDates={WORKOUT_DAYS} />
          </div>
        </div>
      </div>

    </section>
  )
}
