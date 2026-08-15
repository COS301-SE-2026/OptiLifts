import { customFetch } from '@/lib/custom-fetch'
import { STORE_WORKOUT_LOGS, tx } from './db'
import { warmOfflineCache } from './workouts-cache'

export const WORKOUT_LOG_SYNC_EVENT = 'ol-workout-log-synced'

export type WorkoutLogSetPayload = {
  setId: string | null
  type: 'Warmup' | 'Normal' | 'DropSet'
  reps: number
  weight: number
  duration: number | null
  distance: number | null
  restTime: number
  orderIndex: number
  groupNumber: number
  rpe: number
}

export type WorkoutLogPayload = {
  logId: string
  workoutId: string
  entryId: string | null
  startedAt: string
  completedAt: string
  exercises: WorkoutLogExercisePayload[]
  notes: string | null
}

export type WorkoutLogExercisePayload = {
  exerciseId: string
  workoutExerciseId: string | null
  sets: WorkoutLogSetPayload[]
  orderIndex: number
  groupNumber: number
}


type Items = {
  logId: string
  payload: WorkoutLogPayload
  status: 'pending' | 'error'
  attempts: number
  lastError?: string
  updatedAt: string
}

const store = <T>(mode: IDBTransactionMode, action: (store: IDBObjectStore) => IDBRequest<T>) =>
  tx<T>(STORE_WORKOUT_LOGS, mode, action)


export function enqueue(payload: WorkoutLogPayload): Promise<IDBValidKey> {
  const item: Items = { logId: payload.logId, payload, status: 'pending', attempts: 0, updatedAt: new Date().toISOString(), }
  return store('readwrite', (store) => store.put(item))
}

function getOutBox(): Promise<Items[]> {
  return store('readonly', (store) => store.getAll())
}

let flush = false

export async function flushOutBox(): Promise<void> {
  if (flush || !navigator.onLine) {
    return
  }

  flush = true

  try {
    const items = await getOutBox()

    for (const item of items) {
      try {
        const res = await customFetch(`/api/workouts/${item.payload.workoutId}/logs`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(item.payload),
        })

        if (res.ok || res.status === 409) {
          await store('readwrite', (store) => store.delete(item.logId))
          globalThis.dispatchEvent(new CustomEvent(WORKOUT_LOG_SYNC_EVENT, {
            detail: {
              logId: item.logId,
              workoutId: item.payload.workoutId,
            },
          }))
        } 
        else {
          await mark(item, `HTTP ${res.status}`)
        }
      } 
      catch (err) {
        await mark(item, err instanceof Error ? err.message : 'network error')
      }
    }
  } 
  finally {
    flush = false
  }
}

function mark(item: Items, lastError: string): Promise<IDBValidKey> {
  const updated: Items = {
    ...item,
    status: 'error',
    attempts: item.attempts + 1,
    lastError,
    updatedAt: new Date().toISOString(),
  }

  return store('readwrite', (store) => store.put(updated))
}

export function initOfflineWorkoutLogSync(): () => void {
  const handler = () => {
    void flushOutBox()
    void warmOfflineCache(true)
  }
  window.addEventListener('online', handler)
  void flushOutBox()
  return () => window.removeEventListener('online', handler)
}

