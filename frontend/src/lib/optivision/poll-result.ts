import { fetchResult } from '@/lib/optivision/api'
import { POLL_INTERVAL_MS, POLL_TIMEOUT_MS } from '@/constants/optivision'

export class VisionJobErr extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'VisionJobError'
  }
}

function resDelay(ms: number, signal: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    signal.throwIfAborted()

    const timer = setTimeout(() => {
      signal.removeEventListener('abort', onAbort)
      resolve()
    }, ms)

    function onAbort() {
      clearTimeout(timer)
      reject(signal.reason)
    }

    signal.addEventListener('abort', onAbort, { once: true })
  })
}

export async function pollForSumm(jobId: string, signal: AbortSignal): Promise<{ coachSummary: string, score: number, issues: readonly string[] }> {
  const deadline = Date.now() + POLL_TIMEOUT_MS

  while (Date.now() < deadline) {
    const res = await fetchResult(jobId, signal)

    if (res.status === 'completed') {
      return { 
        coachSummary: res.coach_summary ?? '', 
        score: res.score ?? 100,
        issues: res.issues ?? []
      }
    }
    if (res.status === 'failed') {
      throw new VisionJobErr("We couldn't analyse this video. Please try again.")
    }

    await resDelay(POLL_INTERVAL_MS, signal)
  }

  throw new VisionJobErr('The analysis is taking longer than expected. Please try again later.')
}
