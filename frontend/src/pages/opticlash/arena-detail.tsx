import { AthleteAvatar } from "@/components/opticlash/athlete-avatar";
import { ProfileInspectorDrawer } from "@/components/opticlash/profile-inspector-drawer";
import { TierBadge } from "@/components/opticlash/tier-badge";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { getWeightClassBracket, WEIGHT_CLASS_BRACKETS, type ClashAthlete, type ClashArena } from "@/types/clash";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Clock, ArrowLeft, Award, ChevronLeft, ChevronRight, Filter, Medal, Minus, RotateCw, TrendingDown, TrendingUp, Trophy, Users, Copy, Check, LogOut, Share2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useParams, useNavigate } from "react-router-dom";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { ShareArenaModal } from "@/components/opticlash/share-arena-modal";
import { customFetch } from "@/lib/custom-fetch";
import { useAuth } from "@/context/auth-context";

const PAGE_SIZE = 10;
interface LeaderboardAthleteDto {
    userId: string;
    rank: number;
    displayName: string;
    avatarUrl?: string;
    gender?: string;
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
interface LeaderboardApiResponse {
    entries: LeaderboardAthleteDto[];
    totalCount: number;
    page: number;
    pageSize: number;
    currentUserEntry?: LeaderboardAthleteDto | null;
}

export default function ArenaLeaderboardPage() {//
    const {arenaId} = useParams<{arenaId: string}>();
    const navigate = useNavigate();
    const {user} = useAuth();
    
    const [liveArena, setLiveArena] = useState<ClashArena | null>(null);
    const [standings, setStandings] = useState<LeaderboardAthleteDto[]>([]);
    const [currentUserStanding, setCurrentUserStanding] = useState<LeaderboardAthleteDto | null>(null);
    const [totalAthletes, setTotalAthletes] = useState<number>(0);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [selectedMetric, setSelectedMetric] = useState<'dots' | 'volume' | 'squat' | 'bench' | 'deadlift'>('dots');
    const defaultGender = user?.sex?.toLowerCase() === 'female' ? 'female' : 'male';
    const [selectedGender, setSelectedGender] = useState<'male' | 'female'>(defaultGender);
    const [selectedBracketId, setSelectedBracketId] = useState<string>('u59');
    const [currentPage, setCurrentPage] = useState<number>(1);

    const [selectedTimeframe, setSelectedTimeframe] = useState<'monthly' | 'all-time'>('monthly');
    const [selectedAthlete, setSelectedAthlete] = useState<ClashAthlete | null>(null);
    const [isDrawerOpen, setIsDrawerOpen] = useState<boolean>(false);
    const [isRefreshing, setIsRefreshing] = useState<boolean>(false);

    const [isShareModalOpen, setIsShareModalOpen] = useState(false);
    const [isLeaveConfirmOpen, setIsLeaveConfirmOpen] = useState(false);
    const [copied, setCopied] = useState(false);
    const [isLeaving, setIsLeaving] = useState(false);

    const isDivisional = arenaId === 'weight-class-league';
    const isGlobal = arenaId === 'global-league';
    const isPrivate = !isDivisional && !isGlobal;
    const isCreator = (liveArena as { userRole?: string; createdById?: string})?.userRole === 'Owner' || liveArena?.createdById === user?.id;

    const isConcluded = isPrivate && liveArena ? (liveArena.isActive === false || (liveArena.daysRemaining !== undefined && liveArena.daysRemaining <= 0)): false;
    const getFrontendMetric = (backendMetric?: string): 'dots' | 'volume' | 'squat' | 'bench' | 'deadlift' => {
        const m = backendMetric?.toLowerCase() || '';
        if (m.includes('volume')) return 'volume';
        if (m.includes('squat')) return 'squat';
        if (m.includes('bench')) return 'bench';
        if (m.includes('deadlift')) return 'deadlift';
        return 'dots';
    };
    const primaryMetric = isPrivate ? getFrontendMetric(liveArena?.metricType) : 'dots';
    useEffect(() => {
        if (liveArena?.metricType && isPrivate) {
            setSelectedMetric(getFrontendMetric(liveArena.metricType));
        }
    }, [liveArena?.metricType, isPrivate]);

    const activeBracket = WEIGHT_CLASS_BRACKETS.find((b) => b.id === selectedBracketId) || WEIGHT_CLASS_BRACKETS[0];
    const userWeightBracket = getWeightClassBracket(currentUserStanding?.bodyweightKg ?? 74);

    //mission avoid sonarqube errors
    const getBackendMetricName = (metric: string): string => {
        switch (metric) {
            case 'volume':
                return 'TotalVolume';
            case 'squat':
                return 'SquatE1RM';
            case 'bench':
                return 'BenchE1RM';
            case 'deadlift':
                return 'DeadliftE1RM';
            default:
                return 'DotsOverall';
        }
    };
    const getNormalisedBracketId = (bracketId: string): string => {
        if (bracketId.startsWith('u') || bracketId.endsWith('p')) {
            return bracketId;
        }
        return `u${bracketId}`;
    }

    const fetchLeaderboard = async () => {
        if (!arenaId) return;
        setIsLoading(true);

        const metricParam = getBackendMetricName(selectedMetric);
        try {
            if (isPrivate) {
                const res = await customFetch(`/api/clash/arenas/${arenaId}`);
                if (res.ok) {
                    const data = await res.json();
                    if (data?.arena) {
                        setLiveArena(data.arena);
                    }
                    if (Array.isArray(data?.standings)) {
                        const mapped: LeaderboardAthleteDto[] = data.standings.map((s: Record<string, unknown>) => ({
                            userId: String(s.userId),
                            rank: Number(s.rank),
                            displayName: String(s.displayName),
                            avatarUrl: s.avatarUrl ? String(s.avatarUrl) : undefined,
                            bodyweightKg: Number(s.bodyweightKg ?? 0),
                            tier: String(s.tier ?? 'Unranked'),
                            tierLevel: Number(s.tierLevel ?? 1),
                            dotsScore: Number(s.dotsScore ?? 0),
                            squat1RM: Number(s.squat1RM ?? 0),
                            bench1RM: Number(s.bench1RM ?? 0),
                            deadlift1RM: Number(s.deadlift1RM ?? 0),
                            totalE1RM: Number(s.totalE1RM ?? 0),
                            weeklyVolumeKg: Number(s.weeklyVolumeKg ?? 0),
                            rankTrend: Number(s.trend ?? 0),
                            isCurrentUser: Boolean(s.isCurrentUser)
                        }));
                        setStandings(mapped);
                        setTotalAthletes(mapped.length);
                    }
                    if (data?.userStanding) {
                        setCurrentUserStanding(data.userStanding as LeaderboardAthleteDto);
                    }
                }
            } else if (isDivisional) {
                const normalisedBracket = getNormalisedBracketId(selectedBracketId);
                const res = await customFetch(`/api/clash/leaderboard/divisional?gender=${selectedGender}&bracketId=${normalisedBracket}&metric=${metricParam}&timeframe=${selectedTimeframe}&page=${currentPage}&pageSize=${PAGE_SIZE}`);
                if (res.ok) {
                    const data: LeaderboardApiResponse = await res.json();
                    setStandings(data.entries ?? []);
                    setTotalAthletes(data.totalCount ?? 0);
                    setCurrentUserStanding(data.currentUserEntry ?? null);
                }
            } else {
                const res = await customFetch(`/api/clash/leaderboard/global?metric=${metricParam}&timeframe=${selectedTimeframe}&page=${currentPage}&pageSize=${PAGE_SIZE}`);
                if (res.ok) {
                    const data: LeaderboardApiResponse = await res.json();
                    setStandings(data.entries ?? []);
                    setTotalAthletes(data.totalCount ?? 0);
                    setCurrentUserStanding(data.currentUserEntry ?? null);
                }
            }
        } catch {
            toast.error('Failed to load leaderboard data', 'Connection Error');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch on dependency change
        void fetchLeaderboard();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [arenaId, selectedMetric, selectedGender, selectedBracketId, currentPage, selectedTimeframe]);

    //sonqorqube nested ternary issues
    const arenaName = liveArena?.name || (isDivisional ? 'Divisional Weight-Class League': 'Global Season League');
    const totalPages = Math.max(1, Math.ceil(totalAthletes / PAGE_SIZE));
    const startIndex = (currentPage-1) * PAGE_SIZE;

    const handleOpenAthleteDrawer = (ath: LeaderboardAthleteDto) => {
        const initials = ath.displayName.slice(0,2).toUpperCase();
        const fallbackAthlete: ClashAthlete = {
            id: ath.userId,
            name: ath.displayName,
            initials: initials || 'AT',
            avatarUrl: ath.avatarUrl,
            code: 'OPTICLASH',
            gender: (ath.gender as 'male' | 'female') || selectedGender,
            bodyweightKg: ath.bodyweightKg,
            squat1RM: ath.squat1RM,
            bench1RM: ath.bench1RM,
            deadlift1RM: ath.deadlift1RM,
            totalE1RM: ath.totalE1RM,
            dotsScore: ath.dotsScore,
            tier: (ath.tier as ClashAthlete['tier']) || 'Unranked',
            tierLevel: ath.tierLevel,
            rankTrend: ath.rankTrend,
            weeklyVolumeKg: ath.weeklyVolumeKg,
            lastWorkoutDate: new Date().toISOString(),
            muscleBalance30d: { Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0 },
            trophies: [],
            recentWorkouts: []
        };
        setSelectedAthlete(fallbackAthlete);
        setIsDrawerOpen(true);
    };

    const handleShareClick = () => {
        if (isCreator) {
            setIsShareModalOpen(true);
        } else if (liveArena?.code) {
            navigator.clipboard.writeText(liveArena.code);
            setCopied(true);
            toast.success('Arena join code copied to clipboard', liveArena.code);
            setTimeout(() => setCopied(false), 2000);
        }
    };

    const handleLeaveArena = async () => {
        if (!liveArena) return;
        setIsLeaving(true);
        try {
            const res = await customFetch(`/api/clash/arenas/${liveArena.id}/leave`, {
                method: 'POST',
            });
            const data = await res.json().catch(() => null);
            if (res.ok){
                setIsLeaveConfirmOpen(false);
                toast.success(`You have left ${liveArena.name}`, 'Left Arena');
                navigate('/clash');
            } else {
                toast.error(data?.message ?? 'Failed to leave arena');
                setIsLeaveConfirmOpen(false);
            }
        } catch {
            toast.error('Network error leaving arena');
            setIsLeaveConfirmOpen(false);
        } finally {
            setIsLeaving(false);
        }
    };
    
    const handleRefresh = async () => {
        setIsRefreshing(true);
        await fetchLeaderboard();
        setIsRefreshing(false);
        toast.success('Leaderboard updated', 'Refreshed');
    };

    const getRankBadgeIcons = (index: number) => {
        if (index === 1) {
            return <Medal className="w-5 h-5 md:w-6 md:h-6 text-warning"/>;
        }
        if (index === 2){
            return <Medal className="w-5 h-5 md:w-6 md:h-6 text-slate-400" />;
        }
        if (index === 3){
            return <Medal className="w-5 h-5 md:w-6 md:h-6 text-amber-700" />;
        }
        return <span className="font-display text-base md:text-lg text-muted-foreground">#{index}</span>;
    };

     const renderMetricValue = (athlete: LeaderboardAthleteDto) => {
        switch (selectedMetric) {
            case 'volume':
                return <span className="font-bold text-brand font-sans text-sm md:text-base">{athlete.weeklyVolumeKg.toLocaleString()} kg</span>;
            case 'squat':
                return <span className="font-bold text-foreground font-sans text-sm md:text-base">{athlete.squat1RM} kg</span>;
            case 'bench':
                return <span className="font-bold text-foreground font-sans text-sm md:text-base">{athlete.bench1RM} kg</span>;
            case 'deadlift':
                return <span className="font-bold text-foreground font-sans text-sm md:text-base">{athlete.deadlift1RM} kg</span>;
            case 'dots':
            default:
                return (
                    <div className="flex flex-col items-center justify-center">
                        <span className="font-bold text-brand text-base md:text-lg font-sans">{athlete.dotsScore}</span>
                        <span className="text-[10px] text-muted-foreground block font-sans font-semibold">DOTS</span>
                    </div>
                );
        }
    };

    const getAthleteNameClass = (isMaster: boolean, isCurrent: boolean) => {
        if (isMaster){
            return 'text-overload-master';
        }
        if (isCurrent) {
            return 'text-brand';
        }
        return 'text-foreground';
    };
    const renderUserStandingSTatus = () => {
        if (!currentUserStanding) {
            return 'Not ranked in this divisin yet';
        }
        if (currentUserStanding.rank === 1) {
            return 'Leading Division #1';
        }
        return `Rank #${currentUserStanding.rank}`;
    }

    return (
        <div className="min-h-screen bg-background text-foreground pb-24">
            <div className="max-w-6xl mx-auto px-4 pt-8 pb-4">
                <Link to="/clash" className="inline-flex items-center gap-2 text-xs font-bold text-muted-foreground hover:text-brand uppercase tracking-[1px] mb-4 transition font-sans">
                    <ArrowLeft className="w-4 h-4"/>Back to Arenas
                </Link>

                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                    <div>
                        <div className="flex items-center gap-3 flex-wrap">
                            <h1 className="font-display text-3xl md:text-4xl tracking-wide text-foreground flex items-center gap-2.5">
                                <Trophy className="w-7 h-7 md:w-8 md:h-8 text-warning shrink-0"/>
                                <span>{arenaName}</span>
                                {isCreator && (
                                    <span className="text-[11px] font-bold uppercase tracking-wider bg-brand-fill text-brand border border-brand/30 px-2.5 py-0.5 rounded-full font-sans">
                                        Squad Creator
                                    </span>
                                )}
                                {isPrivate && liveArena && (
                                    isConcluded ? (
                                        <span className="text-[11px] font-bold uppercase tracking-wider bg-destructive/10 text-destructive border border-destructive/30 px-2.5 py-0.5 rounded-full font-sans">
                                            Season Concluded
                                        </span>
                                    ) : (
                                        <span className="text-[11px] font-bold uppercase tracking-wider bg-warning/10 text-warning border border-warning/30 px-2.5 py-0.5 rounded-full font-sans flex items-center gap-1.5">
                                            <Clock className="w-3.5 h-3.5"/>
                                            {liveArena.daysRemaining ?? liveArena.durationDays} Days Left
                                        </span>
                                    )
                                )}
                            </h1>
                        </div>
                    </div>
                    {/* action btns */}
                    <div className="flex items-center gap-2.5 self-start md:self-auto flex-wrap">
                        <Button variant="outline" size="sm" onClick={handleRefresh} disabled={isRefreshing}
                        className="h-8 text-xs flex items-center gap-1.5 border-border">
                            <RotateCw className={`w-3.5 h-3.5 ${
                                isRefreshing ? 'animate-spin' : ''}`}/>
                            <span>{isRefreshing ? 'Refreshing...' : 'Refresh'}</span>
                        </Button>

                        {/* f3: share arena + leave arena btns */}
                        {isPrivate && (
                            <>
                            <Button variant={isCreator ? 'default' : 'secondary'} size="sm" onClick={handleShareClick} 
                            className="h-8 text-xs flex items-center gap-2">
                                {isCreator ? (
                                    <>
                                        <Share2 className="w-3.5 h-3.5"/>
                                        <span>Share Arena and Invite</span>
                                    </>
                                    ) : (
                                        <>
                                        {copied ? <Check className="w-3.5 h-3.5 text-success"/> : <Copy className="w-3.5 h-3.5"/>}
                                        <span>{copied ? 'Code Copied' : 'Copy Code'}</span></>
                                    )}    
                            </Button>
                            <Button variant="outline" size="sm" onClick={() => setIsLeaveConfirmOpen(true)}
                            className="h-8 text-xs flex items-center gap-1.5 border-border text-muted-foreground hover:text-destructive hover:border-destructive/40 transition-colors">
                                <LogOut className="w-3.5 h-3.5"/>
                                <span>Leave Arena</span>    
                            </Button></>
                        )}
                    </div>
                </div>
            </div>

            <div className="max-w-6xl mx-auto px-4 py-4 space-y-6">
            {/* filters */}
                <Card className="bg-surface border-border p-4 shadow-sm">
                    <CardContent className="p-0 flex flex-col md:flex-row md:items-center justify-between gap-4">
                        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 md:pb-0">
                            <span className="text-xs font-bold uppercase tracking-[1px] text-muted-foreground mr-2 flex items-center gap-1 shrink-0 font-sans">
                                <Filter className="w-3.5 h-3.5"/> Metric:
                            </span>
                            {/* primary metric */}
                            <Button variant={selectedMetric === 'dots' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('dots');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap flex items-center gap-1.5 shrink-0">
                                <span>Overall DOTS</span>
                                {primaryMetric === 'dots' && (
                                    <span className={`px-1.5 py-0.2 rounded text-[9px] font-extrabold uppercase tracking-wider ${selectedMetric === 'dots' ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-brand/10 text-brand'
                                        }`}>
                                        PRIMARY
                                    </span>
                                )}
                            </Button>

                            <Button variant={selectedMetric === 'volume' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('volume');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap flex items-center gap-1.5 shrink-0">
                                <span>Total Volume</span>
                                {primaryMetric === 'volume' && (
                                    <span className={`px-1.5 py-0.2 rounded text-[9px] font-extrabold uppercase tracking-wider ${selectedMetric === 'volume' ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-brand/10 text-brand'
                                        }`}>
                                        PRIMARY
                                    </span>
                                )}
                            </Button>

                            <Button variant={selectedMetric === 'squat' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('squat');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap flex items-center gap-1.5 shrink-0">
                                <span>Squat e1RM</span>
                                {primaryMetric === 'squat' && (
                                    <span className={`px-1.5 py-0.2 rounded text-[9px] font-extrabold uppercase tracking-wider ${selectedMetric === 'squat' ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-brand/10 text-brand'
                                        }`}>
                                        PRIMARY
                                    </span>
                                )}
                            </Button>

                            <Button variant={selectedMetric === 'bench' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('bench');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap flex items-center gap-1.5 shrink-0">
                                <span>Bench e1RM</span>
                                {primaryMetric === 'bench' && (
                                    <span className={`px-1.5 py-0.2 rounded text-[9px] font-extrabold uppercase tracking-wider ${selectedMetric === 'bench' ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-brand/10 text-brand'
                                        }`}>
                                        PRIMARY
                                    </span>
                                )}
                            </Button>
                            <Button variant={selectedMetric === 'deadlift' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('deadlift');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap flex items-center gap-1.5 shrink-0">
                                <span>Deadlift e1RM</span>
                                {primaryMetric === 'deadlift' && (
                                    <span className={`px-1.5 py-0.2 rounded text-[9px] font-extrabold uppercase tracking-wider ${selectedMetric === 'deadlift' ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-brand/10 text-brand'
                                        }`}>
                                        PRIMARY
                                    </span>
                                )}
                            </Button>
                        </div>

                        {/* timeframe month vs all time */}
                        {!isPrivate && (
                        <div className="flex bg-surface-2 border border-border rounded-lg p-1 self-start md:self-auto shrink-0 overflow-x-auto">
                            <button type="button" onClick={() => {
                                setSelectedTimeframe('monthly');
                                setCurrentPage(1);
                            }}
                            className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans whitespace-nowrap shrink-0 ${
                                selectedTimeframe === 'monthly' ? 'bg-surface text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
                            }`}>
                                Monthly Season
                            </button>
                            <button type="button" onClick={() => {
                                setSelectedTimeframe('all-time');
                                setCurrentPage(1);
                            }}
                            className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans whitespace-nowrap shrink-0 ${
                                selectedTimeframe === 'all-time' ? 'bg-surface text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
                            }`}>
                                All Time
                            </button>
                        </div>
                        )}
                    </CardContent>
                </Card>

                {/* for divisional ie weight class league */}
                {isDivisional && (
                    <Card className="bg-surface border-border p-4 shadow-sm">
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                            <div className="flex items-center gap-2.5">
                                <Users className="w-4 h-4 text-brand"/>
                                <div>
                                    <h3 className="text-sm font-bold font-sans uppercase tracking-[1px] text-foreground">
                                        Divisional Weight Class
                                    </h3>
                                    <span className="text-xs text-muted-foreground font-sans block mt-0.5">
                                        {selectedGender === 'male' ? "Men's" : "Women's"} {activeBracket.label} ({activeBracket.minKg > 0 ? `${activeBracket.minKg} - ` : 'Up to '}{activeBracket.maxKg < 999 ? `${activeBracket.maxKg} kg` : '+ kg'})
                                    </span>
                                </div>
                            </div>

                            <div className="flex flex-wrap items-center gap-3">
                                {/* gender toggle */}
                                <div className="flex bg-surface-2 border border-border rounded-lg p-1 self-start sm:self-auto shrink-0">
                                    <button type="button" onClick={() => {
                                        setSelectedGender('male');
                                        setCurrentPage(1);
                                    }}
                                    className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans ${
                                        selectedGender === 'male' ? 'bg-surface text-brand shadow-sm font-extrabold' : 'text-muted-foreground hover:text-foreground'
                                    }`}>
                                        Men's
                                    </button>
                                    <button type="button" onClick={() => {
                                        setSelectedGender('female');
                                        setCurrentPage(1);
                                    }}
                                    className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans ${
                                        selectedGender === 'female' ? 'bg-surface text-brand shadow-sm font-extrabold' : 'text-muted-foreground hover:text-foreground'
                                    }`}>
                                        Women's
                                    </button>
                                </div>

                                {/* weight class */}
                                <div className="w-full sm:w-auto">
                                    <DropdownMenu>
                                        <DropdownMenuTrigger variant="filter" className="w-full sm:w-[220px] bg-surface-2" aria-label="Select Weight Class Bracket">
                                            <span className="min-w-0 truncate font-sans font-semibold">
                                                {activeBracket.label}
                                                {currentUserStanding && userWeightBracket.id === activeBracket.id ? ' (Your Class)' : ''}
                                            </span>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                                            {WEIGHT_CLASS_BRACKETS.map((bracket) => {
                                                const isUserBracket = currentUserStanding && userWeightBracket.id === bracket.id;
                                                return (
                                                    <DropdownMenuItem key={bracket.id} onSelect={() => {
                                                        setSelectedBracketId(bracket.id);
                                                        setCurrentPage(1);
                                                    }}
                                                    className="cursor-pointer flex items-center justify-between">
                                                        <span>{bracket.label}</span>
                                                        {isUserBracket && (
                                                            <span className="text-[10px] font-bold text-brand bg-brand-fill border border-brand/30 px-1.5 py-0.2 rounded">
                                                                YOU
                                                            </span>
                                                        )}
                                                    </DropdownMenuItem>
                                                );
                                            })}
                                            {/* ... */}
                                        </DropdownMenuContent>
                                    </DropdownMenu>
                                </div>
                            </div>
                        </div>
                    </Card>
                )}

                {/* leaderboard table card */}
                <Card className="bg-surface border-border shadow-sm overflow-hidden p-0">
                    <div className="overflow-x-auto">
                        <table className="w-full text-left border-collapse">
                            <thead>
                                <tr className="border-b border-border bg-surface-2 text-xs md:text-sm font-sans font-bold text-muted-foreground uppercase tracking-[1px]">
                                    <th className="py-3.5 px-4 w-16 text-center">Rank</th>
                                    <th className="py-3.5 px-4">Athlete</th>
                                    <th className="py-3.5 px-4 text-center">Tier</th>
                                    <th className="py-3.5 px-4 text-center">Bodyweight</th>
                                    <th className="py-3.5 px-4 text-center">e1RM Total</th>
                                    <th className="py-3.5 px-4 text-center">
                                        <span className="text-foreground">
                                            {selectedMetric === 'dots' ? 'DOTS Score' : 'Score'}
                                        </span>
                                    </th>
                                    <th className="py-3.5 px-4 text-center w-20">Trend</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-border text-sm md:text-base">
                                {isLoading ? (
                                    <tr>
                                        <td colSpan={7} className="py-8 text-center text-xs text-muted-foreground font-sans">
                                            Loading leaderboad standings...
                                        </td>
                                    </tr>
                                    ) : standings.length === 0 ? (
                                        <tr>
                                            <td colSpan={7} className="py-8 text-center text-xs text-muted-foreground font-sans">
                                                No athletes ranked yet in this division.
                                            </td>
                                        </tr>
                                        ) : (
                                            standings.map((ath) => {
                                                const isCurrentUser = ath.isCurrentUser;
                                                const isMaster = ath.rank === 1 && ath.tier === 'Overload Master';
                                                const initials = ath.displayName.slice(0,2).toUpperCase() || 'AT';

                                                return (
                                                    <tr key={ath.userId} onClick={() => handleOpenAthleteDrawer(ath)}
                                                    className={`cursor-pointer transition hover:bg-surface-2/60 ${isCurrentUser ? 'bg-brand-fill/40 border-l-4 border-l-brand' : '' }
                                                    ${isMaster ? 'bg-overload-master/5' : ''}`}>
                                                        <td className="py-4 px-4 text-center">
                                                            <div className="flex justify-center items-center">
                                                                {getRankBadgeIcons(ath.rank)}
                                                            </div>
                                                        </td>

                                                        <td className="py-4 px-4">
                                                            <div className="flex items-center gap-3">
                                                                <AthleteAvatar initials={initials} name={ath.displayName} avatarUrl={ath.avatarUrl} isCurrentUser={isCurrentUser} size="md"/>
                                                                <div>
                                                                    <strong className={`font-sans font-bold text-base md:text-lg block ${getAthleteNameClass(isMaster, isCurrentUser)}`}>
                                                                        {ath.displayName}
                                                                    </strong>
                                                                </div>
                                                            </div>
                                                        </td>

                                                        <td className="py-4 px-4 text-center">
                                                            <TierBadge tier={ath.tier} size="md"/>
                                                        </td>

                                                        <td className="py-4 px-4 text-center font-sans font-semibold text-foreground text-sm md:text-base">
                                                            {ath.bodyweightKg} kg
                                                        </td>
                                                        <td className="py-4 px-4 text-center font-sans font-semibold text-foreground text-sm md:text-base">
                                                            {ath.totalE1RM} kg
                                                        </td>

                                                        <td className="py-4 px-4 text-center">
                                                            {renderMetricValue(ath)}
                                                        </td>

                                                        {/* trend arrow */}
                                                        <td className="py-4 px-4 text-center">
                                                            {ath.rankTrend > 0 && (
                                                                <span className="inline-flex items-center gap-0.5 text-xs font-sans font-bold text-success">
                                                                    <TrendingUp className="w-3.5 h-3.5" /> +{ath.rankTrend}
                                                                </span>
                                                            )}
                                                            {ath.rankTrend < 0 && (
                                                                <span className="inline-flex items-center gap-0.5 text-xs font-sans font-bold text-brand">
                                                                    <TrendingDown className="w-3.5 h-3.5" /> {ath.rankTrend}
                                                                </span>
                                                            )}
                                                            {ath.rankTrend === 0 && (
                                                                <span className="inline-flex items-center text-muted-foreground">
                                                                    <Minus className="w-3.5 h-3.5" />
                                                                </span>
                                                            )}
                                                        </td>
                                                    </tr>
                                                );
                                            })
                                        )}
                            </tbody>
                        </table>
                    </div>

                    {/* table pagination controls */}
                    <div className="p-4 border-t border-border bg-surface-2/50 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs md:text-sm text-muted-foreground font-sans">
                        <div>
                            Showing <strong className="text-foreground">{startIndex + 1}</strong> to{' '}
                            <strong className="text-foreground">{Math.min(startIndex + PAGE_SIZE, totalAthletes)}</strong> of{' '}
                            <strong className="text-foreground">{totalAthletes}</strong> athletes
                        </div>

                        <div className="flex items-center gap-2 self-end sm:self-auto">
                            <Button variant="outline" size="sm" onClick={() => setCurrentPage((p) => Math.max(p-1,1))}
                                disabled={currentPage === 1} className="h-7 px-2.5 text-xs border-border">
                                <ChevronLeft className="w-3.5 h-3.5 mr-1"/>
                                <span>Prev</span>
                            </Button>
                            <span className="px-2 font-semibold text-foreground">
                                Page {currentPage} of {totalPages || 1}
                            </span>

                            <Button variant="outline" size="sm" onClick={() => setCurrentPage((p) => Math.min(p + 1, totalPages))}
                                disabled={currentPage === totalPages || totalPages === 0}
                                className="h-7 px-2.5 text-xs border-border">
                                <span>Next</span>
                                <ChevronRight className="w-3.5 h-3.5 ml-1"/>
                            </Button>
                        </div>
                    </div>
                </Card>
            </div>
            {/* user position bar */}
            <aside aria-label="Your ranking snapshot"
            className="fixed bottom-0 left-0 right-0 z-30 bg-surface/95 border-t border-brand/40 px-4 py-3 shadow-2xl backdrop-blur-md">
                <div className="max-w-6xl mx-auto flex items-center justify-between">
                    <div className="flex items-center gap-3">
                        <Award className="w-5 h-5 text-brand"/>
                        <div>
                            <span className="text-[11px] text-muted-foreground block font-sans font-bold uppercase tracking-[1px]">
                                YOUR STANDING
                            </span>
                            <strong className="text-sm font-sans font-bold text-foreground">
                                {currentUserStanding ? `Rank #${currentUserStanding.rank} — ${currentUserStanding.displayName} (You)` : `${user?.name ?? 'You'} — Not ranked in this category yet`}
                            </strong>
                        </div>
                    </div>

                    <div className="flex items-center gap-4">
                        <div className="text-right">
                            <span className="text-xs font-sans font-bold text-brand">
                                {currentUserStanding?.dotsScore ?? 0} DOTS
                            </span>
                            <span className="text-[10px] text-success block font-sans font-semibold">
                                {renderUserStandingSTatus()}
                            </span>
                        </div>
                        {currentUserStanding && (
                        <Button variant="default" size="sm" onClick={() => handleOpenAthleteDrawer(currentUserStanding)}
                        className="h-8 text-xs">
                            Profile
                        </Button>

                        )}
                    </div>
                </div>
            </aside>

            <ProfileInspectorDrawer athlete={selectedAthlete} isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)}/>

            {liveArena && (
                <>
                {/* f3: share arena modal and leave arena confirmation dialog */}
                <ShareArenaModal isOpen={isShareModalOpen} onClose={() => setIsShareModalOpen(false)} arena={liveArena}/>
                <ConfirmDialog isOpen={isLeaveConfirmOpen} onClose={() => setIsLeaveConfirmOpen(false)} onConfirm={handleLeaveArena} 
                isLoading={isLeaving} title={`Leave ${liveArena.name}?`} description="You will be removed from this arena's leaderboard. You can rejoin at any time using the arena invite code" confirmText="Leave Arena"
                cancelText="Cancel" variant="danger"/>
                
                </>
            )}

        </div>
    )
}