import { useEffect, useMemo, useState } from 'react'
import { UpcomingWorkoutsCard } from '@/components/ui/upcoming-workouts'
import { VolumeChart } from '@/components/ui/volume-chart'
import { SpiderGraph } from '@/components/ui/spider-graph'
import { Card, CardContent } from '@/components/ui/card'
import streakFlame from '@/assets/streak_flame.png'
import badgeIcon from '@/assets/badge.png'
import { customFetch } from '@/lib/custom-fetch'
import { useAuth } from '@/context/auth-context'
import type { ProfilePageResponse } from '@/types/profile'
import type { WorkoutDetailResponse } from '@/types/workout-detail'
import type { VolumeChartPeriod } from '@/components/ui/volume-chart'

type ScheduleAnalyticsResponse = Readonly<{
    totalWorkouts: number
    totalVolume: number
    totalSets: number
    muscleDistribution: readonly {
        muscleGroup: string
        setCount: number
        percentage: number
    }[]
}>

type ScheduledEntry = Readonly<{
    id: string
    workoutId: string
    workoutName: string
    scheduled: string
    status: string
    primaryMuscleGroups: string[]
    exerciseCount: number
    exercisePreview: string[]
    totalVolume: number
    totalSets: number
    recordCount?: number | null
    startedAt?: string | null
    completedAt?: string | null
}>

type ChartPoint = Readonly<{
    label: string
    value: number
}>

type ChartBucket = Readonly<{
    label: string
    start: Date
    end: Date
}>

const MUSCLE_CATEGORY_MAP: Record<string, 'Chest' | 'Core' | 'Shoulders' | 'Arms' | 'Legs' | 'Back'> = {
    Chest: 'Chest',
    Lats: 'Back',
    'Lower Back': 'Back',
    'Middle Back': 'Back',
    Trapezius: 'Back',
    Shoulders: 'Shoulders',
    Biceps: 'Arms',
    Forearms: 'Arms',
    Triceps: 'Arms',
    Quadriceps: 'Legs',
    Hamstrings: 'Legs',
    Calves: 'Legs',
    Glutes: 'Legs',
    Abductors: 'Legs',
    Adductors: 'Legs',
    Abdominals: 'Core',
}

const MUSCLE_KEYS = ['Chest', 'Core', 'Shoulders', 'Arms', 'Legs', 'Back'] as const
type MuscleFilter = 'All' | (typeof MUSCLE_KEYS)[number]

function startOfDay(date: Date) {
    const next = new Date(date)
    next.setHours(0, 0, 0, 0)
    return next
}

function startOfWeek(date: Date) {
    const day = startOfDay(date)
    const offset = (day.getDay() + 6) % 7
    day.setDate(day.getDate() - offset)
    return day
}

function addDays(date: Date, days: number) {
    const next = new Date(date)
    next.setDate(next.getDate() + days)
    return next
}

function formatDayLabel(date: Date) {
    return date.toLocaleDateString('en-US', { weekday: 'short' })
}

function formatMonthLabel(date: Date) {
    return date.toLocaleDateString('en-US', { month: 'short' })
}

function buildChartBuckets(period: VolumeChartPeriod): ChartBucket[] {
    const currentWeekStart = startOfWeek(new Date())

    if (period === 'Week'){
        return Array.from({ length: 7 }, (_, index) => {
            const day = addDays(currentWeekStart, index)
            return {
                label: formatDayLabel(day),
                start: day,
                end: day,
            }
        })
    }

    if (period === 'Month'){
        return Array.from({ length: 4 }, (_, index) => {
            const start = addDays(currentWeekStart, -(3 - index) * 7)
            return {
                label: `Week ${index + 1}`,
                start,
                end: addDays(start, 6),
            }
        })
    }

    const currentMonthStart = new Date(new Date().getFullYear(), new Date().getMonth(), 1)

    return Array.from({ length: 12 }, (_, index) => {
        const start = new Date(currentMonthStart.getFullYear(), currentMonthStart.getMonth() - (11 - index), 1)
        const end = new Date(start.getFullYear(), start.getMonth() + 1, 0)
        return {
            label: formatMonthLabel(start),
            start,
            end,
        }
    })
}

function getEntryDate(entry: ScheduledEntry) {
    return new Date(entry.completedAt ?? entry.startedAt ?? entry.scheduled)
}

function buildVolumeChartData(entries: readonly ScheduledEntry[], period: VolumeChartPeriod, muscleFilter: MuscleFilter): ChartPoint[] {
    const buckets = buildChartBuckets(period)

    return buckets.map((bucket) => {
        const total = entries.reduce((sum, entry) => {
            const entryDate = getEntryDate(entry)
            const withinBucket = entryDate >= bucket.start && entryDate <= bucket.end

            if (!withinBucket) return sum
            if (muscleFilter !== 'All'){
                const entryMuscles = entry.primaryMuscleGroups.map((m) => MUSCLE_CATEGORY_MAP[m] || m)
                if (!entryMuscles.includes(muscleFilter)) return sum
            }

            return sum + entry.totalVolume
        }, 0)

        return {
            label: bucket.label,
            value: total,
        }
    })
}

function getDayPillClass(index: number) {
    const palette = [
        'bg-brand/15 text-brand border-brand/30',
        'bg-foreground/10 text-foreground border-border',
        'bg-surface-2 text-foreground border-border',
    ]

    return palette[index % palette.length]
}

function formatUpcomingDate(dateString: string) {
    const today = startOfDay(new Date())
    const scheduledDate = startOfDay(new Date(dateString))
    const diffDays = Math.round((scheduledDate.getTime() - today.getTime()) / 86400000)

    if (diffDays === 0) return 'Today'
    if (diffDays === 1) return 'Tomorrow'
    if (diffDays < 7) {
        return scheduledDate.toLocaleDateString('en-US', { weekday: 'long' })
    }

    return scheduledDate.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
}

