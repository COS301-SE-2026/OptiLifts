import WorkoutLogDetailPage from '@/pages/workout-log-detail';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { cleanup, render, screen, fireEvent, waitFor } from '@testing-library/react';
import { customFetch } from '@/lib/custom-fetch';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useNavigate: () => mockNavigate,
        useLocation: () => ({ state: null }),
        useParams: () => ({ workoutId: 'w-1', logId: 'l-1' }),
    };
});

vi.mock('@/context/auth-context', () => ({
    useAuth: () => ({
        isAuthenticated: true,
        isHydrated: true,
    }),
}));

vi.mock('@/lib/custom-fetch', () => ({
    customFetch: vi.fn(),
}));

describe('WorkoutLogDetailPage', () => {
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        cleanup();
    });

    beforeEach(() => {
        vi.clearAllMocks();
        mockFetch.mockImplementation(async (url: string) => {
            if (url.includes('/api/workouts/w-1/logs/l-1')) {
                return {
                    ok: true,
                    json: async () => ({
                        workoutId: 'w-1',
                        logId: 'l-1',
                        name: 'Leg Day Blast',
                        createdAt: '2026-07-20T10:00:00Z',
                        completedAt: '2026-07-20T11:00:00Z',
                        duration: '01:00',
                        primaryMuscleGroups: ['Quadriceps'],
                        exercisePreview: ['Squats'],
                        exercises: [
                            {
                                id: 'ex-1',
                                exerciseId: 'e-1',
                                name: 'Barbell Squat',
                                primaryMuscle: 'Quadriceps',
                                exerciseType: 'WeightReps',
                                orderIndex: 1,
                                sets: [
                                    {
                                        id: 's-1',
                                        setId: null,
                                        type: 'Normal',
                                        reps: 10,
                                        weight: 100,
                                        orderIndex: 1,
                                        duration: null,
                                        distance: null,
                                        restTime: 90,
                                        groupNumber: 1,
                                        rpe: 8,
                                    },
                                ],
                            },
                        ],
                    }),
                };
            }
            return { ok: false };
        });
    });

    it('renders Back to Past Workout button and navigates to correct past workout week date', async () => {
        render(<WorkoutLogDetailPage />);

        await waitFor(() => {
            expect(screen.getByText('Leg Day Blast')).toBeDefined();
        });

        const backButton = screen.getByRole('button', { name: /Back to Past Workout/i });
        expect(backButton).toBeDefined();

        fireEvent.click(backButton);

        expect(mockNavigate).toHaveBeenCalledWith(
            '/past-workouts?date=2026-07-20T11%3A00%3A00Z',
            { state: { date: '2026-07-20T11:00:00Z' } }
        );
    });
});