import WorkoutDetailPage from '@/pages/workout-detail';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import { customFetch } from '@/lib/custom-fetch';
//import type { ReactNode } from 'react';

//mocking dependencies -> prevents network requests + isolates component

const mockNav = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useParams: () => ({workoutId: 'workout-abc'}),
        useNavigate: () => mockNav,};
});

vi.mock('@/context/auth-context', () =>({useAuth: vi.fn()}));

//mock customfetch
vi.mock('@/lib/custom-fetch', () => ({customFetch: vi.fn()}));

//mock barchart comp (simplified tho)
vi.mock('@/components/ui/exercise-plan', () => ({
    default: ({exercises}:Readonly<{ exercises: readonly { name: string }[] }>) => (
    <div data-testid="exercise-plan">
        {exercises.map((e) => (
            <div key={e.name}>{e.name}</div>))}
    </div>
    ),}));

vi.mock('@/components/ui/muscle-diagram', () => ({
    default: () => <div data-testid="muscle-diagram" />,
}));

vi.mock('@/components/ui/confirm-dialog', async () => {
    const { mockConfirmDialog } = await import('../mocks/ui-mocks');
    return mockConfirmDialog();
});
vi.mock('@/components/ui/dropdown-menu', async () => {
    const { mockDropdownMenu } = await import('../mocks/ui-mocks');
    return mockDropdownMenu();
});



//'describe' defines suite of related tests
describe('WorkoutDetailPage', () => {
    const mockAuth = useAuth as unknown as Mock;
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        cleanup();
    });

    beforeEach(() => { //runs each 'it' test block + resets spy functions
        vi.clearAllMocks();
    });

    it('fetches and renders workout detail and stats', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });

        mockFetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                id: 'workout-abc',
                name: 'Hypertrophy Upper',
                primaryMuscleGroups: ['Chest', 'Shoulders'],
                exercises: [
                    {
                        name: 'Incline Bench Press',
                        primaryMuscle: 'Chest',
                        exerciseType: 'strength',
                        sets: [
                            {
                                orderIndex: 1,
                                reps: 8,
                                weight: 80,
                                restTime: 90
                            },
                        ],
                    },
                ],
            }),
        });
        render(<WorkoutDetailPage />);
        await waitFor(() => {
            expect(screen.getByText('Hypertrophy Upper')).toBeDefined();
        });
        expect(screen.getByText('Incline Bench Press')).toBeDefined();
        expect(screen.getByRole('button', { name: /Start Workout/i })).toBeDefined();
    });

    it('navigates to active session when Start Workout is clicked', async () => {
        const mockWorkoutData = {
            id: 'workout-abc',
            name: 'Hypertrophy Upper',
            primaryMuscleGroups: ['Chest', 'Shoulders'],
            exercises: [
                {
                    name: 'Incline Bench Press',
                    primaryMuscle: 'Chest',
                    exerciseType: 'strength',
                    sets: [
                        {
                            orderIndex: 1,
                            reps: 8,
                            weight: 80,
                            restTime: 90
                        },
                    ],
                },
            ],
        };

        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });

        mockFetch.mockResolvedValue({
            ok: true,
            json: async () => mockWorkoutData,
        });

        render(<WorkoutDetailPage />);

        await waitFor(() => {
            expect(screen.getByText('Hypertrophy Upper')).toBeDefined();
        });

        const startBtn = screen.getByRole('button', { name: /Start Workout/i });
        fireEvent.click(startBtn);

        expect(mockNav).toHaveBeenCalledWith('/active-session', {
            state: { workout: mockWorkoutData },
        });
    });

    it('handles 404 workout not found', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        mockFetch.mockResolvedValue({
            ok: false,
            status: 404
        });
        render(<WorkoutDetailPage />);

        await waitFor(() => {
            expect(screen.getByText(/Workout not found/i)).toBeDefined();
        });
    });

    it('handles error loading workout', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        mockFetch.mockResolvedValue({
            ok: false,
            status: 500
        });
        render(<WorkoutDetailPage />);

        await waitFor(() => {
            expect(screen.getByText(/Failed to load workout \(500\)/i)).toBeDefined();
        });
    });

    it('navigates to edit workout page when edit dropdown item is clicked', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        mockFetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                id: 'workout-abc',
                name: 'Hypertrophy Upper',
                primaryMuscleGroups: ['Chest'],
                exercises: [],
            }),
        });
        render(<WorkoutDetailPage />);

        await waitFor(() => {
            expect(screen.getByText('Hypertrophy Upper')).toBeDefined();
        });

        const editBtn = screen.getByTestId('dropdown-item-Edit');
        fireEvent.click(editBtn);

        expect(mockNav).toHaveBeenCalledWith('/workouts/edit/workout-abc');
    });

    it('handles deleting workout with confirm dialog and navigates to /workouts', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        mockFetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                id: 'workout-abc',
                name: 'Hypertrophy Upper',
                primaryMuscleGroups: ['Chest'],
                exercises: [],
            }),
        });
        render(<WorkoutDetailPage />);

        await waitFor(() => {
            expect(screen.getByText('Hypertrophy Upper')).toBeDefined();
        });

        const deleteBtn = screen.getByTestId('dropdown-item-Delete');
        fireEvent.click(deleteBtn);

        expect(screen.getByTestId('confirm-dialog')).toBeDefined();

        mockFetch.mockImplementation(async (url: string, init?: RequestInit) => {
            if (url === '/api/workouts/workout-abc' && init?.method === 'DELETE') {
                return {
                    ok: true,
                };
            }
            return {
                ok: false,
            };
        });

        fireEvent.click(screen.getByRole('button', { name: 'Confirm Delete' }));

        await waitFor(() => {
            expect(mockNav).toHaveBeenCalledWith('/workouts');
        });
    });

    it('handles error when deleting workout fails', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        mockFetch.mockResolvedValue({
            ok: true,
            json: async () => ({
                id: 'workout-abc',
                name: 'Hypertrophy Upper',
                primaryMuscleGroups: ['Chest'],
                exercises: [],
            }),
        });
        render(<WorkoutDetailPage />);

        await waitFor(() => {
            expect(screen.getByText('Hypertrophy Upper')).toBeDefined();
        });

        const deleteBtn = screen.getByTestId('dropdown-item-Delete');
        fireEvent.click(deleteBtn);

        mockFetch.mockImplementationOnce(async () => ({
            ok: false,
            status: 400,
        }));

        fireEvent.click(screen.getByRole('button', { name: 'Confirm Delete' }));

        await waitFor(() => {
            expect(screen.getByText(/Failed to delete workout/i)).toBeDefined();
        });
    });
    it('starts time constrained workout when 15 Minutes is clicked', async () => {
        mockAuth.mockReturnValue({ isHydrated: true, isAuthenticated: true });
        mockFetch.mockImplementation(async (url) => {
            if (url.includes('/api/workouts/workout-abc')) {
                return { ok: true, json: async () => ({
                    id: 'w-1',
                    name: 'Push Day A',
                    primaryMuscleGroups: ['Chest'],
                    exercises: []
                }) };
            }
            return { ok: false };
        });
        
        render(<WorkoutDetailPage />);
        await waitFor(() => {
            expect(screen.getByText('Push Day A')).toBeDefined();
        });

        const quickBtn = screen.getAllByTestId('dropdown-item-15 Minutes')[0];
        fireEvent.click(quickBtn);
        expect(mockNav).toHaveBeenCalledWith('/active-session', expect.objectContaining({
            state: expect.objectContaining({ isTimeConstrained: true, timeBudgetMinutes: 15 })
        }));
    });

});