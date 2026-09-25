import { AthleteAvatar } from "@/components/opticlash/athlete-avatar";
import { ProfileInspectorDrawer } from "@/components/opticlash/profile-inspector-drawer";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { type ClashActivityItem, type ClashAthlete, type ClashArena } from "@/types/clash";
import { ArrowRight, Clock, Trophy, UserPlus, Users, Flame, Heart, Plus } from "lucide-react";
import { useState, useEffect, useRef, type MouseEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { CreateJoinArenaModal } from "@/components/opticlash/create-join-arena-modal";
import { ClashTabs } from "@/components/opticlash/clash-tabs";
import { customFetch } from "@/lib/custom-fetch";
import confetti from "canvas-confetti";
import * as signalR from "@microsoft/signalr";
import { useAuth } from "@/context/auth-context";

interface SignalRActivityPayload {
    id: string;
    arenaId?: string;
    userId: string
    userName: string
    userInitials?: string;
    userAvatarUrl?: string;
    eventText: string;
    details: string;
    kudosCount?: number;
    createdAt?: string;
    hasUserKudoed?: boolean;
}

interface UserStandingDto {
    userId: string;
    rank: number;
    displayName: string;
    avatarUrl?: string;
    bodyweightKg: number;
    tier: string;
    tierLevel: number;
    dotsScore: number;
    squat1RM: number;
    bench1RM: number;
    deadlift1RM: number;
    totalE1RM: number;
    weeklyVolumeKg: number;
    rankTrend: number;
    isCurrentUser: boolean;
}

const SYSTEM_ARENAS: ClashArena[] = [
  {
    id: 'global-league',
    name: 'OptiLifts Global League',
    type: 'global',
    metricType: 'DOTS Overall',
    memberCount: 0,
  },
  {
    id: 'weight-class-league',
    name: 'Divisional Weight-Class League',
    type: 'divisional',
    metricType: 'DOTS Overall',
    memberCount: 0,
  },
];

export default function ArenaHubPage() {
    const {user } = useAuth();
    const [selectedAthlete, setSelectedAthlete] = useState<ClashAthlete | null>(null);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const [isLeaderboardOptedIn, setIsLeaderboardOptedIn] = useState(false);
    //f3-5 states go here
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [arenaMode, setArenaMode] = useState<'global' | 'private'>('global');
    const [activityList, setActivityList] = useState<ClashActivityItem[]>([]);
    const [kudosGivenMap, setKudosGivenMap] = useState<Record<string, boolean>>({});
    const [myArenas, setMyArenas] = useState<ClashArena[]>([]);
    const activityListRef = useRef<ClashActivityItem[]>([]);
    const kudosGivenMapRef = useRef<Record<string, boolean>>({});

    const [userStanding, setUserStanding] = useState<UserStandingDto | null>(null);

    useEffect(() => {
        activityListRef.current = activityList;
    }, [activityList]);
    useEffect(() => {
        kudosGivenMapRef.current = kudosGivenMap;
    }, [kudosGivenMap]);
    const myArenasRef = useRef<ClashArena[]>([]);
    useEffect(() => {
        myArenasRef.current = myArenas;
    }, [myArenas]);

    const fetchMyArenas = async () => {
        try {
            const res = await customFetch('/api/clash/arenas/my');
            if (res.ok) {
                const data = await res.json();
                if (Array.isArray(data)) {
                    setMyArenas(data);
                }
            } 
        } catch {
            //keep def
        }
    };
    
    const fetchFeed = async () => {
        try {
            const res = await customFetch('/api/clash/arenas/feed')
            if (res.ok) {
                const data = await res.json();
                if (Array.isArray(data) && data.length > 0) {
                    setActivityList(data.map((item: SignalRActivityPayload) => ({
                        id: item.id,
                        arenaId: item.arenaId || '',
                        athleteId: item.userId,
                        athleteName: item.userName,
                        athleteInitials: item.userInitials || 'AT',
                        athleteAvatarUrl: item.userAvatarUrl,
                        arenaName: myArenas.find((a) => a.id === item.arenaId)?.name || 'Squad Arena',
                        eventText: item.eventText,
                        details: item.details,
                        kudosCount: item.kudosCount ?? 0,
                        timeAgo: item.createdAt ? new Date(item.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Just now',
                    })));

                    const kudoMap: Record<string, boolean> = {};
                    data.forEach((item: SignalRActivityPayload) => {
                        if (item.hasUserKudoed) {
                            kudoMap[item.id] = true;
                        }
                    });
                    setKudosGivenMap((prev) => ({
                        ...prev,
                        ...kudoMap
                    }));
                }
            }
        } catch {
            //keep fallback
        }
    };

    const fetchUserSnapshot = async () => {
        try {
            const res = await customFetch('/api/clash/leaderboard/global?pageSize=1');
            if (res.ok) {
                const data = await res.json();
                setIsLeaderboardOptedIn(data?.isUserOptedIn ?? false);
                if (data?.currentUserEntry) {
                    setUserStanding(data.currentUserEntry);
                } else {
                    setUserStanding(null);
                }
            }
        } catch {
            //keep default
        }
    };

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch-on-mount
        void fetchMyArenas();
        void fetchFeed();
        void fetchUserSnapshot();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const allArenas = [
        ...SYSTEM_ARENAS,
        ...myArenas
    ];

    const filtArenas = allArenas.filter((a) => arenaMode === 'global' ? (a.type?.toLowerCase() === 'global' || a.type?.toLowerCase() === 'divisional') : a.type?.toLowerCase() === 'private');


    const navigate = useNavigate();
    const [isTogglingOptIn, setIsTogglingOptIn] = useState<boolean>(false);
    const handleToggleOptIn = async () => {
        if (isTogglingOptIn) return;
        const nextState = !isLeaderboardOptedIn;
        setIsTogglingOptIn(true);
        try {
            const res = await customFetch('/api/clash/leaderboard/opt-in', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    optIn: nextState
                }),
            });
            const data = await res.json().catch(() => null);
            if (res.ok && data?.success) {
                setIsLeaderboardOptedIn(data.isOptedIn ?? nextState);
                if (data.isOptedIn ?? nextState) {
                    toast.success("Your lifts and e1RM are now actively ranked in global and divisonal leaderboards.", 'Opted in to Public Leagues');
                } else {
                    toast.success('Your profile and lifts have been hidden from public leagues.', 'Opted out of Public Leagues');
                }
                void fetchUserSnapshot();
            } else {
                toast.error(data?.message || 'Failed to update leaderboard privacy settings', 'Bodyweight Required');
            }
        } catch {
            toast.error('Network error updating leaderboard privacy settings');
        } finally {
            setIsTogglingOptIn(false);
        }
    };

    const handleOpenAthlete = (athleteId?: string) => {
        const athId = athleteId || user?.id || '';
        const athleteName = userStanding?.displayName || user?.name || 'You';
        const fallbackAthlete: ClashAthlete = {
            id: athId,
            name: athleteName,
            initials: athleteName.slice(0, 2).toUpperCase() || 'AT',
            avatarUrl: userStanding?.avatarUrl || user?.avatarUrl,
            code: 'OPTICLASH',
            gender: 'female',
            bodyweightKg: userStanding?.bodyweightKg || 74,
            squat1RM: userStanding?.squat1RM || 0,
            bench1RM: userStanding?.bench1RM || 0,
            deadlift1RM: userStanding?.deadlift1RM || 0,
            totalE1RM: userStanding?.totalE1RM || 0,
            dotsScore: userStanding?.dotsScore || 0,
            tier: (userStanding?.tier as ClashAthlete['tier']) || 'Bronze',
            tierLevel: userStanding?.tierLevel || 1,
            rankTrend: userStanding?.rankTrend || 0,
            weeklyVolumeKg: userStanding?.weeklyVolumeKg || 0,
            lastWorkoutDate: new Date().toISOString(),
            muscleBalance30d: { Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0 },
            trophies: [],
            recentWorkouts: []
        }
        setSelectedAthlete(fallbackAthlete);
        setIsDrawerOpen(true);
    };

    //f3-5: handle feed kudos
    const handleFeedKudos = async (e:MouseEvent<HTMLButtonElement>, activityId: string) => {
        e.stopPropagation();
        if (!kudosGivenMap[activityId]) {
            setKudosGivenMap((prev) => ({
                ...prev,
                [activityId]: true
            }));
            setActivityList((prev) => prev.map((item) => item.id === activityId ? {
                ...item,
                kudosCount: item.kudosCount + 1
            } : item));

            const targetItem = activityList.find((i) => i.id === activityId);
            if (targetItem) {
                toast.success(`You cheered on ${targetItem.athleteName}'s lift`, 'Hype Reaction Sent');
            }

            const rect = e.currentTarget.getBoundingClientRect();
            const x = (rect.left + rect.width / 2) / window.innerWidth;
            const y = (rect.top + rect.height / 2) / window.innerHeight;
            confetti({
                particleCount: 25,
                spread: 50,
                origin: {x,y},
                colors: ['#CC0022', '#FF9800', '#FFFFFF'],
            });

            try {
                await customFetch(`/api/clash/activities/${activityId}/kudos`, {
                    method: 'POST',
                });
            } catch {
                //nonblocking
            }
        }
    };

    useEffect(() => {
        let isCancelled = false;
        const connection = new signalR.HubConnectionBuilder().withUrl("/api/hubs/clash").withAutomaticReconnect().build();

        connection.start().then(() => {
            if (isCancelled) {
                void connection.stop();
                return;
            }
            myArenasRef.current.forEach((arena) => {
                if (arena.type?.toLowerCase() === "private") {
                    connection.invoke("JoinArena", arena.id);
                }
            });
        }).catch(() => {
            //retry handles automatically
        });

        connection.on("ReceiveActivity", (newActivity: SignalRActivityPayload) => {
            const formatted = {
                id: newActivity.id,
                arenaId: newActivity.arenaId || '',
                athleteId: newActivity.userId,
                athleteName: newActivity.userName,
                athleteInitials: newActivity.userInitials || "AT",
                athleteAvatarUrl: newActivity.userAvatarUrl,
                arenaName: myArenasRef.current.find((a) => a.id === newActivity.arenaId)?.name || "Squad Arena",
                eventText: newActivity.eventText,
                details: newActivity.details,
                kudosCount: newActivity.kudosCount ?? 0,
                timeAgo: "Just now",
            };
            setActivityList((prev) => {
                if (prev.some((a) => a.id === formatted.id)) return prev;
                return [formatted, ...prev];
            });
            if (newActivity.userId !== user?.id) {
                toast.info(newActivity.eventText, newActivity.details);
            }
        });
        connection.on("ReceiveUserJoined", (_arenaId: string, userName: string) => {
            toast.success(`${userName} just joined your squad!`, "New Squad Member");
        });

        connection.on("ReceiveUserLeft", (_arenaId, userName) => toast.info(`${userName} has left the squad`, "Member left Arena"));
        connection.on("ReceiveKudos", (activityId: string, count: number) => {
            const target = activityListRef.current.find((a) => a.id === activityId);
            if ((target && (target.athleteId === user?.id || (user?.name && target.athleteName === user.name))) && !kudosGivenMapRef.current[activityId]) {
                toast.success(`Someone cheered on your lift! (${count} kudos)`, "Hype Kudos");
            }
            setActivityList((prev) => prev.map(a => a.id === activityId ? {
                    ...a,
                    kudosCount: count
                } : a));
        });

        return () => {
            isCancelled = true;
            if (connection.state === signalR.HubConnectionState.Connected){
                void connection.stop();
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [user?.id]);

    const displayName = userStanding?.displayName || user?.name || 'Athlete';
    const userInitials = displayName.slice(0,2).toUpperCase() || 'AT';
    const currentMonth = new Date().toLocaleString('default', { month: 'long' });
    const tierBadge = userStanding ? `${userStanding.tier.toUpperCase()} TIER ${userStanding.tierLevel}` : 'UNRANKED';

    const getSeasonCountDown = (): string =>{
        const now = new Date();
        const endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59);
        const diffMS = endOfMonth.getTime() - now.getTime();
        if (diffMS <= 0) {
            return '0D 00H';
        }
        const days = Math.floor(diffMS/(1000 * 60 * 60 * 24));
        const hours = Math.floor((diffMS % (1000*60*60*24))/(1000*60*60));
        return `${days}D ${String(hours).padStart(2, '0')}H`;
    }
    const seasonCountdown = getSeasonCountDown();

    return (
        <div className="min-h-screen bg-background text-foreground pb-20">
            {/* optiflitsheader */}
            <div className="max-w-6xl mx-auto px-4 pt-8 pb-4">
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                        <div className="inline-flex items-center gap-4">
                            <span className="w-1 h-9 bg-brand rounded-full flex-shrink-0"/>
                            <h1 className="font-display text-[42px] leading-none tracking-[2px] select-none">
                                <span className="text-foreground">OPTI</span><span className="text-brand">CLASH</span>
                            </h1>
                        </div>
                    </div>

                    {/* quick actions */}
                    <div className="flex items-center gap-3">
                        <Button variant="secondary" size="sm" asChild className="h-9 text-xs flex items-center gap-2">
                            <Link to="/clash/friends">
                                <UserPlus className="w-4 h-4"/>
                                <span>Friends & Invites</span>
                            </Link>
                        </Button>

                        {/* f3: create or join private squad arena btn goes here */}
                        <Button variant="default" size="sm" onClick={() => setIsModalOpen(true)} className="h-9 text-xs flex items-center gap-2">
                            <Plus className="w-4 h-4"/>
                            <span>Create/Join Arena</span>
                        </Button>
                    </div>
                </div>
            </div>

            <div className="max-w-6xl mx-auto px-4 py-4 space-y-8">
                {/* user status card */}
                <Card className="bg-surface border-border overflow-hidden shadow-sm p-6 relative">
                    <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-6 relative z-10">
                        <div className="flex items-start gap-4">
                            <button type="button" onClick={() => handleOpenAthlete(user?.id)}
                            className="hover:opacity-90 transition cursor-pointer shrink-0 rounded-2xl">
                                <AthleteAvatar initials={userInitials} name={displayName} avatarUrl={userStanding?.avatarUrl || user?.avatarUrl} isCurrentUser={true} size="xl"/>
                            </button>

                            <div>
                                <div className="flex items-center gap-2 font-sans">
                                    <span className="text-xs font-bold text-brand bg-brand-fill border border-brand/30 px-2 py-0.5 rounded uppercase tracking-wider">
                                        {tierBadge}
                                    </span>
                                    <span className="text-xs text-muted-foreground">
                                        {currentMonth} Season
                                    </span>
                                </div>
                                <h2 className="font-display text-2xl md:text-3xl tracking-wide text-foreground mt-1">
                                    {displayName}
                                </h2>
                                <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground mt-1.5 font-sans">
                                    {userStanding ? (
                                        <>
                                            <span className="flex items-center gap-1 font-bold text-brand">
                                                <Trophy className="w-4 h-4 text-warning" />Rank #2 on Season League
                                            </span>                                            
                                            <span>-</span>
                                            <span>Score:<strong className="text-foreground">{userStanding.dotsScore} DOTS</strong></span>
                                            <span>-</span>
                                            <span className="text-success font-semibold">6.3 pts to Rank #1</span>
                                        </>
                                        ) : (
                                            <span className="text-muted-foreground">Log your workouts to earn a rank and DOTS score in this month&apos;s league.</span>
                                        )}
                                </div>
                            </div>
                        </div>
                        {/* countdown for global league */}
                        <div className="flex items-center gap-4 border-t lg:border-t-0 lg:border-l border-border pt-4 lg:pt-0 lg:pl-6">
                            <div>
                                <span className="text-[11px] text-muted-foreground uppercase tracking-[1px] block font-sans font-semibold">
                                    Season Reset In
                                </span>
                                <span className="font-display text-2xl text-foreground flex items-center gap-1.5 mt-0.5">
                                    <Clock className="w-4 h-4 text-warning"/> {seasonCountdown}
                                </span>
                            </div>

                            <Button variant="secondary" size="sm" asChild className="h-8 text-xs">
                                <Link to="/clash/global-league" className="flex items-center gap-1.5">
                                    <span>View League</span>
                                    <ArrowRight className="w-3.5 h-3.5"/>
                                </Link>
                            </Button>
                        </div>
                    </div>
                </Card>

                {/* f4: 1v1 duels section goes here */}

                <div>
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
                        <div className="flex items-center gap-2">
                            <Users className="w-5 h-5 text-brand"/>
                            <h3 className="font-display text-xl tracking-wide text-foreground">Gym Arenas and Leagues</h3>
                        </div>

                        <div className="flex flex-wrap items-center gap-3">
                            {/* slider todo: accomdate for private arenas in future */}
                            <button type="button" onClick={handleToggleOptIn} 
                            className="flex items-center gap-2.5 bg-surface-2 hover:bg-surface-2/80 border border-border px-3 py-1.5 rounded-xl cursor-pointer transition select-none focus:outline-none focus-visible:ring-2 focus-visible:ring-brand" aria-pressed={isLeaderboardOptedIn} title="Toggle opt into public global and divisional leaderboards">
                                <span className="text-xs font-sans font-semibold text-muted-foreground">
                                    Global Rankings:
                                </span>

                                {/* the slider switch */}
                                <span className={`w-9 h-5 rounded-full transition-colors relative flex items-center p-0.5 ${
                                    isLeaderboardOptedIn ? 'bg-brand' : 'bg-muted-foreground/30'
                                }`}>
                                    <span className={`w-4 h-4 rounded-full bg-white shadow-sm transform transition-transform block ${
                                        isLeaderboardOptedIn ? 'translate-x-4' : 'translate-x-0'}`}/>
                                </span>

                                <span className={`text-xs font-sans font-bold ${
                                    isLeaderboardOptedIn ? 'text-brand' : 'text-muted-foreground'
                                }`}>
                                    {isLeaderboardOptedIn ? 'Opted In' : 'Opted Out'}
                                </span>
                            </button>

                            {/* f3: private arena toggle  */}
                            <div className="w-full sm:w-auto">
                                <ClashTabs activeTab={arenaMode} onChange={(mode) => setArenaMode(mode as 'global' | 'private')}
                                tabs={[
                                    {id: 'global', label: 'Global Leagues'},
                                    { id: 'private', label: 'Private Arenas', count: allArenas.filter((a) => a.type?.toLowerCase() === 'private').length,},
                                ]}/>
                            </div>
                        </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                        {filtArenas.length === 0 ? (
                            <Card className="col-span-full p-8 text-center bg-surface border-border">
                                <Users className="w-8 h-8 text-muted-foreground mx-auto mb-2 opacity-50"/>
                                <p className="text-xs text-muted-foreground font-sans mt-1">Create a new arena or join one with an invite code</p>
                                <p className="text-sm font-semibold text-foreground font-sans">No Private Arenas yet</p>
                            </Card>
                            ) : (
                                filtArenas.map((arena) => (                            
                                <Card key={arena.id} onClick={() => navigate(`/clash/${arena.id}`)}
                                className="bg-surface hover:bg-surface-2/40 border-border p-5 cursor-pointer transition shadow-sm flex flex-col justify-between group">
                                    <CardContent className="p-0 flex flex-col justify-between h-full">
                                        <div>
                                            <div className="flex items-start justify-between gap-2 mb-3">
                                                <h4 className="font-sans text-lg font-bold text-foreground group-hover:text-brand transition">
                                                    {arena.name}
                                                </h4>
                                            </div>
                                            {/* f3: when youve done private arenas, you can add the arena code here */}

                                            <div className="flex items-center gap-1.5">
                                                <span className="text-xs uppercase font-bold text-brand bg-brand-fill border border-brand/30 px-2.5 py-1 rounded-md font-sans">
                                                    Metric: {arena.metricType}
                                                </span>
                                            </div>
                                        </div>

                                        <div className="mt-5 pt-3 border-t border-border flex items-center justify-between text-xs text-muted-foreground font-sans">
                                            <span className="flex items-center gap-1.5 font-medium">
                                                <Users className="w-3.5 h-3.5 text-muted-foreground"/>{arena.memberCount} Athletes
                                            </span>
                                            <span className="text-brand font-bold flex items-center gap-1 group-hover:translate-x-1 transition">
                                                Leaderboard <ArrowRight className="w-3.5 h-3.5"/>
                                            </span>
                                            </div>
                                        </CardContent>
                                    </Card>
                                ))
                        )}
                    </div>
                </div>

                {/* f3: live private arena activity feeds go here */}
                <div>
                    <div className="flex items-center justify-between mb-4">
                        <div className="flex items-center gap-2">
                            <Flame className="w-5 h-5 text-brand"/>
                            <h3 className="font-display text-xl tracking-wide text-foreground">Live Arena Feed</h3>
                            <span className="text-xs text-muted-foreground font-sans">From your private arenas</span>
                        </div>
                    </div>

                    <Card className="bg-surface border-border overflow-hidden shadow-sm divide-y divide-border p-0">
                        {activityList.length === 0 ? (
                            <div className="p-8 text-center text-xs text-muted-foreground font-sans">
                                No live activity in your arenas yet. When a member joins your arena or logs a workout, it will appear here in real-time.
                            </div>
                            ) : (
                            activityList.map((act) => (
                                <div key={act.id} onClick={() => handleOpenAthlete(act.athleteId)}
                                className="p-4 flex items-center justify-between hover:bg-surface-2/50 transition cursor-pointer">
                                    <div className="flex items-center gap-3.5">
                                        <AthleteAvatar initials={act.athleteInitials} name={act.athleteName} avatarUrl={act.athleteAvatarUrl} size="md"/>
                                        <div>
                                            <div className="flex items-center gap-2 font-sans">
                                                <strong className="text-sm font-bold text-foreground hover:text-brand transition">
                                                    {act.athleteName}
                                                </strong>
                                                <span className="text-[11px] text-muted-foreground">{act.timeAgo}</span>
                                                <span className="text-[10px] text-muted-foreground bg-surface-2 px-1.5 py-0.5 rounded border border-border">{act.arenaName}</span>
                                            </div>
                                            <p className="text-xs text-muted-foreground mt-0.5 font-sans">
                                                <span className="font-semibold text-brand">{act.eventText}</span> - {act.details}
                                            </p>
                                        </div>
                                    </div>

                                    <div className="flex items-center gap-3">
                                        <Button variant="secondary" size="sm" onClick={(e) => handleFeedKudos(e, act.id)}
                                            className={`h-8 text-xs flex items-center gap-1.5 ${
                                                kudosGivenMap[act.id] ? 'border border-brand text-brand' : ''
                                            }`}>
                                            <Heart className={`w-3.5 h-3.5 ${kudosGivenMap[act.id] ? 'fill-brand text-brand' : ''}`}/>
                                            <span>{act.kudosCount}</span>
                                        </Button>
                                    </div>
                                </div>
                            ))
                        )}
                    </Card>
                </div>
            </div>

            <ProfileInspectorDrawer athlete={selectedAthlete} isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)}/>

            {/* f3: create/join arena modal */}
            <CreateJoinArenaModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} onSuccess={() => { void fetchMyArenas(); void fetchFeed();}}/>
            {/* f4: create duel modal */}
        </div>
    )
}