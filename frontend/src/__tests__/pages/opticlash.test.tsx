import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { cleanup, render, screen, fireEvent, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import FriendsManagementPage from '@/pages/opticlash/friends';
import ArenaHubPage from '@/pages/opticlash/index';
import { customFetch } from '@/lib/custom-fetch';
import { useAuth } from '@/context/auth-context';

const mockNavig = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        Link: ({ children, to }: Readonly<{ children: ReactNode; to: string }>) => <a href={to}>{children}</a>,
        useNavigate: () => mockNavig,
    };
});

vi.mock('@/lib/custom-fetch', () => ({
    customFetch: vi.fn(),
}));

vi.mock('@/context/auth-context', () => ({
    useAuth: vi.fn(),
}));

vi.mock('canvas-confetti', () => ({
    default: vi.fn(),
}));

vi.mock('@/components/ui/spider-graph', () => ({
    default: () => <div data-testid="spider-graph" />,
}));

vi.mock('@/components/ui/dropdown-menu', () => ({
    DropdownMenu: ({ children }: Readonly<{ children: ReactNode }>) => <div>{children}</div>,
    DropdownMenuTrigger: ({ children }: Readonly<{ children: ReactNode }>) => <button type="button">{children}</button>,
    DropdownMenuContent: ({ children }: Readonly<{ children: ReactNode }>) => <div>{children}</div>,
    DropdownMenuItem: ({ children, onSelect }: Readonly<{ children: ReactNode; onSelect?: () => void }>) => (
        <button type="button" onClick={() => onSelect?.()}>{children}</button>
    ),
}));

const { mockConn } = vi.hoisted(() => ({
    mockConn: {
        start: vi.fn().mockResolvedValue(undefined),
        on: vi.fn(),
        invoke: vi.fn(),
        stop: vi.fn().mockResolvedValue(undefined),
        state: 'Disconnected',
    },
}));

vi.mock('@microsoft/signalr', () => ({
    HubConnectionBuilder: vi.fn().mockImplementation(function HubConnectionBuilder() {
        const builder = {
            withUrl: () => builder,
            withAutomaticReconnect: () => builder,
            build: () => mockConn,
        };
        return builder;
    }),
    HubConnectionState: { Connected: 'Connected', Disconnected: 'Disconnected' },
}));

function mockFriendEps({
    friends = [],
    incoming = [],
    code = 'AB1234',
    invites = [],
}: {
    friends?: unknown[];
    incoming?: Array<{ id: string; [key: string]: unknown }>;
    code?: string;
    invites?: Array<{ id: string; [key: string]: unknown }>;
} = {}) {
    let currIncoming = [...incoming];
    let currInvites = [...invites];
    const mockingFtch = customFetch as unknown as Mock;
    mockingFtch.mockImplementation(async (url: string) => {
        const reqRespondMatch = url.match(/\/api\/clash\/friends\/requests\/([^/]+)\/respond/);
        if (reqRespondMatch) {
            currIncoming = currIncoming.filter((r) => r.id !== reqRespondMatch[1]);
            return { ok: true, json: async () => ({ success: true }) };
        }
        const inviteRespondMatch = url.match(/\/api\/clash\/arenas\/invites\/([^/]+)\/respond/);
        if (inviteRespondMatch) {
            currInvites = currInvites.filter((i) => i.id !== inviteRespondMatch[1]);
            return { ok: true, json: async () => ({ success: true }) };
        }
        if (url.includes('/api/clash/friends/requests/reject-all')) {
            currIncoming = [];
            return { ok: true, json: async () => ({ success: true }) };
        }
        if (url.includes('/api/clash/friends/requests')) {
            return { ok: true, json: async () => ({ incoming: currIncoming }) };
        }
        if (url.includes('/api/clash/friends/code')) {
            return { ok: true, json: async () => ({ code }) };
        }
        if (url.includes('/api/clash/arenas/invites')) {
            return { ok: true, json: async () => currInvites };
        }
        if (url.includes('/api/clash/friends')) {
            return { ok: true, json: async () => friends };
        }
        return { ok: true, json: async () => ({ success: true }) };
    });
    return mockingFtch;
}

const mockingAuth = useAuth as unknown as Mock;

beforeEach(() => {
    vi.clearAllMocks();
    mockingAuth.mockReturnValue({ user: { id: 'me' } });
});

afterEach(() => {
    cleanup();
});

const CHARLIE_REQ = { id: 'r1', fromAthleteId: 'a1', fromName: 'Charlie', fromInitials: 'CH', fromCode: 'CH123', sentAt: 'today' };
const DANA_REQ = { id: 'r2', fromAthleteId: 'a2', fromName: 'Dana', fromInitials: 'DA', fromCode: 'DA123', sentAt: 'today' };
const IRON_INVITE = { id: 'i1', arenaId: 'arena-1', arenaName: 'Iron Squad', invitedBNyUserId: 'u1', invitedByName: 'Eve', invitedByInitials: 'EV', status: 'Pending', createdAt: 'today' };

