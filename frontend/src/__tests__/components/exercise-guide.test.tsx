import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ExerGuide } from '@/components/optivision/exercise-guide'
import { EXERCISE_GUIDES } from '@/constants/optivision-guides'

afterEach(() => {
  cleanup()
})

describe('ExerGuide', () => {
  it('shows the guide for the selected exercise', () => {
    render(<ExerGuide exercise="deadlift" onExerciseChange={vi.fn()} disabled={false} />)

    const guide = EXERCISE_GUIDES.deadlift
    expect(screen.getByText(guide.name)).toBeTruthy()
    expect(screen.getByText(guide.checks[0].label)).toBeTruthy()
    expect(screen.getByRole('img').getAttribute('src')).toBe(guide.gifUrl)
  })

  it('tells the page when another exercise is picked', () => {
    const onExerChange = vi.fn()
    render(<ExerGuide exercise="squat" onExerciseChange={onExerChange} disabled={false} />)

    fireEvent.click(screen.getByRole('button', { name: /bench press/i }))

    expect(onExerChange).toHaveBeenCalledWith('bench')
  })

  it('locks the exercise buttons while a job is running', () => {
    render(<ExerGuide exercise="squat" onExerciseChange={vi.fn()} disabled />)

    expect((screen.getByRole('button', { name: /deadlift/i }) as HTMLButtonElement).disabled).toBe(true)
  })
})
