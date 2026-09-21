import { useRef, useState, type ChangeEvent, type SyntheticEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { MAX_VIDEO_SECONDS, MIN_VIDEO_SECONDS } from '@/constants/optivision'
import { validateVideoFile } from '@/lib/optivision/validate-video'

const LABEL_CLASS = 'text-xs sm:text-sm font-semibold uppercase tracking-[0.08em] text-muted-foreground'

type UploadPanelProps = Readonly<{
  disabled: boolean
  onAnalyse: (file: File) => void
}>

export function UploadPanel({ disabled, onAnalyse }: UploadPanelProps) {
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
      onAnalyse(file)
    }
  }

  return (
    <Card className="border-border bg-card">
      <CardHeader className="px-5">
        <CardTitle className="text-base font-bold text-foreground">Analyse a set</CardTitle>
        <CardDescription>Upload a side-on video of your set and get coaching on your technique.</CardDescription>
      </CardHeader>
      <CardContent>
        <form className="flex flex-col gap-5" onSubmit={handleSubmit}>
          <div className="flex flex-col gap-1.5">
            <span className={LABEL_CLASS}>For best results</span>
            <ul className="list-disc space-y-1 pl-5 text-sm text-muted-foreground">
              <li>Film from a side-view, so we see your profile.</li>
              <li>Ensure that your entire body is in frame.</li>
              <li>Keep camera steady and preferably at hip height.</li>
              <li>Only one lifter in view, with good lighting.</li>
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

          <Button type="submit" disabled={!file || disabled}>
            Analyse my form
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
