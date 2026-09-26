import type { ReactNode } from 'react';
import { AthleteAvatar } from '@/components/opticlash/athlete-avatar';
import { ClashTabs } from '@/components/opticlash/clash-tabs';
import { TierBadge } from '@/components/opticlash/tier-badge';
import { AddFriendModal } from '@/components/opticlash/add-friend-modal';
import { CreateJoinArenaModal } from '@/components/opticlash/create-join-arena-modal';
import { ShareArenaModal } from '@/components/opticlash/share-arena-modal';
import { ProfileInspectorDrawer } from '@/components/opticlash/profile-inspector-drawer';
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { cleanup, render, screen, fireEvent, waitFor } from '@testing-library/react';
import { customFetch } from '@/lib/custom-fetch';
import { useAuth } from '@/context/auth-context';
import type { ClashAthlete } from '@/types/clash';

const mockNavig = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useNavigate: () => mockNavig,
    };
});

vi.mock('@/lib/custom-fetch', () => ({
    customFetch: vi.fn(),
}));

vi.mock('@/context/auth-context', () => ({
    useAuth: vi.fn(),
}));

vi.mock('@/components/ui/dropdown-menu', () => ({
    DropdownMenu: ({ children }: Readonly<{ children: ReactNode }>) => <div>{children}</div>,
    DropdownMenuTrigger: ({ children }: Readonly<{ children: ReactNode }>) => <button type="button">{children}</button>,
    DropdownMenuContent: ({ children }: Readonly<{ children: ReactNode }>) => <div>{children}</div>,
    DropdownMenuItem: ({ children, onSelect }: Readonly<{ children: ReactNode; onSelect?: () => void }>) => (
        <button type="button" onClick={() => onSelect?.()}>{children}</button>
    ),
}));

vi.mock('canvas-confetti', () => ({
    default: vi.fn(),
}));

vi.mock('@/components/ui/spider-graph', () => ({
    default: ({ data }: Readonly<{ data: Record<string, number> }>) => <div data-testid="spider-graph">{JSON.stringify(data)}</div>,
}));

describe('AthleteAvtr', () => {
    afterEach(() => {
        cleanup();
    });

    it('renders init if no avatarUrl', () => {
        render(<AthleteAvatar initials="JD" />);
        expect(screen.getByText('JD')).toBeDefined();
    });

    it('renders img if avatarUrl given', () => {
        render(<AthleteAvatar initials="JD" avatarUrl="http://test.com/a.jpg" name="John" />);
        const img = screen.getByAltText('John') as HTMLImageElement;
        expect(img).toBeDefined();
        expect(img.src).toContain('a.jpg');
    });

    it('falls back to init on img error', () => {
        render(<AthleteAvatar initials="JD" avatarUrl="http://test.com/broken.jpg" />);
        const img = screen.getByAltText('JD');
        fireEvent.error(img);
        expect(screen.getByText('JD')).toBeDefined();
    });

    it('uses init as title if no name', () => {
        render(<AthleteAvatar initials="JD" />);
        expect(screen.getByTitle('JD')).toBeDefined();
    });
});

describe('ClashTabs', () => {
    afterEach(() => {
        cleanup();
    });

    const tabs = [
        { id: 'a', label: 'Tab A', count: 3 },
        { id: 'b', label: 'Tab B' },
    ];

    it('renders tab labels', () => {
        render(<ClashTabs tabs={tabs} activeTab="a" onChange={vi.fn()} />);
        expect(screen.getByText('Tab A')).toBeDefined();
        expect(screen.getByText('Tab B')).toBeDefined();
    });

    it('shows count badge if given', () => {
        render(<ClashTabs tabs={tabs} activeTab="a" onChange={vi.fn()} />);
        expect(screen.getByText('3')).toBeDefined();
    });

    it('marks active tab selected', () => {
        render(<ClashTabs tabs={tabs} activeTab="b" onChange={vi.fn()} />);
        const tabB = screen.getByRole('tab', { name: /Tab B/i });
        expect(tabB.getAttribute('aria-selected')).toBe('true');
    });

    it('calls onChange w/ clicked id', () => {
        const onChange = vi.fn();
        render(<ClashTabs tabs={tabs} activeTab="a" onChange={onChange} />);
        fireEvent.click(screen.getByText('Tab B'));
        expect(onChange).toHaveBeenCalledWith('b');
    });
});

