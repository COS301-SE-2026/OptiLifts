import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest'
import type { ReactNode } from 'react'
import ActiveSessionPage from '@/pages/active-session'
import { customFetch } from '@/lib/custom-fetch'

const mockNavigate = vi.fn()
let locationState: unknown

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom')

  return {
    ...actual,
    useNavigate: () => mockNavigate,
    useLocation: () => ({ state: locationState }),
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

    const scoped = within(summaryCard as HTMLElement)
    expect(scoped.getByText('Front')).toBeDefined()
    expect(scoped.getByText('Back')).toBeDefined()
    expect(scoped.getByText('Muscle')).toBeDefined()
    expect(scoped.getByText('Sets')).toBeDefined()
    expect(scoped.getAllByText('Chest').length).toBeGreaterThan(0)
    expect(scoped.getAllByText('Biceps').length).toBeGreaterThan(0)
    expect(scoped.getAllByText('2').length).toBeGreaterThan(0)
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
