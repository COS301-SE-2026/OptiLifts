import ProfilePage from '@/pages/profile';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, render, screen, fireEvent, waitFor } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import { customFetch } from '@/lib/custom-fetch';
import type { ReactNode } from 'react';

//mocking dependencies -> prevents network requests + isolates component

const mockNavigate = vi.fn(); //mock trackin function (spies on nav calls)
vi.mock('react-router-dom', async() => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useNavigate: () => mockNavigate, //inject trackable spy func
        Link: ({children, to}: Readonly<{ children: ReactNode; to: string }>) => <a href={to}>{children}</a>,
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
vi.mock('@/components/ui/barchart', () => ({
    default: ({title}:Readonly<{ title: string }>) => <div data-testid="barchart">{title}</div>,
    BarChart: ({title}: Readonly<{ title: string }>) => <div data-testid="barchart">{title}</div>,
}));

vi.mock('@/components/ui/calendar', () => ({
    default: ({onHighlightedDateClick }: Readonly<{ onHighlightedDateClick: (date: string)  => void }>) => 
    (<button data-testid="calendar-date" onClick={() => onHighlightedDateClick('2026-07-17')}>
        Calendar
    </button>),
    Calendar: ({ onHighlightedDateClick } :Readonly<{ onHighlightedDateClick: (date: string) => void }>)=> (
        <button data-testid="calendar-date" onClick={() => onHighlightedDateClick('2026-07-17')}>Calendar</button>
    ),        
}));

//'describe' defines suite of related tests
describe('ProfilePage', () => {
    const mockAuth = useAuth as unknown as Mock;
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        cleanup();
    });

    beforeEach(() => { //runs each 'it' test block + resets spy functions
        vi.clearAllMocks();
    });

    it('renders loading states initially', () => {
        mockAuth.mockReturnValue({
            isHydrated: false,
            isAuthenticated: false
        });
        render(<ProfilePage />)

        expect(screen.queryByText(/Loading profile.../i)).not.toBeNull();
    });

    it('fetches and renders profile data, recent workouts and badges', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });

        //simu http responses
        mockFetch.mockImplementation(async (url: string) => {
            if (url.includes('/api/profile/overview')){
                return {
                    ok: true,
                    json: async() => ({
                        profile: {
                            name: 'John Doe',
                            email: 'john@doe.com',
                            bio: 'Gym Enthus',
                            profileImageUrl: 'http://test.com/img.jpg',
                        },
                        badges: [{ 
                            name: 'Iron Lifter', 
                            description: 'Lifted 1000kg total', 
                            category: 'Strength',
                            earnedAt: '2026-01-01',
                            iconUrl: undefined
                        }],
                        recentWorkouts: [{ 
                            workoutId: 'w-1', 
                            logId: 'l-1', 
                            name: 'Morning Push',
                            exercises: ['Bench Press', 'Incline Press'],
                            prs: '1 PR',
                            duration: '45 min',
                            volume: '2000',
                            sets: '12',
                        }],
                        chartData: [{
                            label: 'Mon',
                            value: 5
                        }],
                        chartTitle: 'Hours this week',
                    }),
                };
            }
            if (url.includes('/api/profile/calendar')){
                return {
                    ok: true,
                    json: async () => ({
                        entries: [{
                            date: '2026-07-17',
                            workoutId: 'w-1',
                            logId: 'l-1'
                        }],
                    }),
                };
            }
            return {
                ok: false,
                status: 404,
                json: async() => ({})
            };
        });
        render(<ProfilePage />)
        
        await waitFor(() => {
            expect(screen.queryByText('John Doe')).not.toBeNull();
        });

        //check that all stuff rendered correctly
        expect(screen.queryByText('Gym Enthus')).not.toBeNull();
        expect(screen.queryByText('Iron Lifter')).not.toBeNull();
        expect(screen.queryByText('Morning Push')).not.toBeNull();

        const calendarDateBtn = screen.getByTestId('calendar-date');
        fireEvent.click(calendarDateBtn);

        expect(mockNavigate).toHaveBeenCalledWith('/workouts/w-1/logs/l-1');
    });

    it('handles api error stats gracefully', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        mockFetch.mockResolvedValue({
            ok: false,
            status: 500
        });
        render(<ProfilePage />);

        await waitFor(() => {
            expect(screen.queryByText(/Failed to load profile \(500\)/i)).not.toBeNull();
        });
    });
});