describe('AllTierBadges', () => {
    afterEach(() => {
        cleanup();
    });

    it.each([
        ['Overload Master', 'Overload Master'],
        ['Diamond', 'Diamond'],
        ['Gold', 'Gold'],
        ['Silver', 'Silver'],
        ['Bronze', 'Bronze'],
        ['SomeUnknownTier', 'Unranked'],
    ])('renders %s tier', (tier, expectedText) => {
        render(<TierBadge tier={tier} />);
        expect(screen.getByText(expectedText)).toBeDefined();
    });
});

describe('ModelAddFriend', () => {
    const mockingFtch = customFetch as unknown as Mock;

    beforeEach(() => {
        vi.clearAllMocks();
    });

    afterEach(() => {
        cleanup();
    });

    it('renders null if closed', () => {
        const { container } = render(<AddFriendModal isOpen={false} onClose={vi.fn()} />);
        expect(container.firstChild).toBeNull();
    });

    it('errors on short code', () => {
        render(<AddFriendModal isOpen={true} onClose={vi.fn()} />);
        const inpt = screen.getByPlaceholderText('eg. AB1234');
        fireEvent.change(inpt, { target: { value: 'AB' } });
        fireEvent.submit(inpt.closest('form')!);
        expect(screen.getByText(/valid 6-character friend code/i)).toBeDefined();
    });

    it('sends req, shows success', async () => {
        mockingFtch.mockResolvedValue({ ok: true, json: async () => ({ success: true }) });
        const onAdded = vi.fn();
        render(<AddFriendModal isOpen={true} onClose={vi.fn()} onFriendAdded={onAdded} />);

        const inpt = screen.getByPlaceholderText('eg. AB1234');
        fireEvent.change(inpt, { target: { value: 'ab1234' } });
        fireEvent.submit(inpt.closest('form')!);

        await waitFor(() => {
            expect(screen.getByText('Friend Request Sent')).toBeDefined();
        });
        expect(onAdded).toHaveBeenCalledWith('AB1234');
    });

    it('shows error on fail', async () => {
        mockingFtch.mockResolvedValue({ ok: false, json: async () => ({ message: 'Code not found' }) });
        render(<AddFriendModal isOpen={true} onClose={vi.fn()} />);

        const inpt = screen.getByPlaceholderText('eg. AB1234');
        fireEvent.change(inpt, { target: { value: 'AB1234' } });
        fireEvent.submit(inpt.closest('form')!);

        await waitFor(() => {
            expect(screen.getByText('Code not found')).toBeDefined();
        });
    });

    it('handles network error', async () => {
        mockingFtch.mockRejectedValue(new Error('network down'));
        render(<AddFriendModal isOpen={true} onClose={vi.fn()} />);

        const inpt = screen.getByPlaceholderText('eg. AB1234');
        fireEvent.change(inpt, { target: { value: 'AB1234' } });
        fireEvent.submit(inpt.closest('form')!);

        await waitFor(() => {
            expect(screen.getByText(/Network error/i)).toBeDefined();
        });
    });

    it('closes on Escape', () => {
        const onClose = vi.fn();
        render(<AddFriendModal isOpen={true} onClose={onClose} />);
        fireEvent.keyDown(window, { key: 'Escape' });
        expect(onClose).toHaveBeenCalled();
    });

    it('closes on backdrop click', () => {
        const onClose = vi.fn();
        render(<AddFriendModal isOpen={true} onClose={onClose} />);
        fireEvent.click(screen.getByLabelText('Close modal'));
        expect(onClose).toHaveBeenCalled();
    });
});

