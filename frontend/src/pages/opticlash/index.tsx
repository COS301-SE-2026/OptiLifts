import { AthleteAvatar } from "@/components/opticlash/athlete-avatar";
import { ProfileInspectorDrawer } from "@/components/opticlash/profile-inspector-drawer";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { CURRENT_USER_ID, MOCK_ARENAS, MOCK_ATHLETES, type ClashAthlete } from "@/data/clash-mock-data";
import { ArrowRight, Clock, Trophy, UserPlus, Users, Flame, Heart, Plus } from "lucide-react";
import { useState, useEffect, type MouseEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { CreateJoinArenaModal } from "@/components/opticlash/create-join-arena-modal";
import { ClashTabs } from "@/components/opticlash/clash-tabs";
import { MOCK_ACTIVITY_FEED } from "@/data/clash-mock-data";
import { customFetch } from "@/lib/custom-fetch";
import confetti from "canvas-confetti";

export default function ArenaHubPage() {
    const [selectedAthlete, setSelectedAthlete] = useState<ClashAthlete | null>(null);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const [isLeaderboardOptedIn, setIsLeaderboardOptedIn] = useState(false);
    //f3-5 states go here
    const [isModalOpen, setIsModalOpen] = useState(false);
    const [arenaMode, setArenaMode] = useState<'global' | 'private'>('global');
    const [activityList, setActivityList] = useState(MOCK_ACTIVITY_FEED);
    const [kudosGivenMap, setKudosGivenMap] = useState<Record<string, boolean>>({});
    const [myArenas, setMyArenas] = useState<typeof MOCK_ARENAS>([]);

    const fetchMyArenas = async () => {
        try {
            const res = await customFetch('/api/clash/arenas/my');
            if (res.ok) {
                const data = await res.json();
                if (Array.isArray(data) && data.length > 0) {
                    setMyArenas(data);
                }
            } 
        } catch {
            //keep def
        }
    };

    useEffect(() => {
        void fetchMyArenas();
    }, []);

    const allArenas = myArenas.length > 0 ? [
        ...MOCK_ARENAS.filter((a) => a.type?.toLowerCase() !== 'private'),
        ...myArenas
    ] : MOCK_ARENAS;

    const filtArenas = allArenas.filter((a) => arenaMode === 'global' ? (a.type?.toLowerCase() === 'global' || a.type?.toLowerCase() === 'divisional') : a.type?.toLowerCase() === 'private');


    const navigate = useNavigate();
    const currentUser = MOCK_ATHLETES.find((a) => a.id === CURRENT_USER_ID)!; //todo: replace mock data
    const handleToggleOptIn = () => {
        const nextState = !isLeaderboardOptedIn;
        setIsLeaderboardOptedIn(nextState);
        if (nextState){
            toast.success("Your lifts and e1RM are now actively ranked in global and divisonal leaderboards.", 'Opted in to Public Leagues');
        } else {
            toast.success('Your profile and lifts have been hidden from public leagues.', 'Opted out of Public Leagues');
        }
    };
    const handleOpenAthlete = (athleteId: string) => {
        const found = MOCK_ATHLETES.find((a) => a.id === athleteId);//todo: replace mock data
        if (found) {
            setSelectedAthlete(found);
            setIsDrawerOpen(true);
        }
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
                            <button type="button" onClick={() => handleOpenAthlete(currentUser.id)}
                            className="hover:opacity-90 transition cursor-pointer shrink-0 rounded-2xl">
                                <AthleteAvatar initials={currentUser.initials} name={currentUser.name} avatarUrl={currentUser.avatarUrl} isCurrentUser={true} size="xl"/>
                            </button>

                            <div>
                                <div className="flex items-center gap-2 font-sans">
                                    <span className="text-xs font-bold text-brand bg-brand-fill border border-brand/30 px-2 py-0.5 rounded uppercase tracking-wider">
                                        {/* todo: replace alll this mock data */}
                                        GOLD TIER II 
                                    </span>
                                    <span className="text-xs text-muted-foreground">
                                        September Season
                                    </span>
                                </div>
                                <h2 className="font-display text-2xl md:text-3xl tracking-wide text-foreground mt-1">
                                    Cailin Smith
                                </h2>
                                <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground mt-1.5 font-sans">
                                    <span className="flex items-center gap-1 font-bold text-brand">
                                        <Trophy className="w-4 h-4 text-warning"/>Rank #2 on Season League
                                    </span>
                                    <span>-</span>
                                    <span>Score:<strong className="text-foreground">{currentUser.dotsScore} DOTS</strong></span>
                                    <span>-</span>
                                    <span className="text-success font-semibold">6.3 pts to Rank #1</span>
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
                                    <Clock className="w-4 h-4 text-warning"/> 17D 08H
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
                        {filtArenas.map((arena) => (                            
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
                        ))}
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
                        {activityList.map((act) => (
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
                        ))}
                    </Card>
                </div>
            </div>

            <ProfileInspectorDrawer athlete={selectedAthlete} isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)}/>

            {/* f3: create/join arena modal */}
            <CreateJoinArenaModal isOpen={isModalOpen} onClose={() => setIsModalOpen(false)} onSuccess={() => void fetchMyArenas()}/>
            {/* f4: create duel modal */}
        </div>
    )
}