describe('PgFriends', () => {
    it('fetches friends, code on mount', async () => {
        mockFriendEps({
            friends: [{ id: 'f1', name: 'Alice', initials: 'AL', code: 'AL123', dotsScore: 300, tier: 'Gold' }],
            code: 'MYCODE',
        });
        render(<FriendsManagementPage />);

        await waitFor(() => {
            expect(screen.getByText('Alice')).toBeDefined();
        });
        expect(screen.getByText('MYCODE')).toBeDefined();
    });

    it('filters friends by search', async () => {
        mockFriendEps({
            friends: [
                { id: 'f1', name: 'Alice', initials: 'AL', code: 'AL123', dotsScore: 300, tier: 'Gold' },
                { id: 'f2', name: 'Bob', initials: 'BB', code: 'BB123', dotsScore: 250, tier: 'Silver' },
            ],
        });
        render(<FriendsManagementPage />);

        await waitFor(() => {
            expect(screen.getByText('Alice')).toBeDefined();
        });
        fireEvent.change(screen.getByPlaceholderText('Search friends by name or code'), { target: { value: 'ali' } });
        expect(screen.getByText('Alice')).toBeDefined();
        expect(screen.queryByText('Bob')).toBeNull();
    });

    it('switches tab, accepts req', async () => {
        mockFriendEps({
            incoming: [CHARLIE_REQ],
        });
        render(<FriendsManagementPage />);

        fireEvent.click(await screen.findByText('Requests'));
        expect(await screen.findByText('Charlie')).toBeDefined();

        fireEvent.click(screen.getByText('Accept Friend'));

        await waitFor(() => {
            expect(screen.queryByText('Charlie')).toBeNull();
        });
    });

    it('rejects req', async () => {
        mockFriendEps({
            incoming: [CHARLIE_REQ],
        });
        render(<FriendsManagementPage />);

        fireEvent.click(await screen.findByText('Requests'));
        expect(await screen.findByText('Charlie')).toBeDefined();

        fireEvent.click(screen.getByText('Reject'));

        await waitFor(() => {
            expect(screen.queryByText('Charlie')).toBeNull();
        });
    });

    it('rejects all req', async () => {
        mockFriendEps({
            incoming: [CHARLIE_REQ, DANA_REQ],
        });
        render(<FriendsManagementPage />);

        fireEvent.click(await screen.findByText('Requests'));
        expect(await screen.findByText('Charlie')).toBeDefined();

        fireEvent.click(screen.getByText('Reject All Requests'));

        await waitFor(() => {
            expect(screen.getByText('No pending incoming friend requests')).toBeDefined();
        });
    });

    it('switches tab, accepts invite', async () => {
        mockFriendEps({
            invites: [IRON_INVITE],
        });
        render(<FriendsManagementPage />);

        fireEvent.click(await screen.findByText('Arena Invites'));
        expect(await screen.findByText('Iron Squad')).toBeDefined();

        fireEvent.click(screen.getByText('Join Arena'));

        await waitFor(() => {
            expect(screen.queryByText('Iron Squad')).toBeNull();
        });
    });

    it('declines invite', async () => {
        mockFriendEps({
            invites: [IRON_INVITE],
        });
        render(<FriendsManagementPage />);

        fireEvent.click(await screen.findByText('Arena Invites'));
        expect(await screen.findByText('Iron Squad')).toBeDefined();

        fireEvent.click(screen.getByText('Decline'));

        await waitFor(() => {
            expect(screen.queryByText('Iron Squad')).toBeNull();
        });
    });

    it('declines all invites', async () => {
        mockFriendEps({
            invites: [IRON_INVITE],
        });
        render(<FriendsManagementPage />);

        fireEvent.click(await screen.findByText('Arena Invites'));
        expect(await screen.findByText('Iron Squad')).toBeDefined();

        fireEvent.click(screen.getByText('Decline All Invites'));

        await waitFor(() => {
            expect(screen.getByText('No pending arena invites.')).toBeDefined();
        });
    });

    it('copies code to clipboard', async () => {
        Object.assign(navigator, { clipboard: { writeText: vi.fn() } });
        mockFriendEps({ code: 'MYCODE' });
        render(<FriendsManagementPage />);

        await waitFor(() => {
            expect(screen.getByText('MYCODE')).toBeDefined();
        });
        fireEvent.click(screen.getByText('Copy'));
        expect(navigator.clipboard.writeText).toHaveBeenCalledWith('MYCODE');
        expect(await screen.findByText('Copied')).toBeDefined();
    });

    it('opens add friend modal', async () => {
        mockFriendEps();
        render(<FriendsManagementPage />);

        await waitFor(() => {
            expect(screen.getByText('Add Friend By Code')).toBeDefined();
        });
        fireEvent.click(screen.getByText('Add Friend By Code'));
        expect(screen.getByText('Add Friend by their Code')).toBeDefined();
    });

    it('opens drawer on card click', async () => {
        mockFriendEps({
            friends: [{ id: 'f1', name: 'Alice', initials: 'AL', code: 'AL123', dotsScore: 300, tier: 'Gold' }],
        });
        render(<FriendsManagementPage />);

        await waitFor(() => {
            expect(screen.getByText('Alice')).toBeDefined();
        });
        fireEvent.click(screen.getByText('Alice'));
        expect(await screen.findByLabelText(/Athlete profile for Alice/i)).toBeDefined();
    });
});

