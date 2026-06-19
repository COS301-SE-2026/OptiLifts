import { Settings } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { PageTitle } from '@/components/ui/page-title'

type ProfileOverviewProps = Readonly<{
  name: string
  email: string
  bio: string
  initials?: string
}>

export function ProfileOverview({ name, email, bio, initials = name.slice(0, 1).toUpperCase() }: ProfileOverviewProps) {
  return (
    <section className="relative rounded-lg border border-border bg-card p-3 sm:p-4">
      <Button variant="default" size="sm" className="absolute right-3 top-3 sm:right-4 sm:top-4">
        <span>Settings</span>
        <Settings size={16} className="ml-2" />
      </Button>

      <div className="flex items-center gap-3 sm:gap-4">
        <div className="flex size-20 shrink-0 items-center justify-center rounded-full border border-border bg-background text-xl font-semibold tracking-[0.12em] text-foreground sm:size-24 sm:text-2xl">
          {initials}
        </div>

        <div className="h-16 w-px shrink-0 bg-border sm:h-20" />

        <div className="min-w-0">
          <PageTitle title={name} />
            <p className="text-foreground">
              <span className="font-semibold">Email:</span>{' '}
              <span className="text-muted-foreground">{email}</span>
            </p>
            <p className="text-foreground">
              <span className="font-semibold">Bio:</span>{' '}
              <span className="text-muted-foreground">{bio}</span>
            </p>
        </div>
      </div>
    </section>
  )
}

export default ProfileOverview