describe('ModelCreateJoinArena', () => {
    const mockingFtch = customFetch as unknown as Mock;

    beforeEach(() => {
        vi.clearAllMocks();
    });

    afterEach(() => {
        cleanup();
    });

    it('renders null if closed', () => {
        const { container } = render(<CreateJoinArenaModal isOpen={false} onClose={vi.fn()} />);
        expect(container.firstChild).toBeNull();
    });

    it('defaults to join tab', () => {
        render(<CreateJoinArenaModal isOpen={true} onClose={vi.fn()} />);
        expect(screen.getByPlaceholderText('e.g. IRON99')).toBeDefined();
    });

    it('switches to create tab', () => {
        render(<CreateJoinArenaModal isOpen={true} onClose={vi.fn()} />);
        fireEvent.click(screen.getByText('Create New Arena'));
        expect(screen.getByPlaceholderText('e.g. Hazelwood Overloaders')).toBeDefined();
    });

    it('errors on empty join code', () => {
        render(<CreateJoinArenaModal isOpen={true} onClose={vi.fn()} />);
        const inpt = screen.getByPlaceholderText('e.g. IRON99');
        fireEvent.submit(inpt.closest('form')!);
        expect(screen.getByText(/Please enter a 6 character arena code/i)).toBeDefined();
    });

    it('errors on short join code', () => {
        render(<CreateJoinArenaModal isOpen={true} onClose={vi.fn()} />);
        const inpt = screen.getByPlaceholderText('e.g. IRON99');
        fireEvent.change(inpt, { target: { value: 'AB' } });
        fireEvent.submit(inpt.closest('form')!);
        expect(screen.getByText(/Arena code must be 6 characters/i)).toBeDefined();
    });

    it('joins arena, navigates', async () => {
        mockingFtch.mockResolvedValue({
            ok: true,
            json: async () => ({ success: true, arena: { id: 'arena-1' } }),
        });
        const onClose = vi.fn();
        const onSuccess = vi.fn();
        render(<CreateJoinArenaModal isOpen={true} onClose={onClose} onSuccess={onSuccess} />);

        const inpt = screen.getByPlaceholderText('e.g. IRON99');
        fireEvent.change(inpt, { target: { value: 'iron99' } });
        fireEvent.submit(inpt.closest('form')!);

        await waitFor(() => {
            expect(onSuccess).toHaveBeenCalledWith('arena-1');
        });
        expect(onClose).toHaveBeenCalled();
        expect(mockNavig).toHaveBeenCalledWith('/clash/arena-1');
    });

    it('errors if join fails', async () => {
        mockingFtch.mockResolvedValue({
            ok: false,
            json: async () => ({ message: 'Arena not found' }),
        });
        render(<CreateJoinArenaModal isOpen={true} onClose={vi.fn()} />);

        const inpt = screen.getByPlaceholderText('e.g. IRON99');
        fireEvent.change(inpt, { target: { value: 'IRON99' } });
        fireEvent.submit(inpt.closest('form')!);

        await waitFor(() => {
            expect(screen.getByText('Arena not found')).toBeDefined();
        });
    });

    it('errors on empty arena name', () => {
        render(<CreateJoinArenaModal isOpen={true} onClose={vi.fn()} />);
        fireEvent.click(screen.getByText('Create New Arena'));
        const inpt = screen.getByPlaceholderText('e.g. Hazelwood Overloaders');
        fireEvent.submit(inpt.closest('form')!);
        expect(screen.getByText(/Please provide an arena name/i)).toBeDefined();
    });

    it('creates arena, navigates', async () => {
        mockingFtch.mockResolvedValue({
            ok: true,
            json: async () => ({ success: true, arena: { id: 'arena-2' } }),
        });
        const onClose = vi.fn();
        render(<CreateJoinArenaModal isOpen={true} onClose={onClose} />);

        fireEvent.click(screen.getByText('Create New Arena'));
        const inpt = screen.getByPlaceholderText('e.g. Hazelwood Overloaders');
        fireEvent.change(inpt, { target: { value: 'My Squad' } });
        fireEvent.submit(inpt.closest('form')!);

        await waitFor(() => {
            expect(mockNavig).toHaveBeenCalledWith('/clash/arena-2');
        });
        expect(onClose).toHaveBeenCalled();
    });

    it('errors if create fails', async () => {
        mockingFtch.mockResolvedValue({
            ok: false,
            json: async () => ({ message: 'Name already taken' }),
        });
        render(<CreateJoinArenaModal isOpen={true} onClose={vi.fn()} />);

        fireEvent.click(screen.getByText('Create New Arena'));
        const inpt = screen.getByPlaceholderText('e.g. Hazelwood Overloaders');
        fireEvent.change(inpt, { target: { value: 'My Squad' } });
        fireEvent.submit(inpt.closest('form')!);

        await waitFor(() => {
            expect(screen.getByText('Name already taken')).toBeDefined();
        });
    });
});

