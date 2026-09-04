import PastWorkoutsPage from '@/pages/past-workouts';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, render, screen, fireEvent, waitFor, within } from '@testing-library/react';
import { customFetch } from '@/lib/custom-fetch';

//mocking dependencies -> prevents network requests + isolates component

const mockNavigate = vi.fn();
const mockLocationState: unknown = null;
let mockQueryDate: string | null = null;

vi.mock('react-router-dom', async() => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useNavigate: () => mockNavigate, 
        useLocation: () => ({ state: mockLocationState }),
        useSearchParams: () => [new URLSearchParams(mockQueryDate ? { date: mockQueryDate } : {})],
    };
});

vi.mock('@/lib/custom-fetch', () => ({
    customFetch: vi.fn()
}));

vi.mock('@/components/ui/circular-image', () => ({
    CircularProfileImage: () => <div data-testid="circular-image"/>,
}));

vi.mock('@/components/ui/dropdown-menu', () => ({
    DropdownMenu: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
    DropdownMenuEllipsisTrigger: ({ ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) => (
        <button type="button" {...props}>...</button>
    ),
    DropdownMenuEllipsisContent: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
    DropdownMenuItem: ({ children, onSelect, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement> & { onSelect?: (event: Event) => void }) => (
        <button
            type="button"
            {...props}
            onClick={(event) => {
                onSelect?.(event.nativeEvent as Event)
            }}
        >
            {children}
        </button>
    ),
}));

describe('PastWorkoutsPage', () => {
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        cleanup();
    });

    beforeEach(() => { //runs each 'it' test block + resets spy functions
        vi.clearAllMocks();
        mockFetch.mockImplementation(async (url: string, options?: RequestInit) => {
            if (url.includes('/api/workouts/') && url.includes('/logs/') && options?.method === 'DELETE') {
                return {
                    ok: true,
                    json: async () => ({ message: 'Workout log deleted successfully.' }),
                };
            }
            if(url.includes('/api/exercises/images')){
                return {
                    ok: true,
                    json: async() => ({
                        'Bench Press': 'http://test.com/bench.png',
                    }),
                };
            }
            if(url.includes('/api/users/me/schedule')){
                return {
                    ok: true,
                    json: async () => [
                        {
                            id: 'log-1',
                            workoutId: 'w-1',
                            logId: 'l-1',
                            workoutName: 'Morning Bench Routine',
                            startedAt: '2026-07-17T09:00:00Z',
                            completedAt: '2026-07-17T09:00:20Z',
                            primaryMuscleGroups: ['Chest'],
                            exerciseCount: 3,
                            exercisePreview: ['Bench Press'],
                            totalVolume: 4500,
                            recordCount: 2,
                        },
                    ],
                };
            }
            return {
                ok: false
            };
        });
    });

    it('renders loading initially', () => {
        mockFetch.mockReturnValue(new Promise(() => {}));
        render(<PastWorkoutsPage/>);
        expect(screen.getByText(/Loading workouts.../i)).toBeDefined();
    });

    it('loads and lists completed workouts with details', async () => {
        render(<PastWorkoutsPage/>);

        await waitFor(() => {
            expect(screen.getByText('Morning Bench Routine')).toBeDefined();
        });

        expect(screen.getByText('<1m')).toBeDefined();
        expect(screen.getByText(/Muscles:\s*Chest/)).toBeDefined();
        expect(screen.getByText('3')).toBeDefined();
        expect(screen.getByText('2')).toBeDefined();

        const title = screen.getByText('Morning Bench Routine')
        const card = title.closest('[role="button"]')
        expect(card).not.toBeNull()
        fireEvent.click(card as HTMLElement)

        expect(mockNavigate).toHaveBeenCalledWith('/workouts/w-1/logs/l-1', expect.anything());
    });

    it('renders placeholder when list is empty', async () => {
        mockFetch.mockImplementation(async (url: string) => {
            if (url.includes('/api/exercises/images')){
                return {
                    ok: true,
                    json: async() => ({})
                };
            }
            if (url.includes('/api/users/me/schedule')){
                return {
                    ok: true,
                    json: async () => []
                };
            }
            return {
                ok: false
            };
        });

        render(<PastWorkoutsPage/>)
        await waitFor(() => {
            expect(screen.getByText(/You have not completed any workouts this week/i)).toBeDefined();
        });
    });

    it('fetches schedule for week corresponding to date query param', async () => {
        mockQueryDate = '2026-06-17T09:00:00Z';
        render(<PastWorkoutsPage />);
        await waitFor(() => {
            expect(mockFetch).toHaveBeenCalledWith(expect.stringContaining('/api/users/me/schedule?startDate=2026-06-15T'));
        });
        mockQueryDate = null;
    });

    it('shows delete option in card menu and deletes completed workout after confirmation', async () => {
        render(<PastWorkoutsPage />)

        await waitFor(() => {
            expect(screen.getByText('Morning Bench Routine')).toBeDefined()
        })

        fireEvent.click(screen.getByLabelText('Options for Morning Bench Routine'))
        fireEvent.click(screen.getAllByRole('button', { name: 'Delete' })[0])

        const dialog = screen.getByRole('alertdialog')
        fireEvent.click(within(dialog).getByText('Delete'))

        await waitFor(() => {
            expect(mockFetch).toHaveBeenCalledWith('/api/workouts/w-1/logs/l-1', expect.objectContaining({ method: 'DELETE' }))
        })

        await waitFor(() => {
            expect(screen.queryByText('Morning Bench Routine')).toBeNull()
        })
    })

    it('navigates to edit page when clicking Edit in card options menu', async () => {
        render(<PastWorkoutsPage />)

        await waitFor(() => {
            expect(screen.getByText('Morning Bench Routine')).toBeDefined()
        })

        fireEvent.click(screen.getByLabelText('Options for Morning Bench Routine'))
        fireEvent.click(screen.getAllByRole('button', { name: 'Edit' })[0])

        expect(mockNavigate).toHaveBeenCalledWith('/workouts/w-1/logs/l-1/edit')
    })
});
