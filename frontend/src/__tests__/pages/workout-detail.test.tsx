import WorkoutDetailPage from '@/pages/workout-detail';
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import { customFetch } from '@/lib/custom-fetch';

//mocking dependencies -> prevents network requests + isolates component

vi.mock('react-router-dom', async() => {
    return {
        useParams: () => ({
            workoutId: 'workout-abc'
        }),
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
    default: ({exercises}:any) => (
    <div data-testid="exercise-plan">
        {exercises.map((e: any) => (
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
    const mockAuth = useAuth as any;
    const mockFetch = customFetch as any;

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
            expect(screen.queryByText('Hypertrophy Upper')).not.toBeNull();
        });
        expect(screen.queryByText('Incline Bench Press')).not.toBeNull();
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
            expect(screen.queryByText(/Workout not found/i)).not.toBeNull();
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
            expect(screen.queryByText(/Failed to load workout \(500\)/i)).not.toBeNull();
        });
    });

});