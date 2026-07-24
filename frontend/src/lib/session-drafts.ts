const PREFIX = 'optilifts:session-draft:'

export function saveDraft<T>(workoutId: string, draft: T): void {
  try {
    localStorage.setItem(PREFIX + workoutId, JSON.stringify(draft))
  } 
  catch {
    // serilatisation or quota fail - we can't do anything
  }
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
