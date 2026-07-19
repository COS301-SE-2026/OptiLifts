import SchedulePage from "@/pages/schedule";
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, render, screen, fireEvent, waitFor } from '@testing-library/react';
import { customFetch } from '@/lib/custom-fetch';
import type { ReactNode } from 'react';

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async() => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useNavigate: () => mockNavigate, 
    };
});

vi.mock('@/lib/custom-fetch', () => ({
    customFetch: vi.fn()
}));

vi.mock('@/components/ui/spider-graph', () => ({
    SpiderGraph: () => <div data-testid="spider-graph">Spider Graph</div>,
}));

vi.mock('@/components/ui/confirm-dialog', () => ({
    ConfirmDialog: ({isOpen, onConfirm}: any) => isOpen? (<div data-testid="confirm-dialog"><button onClick={onConfirm}>Confirm Delete</button></div>):null,
}));

vi.mock('@/components/ui/select-workout-dialog', () => ({
    SelectWorkoutDialog: ({isOpen, onSchedule}: any) => isOpen? (<div data-testid="select-dialog"><button onClick={() => onSchedule('workout-123')}>Schedule Workout</button></div>):null,
}));

describe('SchedulePage', () => {
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        vi.useRealTimers();
        cleanup();
    });

    beforeEach(() => {
        vi.useFakeTimers({
            toFake: ['Date']
        });
        vi.setSystemTime(new Date('2026-07-13T12:00:00Z'));
        vi.clearAllMocks();
        mockFetch.mockImplementation(async (url: string) => {
            if(url.includes('/api/users/me/schedule/analytics')){
                return {
                    ok: true,
                    json: async () => ({
                        totalWorkouts: 4,
                        totalVolume: 5000,
                        totalSets: 20,
                        muscleDistribution: [{
                            muscleGroup: 'Chest',
                            setCount: 10,
                            percentage: 50
                        }],
                    }),
                };
            }
            if (url.includes('/api/users/me/schedule')){
                return {
                    ok: true,
                    json: async () => [
                        {
                            id: 'session1',
                            workoutId: 'w-1',
                            workoutName: 'Chest Day',
                            scheduled: new Date().toISOString(),
                            status: 'Scheduled',
                            primaryMuscleGroups: ['Chest'],
                            exerciseCount: 2,
                            exercisePreview: ['Bench Press', 'Incline Press'],
                            totalVolume: 3000,
                            totalSets: 10,
                        },
                    ],
                };
            }
            if (url.includes('/api/workouts')){
                return {
                    ok: true,
                    json: async () => [
                        {
                            id: 'w-1',
                            name: 'Chest Day'
                        },
                    ],
                };
            }
            if (url.includes('/api/profile/calendar')){
                return {
                    ok: true,
                    json: async() => ({
                        entries: []
                    }),
                };
            }
            return {
                ok: false
            };
        });
    });

    it('renders and fetches scheduled workouts and analytics', async () => {
        render(<SchedulePage/>);
        await waitFor(() => {
            expect(screen.getByText('Weekly Summary')).toBeDefined();
        });

        expect(screen.getByText('Chest Day')).toBeDefined();
        expect(screen.getByText('Bench Press, Incline Press')).toBeDefined();
    });

    it('handles scheduling new workout with select workout dialog comp', async () =>{
        render(<SchedulePage/>);
        await waitFor(() => {
            expect(screen.getByText('Weekly Summary')).toBeDefined();
        });

        const addButton = screen.getAllByRole('button', {
            name: /Add Workout for/i
        });
        expect(addButton.length).toBeGreaterThan(0);

        fireEvent.click(addButton[0]);
        await waitFor(() => {
            expect(screen.getByTestId('select-dialog')).toBeDefined();//check that it opens the popup
        });        
        
        mockFetch.mockImplementation(async (url: string, init?:any) => {
            if (url === '/api/users/me/schedule/sessions' && init?.method === 'POST'){
                return {
                    ok: true,
                    json: async () => ({})
                };
            }
            return {
                ok: true,
                json:async () => []
            };
        });
        
        const scheduleBtn = screen.getByRole('button', {
            name: 'Schedule Workout'
        });
        fireEvent.click(scheduleBtn);
        await waitFor(() => {
            expect(screen.queryByTestId('select-dialog')).toBeNull();
        });
    });

    it('handles deleting a schedule session with confirm dialog comp', async () => {
        render(<SchedulePage/>);

        await waitFor(() => {
            expect(screen.getByText('Chest Day')).toBeDefined();
        });

        const deleteBtn = screen.getByRole('button', {
            name: /Delete Chest Day from schedule/i
        });
        fireEvent.click(deleteBtn);

        expect(screen.getByTestId('confirm-dialog')).toBeDefined();

        mockFetch.mockImplementation(async (url: string, init?:any) => {
            if (url.includes('/api/users/me/schedule/sessions/session1') && init?.method === 'DELETE'){
                return {ok:true};
            }
            return {
                ok: true,
                json: async () => []
            };
        });

        //check it was deleted
        const removebtn = screen.getByRole('button', {
            name: 'Confirm Delete'
        });
        fireEvent.click(removebtn);
        await waitFor(() => {
            expect(screen.queryByTestId('confirm-dialog')).toBeNull();
        })

    });
    //^ 59% coverage


});
