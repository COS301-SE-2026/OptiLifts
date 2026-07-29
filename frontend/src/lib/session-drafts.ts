const PREFIX = 'optilifts:session-draft:'

export function saveDraft<T>(workoutId: string, draft: T): void {
  try {
    localStorage.setItem(PREFIX + workoutId, JSON.stringify(draft))
  } 
  catch {
    // serilatisation or quota fail - we can't do anything
  }
}

// used for actual retrieving the draft
export function getDraftFromStorage(): { workoutId: string; workoutName: string } | null {
  for (let i = 0; i < localStorage.length; i++) {
    const keyInStorage = localStorage.key(i)

    if (!keyInStorage?.startsWith(PREFIX)) {
      continue
    }

    try {
      const data = JSON.parse(localStorage.getItem(keyInStorage) ?? '') as { workoutId?: string; workoutName?: string }

      if (data.workoutId && data.workoutName) {
        return { workoutId: data.workoutId, workoutName: data.workoutName }
      }
    } 
    catch {
      continue
    }
  }

  return null
}

export function getDraft<T>(workoutId: string): T | null {
  try {
    const data = localStorage.getItem(PREFIX + workoutId)

    return data ? (JSON.parse(data) as T) : null
  } 
  catch {
    return null
  }
}

export function clearDraft(workoutId: string): void {
  localStorage.removeItem(PREFIX + workoutId)
}
