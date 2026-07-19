 import WorkoutsPage from '@/pages/workouts';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import { customFetch } from '@/lib/custom-fetch';
import type { ReactNode } from 'react';

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

vi.mock('@/componentts/ui/muscle-diagram', () =>({
    default: () => <div data-testid="muscle-diagram"/>
}));

vi.mock('@/components/ui/confirm-dialog', () => ({
    ConfirmDialog: ({isOpen, onConfirm}: Readonly<{ isOpen: boolean; onConfirm: () => void}>) => isOpen? (<div data-testid="confirm-dialog"><button onClick={onConfirm}>Confirm Delete</button></div>):null,
}));

//mock dropdown comp into html btns
vi.mock('@/components/ui/dropdown-menu', () => ({
    DropdownMenu: ({children}: Readonly<{children: ReactNode}>) => <div data-testid="dropdown">{children}</div>,
    DropdownMenuEllipsisTrigger: () => <button data-testid="dropdown-trigger">...</button>,
    DropdownMenuEllipsisContent: ({children}: Readonly<{children: ReactNode}>) => <div data-testid="dropdown-content">{children}</div>,
    DropdownMenuItem: ({ children, onSelect }: Readonly<{ children: ReactNode; onSelect: () => void }>) => (<button onClick={onSelect} data-testid={`dropdown-item-${children}`}>{children}</button>),
}));

