import { customFetch } from '@/lib/custom-fetch'
import type { Workout } from '@/types/workout'
import type { WorkoutDetailResponse } from '@/types/workout-detail'
import { STORE_WORKOUTS, tx } from './db'

const KEY_LIST = '__workout-list__'

type CachedList = { id: string; workouts: Workout[]; cachedAt: string }
type CachedDetail = { id: string; detail: WorkoutDetailResponse; cachedAt: string }

export function cacheWorkoutList(workouts: Workout[]): Promise<IDBValidKey> {
  const row: CachedList = { id: KEY_LIST, workouts, cachedAt: new Date().toISOString() }
  return tx(STORE_WORKOUTS, 'readwrite', (store) => store.put(row))
}

export async function getCachedWorkoutList(): Promise<Workout[] | null> {
  try {
    const row = await tx<CachedList | undefined>(STORE_WORKOUTS, 'readonly', (store) => store.get(KEY_LIST))
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
      const resp = await customFetch(`/api/workouts/${workoutId}`, {
        headers: { Accept: 'application/json' },
      })

      if (!resp.ok) {
        continue
      }

      await cacheWorkoutDetail((await resp.json()) as WorkoutDetailResponse)
    }
    catch {
      return
    }
  }
}

const KEY_SCHED = '__schedule__'

type CachedSchedule = { id: string; entries: unknown[]; cachedAt: string }

export function cacheScheduleEntries(entries: readonly unknown[]): Promise<IDBValidKey> {
  const row: CachedSchedule = { id: KEY_SCHED, entries: [...entries], cachedAt: new Date().toISOString() }
  return tx(STORE_WORKOUTS, 'readwrite', (store) => store.put(row))
}

export async function getCachedScheduleEntries<T>(): Promise<T[] | null> {
  try {
    const row = await tx<CachedSchedule | undefined>(STORE_WORKOUTS, 'readonly', (store) => store.get(KEY_SCHED))
    return (row?.entries as T[]) ?? null
  }
  catch {
    return null
  }
}

async function getListCachedAt(): Promise<number | null> {
  try {
    const row = await tx<CachedList | undefined>(STORE_WORKOUTS, 'readonly', (store) => store.get(KEY_LIST))
    return row ? new Date(row.cachedAt).getTime() : null
  }
  catch {
    return null
  }
}

const WARM_IN_MS = 30 * 60 * 1000

export async function warmOfflineCache(force = false): Promise<void> {
  if (!navigator.onLine) {
    return
  }

  if (!force) {
    const cachedGottenAt = await getListCachedAt()

    if (cachedGottenAt && Date.now() - cachedGottenAt < WARM_IN_MS) {
      return
    }
  }

  try {
    const resp = await customFetch('/api/workouts', { headers: { Accept: 'application/json' } })

    if (!resp.ok) {
      return
    }

    const workouts = (await resp.json()) as Workout[]

    await cacheWorkoutList(workouts)
    await precacheWorkoutDetails(workouts.map((workout) => workout.id))
  }
  catch {
    // nothing to warm
  }

    try {
    const today = new Date()
    const firstDay = new Date(today.getFullYear(), today.getMonth(), 1)
    const leadingDays = firstDay.getDay() === 0 ? 6 : firstDay.getDay() - 1
    const firstCell = new Date(today.getFullYear(), today.getMonth(), 1 - leadingDays)
    const lastCell = new Date(today.getFullYear(), today.getMonth(), 42 - leadingDays)
    const start = new Date(Date.UTC(firstCell.getFullYear(), firstCell.getMonth(), firstCell.getDate()))
    const end = new Date(Date.UTC(lastCell.getFullYear(), lastCell.getMonth(), lastCell.getDate(), 23, 59, 59, 999))

    const schedResp = await customFetch(
      `/api/users/me/schedule?startDate=${start.toISOString()}&endDate=${end.toISOString()}`
    )

    if (schedResp.ok) {
      await cacheScheduleEntries(await schedResp.json())
    }
  }
  catch {
    // schedule uncached until page load
  }
}
