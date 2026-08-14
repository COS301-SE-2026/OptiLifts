import { customFetch } from '@/lib/custom-fetch'
import type { Workout } from '@/types/workout'
import type { WorkoutDetailResponse } from '@/types/workout-detail'
import { STORE_WORKOUTS, tx } from './db'

const LIST_KEY = '__workout-list__'

type CachedList = { id: string; workouts: Workout[]; cachedAt: string }
type CachedDetail = { id: string; detail: WorkoutDetailResponse; cachedAt: string }

export function cacheWorkoutList(workouts: Workout[]): Promise<IDBValidKey> {
  const row: CachedList = { id: LIST_KEY, workouts, cachedAt: new Date().toISOString() }
  return tx(STORE_WORKOUTS, 'readwrite', (store) => store.put(row))
}

export async function getCachedWorkoutList(): Promise<Workout[] | null> {
  try {
    const row = await tx<CachedList | undefined>(STORE_WORKOUTS, 'readonly', (store) => store.get(LIST_KEY))
    return row?.workouts ?? null
  }
  catch {
    return null
  }
}

export function cacheWorkoutDetail(detail: WorkoutDetailResponse): Promise<IDBValidKey> {
  const row: CachedDetail = { id: detail.id, detail, cachedAt: new Date().toISOString() }
  return tx(STORE_WORKOUTS, 'readwrite', (store) => store.put(row))
}

export async function getCachedWorkoutDetail(workoutId: string): Promise<WorkoutDetailResponse | null> {
  try {
    const row = await tx<CachedDetail | undefined>(STORE_WORKOUTS, 'readonly', (store) => store.get(workoutId))
    return row?.detail ?? null
  }
  catch {
    return null
  }
}

// gets all workout detail for offline starts
export async function precacheWorkoutDetails(workoutIds: readonly string[]): Promise<void> {
  for (const workoutId of workoutIds) {
    if (!navigator.onLine) {
      return
    }

    try {
      const response = await customFetch(`/api/workouts/${workoutId}`, {
        headers: { Accept: 'application/json' },
      })

      if (!response.ok) {
        continue
      }

      await cacheWorkoutDetail((await response.json()) as WorkoutDetailResponse)
    }
    catch {
      return
    }
  }
}
