import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import PlateauPage from '@/pages/plateau'
import { customFetch } from '@/lib/custom-fetch'
import { useOnlineStatus } from '@/lib/use-online-status'

vi.mock('@/lib/custom-fetch', () => ({
  customFetch: vi.fn(),
}))

vi.mock('@/lib/use-online-status', () => ({
  useOnlineStatus: vi.fn(() => true),
}))

vi.mock('@/components/ui/exercise-picker-dialog', () => ({
  ExercisePickerDialog: ({ isOpen, title, onSelect }: {
    isOpen: boolean
    title?: string
    onSelect: (exercise: { id: string; name: string; muscleGroup: string }) => void
  }) =>
    isOpen ? (
      <div>
        <p>{title}</p>
        <button type="button" onClick={() => onSelect({ id: 'new-ex-1', name: 'Cable Row', muscleGroup: 'Back' })}>
          Pick Cable Row
        </button>
      </div>
    ) : null,
}))

type ExerFixtr = {
  exerciseId: string
  exerciseName: string
  muscleGroup: string
  status: 'Progressing' | 'Plateau' | 'Regressing'
  slopePctPerWeek: number
  recommendation: string | null
  canSwapExercise: boolean
  computedAt: string
  workouts: { workoutId: string; workoutName: string }[]
}

const baseExer = (overrides: Partial<ExerFixtr> = {}): ExerFixtr => ({
  exerciseId: 'ex-1',
  exerciseName: 'Bench Press',
  muscleGroup: 'Chest',
  status: 'Plateau',
  slopePctPerWeek: -0.2,
  recommendation: 'Try changing this exercise or adjusting your rep range.',
  canSwapExercise: true,
  computedAt: '2026-08-01T00:00:00.000Z',
  workouts: [{ workoutId: 'w-1', workoutName: 'Push Day' }],
  ...overrides,
})

describe('PlateauPage', () => {
  const fetchMock = customFetch as unknown as Mock
  const onlineStatMock = useOnlineStatus as unknown as Mock

  beforeEach(() => {
    vi.clearAllMocks()
    onlineStatMock.mockReturnValue(true)
  })

  afterEach(() => {
    cleanup()
  })

  it('shows empty state', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => [] })

    render(<PlateauPage />)

    expect(await screen.findByText(/No exercises have enough data yet/)).toBeDefined()
  })

  it('renders the status badge and recommendation', async () => {
    fetchMock.mockResolvedValue({ ok: true, json: async () => [baseExer()] })

    render(<PlateauPage />)

    expect(await screen.findByText('Bench Press')).toBeDefined()
    expect(screen.getByText('Plateau')).toBeDefined()
    expect(screen.getByText('Try changing this exercise or adjusting your rep range.')).toBeDefined()
  })

  it('shows swap button per workout when canSwapExer and workouts exist', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => [baseExer({
        canSwapExercise: true,
        workouts: [{ workoutId: 'w-1', workoutName: 'Push Day' }, { workoutId: 'w-2', workoutName: 'Chest Day' }],
      })],
    })

    render(<PlateauPage />)

    expect(await screen.findByText('Swap in Push Day')).toBeDefined()
    expect(screen.getByText('Swap in Chest Day')).toBeDefined()
    expect(screen.queryByText(/already swapped/)).toBeNull()
  })

  it('shows the already swapped message and no button', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => [baseExer({ canSwapExercise: true, workouts: [] })],
    })

    render(<PlateauPage />)

    expect(await screen.findByText('You have already swapped this exercise to an alternative exercise.')).toBeDefined()
    expect(screen.queryByText(/^Swap in/)).toBeNull()
  })

  it('hides the swap when canSwapExercise false', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => [baseExer({ status: 'Progressing', canSwapExercise: false, recommendation: null })],
    })

    render(<PlateauPage />)

    await screen.findByText('Bench Press')
    expect(screen.queryByText(/^Swap in/)).toBeNull()
    expect(screen.queryByText(/already swapped/)).toBeNull()
  })

  it('Opens the picker, selects an exer, PUTs, and refetches', async () => {
    fetchMock.mockImplementation(async (url: string, options?: RequestInit) => {
      if (options?.method === 'PUT') {
        return { ok: true, json: async () => ({}) }
      }
      return { ok: true, json: async () => [baseExer()] }
    })

    render(<PlateauPage />)

    const swapBtn = await screen.findByText('Swap in Push Day')
    swapBtn.click()

    expect(await screen.findByText('Swap exercise in Push Day')).toBeDefined()

    screen.getByText('Pick Cable Row').click()

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        '/api/workouts/w-1/exercises/ex-1',
        expect.objectContaining({
          method: 'PUT',
          body: JSON.stringify({ newExerciseId: 'new-ex-1' }),
        }),
      )
    })

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith('/api/training/plateau-page')
    })
  })

  it('disables swap buttons and offline banner', async () => {
    onlineStatMock.mockReturnValue(false)
    fetchMock.mockResolvedValue({ ok: true, json: async () => [baseExer()] })

    render(<PlateauPage />)

    const swapBtn = (await screen.findByText('Swap in Push Day')) as HTMLButtonElement
    expect(swapBtn.disabled).toBe(true)
    expect(screen.getByText(/offline/i)).toBeDefined()
  })
})