describe('ModelShareArena', () => {
    const mockingFtch = customFetch as unknown as Mock;
    const testArena = { id: 'arena-1', name: 'Iron Squad', code: 'IRON99' };

    beforeEach(() => {
        vi.clearAllMocks();
        Object.assign(navigator, { clipboard: { writeText: vi.fn() } });
    });

    afterEach(() => {
        cleanup();
    });

    it('renders null if closed', () => {
        const { container } = render(<ShareArenaModal isOpen={false} onClose={vi.fn()} arena={testArena} />);
        expect(container.firstChild).toBeNull();
    });

    it('renders null if no arena', () => {
        const { container } = render(<ShareArenaModal isOpen={true} onClose={vi.fn()} arena={null} />);
        expect(container.firstChild).toBeNull();
    });

    it('shows code, copies to clipboard', async () => {
        mockingFtch.mockResolvedValue({ ok: true, json: async () => [] });
        render(<ShareArenaModal isOpen={true} onClose={vi.fn()} arena={testArena} />);

        expect(screen.getByText('IRON99')).toBeDefined();
        fireEvent.click(screen.getByText('Copy'));
        expect(navigator.clipboard.writeText).toHaveBeenCalledWith('IRON99');
        expect(await screen.findByText('Copied')).toBeDefined();
    });

    it('shows dashes if no code', () => {
        mockingFtch.mockResolvedValue({ ok: true, json: async () => [] });
        render(<ShareArenaModal isOpen={true} onClose={vi.fn()} arena={{ id: 'a', name: 'X', code: null }} />);
        expect(screen.getByText('------')).toBeDefined();
    });

    it('fetches friends, filters by search', async () => {
        mockingFtch.mockResolvedValue({
            ok: true,
            json: async () => [
                { id: 'f1', name: 'Alice', initials: 'AL', code: 'AL123', dotsScore: 300, tier: 'Gold' },
                { id: 'f2', name: 'Bob', initials: 'BB', code: 'BB123', dotsScore: 250, tier: 'Silver' },
            ],
        });
        render(<ShareArenaModal isOpen={true} onClose={vi.fn()} arena={testArena} />);

        await waitFor(() => {
            expect(screen.getByText('Alice')).toBeDefined();
        });
        expect(screen.getByText('Bob')).toBeDefined();

        fireEvent.change(screen.getByPlaceholderText('Search friend by name'), { target: { value: 'ali' } });
        expect(screen.getByText('Alice')).toBeDefined();
        expect(screen.queryByText('Bob')).toBeNull();
    });

    it('shows empty state if none', async () => {
        mockingFtch.mockResolvedValue({ ok: true, json: async () => [] });
        render(<ShareArenaModal isOpen={true} onClose={vi.fn()} arena={testArena} />);

        await waitFor(() => {
            expect(screen.getByText(/No friends found/i)).toBeDefined();
        });
    });

    it('invites friend, shows invited', async () => {
        mockingFtch.mockImplementation(async (url: string) => {
            if (url.includes('/invite')) {
                return { ok: true, json: async () => ({ success: true }) };
            }
            return {
                ok: true,
                json: async () => [{ id: 'f1', name: 'Alice', initials: 'AL', code: 'AL123', dotsScore: 300, tier: 'Gold' }],
            };
        });
        render(<ShareArenaModal isOpen={true} onClose={vi.fn()} arena={testArena} />);

        await waitFor(() => {
            expect(screen.getByText('Alice')).toBeDefined();
        });
        fireEvent.click(screen.getByText('Invite'));

        await waitFor(() => {
            expect(screen.getByText('Invited')).toBeDefined();
        });
    });

    it('calls onClose on Done click', () => {
        mockingFtch.mockResolvedValue({ ok: true, json: async () => [] });
        const onClose = vi.fn();
        render(<ShareArenaModal isOpen={true} onClose={onClose} arena={testArena} />);

        fireEvent.click(screen.getByText('Done'));
        expect(onClose).toHaveBeenCalled();
    });
});

