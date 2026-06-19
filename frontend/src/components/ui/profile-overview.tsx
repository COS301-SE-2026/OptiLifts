import { Settings } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { PageTitle } from '@/components/ui/page-title'
import { CircularProfileImage } from '@/components/ui/circular-image'
import type { ProfileOverviewProps } from '@/types/profile'

export function ProfileOverview({ name, email, bio, photoUrl }: ProfileOverviewProps) {
  return (
    <section className="relative rounded-lg border border-border bg-card p-3 sm:p-4">
      <Button variant="default" size="sm" className="absolute right-3 top-3 sm:right-4 sm:top-4">
        <span>Settings</span>
        <Settings size={16} className="ml-2" />
      </Button>

      <div className="flex items-center gap-3 sm:gap-4">
        <CircularProfileImage src={photoUrl} alt={`${name}'s profile`} className="size-20 sm:size-24" />

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