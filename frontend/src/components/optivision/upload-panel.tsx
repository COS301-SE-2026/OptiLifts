import { useRef, useState, type ChangeEvent, type SyntheticEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { MAX_VIDEO_SECONDS, MIN_VIDEO_SECONDS, VISION_EXERCISES } from '@/constants/optivision'
import { validateVideoFile } from '@/lib/optivision/validate-video'
import { cn } from '@/lib/utils'
import type { VisionExercise } from '@/types/optivision'

const LABEL_CLASS = 'text-xs sm:text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground'

type UploadPanelProps = Readonly<{
  disabled: boolean
  onAnalyse: (file: File, exercise: VisionExercise) => void
}>

export function UploadPanel({ disabled, onAnalyse }: UploadPanelProps) {
  const [exercise, setExercise] = useState<VisionExercise>('squat')
  const [file, setFile] = useState<File | null>(null)
  const [fileError, setFileError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const picked = event.target.files?.[0] ?? null
    const error = picked ? validateVideoFile(picked) : null
    setFileError(error)
    setFile(error ? null : picked)
  }

  function handleSubmit(event: SyntheticEvent<HTMLFormElement>) {
    event.preventDefault()
    if (file) {
      onAnalyse(file, exercise)
    }
  }

  return (
    <Card className="border-border bg-card">
      <CardHeader className="px-5">
        <CardTitle className="text-base font-bold text-foreground">Analyse a set</CardTitle>
      </CardHeader>
      <CardContent>
        <form className="flex flex-col gap-5" onSubmit={handleSubmit}>
          <div className="flex flex-col gap-1.5">
            <span className={LABEL_CLASS}>Exercise</span>
            <div className="flex gap-2">
              {VISION_EXERCISES.map(({ value, label }) => (
                <button
                  key={value}
                  type="button"
                  aria-pressed={exercise === value}
                  onClick={() => setExercise(value)}
                  className={cn(
                    'min-h-11 flex-1 cursor-pointer rounded-lg px-2 text-xs font-semibold uppercase tracking-[0.05em] outline-none transition-all focus-visible:ring-2 focus-visible:ring-brand',
                    exercise === value
                      ? 'bg-brand text-white shadow-xs'
                      : 'border border-border bg-surface text-muted-foreground hover:bg-surface-2',
                  )}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>

          <div className="flex flex-col gap-1.5">
            <span className={LABEL_CLASS}>For best results</span>
            <ul className="list-disc space-y-1 pl-5 text-sm text-muted-foreground">
              <li>Film from the side, so the camera sees your profile.</li>
              <li>Keep your whole body in frame, from head to feet.</li>
              <li>Hold the camera still, ideally on a tripod at hip height.</li>
              <li>One lifter in shot, with good lighting.</li>
              <li>
                Film one set, between {MIN_VIDEO_SECONDS} and {MAX_VIDEO_SECONDS} seconds.
              </li>
            </ul>
          </div>

          <div className="flex flex-col gap-1.5">
            <span className={LABEL_CLASS}>Video</span>
            <div className="rounded-lg border border-dashed border-border p-3">
              <input
                ref={fileInputRef}
                type="file"
                accept="video/*"
                className="sr-only"
                onChange={handleFileChange}
              />
              <div className="flex flex-wrap items-center gap-3">
                <Button type="button" variant="secondary" size="sm" onClick={() => fileInputRef.current?.click()}>
                  {file ? 'Change video' : 'Choose video'}
                </Button>
                {file && (
                  <span className="min-w-0 break-all text-xs text-muted-foreground">
                    {file.name} ({(file.size / 1024 / 1024).toFixed(1)} MB)
                  </span>
                )}
              </div>
            </div>
            {fileError && (
              <p role="alert" className="rounded-xl border border-destructive/30 bg-destructive/10 px-3.5 py-2.5 text-xs text-destructive">
                {fileError}
              </p>
            )}
          </div>

          <p className="text-xs text-muted-foreground">
            Your video stays on your device. Only body-point coordinates are sent for analysis.
          </p>

          <Button type="submit" disabled={!file || disabled}>
            Analyse my form
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
