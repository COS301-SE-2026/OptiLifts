import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import FormCheckPage from '@/pages/form-check'
import { useOptiVisJob } from '@/lib/optivision/use-optivision-job'
import { useOnlineStatus } from '@/lib/use-online-status'

vi.mock('@/lib/optivision/use-optivision-job', () => ({ useOptiVisJob: vi.fn() }))
vi.mock('@/lib/use-online-status', () => ({ useOnlineStatus: vi.fn() }))

const mockJob = useOptiVisJob as unknown as Mock
const mockOnline = useOnlineStatus as unknown as Mock

describe('FormCheckPage', () => {
  const theStart = vi.fn()

  beforeEach(() => {
    vi.resetAllMocks()
    mockOnline.mockReturnValue(true)
    mockJob.mockReturnValue({ state: { phase: 'idle' }, start: theStart, reset: vi.fn() })
  })

  afterEach(() => {
    cleanup()
  })

  it('shows the exercise guide and the upload panel when idle', () => {
    render(<FormCheckPage />)

    expect(screen.getByText('Choose your exercise')).toBeTruthy()
    expect(screen.getByText('Analyse a set')).toBeTruthy()
  })

  it('starts the analysis with the exercise the user picked', () => {
    const vid = new File(['x'], 'pull.mp4', { type: 'video/mp4' })
    render(<FormCheckPage />)

    fireEvent.click(screen.getByRole('button', { name: /deadlift/i }))
    fireEvent.change(document.querySelector('input[type="file"]') as HTMLInputElement, {
      target: { files: [vid] },
    })
    fireEvent.click(screen.getByRole('button', { name: /analyse my form/i }))

    expect(theStart).toHaveBeenCalledWith(vid, 'deadlift')
  })

  it('shows progress instead of the upload panel while a job is running', () => {
    mockJob.mockReturnValue({ state: { phase: 'extracting', progress: 40 }, start: theStart, reset: vi.fn() })
    render(<FormCheckPage />)

    expect(screen.getByRole('progressbar')).toBeTruthy()
    expect(screen.queryByText('Analyse a set')).toBeNull()
  })
})
