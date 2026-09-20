import { OfflineBanner } from '@/components/ui/offline-banner'
import { PageTitle } from '@/components/ui/page-title'
import { ProgressPanel } from '@/components/optivision/progress-panel'
import { ResultPanel } from '@/components/optivision/result-panel'
import { UploadPanel } from '@/components/optivision/upload-panel'
import { useOptivisionJob } from '@/lib/optivision/use-optivision-job'
import { useOnlineStatus } from '@/lib/use-online-status'

export default function FormCheckPage() {
  const { state, start, reset } = useOptivisionJob()
  const isOnline = useOnlineStatus()

  function renderPanel() {
    switch (state.phase) {
      case 'idle':
        return (
          <UploadPanel
            disabled={!isOnline}
            onAnalyse={(file, exercise) => {
              void start(file, exercise)
            }}
          />
        )
      case 'extracting':
      case 'submitting':
      case 'polling':
        return <ProgressPanel state={state} onCancel={reset} />
      default:
        return <ResultPanel state={state} onReset={reset} />
    }
  }

  return (
    <section className="mx-auto max-w-2xl px-6 pt-12 pb-12">
      <div className="mb-4">
        <PageTitle title="FORM CHECK" />
      </div>
      <p className="mb-6 text-sm text-muted-foreground">
        Upload a side-on video of your set and get coaching on your technique.
      </p>

      {!isOnline && <OfflineBanner message="You're offline - reconnect to analyse a video." />}

      {renderPanel()}
    </section>
  )
}
