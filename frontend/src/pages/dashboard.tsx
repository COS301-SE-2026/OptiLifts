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
