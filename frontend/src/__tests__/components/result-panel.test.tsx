import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ResultPanel } from '@/components/optivision/result-panel'

afterEach(() => {
  cleanup()
})

describe('ResultPanel', () => {
  it('shows the coach summary and lets the user start again', () => {
    const onReset = vi.fn()
    render(<ResultPanel state={{ phase: 'completed', coachSummary: 'Go a bit deeper.', score: 85, issues: ['Shallow Depth'] }} onReset={onReset} />)

    expect(screen.getByText('Go a bit deeper.')).toBeTruthy()
    expect(screen.getByText('85')).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: /analyse another set/i }))
    expect(onReset).toHaveBeenCalled()
  })

  it('shows the error message when the analysis failed', () => {
    render(<ResultPanel state={{ phase: 'failed', message: 'Something went wrong.' }} onReset={vi.fn()} />)

    expect(screen.getByRole('alert').textContent).toContain('Something went wrong.')
  })
})