describe('WorkoutsPage', () => {
    const mockAuth = useAuth as unknown as Mock;
    const mockFetch = customFetch as unknown as Mock;

    afterEach(() => {
        cleanup();
    });

    beforeEach(() => { //runs each 'it' test block + resets spy functions
        vi.clearAllMocks();
        mockFetch.mockImplementation(async (url: string) => {
            if(url === '/api/workouts'){
                return {
                    ok: true,
                    json: async() => [
                        {
                            id: 'w-1',
                            name: 'Push Day',
                            exerciseCount: 4,
                            primaryMuscleGroups: ['Chest', 'Triceps'],
                            exercisePreview: ['Bench Press', 'Overhead Press'],
                        },
                        {
                            id: 'w-2',
                            name: 'Leg Day',
                            exerciseCount: 3,
                            primaryMuscleGroups: ['Quads', 'Hamstrings'],
                            exercisePreview: ['Squats', 'Leg Curls'],
                        },
                    ],
                };
            }
            return {
                ok: false
            };
        });
    });

    it('renders auth warning when unathed', () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: false
        });
        render(<WorkoutsPage/>);

        expect(screen.getByText(/Please log in to view your workouts/i)).toBeDefined();
    });
    
    it ('loads and lists workouts and supports searching', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);

        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });
        expect(screen.getByText('Leg Day')).toBeDefined();

        const searchInput = screen.getByPlaceholderText('Search workouts');
        fireEvent.change(searchInput, {
            target: {
                value: 'Leg'
            }
        });
        expect(screen.getByText('Leg Day')).toBeDefined();
        expect(screen.queryByText('Push Day')).toBeNull(); //sonarqube might get me here
    });
    it('handles duplicating a workout', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);

        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });

        mockFetch.mockImplementation(async (url: string, init?: RequestInit) => {
            if (url === '/api/workouts/w-1/duplicate' && init?.method === 'POST'){
                return {
                    ok: true,
                    json: async () => ({
                        workoutId: 'w-1-dupe'
                    })
                };
            }
            if (url === '/api/workouts'){
                return {
                    ok: true,
                    json: async() => [
                        {
                            id: 'w-1',
                            name: 'Push Day (Copy)',
                            exerciseCount: 4,
                            primaryMuscleGroups: ['Chest'],
                            exercisePreview: []
                        },
                    ],
                };
            }
            return {
                ok: false
            };
        });

        const dupeBtn = screen.getAllByTestId('dropdown-item-Duplicate')[0];
        fireEvent.click(dupeBtn);

        await waitFor(() => {
            expect(screen.getByText('Push Day (Copy)')).toBeDefined();
        });
    });

    it('handles deleting workout w/ confirm dialog', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);

        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });

        const deleteBtn = screen.getAllByTestId('dropdown-item-Delete')[0];
        fireEvent.click(deleteBtn);

        expect(screen.getByTestId('confirm-dialog')).toBeDefined();

        mockFetch.mockImplementation(async (url: string, init?: RequestInit) => {
            if (url === '/api/workouts/w-1' && init?.method === 'DELETE'){
                return {
                    ok: true
                };
            }
            return {
                ok: false
            };
        });
        fireEvent.click(screen.getByRole('button', {
            name: 'Confirm Delete'
        }));

        await waitFor(() => {
            expect(screen.queryByTestId('confirm-dialog')).toBeNull();
        });
    });
    //coverage 80%

    it ('supports searching primary muscle group', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);
        const searchInput = screen.getByPlaceholderText('Search workouts');
        fireEvent.change(searchInput, {
            target: {
                value: 'Triceps'
            }
        });
        expect(screen.getByText('Push Day')).toBeDefined();
        expect(screen.queryByText('Leg Day')).toBeNull();
    });

    it ('returns no workouts found when no search match', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);
        const searchInput = screen.getByPlaceholderText('Search workouts');
        fireEvent.change(searchInput, {
            target: {
                value: 'Nothing'
            }
        });
        expect(screen.getByText('No workouts found')).toBeDefined();
    });

    it ('handles clickign card to navigate to details page', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);

        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });

        const card = screen.getByRole('button', {name: /Push Day/i});
        fireEvent.click(card);
        expect(mockNavigate).toHaveBeenCalledWith('/workouts/w-1');
    });

    it ('handles keyboard nav on workout cards', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);

        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });

        const card = screen.getByRole('button', {name: /Push Day/i});
        fireEvent.keyDown(card, {
            key: 'Enter',
            code: 'Enter'
        });
        expect(mockNavigate).toHaveBeenCalledWith('/workouts/w-1');
    });

    it ('goes to create workout page when add button clicked', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);

        const addbtn = screen.getByRole('button', {name: 'Add'});
        fireEvent.click(addbtn);
        await waitFor(() => {
            expect(mockNavigate).toHaveBeenCalledWith('/workouts/create');
        });
        
    });

    it ('goes to edit workout page when edit dropdown clicked', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);
        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });

        const editbtn = screen.getAllByTestId('dropdown-item-Edit')[0];
        fireEvent.click(editbtn);
        expect(mockNavigate).toHaveBeenCalledWith('/workouts/edit/w-1');
    });

    it ('display error banner when loading workouts fails', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        mockFetch.mockImplementationOnce(async () => ({
            ok: false,
            status: 500
        }));
        render(<WorkoutsPage/>);
        await waitFor(() => {
            expect(screen.getByText(/Failed to load workouts \(500\)/i)).toBeDefined();
        });
    });

    it ('display error banner when duplicating workout fails', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);
        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });

        mockFetch.mockImplementationOnce(async () => ({
            ok: false,
            status: 400
        }));
        const dupeBtn = screen.getAllByTestId('dropdown-item-Duplicate')[0];
        fireEvent.click(dupeBtn);

        await waitFor(() => {
            expect(screen.getByText(/Failed to duplicate workout \(400\)/i)).toBeDefined();
        });
    });

    it ('display error banner when deleting workout fails', async () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        render(<WorkoutsPage/>);
        await waitFor(() => {
            expect(screen.getByText('Push Day')).toBeDefined();
        });
        
        const deletebtn = screen.getAllByTestId('dropdown-item-Delete')[0];
        fireEvent.click(deletebtn);

        mockFetch.mockImplementationOnce(async () => ({
            ok: false,
            status: 403
        }));
        fireEvent.click(screen.getByRole('button', {
            name: 'Confirm Delete'
        }));

        await waitFor(() => {
            expect(screen.getByText(/Failed to delete workout/i)).toBeDefined();
        });
    });
});