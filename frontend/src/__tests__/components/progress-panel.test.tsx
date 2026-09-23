import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ProgressPanel } from '@/components/optivision/progress-panel'

afterEach(() => {
  cleanup()
})

describe('ProgressPanel', () => {
  it('shows a progress bar while reading the video', () => {
    render(<ProgressPanel state={{ phase: 'extracting', progress: 42 }} onCancel={vi.fn()} />)

    expect(screen.getByRole('progressbar').getAttribute('aria-valuenow')).toBe('42')
  })

  it('has no progress bar while waiting for the result, and can be cancelled', () => {
    const onCancel = vi.fn()
    render(<ProgressPanel state={{ phase: 'polling' }} onCancel={onCancel} />)

    expect(screen.queryByRole('progressbar')).toBeNull()
    fireEvent.click(screen.getByRole('button', { name: /cancel/i }))
    expect(onCancel).toHaveBeenCalled()
  })
})
