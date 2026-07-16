import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/ui/badge'
import { BarChart } from '@/components/ui/barchart'
import { Calendar } from '@/components/ui/calendar'
import { ProfileOverview } from '@/components/ui/profile-overview'
import { WorkoutOverview } from '@/components/ui/workout-overview'
import { useAuth } from '@/context/auth-context'
import { customFetch } from '@/lib/custom-fetch'
import type { ProfileCalendarEntry, ProfileCalendarResponse, ProfilePageResponse } from '@/types/profile'
import { Button } from '@/components/ui/button'
import { metricCheck, outputWeight } from '@/lib/weight-utils'

const pad = (value: number) => String(value).padStart(2, '0')

const startOfMonth = (date: Date) => new Date(date.getFullYear(), date.getMonth(), 1)

const toMonthQuery = (date: Date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}`

export default function ProfilePage() {
  const { isAuthenticated, isHydrated } = useAuth()
  const navigate = useNavigate()
  const [profileData, setProfileData] = useState<ProfilePageResponse | null>(null)
  const [calendarMonth, setCalendarMonth] = useState(() => startOfMonth(new Date()))
  const [calendarEntries, setCalendarEntries] = useState<readonly ProfileCalendarEntry[]>([])
  const [calendarLoading, setCalendarLoading] = useState(false)
  const [isFetching, setIsFetching] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!isHydrated || !isAuthenticated) {
      return
    }

    let isActive = true

    async function loadProfile() {
      setIsFetching(true)
      setError(null)

      try {
        const response = await customFetch('/api/profile/overview', {
          headers: {
            Accept: 'application/json',
          },
        })

        if (!response.ok) {
          throw new Error(`Failed to load profile (${response.status})`)
        }

        const data = (await response.json()) as ProfilePageResponse

        if (isActive) {
          setProfileData(data)
        }
      } catch (loadError) {
        if (isActive) {
          setError(loadError instanceof Error ? loadError.message : 'Failed to load profile.')
        }
      } finally {
        if (isActive) {
          setIsFetching(false)
        }
      }
    }

    void loadProfile()

    return () => {
      isActive = false
    }
  }, [isHydrated, isAuthenticated])

  useEffect(() => {
    if (!isHydrated || !isAuthenticated) {
      return
    }

    let isActive = true

    async function loadCalendar() {
      setCalendarLoading(true)

      try {
        const monthQuery = toMonthQuery(calendarMonth)
        const [year, month] = monthQuery.split('-')

        const response = await customFetch(`/api/profile/calendar?year=${year}&month=${month}`, {
          headers: {
            Accept: 'application/json',
          },
        })

        if (!response.ok) {
          throw new Error(`Failed to load calendar (${response.status})`)
        }

        const data = (await response.json()) as ProfileCalendarResponse

        if (isActive) {
          setCalendarEntries(data.entries)
        }
      } catch {
        if (isActive) {
          setCalendarEntries([])
        }
      } finally {
        if (isActive) {
          setCalendarLoading(false)
        }
      }
    }

    void loadCalendar()

    return () => {
      isActive = false
    }
  }, [calendarMonth, isHydrated, isAuthenticated])

  const controlledCalendarMonth = useMemo(() => startOfMonth(calendarMonth), [calendarMonth])
  const calendarDates = useMemo(() => calendarEntries.map((entry) => entry.date), [calendarEntries])
  const calendarEntriesByDate = useMemo(
    () => new Map(calendarEntries.map((entry) => [entry.date, entry] as const)),
    [calendarEntries],
  )

  const displayProfile = profileData?.profile
  const displayBadges = profileData?.badges?.slice(0, 3) ?? []
  const displayWorkouts = profileData?.recentWorkouts ?? []
  const displayChartData = profileData?.chartData ?? []
  const displayChartTitle = profileData?.chartTitle ?? 'Workout activity'
  const isLoading = !isHydrated || isFetching
  const hasWorkouts = displayWorkouts.length > 0

  const formatVol = (volume: string): string => {
    const vol = outputWeight(Number.parseInt(volume.replace(/\D/g, ''), 10));
    const unit = (metricCheck())? 'KG' : 'LB';

    const outNum = vol.toLocaleString('en-ZA', {
      maximumFractionDigits: 0
    });

    return `${outNum} ${unit}`;
  };

  return (
    <section className="mx-auto max-w-6xl px-6 py-8">
      <div className="mb-8 w-full max-w-[1144px]">
        {displayProfile ? (
          <ProfileOverview
            name={displayProfile.name}
            email={displayProfile.email}
            bio={displayProfile.bio ?? ''}
            profileImageUrl={displayProfile.profileImageUrl}
          />
        ) : (
          <div className="rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground">
            {isLoading ? 'Loading profile...' : error ?? 'No profile data available.'}
          </div>
        )}
      </div>

      <div className="mb-8 grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,600px)_minmax(0,520px)] lg:items-stretch">
        <BarChart title={displayChartTitle} data={displayChartData} className="w-full max-w-[600px]" />

        <div className="lg:self-start">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">BADGES</h2>
          </div>

          <div className="grid min-h-[180px] grid-cols-1 gap-4 sm:grid-cols-3 sm:gap-5">
            {displayBadges.length > 0 ? (
              displayBadges.map((badge) => (
                <Badge
                  key={badge.name}
                  name={badge.name}
                  description={badge.description}
                  category={badge.category}
                  earnedAt={badge.earnedAt}
                  iconUrl={badge.iconUrl}
                />
              ))
            ) : (
              <div className="flex min-h-[180px] items-center rounded-lg border border-border bg-card p-4 text-sm text-muted-foreground sm:col-span-3">
                You have not earned any badges yet.
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="mb-8">
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1.65fr)_minmax(320px,0.9fr)]">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-2xl font-bold tracking-tight text-foreground">Recent Workouts</h2>
            <Button
              variant="secondary"
              size="sm"
              className="font-semibold uppercase tracking-wider text-xs scale-[0.85] origin-right"
              onClick={() => navigate('/past-workouts')}
            >
              View All
            </Button>
          </div>
          <div className="hidden lg:block"></div>
        </div>
        <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1.65fr)_minmax(320px,0.9fr)] lg:items-stretch">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:h-full lg:items-stretch">
            {hasWorkouts ? (
              displayWorkouts.map((workout) => (
                <WorkoutOverview
                  key={`${workout.workoutId}-${workout.logId ?? 'planned'}`}
                  {...workout}
                  volume={formatVol(workout.volume)} 
                  href={workout.logId ? `/workouts/${workout.workoutId}/logs/${workout.logId}` : undefined}
                  className="h-full"
                />
              ))
            ) : (
              <div className="rounded-lg border border-border bg-card px-4 py-6 text-sm text-muted-foreground sm:col-span-2">
                {isLoading ? 'Loading workouts...' : 'You do not have any workouts yet.'}
              </div>
            )}
          </div>

          <div className="rounded-lg border border-border bg-card p-3 sm:p-4">
            <Calendar
              highlightedDates={calendarDates}
              month={controlledCalendarMonth}
              onMonthChange={setCalendarMonth}
              onHighlightedDateClick={(dateKey) => {
                const entry = calendarEntriesByDate.get(dateKey)
                if (entry) {
                  navigate(`/workouts/${entry.workoutId}/logs/${entry.logId}`)
                }
              }}
              className={calendarLoading ? 'opacity-70' : undefined}
            />
          </div>
        </div>
      </div>
    </section>
  )
}