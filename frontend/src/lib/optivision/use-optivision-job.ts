import { useCallback, useEffect, useRef, useState } from 'react'
import { useAuth } from '@/context/auth-context'
import { submitAnalysis } from '@/lib/optivision/api'
import { pollForSumm, VisionJobErr } from '@/lib/optivision/poll-result'
import { extractLandmarks, PoseExtractionErr } from '@/lib/optivision/pose-extractor'
import type { VisionExercise, VisionJobState } from '@/types/optivision'

function toUserMessage(error: unknown): string {
  if (error instanceof PoseExtractionErr || error instanceof VisionJobErr) {
    return error.message
  }

  console.error(error)
  if (!navigator.onLine) {
    return "You're offline. Reconnect and try again."
  }

  return 'Something went wrong. Please try again.'
}

export function useOptiVisJob() {
  const { user } = useAuth()
  const [state, setState] = useState<VisionJobState>({ phase: 'idle' })
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])

  const reset = useCallback(() => {
    controllerRef.current?.abort()
    setState({ phase: 'idle' })
  }, [])

  const start = useCallback(
    async (file: File, exercise: VisionExercise) => {
      if (!user) {
        setState({ phase: 'failed', message: 'Please log in again.' })
        return
      }

      controllerRef.current?.abort()
      const controller = new AbortController()
      controllerRef.current = controller

      let lastPerc = -1
      const reportProg = (fraction: number) => {
        const percent = Math.floor(fraction * 100)
        if (percent !== lastPerc) {
          lastPerc = percent
          setState({ phase: 'extracting', progress: percent })
        }
      }

      try {
        setState({ phase: 'extracting', progress: 0 })
        const frames = await extractLandmarks(file, reportProg, controller.signal)

        setState({ phase: 'submitting' })
        const { jobId } = await submitAnalysis(
          { userId: user.id, exercise, view: 'side', frames },
          controller.signal,
        )

        setState({ phase: 'polling' })
        const { coachSummary, score, issues } = await pollForSumm(jobId, controller.signal)
        setState({ phase: 'completed', coachSummary, score, issues })
      } 
      catch (error) {
        if (controller.signal.aborted) {
          return
        }
        setState({ phase: 'failed', message: toUserMessage(error) })
      }
    },
    [user],
  )

  return { state, start, reset }
}
