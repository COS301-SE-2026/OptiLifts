import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import type { ReactNode } from 'react'
import ActiveSessionPage from '@/pages/active-session'
import { customFetch } from '@/lib/custom-fetch'

const mockNavigate = vi.fn()
let locationState: unknown

let mockParams: Record<string, string | undefined> = {}

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')

  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ state: locationState }),
    useParams: () => mockParams,
  }
})

vi.mock('@/lib/custom-fetch', () => ({
  customFetch: vi.fn(),
}))

vi.mock('@/components/ui/dropdown-menu', () => ({
  DropdownMenu: ({ children }: Readonly<{ children: ReactNode }>) => <div>{children}</div>,
  DropdownMenuTrigger: ({ children }: Readonly<{ children: ReactNode }>) => <button type="button">{children}</button>,
  DropdownMenuContent: ({ children }: Readonly<{ children: ReactNode }>) => <div>{children}</div>,
  DropdownMenuItem: ({ children, onSelect }: Readonly<{ children: ReactNode; onSelect?: () => void }>) => (
    <button type="button" onClick={() => onSelect?.()}>{children}</button>
  ),
}))

vi.mock('@/components/ui/exercise-picker-dialog', () => ({
  ExercisePickerDialog: () => null,
}))

vi.mock('@/components/ui/exercise-details-popup', () => ({
  ExerciseDetailsPopup: () => null,
}))

vi.mock('canvas-confetti', () => ({
  default: vi.fn(),
}))

describe('ActiveSessionPage summary section', () => {
  const mockFetch = customFetch as unknown as Mock

  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    locationState = {
      workout: {
        id: 'workout-1',
        name: 'Push Day',
        primaryMuscleGroups: ['Chest'],
      },
    }
  })

  afterEach(() => {
    cleanup()
    localStorage.clear()
  })

  it('renders summary card with muscle diagram and set breakdown', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({
        id: 'workout-1',
        name: 'Push Day',
        folderId: null,
        dayIndex: null,
        createdAt: '2026-07-28T12:00:00.000Z',
        primaryMuscleGroups: ['Chest', 'Biceps'],
        exercisePreview: [],
        exercises: [
          {
            id: 'we-1',
            exerciseId: 'ex-1',
            name: 'Bench Press',
            primaryMuscle: 'Chest',
            exerciseType: 'WeightReps',
            orderIndex: 0,
            sets: [
              { id: 'set-1', type: 'Normal', reps: 10, weight: 60, duration: null, distance: null, orderIndex: 0, restTime: 90 },
              { id: 'set-2', type: 'Normal', reps: 8, weight: 65, duration: null, distance: null, orderIndex: 1, restTime: 90 },
            ],
          },
          {
            id: 'we-2',
            exerciseId: 'ex-2',
            name: 'Bicep Curl',
            primaryMuscle: 'Biceps',
            exerciseType: 'WeightReps',
            orderIndex: 1,
            sets: [
              { id: 'set-3', type: 'Normal', reps: 12, weight: 15, duration: null, distance: null, orderIndex: 0, restTime: 60 },
            ],
          },
        ],
      }),
    })

    render(<ActiveSessionPage />)

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith('/api/workouts/workout-1', expect.any(Object))
    })

    const summaryTitle = await screen.findByText('Summary')
    const summaryCard = summaryTitle.closest('[data-slot="card"]')
    expect(summaryCard).not.toBeNull()

    const setButtons = document.querySelectorAll('.bg-surface-2.hover\\:border-brand')
    if (setButtons.length > 0) {
      const chestSetButton = setButtons[0] as HTMLButtonElement
      const bicepSetButton = setButtons[2] as HTMLButtonElement
      chestSetButton.click()
      bicepSetButton.click()
    }

    const scoped = within(summaryCard as HTMLElement)
    
    await waitFor(() => {
      expect(scoped.getAllByText('Chest').length).toBeGreaterThan(0)
    })
    
    expect(scoped.getByText('Front')).toBeDefined()
    expect(scoped.getByText('Back')).toBeDefined()
    expect(scoped.getByText('Muscle')).toBeDefined()
    expect(scoped.getByText('Sets')).toBeDefined()
    expect(scoped.getAllByText('Biceps').length).toBeGreaterThan(0)
    expect(scoped.getAllByText('1').length).toBeGreaterThan(0)
  })

  it('shows empty summary state when workout has no exercises', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({
        id: 'workout-1',
        name: 'Push Day',
        folderId: null,
        dayIndex: null,
        createdAt: '2026-07-28T12:00:00.000Z',
        primaryMuscleGroups: [],
        exercisePreview: [],
        exercises: [],
      }),
    })

    render(<ActiveSessionPage />)

    const summaryTitle = await screen.findByText('Summary')
    const summaryCard = summaryTitle.closest('[data-slot="card"]')
    expect(summaryCard).not.toBeNull()

    await waitFor(() => {
      expect(within(summaryCard as HTMLElement).getByText('No targeted muscles were recorded for this workout.')).toBeDefined()
    })
  })
})

