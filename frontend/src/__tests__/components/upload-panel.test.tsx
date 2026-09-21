import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { UploadPanel } from '@/components/optivision/upload-panel'

afterEach(() => {
  cleanup()
})

function fileChoose(file: File) {
  const input = document.querySelector('input[type="file"]') as HTMLInputElement
  fireEvent.change(input, { target: { files: [file] } })
}

function analyseButt() {
  return screen.getByRole('button', { name: /analyse my form/i }) as HTMLButtonElement
}

describe('UploadPanel', () => {
  it('cannot analyse until a video is chosen', () => {
    render(<UploadPanel disabled={false} onAnalyse={vi.fn()} />)

    expect(analyseButt().disabled).toBe(true)
  })

  it('analyses the chosen video', () => {
    const onAnalyse = vi.fn()
    const vid = new File(['x'], 'squat.mp4', { type: 'video/mp4' })
    render(<UploadPanel disabled={false} onAnalyse={onAnalyse} />)

    fileChoose(vid)
    expect(analyseButt().disabled).toBe(false)
    fireEvent.click(analyseButt())

    expect(onAnalyse).toHaveBeenCalledWith(vid)
  })

  it('shows an error and stays disabled for a file that is not a video', () => {
    render(<UploadPanel disabled={false} onAnalyse={vi.fn()} />)

    fileChoose(new File(['x'], 'notes.txt', { type: 'text/plain' }))

    expect(screen.getByRole('alert').textContent).toBe('Please choose a video file.')
    expect(analyseButt().disabled).toBe(true)
  })
})
