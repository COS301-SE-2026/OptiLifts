import WorkoutDetailPage from '@/pages/workout-detail';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import { customFetch } from '@/lib/custom-fetch';

//mocking dependencies -> prevents network requests + isolates component

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useParams: () => ({
            workoutId: 'workout-abc'
        }),
        useNavigate: () => mockNavigate,
    };
});

vi.mock('@/context/auth-context', () =>({
    useAuth: vi.fn(),
}));

//mock customfetch
vi.mock('@/lib/custom-fetch', () => ({
    customFetch: vi.fn()
}));

//mock barchart comp (simplified tho)
vi.mock('@/components/ui/exercise-plan', () => ({
    default: ({exercises}:Readonly<{ exercises: readonly { name: string }[] }>) => (
    <div data-testid="exercise-plan">
        {exercises.map((e) => (
            <div key={e.name}>{e.name}</div>
        ))}
    </div>
    ),
}));

vi.mock('@/components/ui/muscle-diagram', () => ({
    default: () => <div data-testid="muscle-diagram" />,
}));

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

        expect(mockNavigate).toHaveBeenCalledWith('/active-session', {
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

});