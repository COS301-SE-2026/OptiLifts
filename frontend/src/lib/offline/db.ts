const NAME = 'offline-db'
const VERSION = 2

export const STORE_WORKOUT_LOGS = 'workoutLogs'
export const STORE_WORKOUTS = 'workouts'

export function openDB(): Promise<IDBDatabase> {
  return new Promise((resolve, reject) => {
    if (typeof indexedDB === 'undefined') {
      reject(new Error('IndexedDB is not available'))
      return
    }

    const req = indexedDB.open(NAME, VERSION)

    req.onupgradeneeded = () => {
      const db = req.result

      if (!db.objectStoreNames.contains(STORE_WORKOUT_LOGS)) {
        db.createObjectStore(STORE_WORKOUT_LOGS, { keyPath: 'logId' })
      }

      if (!db.objectStoreNames.contains(STORE_WORKOUTS)) {
        db.createObjectStore(STORE_WORKOUTS, { keyPath: 'id' })
      }
    }

    req.onsuccess = () => resolve(req.result)
    req.onerror = () => reject(req.error ?? new Error('Could not open offline database'))
  })
}

export function tx<T>( storeName: string, mode: IDBTransactionMode, action: (store: IDBObjectStore) => IDBRequest<T>
): Promise<T> {
  return openDB().then(
    (db) =>
      new Promise<T>((resolve, reject) => {
        const trans = db.transaction(storeName, mode)
        const req = action(trans.objectStore(storeName))

        trans.oncomplete = () => {
          db.close()
          resolve(req.result)
        }

        trans.onerror = () => {
          db.close()
          reject(trans.error ?? new Error('Offline database transaction failed'))
        }
      })
  )
}
