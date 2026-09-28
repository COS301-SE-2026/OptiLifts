import { useState } from 'react'
import { ExerGuide } from '@/components/optivision/exercise-guide'
import { ProgressPanel } from '@/components/optivision/progress-panel'
import { ResultPanel } from '@/components/optivision/result-panel'
import { VideoFeedback } from '@/components/optivision/video-feedback'
import { UploadPanel } from '@/components/optivision/upload-panel'
import { OfflineBanner } from '@/components/ui/offline-banner'
import { PageTitle } from '@/components/ui/page-title'
import { useOptiVisJob } from '@/lib/optivision/use-optivision-job'
import { useOnlineStatus } from '@/lib/use-online-status'
import type { VisionExercise } from '@/types/optivision'

export default function FormCheckPage() {
  const { state, start, reset } = useOptiVisJob()
  const isOnline = useOnlineStatus()
  const [exercise, setExercise] = useState<VisionExercise>('squat')
  const [vidFile, setvidFile] = useState<File | null>(null)

  function renderPanel() {
    switch (state.phase) {
      case 'idle':
        return (
          <UploadPanel
            disabled={!isOnline}
            onAnalyse={(file) => {
              setvidFile(file)
              void start(file, exercise)
            }}
          />
        )
      case 'extracting':
      case 'submitting':
      case 'polling':
        return <ProgressPanel state={state} onCancel={reset} />
      default:
        return <ResultPanel state={state} onReset={() => { setvidFile(null); reset(); }}  />
    }
  }

  return (
    <section className="mx-auto max-w-6xl px-6 py-12">
      <div className="mb-6">
        <PageTitle>
          Opti<span className="text-brand">Vision</span>
        </PageTitle>
      </div>

      {!isOnline && <OfflineBanner message="You're offline - reconnect to analyse a video." />}

      <div className="grid grid-cols-12 gap-6">
        <div className="col-span-12 min-w-0 lg:col-span-5">
          <ExerGuide exercise={exercise} onExerciseChange={setExercise} disabled={state.phase !== 'idle'} />
        </div>
        <div className="col-span-12 min-w-0 lg:col-span-7">{renderPanel()}</div>
      </div>
      {state.phase === 'completed' && state.issues && state.issues.length > 0 && (
        <VideoFeedback issues={state.issues} videoFile={vidFile} exercise={exercise} />
      )}
    </section>
  )
}