function mockHubEps({
    myArenas = [],
    feed = [],
    isUserOptedIn = false,
    totalCount = 0,
    currentUserEntry = null,
}: {
    myArenas?: unknown[];
    feed?: unknown[];
    isUserOptedIn?: boolean;
    totalCount?: number;
    currentUserEntry?: unknown;
} = {}) {
    let optedIn = isUserOptedIn;
    const mockingFtch = customFetch as unknown as Mock;
    mockingFtch.mockImplementation(async (url: string) => {
        if (url.includes('/api/clash/leaderboard/opt-in')) {
            optedIn = !optedIn;
            return { ok: true, json: async () => ({ success: true, isOptedIn: optedIn }) };
        }
        if (url.includes('/api/clash/leaderboard/global')) {
            return { ok: true, json: async () => ({ isUserOptedIn: optedIn, totalCount, currentUserEntry }) };
        }
        if (url.includes('/api/clash/arenas/my')) {
            return { ok: true, json: async () => myArenas };
        }
        if (url.includes('/api/clash/arenas/feed')) {
            return { ok: true, json: async () => feed };
        }
        return { ok: true, json: async () => ({}) };
    });
    return mockingFtch;
}

describe('PgArenaHub', () => {
    it('shows opted out prompt by default', async () => {
        mockHubEps();
        render(<ArenaHubPage />);

        await waitFor(() => {
            expect(mockConn.start).toHaveBeenCalled();
        });
        expect(screen.getByText(/Opt into the global leagues/i)).toBeDefined();
        expect(screen.getByText('Opted Out')).toBeDefined();
    });

    it('shows user rank when opted in with a standing', async () => {
        mockHubEps({
            isUserOptedIn: true,
            currentUserEntry: { rank: 3, displayName: 'Test Lifter', dotsScore: 320, tier: 'Gold', tierLevel: 1 },
        });
        render(<ArenaHubPage />);

        await waitFor(() => {
            expect(screen.getByText(/Rank #3 on Season League/i)).toBeDefined();
        });
    });

    it('toggles opt-in state on click', async () => {
        mockHubEps({ isUserOptedIn: false });
        render(<ArenaHubPage />);

        await waitFor(() => {
            expect(screen.getByText('Opted Out')).toBeDefined();
        });
        fireEvent.click(screen.getByText('Global Rankings:'));

        await waitFor(() => {
            expect(screen.getByText('Opted In')).toBeDefined();
        });
    });

    it('shows private arenas on the private tab', async () => {
        mockHubEps({
            myArenas: [{ id: 'arena-1', name: 'Iron Squad', type: 'private', metricType: 'DOTS Overall', memberCount: 4 }],
        });
        render(<ArenaHubPage />);

        fireEvent.click(await screen.findByText('Private Arenas'));
        expect(await screen.findByText('Iron Squad')).toBeDefined();
    });

    it('navigates to arena leaderboard on card click', async () => {
        mockHubEps({
            myArenas: [{ id: 'arena-1', name: 'Iron Squad', type: 'private', metricType: 'DOTS Overall', memberCount: 4 }],
        });
        render(<ArenaHubPage />);

        fireEvent.click(await screen.findByText('Private Arenas'));
        fireEvent.click(await screen.findByText('Iron Squad'));
        expect(mockNavig).toHaveBeenCalledWith('/clash/arena-1');
    });

    it('shows empty state for live feed by default', async () => {
        mockHubEps();
        render(<ArenaHubPage />);

        await waitFor(() => {
            expect(screen.getByText(/No live activity in your arenas yet/i)).toBeDefined();
        });
    });

    it('sends kudos on feed item, increments count', async () => {
        mockHubEps({
            feed: [{ id: 'act-1', arenaId: 'arena-1', userId: 'u1', userName: 'Sam', eventText: 'Hit a PR', details: '100kg squat', kudosCount: 2 }],
        });
        render(<ArenaHubPage />);

        expect(await screen.findByText('Sam')).toBeDefined();
        fireEvent.click(screen.getByText('2'));

        await waitFor(() => {
            expect(screen.getByText('3')).toBeDefined();
        });
    });

    it('opens create/join arena modal', async () => {
        mockHubEps();
        render(<ArenaHubPage />);

        await waitFor(() => {
            expect(screen.getByText('Create/Join Arena')).toBeDefined();
        });
        fireEvent.click(screen.getByText('Create/Join Arena'));
        expect(screen.getByText('Gym Arenas')).toBeDefined();
    });

    it('opens drawer for own avatar', async () => {
        mockHubEps();
        render(<ArenaHubPage />);

        const avatar = await screen.findByTitle('Athlete');
        fireEvent.click(avatar);
        expect(await screen.findByLabelText(/Athlete profile for/i)).toBeDefined();
    });
});
