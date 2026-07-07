import { useEffect, useMemo, useState } from 'react'

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

