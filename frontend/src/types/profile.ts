import type { BarChartDatum } from '@/types/barchart'

export type ProfileOverviewProps = Readonly<{
  name: string
  email: string
  bio: string
  profileImageUrl?: string | null
}>

export type ProfileStat = Readonly<{
  label: string
  value: string
}>

export type ProfileBadge = Readonly<{
  name: string
  description: string
  category: string
  iconUrl?: string | null
  earnedAt: string
}>

export type ProfileWorkoutSummary = Readonly<{
  name: string
  exercises: string[]
  prs: string
  duration: string
  volume: string
  sets: string
}>

export type ProfilePageResponse = Readonly<{
  profile: ProfileOverviewProps
  stats: readonly ProfileStat[]
  badges: readonly ProfileBadge[]
  recentWorkouts: readonly ProfileWorkoutSummary[]
  chartTitle: string
  chartData: readonly BarChartDatum[]
}>

export type ProfileCalendarResponse = Readonly<{
  highlightedDates: readonly string[]
}>
