import { useState, useEffect, useRef } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from '@/components/ui/dropdown-menu'

  export function VideoFeedback({ issues, videoFile, exercise }: Readonly<{ issues: readonly string[], videoFile?: File | null, exercise: string }>) {
  const userVideoRef = useRef<HTMLVideoElement>(null)
  const [selectedIssue, setSelectedIssue] = useState(issues[0] || '')

  useEffect(() => {
    if (videoFile && userVideoRef.current) {
      const url = URL.createObjectURL(videoFile)
      userVideoRef.current.src = url
      return () => URL.revokeObjectURL(url)
    }
  }, [videoFile])

  if (!issues || issues.length === 0){
    return null
  } 

  const handleErrorSelect = (issue: string) => {
    setSelectedIssue(issue)
    const parts = issue.split('|')
    if (parts.length > 1 && userVideoRef.current) {
      const frameStr = parts[1]
      const startTimeInSeconds = Number.parseInt(frameStr, 10) / 30.0
      userVideoRef.current.currentTime = startTimeInSeconds
      userVideoRef.current.play()
    }
  }

  return (
    <Card className="border-border bg-card mt-6">
      <CardHeader className="px-5 py-4">
        <CardTitle className="text-base font-bold text-foreground">Video Review</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="outline" className="mb-4 w-full justify-start font-normal text-foreground border-border">
              {selectedIssue ? `Review Issue: ${selectedIssue.split('|')[0].replaceAll('_', ' ')}` : 'Select an identified issue'}
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-full min-w-[200px]">
            {issues.map(issue => {
              const name = issue.split('|')[0].replaceAll('_', ' ')
              return (
                <DropdownMenuItem key={issue} onClick={() => handleErrorSelect(issue)} className="cursor-pointer">
                  {name}
                </DropdownMenuItem>
              )
            })}
          </DropdownMenuContent>
        </DropdownMenu>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <h4 className="font-semibold mb-2 text-sm text-green-500">Perfect Form</h4>
            <video src={`/perfect_${exercise}.mp4`} playsInline controls className="w-full rounded border border-border">
              <track kind="captions" srcLang="en" label="English" />
            </video>
          </div>
          <div>
            <h4 className="font-semibold mb-2 text-sm text-destructive">Your Form</h4>
            
            <video ref={userVideoRef} playsInline controls className="w-full rounded border border-destructive">
              <track kind="captions" srcLang="en" label="English" />
            </video>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