describe('ProfInspDrawer', () => {
    const mockingFtch = customFetch as unknown as Mock;
    const mockingAuth = useAuth as unknown as Mock;

    const testAthlete: ClashAthlete = {
        id: 'athlete-1',
        name: 'Jane Lifter',
        initials: 'JL',
        code: 'JL1234',
        gender: 'female',
        bodyweightKg: 65,
        squat1RM: 100,
        bench1RM: 60,
        deadlift1RM: 120,
        totalE1RM: 280,
        dotsScore: 350,
        tier: 'Gold',
        tierLevel: 2,
        rankTrend: 1,
        weeklyVolumeKg: 5000,
        lastWorkoutDate: '2026-09-01',
        muscleBalance30d: { Chest: 100, Core: 50, Shoulders: 80, Arms: 60, Legs: 200, Back: 90 },
        trophies: [],
        recentWorkouts: [],
    };

    beforeEach(() => {
        vi.clearAllMocks();
        mockingAuth.mockReturnValue({ user: { id: 'someone-else' } });
        mockingFtch.mockResolvedValue({ ok: false, json: async () => ({}) });
    });

    afterEach(() => {
        cleanup();
    });

    it('renders null if closed', () => {
        const { container } = render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={false} onClose={vi.fn()} />);
        expect(container.firstChild).toBeNull();
    });

    it('renders null if no athlete', () => {
        const { container } = render(<ProfileInspectorDrawer athlete={null} isOpen={true} onClose={vi.fn()} />);
        expect(container.firstChild).toBeNull();
    });

    it('renders props before fetch', () => {
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);
        expect(screen.getByText('Jane Lifter')).toBeDefined();
        expect(screen.getByText('100 kg')).toBeDefined();
    });

    it('overrides stats after fetch', async () => {
        mockingFtch.mockImplementation(async (url: string) => {
            if (url.includes('/profile')) {
                return {
                    ok: true,
                    json: async () => ({
                        userId: testAthlete.id,
                        displayName: testAthlete.name,
                        bodyweightKg: 66,
                        tier: 'Gold',
                        tierLevel: 2,
                        dotsScore: 360,
                        squat1RM: 999,
                        bench1RM: 60,
                        deadlift1RM: 120,
                        totalE1RM: 1179,
                        weeklyVolumeKg: 5000,
                        muscleBalance30d: [],
                        trophies: [],
                        recentWorkouts: [],
                        kudosCount: 4,
                        hasSentKudos: false,
                    }),
                };
            }
            return { ok: false, json: async () => ({}) };
        });
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);

        await waitFor(() => {
            expect(screen.getByText('999 kg')).toBeDefined();
        });
        expect(screen.getByText('4 Kudos')).toBeDefined();
    });

    it('sends kudos, updates count', async () => {
        mockingFtch.mockImplementation(async (url: string) => {
            if (url.includes('/kudos')) {
                return { ok: true, json: async () => ({}) };
            }
            return { ok: false, json: async () => ({}) };
        });
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);

        fireEvent.click(screen.getByText('0 Kudos'));

        await waitFor(() => {
            expect(screen.getByText('1 Kudos')).toBeDefined();
        });
    });

    it('sends req, shows sent', async () => {
        mockingFtch.mockImplementation(async (url: string) => {
            if (url.includes('/friends/requests')) {
                return { ok: true, json: async () => ({ success: true }) };
            }
            return { ok: false, json: async () => ({}) };
        });
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);

        fireEvent.click(screen.getByText('Add Friend'));

        await waitFor(() => {
            expect(screen.getByText('Request Sent')).toBeDefined();
        });
    });

    it('hides add friend for self', () => {
        mockingAuth.mockReturnValue({ user: { id: testAthlete.id } });
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);
        expect(screen.queryByText('Add Friend')).toBeNull();
    });

    it('opens trophies, shows empty', () => {
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);
        fireEvent.click(screen.getByText('Trophies'));
        expect(screen.getByText(/No trophy milestones earned yet/i)).toBeDefined();
    });

    it('switches tab, shows empty', () => {
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);
        fireEvent.click(screen.getByText('Recent Workouts'));
        expect(screen.getByText(/No public workout logs shared yet/i)).toBeDefined();
    });

    it('calls onClose on close click', () => {
        const onClose = vi.fn();
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={onClose} />);
        fireEvent.click(screen.getByLabelText('Close'));
        expect(onClose).toHaveBeenCalled();
    });

    it('shows workout, expands it', async () => {
        mockingFtch.mockImplementation(async (url: string) => {
            if (url.includes('/profile')) {
                return {
                    ok: true,
                    json: async () => ({
                        userId: testAthlete.id, displayName: testAthlete.name, bodyweightKg: 65,
                        tier: 'Gold', tierLevel: 2, dotsScore: 350,
                        squat1RM: 111, bench1RM: 60, deadlift1RM: 120, totalE1RM: 280,
                        weeklyVolumeKg: 5000, muscleBalance30d: [], trophies: [],
                        recentWorkouts: [{
                            logId: 'log-1', title: 'Leg Day', completedAt: '2026-09-01T10:00:00.000Z',
                            volumeKg: 3000, exercises: [{ exerciseName: 'Squat', sets: 5 }],
                        }],
                        kudosCount: 0, hasSentKudos: false,
                    }),
                };
            }
            return { ok: false, json: async () => ({}) };
        });
        render(<ProfileInspectorDrawer athlete={testAthlete} isOpen={true} onClose={vi.fn()} />);

        await waitFor(() => {
            expect(screen.getByText('111 kg')).toBeDefined();
        });

        fireEvent.click(screen.getByText('Recent Workouts'));
        expect(screen.getByText('Leg Day')).toBeDefined();

        fireEvent.click(screen.getByText('Leg Day'));
        expect(screen.getByText('Squat')).toBeDefined();
        expect(screen.getByText('5 sets')).toBeDefined();
    });
});