describe('ActiveSessionPage edit mode', () => {
  const mockFetch = customFetch as unknown as Mock

  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    mockParams = {
      workoutId: 'w-100',
      logId: 'l-200',
    }
  })

  afterEach(() => {
    cleanup()
    mockParams = {}
  })

  it('loads past workout log and displays static duration without ticking timer', async () => {
    mockFetch.mockResolvedValue({
      ok: true,
      json: async () => ({
        workoutId: 'w-100',
        logId: 'l-200',
        name: 'Heavy Leg Day',
        startedAt: '2026-08-10T10:00:00.000Z',
        completedAt: '2026-08-10T11:15:00.000Z',
        duration: '01:15',
        primaryMuscleGroups: ['Quadriceps', 'Hamstrings'],
        exercises: [
          {
            id: 'we-1',
            exerciseId: 'ex-1',
            name: 'Squat',
            primaryMuscle: 'Quadriceps',
            exerciseType: 'WeightReps',
            orderIndex: 0,
            sets: [
              { id: 'set-1', type: 'Normal', reps: 10, weight: 100, duration: null, distance: null, orderIndex: 0, restTime: 90, groupNumber: 0, rpe: 8 },
            ],
          },
        ],
      }),
    })

    render(<ActiveSessionPage mode="edit" />)

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith('/api/workouts/w-100/logs/l-200', expect.any(Object))
    })

    expect(await screen.findByText('Editing Past Workout')).toBeDefined()
    expect(screen.getByText('Heavy Leg Day')).toBeDefined()
    expect(screen.getByText('1h 15min')).toBeDefined()
    expect(screen.getByText('Save Changes')).toBeDefined()
    expect(screen.getByText('Back to Workout Log')).toBeDefined()
  })

function getSetRow(container: HTMLElement, index: number): HTMLElement {
  const removeButtons = container.querySelectorAll('button[aria-label="Remove set"]')
  return removeButtons[index].closest('.grid') as HTMLElement
}

function getRowInpts(row: HTMLElement) {
  const inpts = row.querySelectorAll('input')
  return { kg: inpts[1], reps: inpts[2], rpe: row.querySelector('input[placeholder="RPE"]') as HTMLInputElement }
}

function getToggleBtn(row: HTMLElement): HTMLButtonElement {
  const removeBtn = row.querySelector('button[aria-label="Remove set"]') as HTMLButtonElement
  return removeBtn.parentElement!.querySelector('div > button') as HTMLButtonElement
}

const benchPressWorkout = (sets: { id: string; reps: number; weight: number }[]) => ({
  id: 'workout-1', name: 'Push Day', folderId: null, dayIndex: null, createdAt: '2026-07-28T12:00:00.000Z',
  primaryMuscleGroups: ['Chest'], exercisePreview: [],
  exercises: [{
    id: 'we-1', exerciseId: 'ex-1', name: 'Bench Press', primaryMuscle: 'Chest', exerciseType: 'WeightReps', orderIndex: 0,
    sets: sets.map((s, i) => ({ ...s, type: 'Normal' as const, duration: null, distance: null, orderIndex: i, restTime: 90 })),
  }],
})

describe('Acute Fatigue', () => {
  const fetchMock = customFetch as unknown as Mock

  beforeEach(() => {
    vi.clearAllMocks()
    localStorage.clear()
    mockParams = {}
    locationState = { workout: { id: 'workout-1', name: 'Push Day', primaryMuscleGroups: ['Chest'] } }
  })

  afterEach(() => {
    cleanup()
    localStorage.clear()
  })

  it('flags the missing-RPE icon when 2+ sets miss target with no RPE', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => benchPressWorkout([{ id: 'set-1', reps: 10, weight: 100 }, { id: 'set-2', reps: 10, weight: 100 }]),
    })

    const { container } = render(<ActiveSessionPage />)
    await screen.findByText('Bench Press')

    const rows = [getSetRow(container, 0), getSetRow(container, 1)]
    for (const row of rows) {
      const { kg } = getRowInpts(row)
      fireEvent.change(kg, { target: { value: '80' } })
    }

    fireEvent.click(getToggleBtn(rows[0]))
    fireEvent.click(getToggleBtn(rows[1]))
    expect(await screen.findByLabelText('Log RPE to enable fatigue detection')).toBeDefined()

    fireEvent.click(getToggleBtn(rows[1]))
    await waitFor(() => {
      expect(screen.queryByLabelText('Log RPE to enable fatigue detection')).toBeNull()
    })
  })


  it('flags muscle-group fatigue for more than 2 sets and clears when unflagged', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      json: async () => benchPressWorkout([{ id: 'set-1', reps: 10, weight: 100 }, { id: 'set-2', reps: 10, weight: 100 }]),
    })

    const { container } = render(<ActiveSessionPage />)
    await screen.findByText('Bench Press')

    const rows = [getSetRow(container, 0), getSetRow(container, 1)]
    for (const row of rows) {
      const { kg, rpe } = getRowInpts(row)
      fireEvent.change(kg, { target: { value: '80' } })
      fireEvent.change(rpe, { target: { value: '9' } })
    }

    fireEvent.click(getToggleBtn(rows[0]))
    fireEvent.click(getToggleBtn(rows[1]))


    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith('/api/training/acute-fatigue', expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ muscleGroup: 'Chest' }),
      }))
    })

    const nameBtn = screen.getByRole('button', { name: 'Bench Press' })
    expect(nameBtn.nextElementSibling).not.toBeNull() 

    fireEvent.click(getToggleBtn(rows[1]))
    await waitFor(() => {
      expect(nameBtn.nextElementSibling).toBeNull()
    })
  })
})

})
