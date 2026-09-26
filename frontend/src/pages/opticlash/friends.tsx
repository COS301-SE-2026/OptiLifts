import { AddFriendModal } from "@/components/opticlash/add-friend-modal";
import { AthleteAvatar } from "@/components/opticlash/athlete-avatar";
import { ClashTabs } from "@/components/opticlash/clash-tabs";
import { CreateDuelModal } from "@/components/opticlash/create-duel-modal";
import { ProfileInspectorDrawer } from "@/components/opticlash/profile-inspector-drawer";
import { TierBadge } from "@/components/opticlash/tier-badge";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { PageTitle } from "@/components/ui/page-title";
import { SearchInput } from "@/components/ui/search-input";
import { customFetch } from "@/lib/custom-fetch";
import type { ClashAthlete, DuelInviteItem } from "@/types/clash";
import { Shield, ArrowLeft, Check, CheckCircle2, Copy, Trash2, UserPlus, Users, XCircle, Mail, Swords } from "lucide-react";
import { useState, useEffect } from "react";
import { Link } from "react-router-dom";

export default function FriendsManagementPage(){
    const [activeTab, setActiveTab] = useState<'friends' | 'requests' | 'invites'>('friends');
    const [searchQuery, setSearchQuery] = useState('');
    const [requestSearchQuery, setRequestSearchQuery] = useState('');
    const [copiedCode, setCopiedCode] = useState(false);

    interface FriendItem{
        id: string;
        name: string;
        initials: string;
        avatarUrl?: string;
        code: string;
        dotsScore: number;
        tier: string;
    }
    interface FriendRequestItem{
        id: string;
        fromAthleteId: string;
        fromName: string;
        fromInitials: string;
        fromAvatarUrl?: string;
        fromCode: string;
        sentAt: string;
    }

    interface ArenaInviteItem {
        id: string;
        arenaId: string;
        arenaName: string;
        arenaCode?: string;
        invitedBNyUserId: string;
        invitedByName: string;
        invitedByInitials: string;
        invitedByAvatarUrl?: string;
        status: string;
        createdAt: string;
    }

    //f1 - friends state
    const [friendsList, setFriendsList] = useState<FriendItem[]>([]);
    const [requestsLists, setRequestsLists] = useState<FriendRequestItem[]>([]);
    const [userCode, setUserCode] = useState<string>('');
    const [isAddFriendOpen, setIsAddFriendOpen] = useState(false);

    //f2 - profile inspector drawer
    const [selectedAthlete, setSelectedAthlete] = useState<ClashAthlete | null>(null);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const handleOpenFriendDrawer = (friend: FriendItem) => {
        const fallbackAthlete: ClashAthlete = {
            id: friend.id,
            name: friend.name,
            initials: friend.initials || 'AT',
            avatarUrl: friend.avatarUrl,
            code: friend.code,
            gender: 'male',
            bodyweightKg: 0,
            squat1RM: 0,
            bench1RM: 0,
            deadlift1RM: 0,
            totalE1RM: 0,
            dotsScore: friend.dotsScore,
            tier: (friend.tier as ClashAthlete['tier']) || 'Unranked',
            tierLevel: 1,
            rankTrend: 0,
            weeklyVolumeKg: 0,
            lastWorkoutDate: new Date().toISOString(),
            muscleBalance30d: { Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0 },
            trophies: [],
            recentWorkouts: []
        };
        setSelectedAthlete(fallbackAthlete);
        setIsDrawerOpen(true);
    }
    //f3 - arena invites
    const [arenaInvites, setArenaInvites] = useState<ArenaInviteItem[]>([]);
    //f4 - duel invites
    const [duelInvites, setDuelInvites] = useState<DuelInviteItem[]>([]);
    const [privacyPolicy, setPrivacyPolicy] = useState<'friends' | 'none'>('friends');
    const [isCreateDuelOpen, setIsCreateDuelOpen] = useState(false);
    const [selectedFriendForDuel, setSelectedFriendForDuel] = useState<{ id: string; name: string; dotsScore?: number } | null>(null);

    //handlers
    const fetchFriendsData = async () => {
        await Promise.resolve();
        try {
            const [friendsRes, requestsRes, codeRes, invitesRes, duelInvitesRes] = await Promise.all([
                customFetch('/api/clash/friends'), customFetch('/api/clash/friends/requests'), customFetch('/api/clash/friends/code'),
                customFetch('/api/clash/arenas/invites'), customFetch('/api/clash/duels/invites'),
            ]);
            if (friendsRes.ok){
                const data = await friendsRes.json();
                setFriendsList(data);
            }
            if (requestsRes.ok){
                const data = await requestsRes.json();
                setRequestsLists(data.incoming ?? []);
            }
            if (codeRes.ok){
                const data = await codeRes.json();
                setUserCode(data.code ?? '');
            }
            if (invitesRes.ok) {
                const data = await invitesRes.json();
                setArenaInvites(Array.isArray(data) ? data : []);
            }
            if (duelInvitesRes.ok) {
                const data = await duelInvitesRes.json();
                setDuelInvites(Array.isArray(data) ? data : []);
            }
        } catch {
            toast.error('Failed to load friends');
        }
    };

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch-on-mount, not a state-adjustment effect
        void fetchFriendsData();
    }, []);

    //copying ur own code to clip
    const handleCopyCode = () =>{
        if (!userCode) return;
        navigator.clipboard.writeText(userCode);
        setCopiedCode(true);
        toast.success('Your friend code is copied to clipboard', userCode);
        setTimeout(() => setCopiedCode(false), 2000);
    };

    //api call post /respond - accept true
    const handleAcceptRequest = async (requestId: string, name:string)=> {
        try {
            const res = await customFetch(`/api/clash/friends/requests/${requestId}/respond`,{
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    accept: true
                }),
            });
            if (res.ok){
                setRequestsLists((prev) => prev.filter((r) => r.id !== requestId));
                toast.success(`You are now friends with ${name}.`, 'Request Accepted');
                fetchFriendsData();
            } else {
                toast.error('Failed to accept friend request');
            }
        } catch {
            toast.error('Failed to accept friend request');
        }        
    };
    //api call post /respond - accept false
    const handleRejectRequest = async (requestId: string)=> {
        try {
            const res = await customFetch(`/api/clash/friends/requests/${requestId}/respond`,{
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    accept: false
                }),
            });
            if (res.ok){
                setRequestsLists((prev) => prev.filter((r) => r.id !== requestId));
                toast.success('Friend request declined');
                fetchFriendsData();
            } else {
                toast.error('Failed to decline friend request');
            }
        } catch {
            toast.error('Failed to decline friend request');
        }
    }
    //post reject/reject-all
    const handleRejectAllRequests= async () =>{
        try {
            const res = await customFetch('/api/clash/friends/requests/reject-all',{
                method: 'POST',
            });
            if (res.ok){
                setRequestsLists([]);
                toast.info('All pending friend requests have been rejected.')
            } else {
                toast.error('Failed to reject requests');
            }
        } catch {
            toast.error('Failed to reject requests');
        }
        
    };

    const handleAcceptArenaInvite = async (inviteId: string, arenaName: string) => {
        try {
            const res = await customFetch(`/api/clash/arenas/invites/${inviteId}/respond`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    accept: true
                }),
            });
            if (res.ok) {
                setArenaInvites((prev) => prev.filter((i) => i.id !== inviteId));
                toast.success(`Joined ${arenaName}`, 'Arena Joined');
                void fetchFriendsData();
            } else {
                toast.error('Failed to accept arena invite');
            }
        } catch {
            toast.error('Network error accepting invite');
        }
    };
    const handleDeclineArenaInvite = async (inviteId: string) => {
        try {
            const res = await customFetch(`/api/clash/arenas/invites/${inviteId}/respond`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    accept: false
                }),
            });
            if (res.ok) {
                setArenaInvites((prev) => prev.filter((i) => i.id !== inviteId));
                toast.info('Arena invitation declined');
            } else {
                toast.error('Failed to decline arena invite');
            }
        } catch {
            toast.error('Network error declining invite');
        }
    };

    //f4 - handle accept duel + handle reject duel 
    const handleAcceptDuel = async (inviteId: string, challengerName: string) => {
        try {
            const res = await customFetch(`/api/clash/duels/${inviteId}/respond`, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json' 
                },
                body: JSON.stringify({ 
                    accept: true
                 }),
            });
            if (res.ok) {
                setDuelInvites((prev) => prev.filter((d) => d.id !== inviteId));
                toast.success(`1v1 duel with ${challengerName} has started`, 'Duel Accepted');
            } else {
                toast.error('Failed to accept duel invite');
            }
        } catch {
            toast.error('Network error accepting duel invite');
        }
    };
    const handleRejectDuel = async (inviteId: string) => {
        try {
            const res = await customFetch(`/api/clash/duels/${inviteId}/respond`, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json' 
                },
                body: JSON.stringify({ 
                    accept: false
                 }),
            });
            if (res.ok) {
                setDuelInvites((prev) => prev.filter((d) => d.id !== inviteId));
                toast.info('Duel challenge declined.');
            } else {
                toast.error('Failed to decline duel challenge');
            }
        } catch {
            toast.error('Network error declining duel challenge');
        }
    };
    const handleDeclineAllInvites = async () => {
        try {
            await Promise.all([
                ...arenaInvites.map((ainv) => customFetch(`/api/clash/arenas/invites/${ainv.id}/respond`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json' 
                    },
                    body: JSON.stringify({ 
                        accept: false 
                    }),
                })),
                ...duelInvites.map((dinv) => customFetch(`/api/clash/duels/${dinv.id}/respond`, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json' 
                    },
                    body: JSON.stringify({ 
                        accept: false 
                    }),
                })),
            ]);
            setArenaInvites([]);
            setDuelInvites([]);
            toast.info('All arena and duel invitations declined.');
        } catch {
            toast.error('Failed to decline all invites');
        }
    };
    //f3 + f4 - handle reject all arena + duel invites
    const handleDeclineAllArenaInvites = async () => {
        try {
            await Promise.all(
                arenaInvites.map((ainv) => customFetch(`/api/clash/arenas/invites/${ainv.id}/respond`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        accept: false
                    }),
                }))
            );
            setArenaInvites([]);
            toast.info('All arena invites declined');
        } catch {
            toast.error('Failed to decline all invites');
        }
    };

    const filteredFriends = friendsList.filter((f) => f.name.toLowerCase().includes(searchQuery.toLowerCase()) 
    || f.code.toLowerCase().includes(searchQuery.toLowerCase()));
    const filteredRequests = requestsLists.filter((f) => f.fromName.toLowerCase().includes(requestSearchQuery.toLowerCase()) 
    || f.fromCode.toLowerCase().includes(requestSearchQuery.toLowerCase()));

    return (
        <div className="min-h-screen bg-background text-foreground pb-20">
            <div className="max-w-6xl mx-auto px-4 pt-8 pb-4">
                <Link to="/clash" className="inline-flex items-center gap-2 text-xs font-bold text-muted-foreground hover:text-brand uppercase tracking-[1px] mb-4 transition font-sans">
                    <ArrowLeft className="w-4 h-4"/>Back to Arena Hub
                </Link>

                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                        <PageTitle title="FRIENDS AND INVITES"/>
                    </div>

                    <div className="flex items-center gap-3 bg-surface border border-border p-3 rounded-xl self-start md:self-auto shadow-sm">
                        <div>
                            <span className="text-[10px] uppercase font-bold text-muted-foreground block font-sans tracking-[1px]">
                                Your Private Friend Code
                            </span>
                            <span className="font-mono text-xl md:text-2xl font-bold text-brand tracking-wider block">
                                {userCode || '------'}
                            </span>
                        </div>
                        <Button variant="secondary" size="sm" onClick={handleCopyCode} className="h-8 px-3 text-xs">
                            {copiedCode ? <Check className="w-3.5 h-3.5 text-success mr-1"/> : <Copy className="w-3.5 h-3.5 mr-1"/>}
                            <span>{copiedCode ? 'Copied' : 'Copy'}</span>
                        </Button>
                    </div>
                </div>

                {/* tabs */}
                <div className="mt-6">
                    <ClashTabs activeTab={activeTab} onChange={(tab) => setActiveTab(tab)}
                        tabs={[
                            { id: 'friends', label: 'My Friends', count: friendsList.length, icon: <Users className="w-3.5 h-3.5"/>},
                            { id: 'requests', label: 'Requests', count: requestsLists.length, icon: <UserPlus className="w-3.5 h-3.5"/>},
                            { id: 'invites', label: 'Arena & Duel Invites', count: arenaInvites.length + duelInvites.length, icon: <Mail className="w-3.5 h-3.5"/>}
                            ]}/>
                </div>
            </div>

            <div className="max-w-6xl mx-auto px-4 py-4 space-y-6">
                {activeTab === 'friends' &&(
                    <div className="space-y-4">
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                            <div className="w-full max-w-md">
                                <SearchInput placeholder="Search friends by name or code" value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                className="h-9 text-xs bg-surface border-border font-sans"/>
                        </div>
                        <Button variant="default" size="sm" onClick={() => setIsAddFriendOpen(true)}
                        className="h-9 text-xs flex items-center gap-1.5 shrink-0">
                            <UserPlus className="w-3.5 h-3.5"/>
                            <span>Add Friend By Code</span>
                        </Button>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {filteredFriends.map((friend) => (
                            <Card key={friend.id} onClick={() => handleOpenFriendDrawer(friend)} className="bg-surface hover:bg-surface-2 transition cursor-pointer border-border group p-5 shadow-sm flex flex-col justify-between">
                                <CardContent className="p-0">
                                    <div className="flex items-start justify-between">
                                        <div className="flex items-center gap-3.5">
                                            <AthleteAvatar initials={friend.initials} name={friend.name} avatarUrl={friend.avatarUrl} size="lg" className="group-hover:border-brand transition"/>
                                            <div>
                                                <div className="flex items-center gap-2">
                                                    <strong className="font-sans font-bold text-foreground text-base">
                                                        {friend.name}
                                                    </strong>
                                                    <span className="font-mono text-xs font-bold text-brand bg-surface-2 border border-border px-2 py-0.5 rounded">
                                                        {friend.code}
                                                    </span>
                                                </div>
                                                <div className="text-xs text-muted-foreground mt-1 font-sans">
                                                    Score: <strong className="font-bold text-brand">{friend.dotsScore} DOTS</strong>
                                                </div>
                                            </div>
                                        </div>
                                        {/* tier badge */}
                                        <div>
                                            <TierBadge tier={friend.tier || 'Unranked'} size="md" />
                                        </div>
                                    </div>

                                    {/* f2 - inspect profile btn */}
                                    {/* f4 - challenge 1v1 btn */}
                                    <div className="flex items-center gap-2 mt-4 pt-3 border-t border-border">
                                        <Button variant="secondary" size="sm" onClick={() => handleOpenFriendDrawer(friend)}
                                        className="flex-1 h-8 text-xs font-semibold">
                                            Inspect Profile
                                        </Button>
                                        <Button variant="default" size="sm"
                                        onClick={() => {
                                            setSelectedFriendForDuel({
                                                id: friend.id,
                                                name: friend.name,
                                                dotsScore: friend.dotsScore,
                                            });
                                            setIsCreateDuelOpen(true);
                                        }}
                                        className="flex-1 h-8 text-xs flex items-center justify-center gap-1.5">
                                            <Swords className="w-3.5 h-3.5" />
                                            <span>Challenge 1v1</span>
                                        </Button>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                        </div>
                    </div>
                )}

                {activeTab === 'requests' && (
                    <div className="space-y-4">
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                            <div className="w-full max-w-md">
                                <SearchInput placeholder="Search requests by name or code" value={requestSearchQuery} onChange={(e) => setRequestSearchQuery(e.target.value)}
                                    className="h-9 text-xs bg-surface border-border font-sans"/>
                            </div>

                            {requestsLists.length > 0 && (
                                <Button variant="outline" size="sm" onClick={handleRejectAllRequests}
                                className="h-9 text-xs text-destructive border-destructive/30 hover:bg-destructive/10 flex items-center gap-1.5 shrink-0">
                                    <Trash2 className="w-3.5 h-3.5"/>
                                    <span>Reject All Requests</span>
                                </Button>
                            )}
                            </div>

                        {filteredRequests.length === 0 ? (
                            <Card className="bg-surface border-border p-10 text-center text-muted-foreground font-sans text-xs">
                                {requestsLists.length === 0 ? 'No pending incoming friend requests' : 'No friend requests found matching your search'}
                            </Card>
                        ) : (
                    <div className="space-y-3">
                        {filteredRequests.map((req) => (
                            <Card key={req.id} className="bg-surface border-border p-4 shadow.sm">
                                <CardContent className="p-0 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                                    <div className="flex items-center gap-3.5">
                                        <AthleteAvatar initials={req.fromInitials} name={req.fromName} avatarUrl={req.fromAvatarUrl} size="md"/>
                                        <div>
                                            <div className="flex items-center gap-2">
                                                <strong className="font-sans font-bold text-foreground">
                                                    {req.fromName}
                                                </strong>
                                                <span className="font-mono text-xs font-bold text-brand bg-surface-2 border border-border px-1.5 py-0.5 rounded">
                                                    {req.fromCode}
                                                </span>
                                            </div>
                                            <span className="text-xs text-muted-foreground font-sans block mt-0.5">
                                                Sent {req.sentAt}
                                            </span>
                                        </div>
                                    </div>
                                    {/* btns */}
                                    <div className="flex items-center gap-2 self-end sm:self-auto">
                                        <Button variant="secondary" size="sm" onClick={() => handleRejectRequest(req.id)}
                                        className="h-8 flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
                                            <XCircle className="w-3.5 h-3.5"/>
                                            <span>Reject</span>
                                        </Button>
                                        <Button variant="default" size="sm" onClick={() => handleAcceptRequest(req.id, req.fromName)}
                                        className="h-8 flex items-center gap-1 text-xs">
                                            <CheckCircle2 className="w-3.5 h-3.5"/>
                                            <span>Accept Friend</span>
                                        </Button>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                        </div>
                    )}
                    </div>
                )}

                {/* f3 + f4 - tab 3 arena + duel invites */}
                {activeTab === 'invites' && (
                    <div className="space-y-6">
                    <Card className="bg-surface border-border p-4 shadow-sm">
                        <CardContent className="p-0 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
                            <div className="flex items-center gap-3">
                                        <Shield className="w-5 h-5 text-brand shrink-0" />
                                        <div>
                                            <h4 className="font-sans font-bold text-sm text-foreground">1v1 Duel Invite Privacy</h4>
                                            <p className="text-xs text-muted-foreground font-sans">
                                                Control who can send you direct 1v1 progressive overload challenges.
                                            </p>
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-3 self-start lg:self-auto flex-wrap">
                                        <div className="flex bg-surface-2 border border-border rounded-lg p-1">
                                            <button type="button" onClick={() => {
                                                    setPrivacyPolicy('friends');
                                                    toast.info('Duel invites set to Friends Only.');
                                                }}
                                                className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans whitespace-nowrap ${
                                                    privacyPolicy === 'friends' ? 'bg-surface text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
                                                }`} >
                                                Friends Only
                                            </button>
                                            <button type="button"
                                                onClick={() => {
                                                    setPrivacyPolicy('none');
                                                    toast.info('Duel invites disabled.');
                                                }}
                                                className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans whitespace-nowrap ${
                                                    privacyPolicy === 'none' ? 'bg-surface text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
                                                }`}>
                                                No One
                                            </button>
                                        </div>
                                        {(arenaInvites.length > 0 || duelInvites.length > 0) && (
                                            <Button variant="outline" size="sm" onClick={handleDeclineAllInvites}
                                            className="h-8 text-xs text-destructive border-destructive/30 hover:bg-destructive/10 flex items-center gap-1.5 shrink-0">
                                                <Trash2 className="w-3.5 h-3.5" />
                                                <span>Decline All Invites</span>
                                            </Button>
                                        )}
                                    </div>
                        </CardContent>
                    </Card>

                        <div className="space-y-3">
                            <h3 className="font-display text-xl tracking-wide text-foreground flex items-center gap-2">
                                <Swords className="w-5 h-5 text-brand" /> 1v1 Duel Invites ({duelInvites.length})
                            </h3>

                            {duelInvites.length === 0 ? (
                                <Card className="bg-surface border-border p-6 text-center text-muted-foreground text-xs font-sans">
                                    No pending 1v1 duel challenges
                                </Card>
                            ) : (
                                <div className="space-y-3">
                                    {duelInvites.map((dinv) => {
                                        const initials = dinv.challengerName ? dinv.challengerName.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase() : 'OP';
                                        const targetLabel = dinv.targetType.toLowerCase().includes('volume') ? 'Total Volume (kg)' : 'E1RM Gain (%)';
                                        return (
                                            <Card key={dinv.id} className="bg-surface border-border p-4 shadow-sm">
                                                <CardContent className="p-0 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                                                    <div className="flex items-center gap-3">
                                                        <AthleteAvatar initials={initials} name={dinv.challengerName} avatarUrl={dinv.challengerAvatarUrl ?? undefined} size="md"/>
                                                        <div>
                                                                <strong className="font-sans font-bold text-base text-foreground block">
                                                                    {dinv.challengerName} challenged you to a 1v1!
                                                                </strong>
                                                                <p className="text-xs text-muted-foreground font-sans mt-0.5">
                                                                    <strong className="text-foreground">{dinv.exerciseName}</strong> &bull; {targetLabel} &bull; <strong className="font-mono text-brand">{dinv.durationDays} Days</strong>
                                                                </p>
                                                            </div>
                                                    </div>
                                                    <div className="flex items-center gap-2 self-end sm:self-auto">
                                                            <Button variant="secondary" size="sm" onClick={() => void handleRejectDuel(dinv.id)}
                                                                className="h-8 flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
                                                                <XCircle className="w-3.5 h-3.5" />
                                                                <span>Decline</span>
                                                            </Button>
                                                            <Button variant="default" size="sm" onClick={() => void handleAcceptDuel(dinv.id, dinv.challengerName)}
                                                                className="h-8 flex items-center gap-1 text-xs">
                                                                <CheckCircle2 className="w-3.5 h-3.5" />
                                                                <span>Accept Duel</span>
                                                            </Button>
                                                        </div>
                                                </CardContent>
                                            </Card>
                                        );
                                    })}
                                    </div>
                                    )}
                                    </div>
                            

                    <div className="space-y-6">
                        <div className="flex items-center justify-between">
                            <h3 className="font-display text-xl tracking-wide text-foreground flex items-center gap-2">
                                <Users className="w-5 h-5 text-brand"/> Arena Invites ({arenaInvites.length})
                            </h3>
                            {arenaInvites.length > 0 && (
                                <Button variant="outline" size="sm" onClick={handleDeclineAllArenaInvites}
                                className="h-8 text-xs text-destructive border-destructive/30 hover:bg-destructive/10 flex items-center gap-1.5">
                                    <Trash2 className="w-3.5 h-3.5"/>
                                    <span>Decline All Invites</span>
                                </Button>
                            )}
                            </div>

                            {arenaInvites.length === 0 ? (
                                <Card className="bg-surface border-border p-6 text-center text-muted-foreground text-xs font-sans">
                                    No pending arena invites.
                                </Card>
                            ) : (
                                <div className="space-y-3">
                                    {arenaInvites.map((ainv) => (
                                        <Card key={ainv.id} className="bg-surface border-border p-4 shadow-sm">
                                            <CardContent className="p-0 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                                                <div className="flex items-center gap-3">
                                                    <AthleteAvatar initials={ainv.invitedByInitials} name={ainv.invitedByName}
                                                    avatarUrl={ainv.invitedByAvatarUrl} size="md"/>
                                                    <div>
                                                        <strong className="font-sans font-bold text-base text-foreground block">
                                                            {ainv.arenaName}
                                                        </strong>
                                                        <p className="text-xs text-muted-foreground font-sans mt-0.5">
                                                            Invited by <strong className="text-foreground">{ainv.invitedByName}</strong>
                                                            {ainv.arenaCode && 
                                                                <span> - Code: <strong className="font-mono text-brand">{ainv.arenaCode}</strong></span>}
                                                        </p>
                                                    </div>
                                                </div>

                                                <div className="flex items-center gap-2 self-end sm:self-auto">
                                                    <Button variant="secondary" size="sm" onClick={() => handleDeclineArenaInvite(ainv.id)}
                                                        className="h-8 flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
                                                        <XCircle className="w-3.5 h-3.5"/>
                                                        <span>Decline</span>
                                                    </Button>
                                                    <Button variant="default" size="sm" onClick={() => handleAcceptArenaInvite(ainv.id, ainv.arenaName)}
                                                        className="h-8 flex items-center gap-1 text-xs">
                                                        <CheckCircle2 className="w-3.5 h-3.5"/>
                                                        <span>Join Arena</span>
                                                    </Button>
                                                </div>
                                            </CardContent>
                                        </Card>
                                    )
                                    )}
                            </div>
                        )}
                    </div>
                    </div>
                )}
                </div>

            <AddFriendModal isOpen={isAddFriendOpen} onClose={() => setIsAddFriendOpen(false)} onFriendAdded={() => void fetchFriendsData()} />
            <ProfileInspectorDrawer athlete={selectedAthlete} isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)}
                onChallengeDuel={(friend) => {
                    setIsDrawerOpen(false);
                    setSelectedFriendForDuel({
                        id: friend.id,
                        name: friend.name,
                        dotsScore: friend.dotsScore,
                    });
                    setIsCreateDuelOpen(true);
                }}/>
                <CreateDuelModal isOpen={isCreateDuelOpen} onClose={() => setIsCreateDuelOpen(false)} defaultFriend={selectedFriendForDuel} onDuelCreated={() => void fetchFriendsData()}/>
        </div>
    );
}