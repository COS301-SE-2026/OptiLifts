import { AthleteAvatar } from "@/components/opticlash/athlete-avatar";
import { ProfileInspectorDrawer } from "@/components/opticlash/profile-inspector-drawer";
import { TierBadge } from "@/components/opticlash/tier-badge";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { CURRENT_USER_ID, getWeightClassBracket, MOCK_ARENAS, MOCK_ATHLETES, WEIGHT_CLASS_BRACKETS, type ClashAthlete } from "@/data/clash-mock-data";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { ArrowLeft, Award, ChevronLeft, ChevronRight, Filter, Medal, Minus, RotateCw, TrendingDown, TrendingUp, Trophy, Users, Copy, Check, LogOut, Share2 } from "lucide-react";
import { useEffect, useState } from "react";
import { Link, useParams, useNavigate } from "react-router-dom";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { ShareArenaModal } from "@/components/opticlash/share-arena-modal";
import { customFetch } from "@/lib/custom-fetch";
import { _adapters } from "chart.js";

const PAGE_SIZE = 10;

export default function ArenaLeaderboardPage() {
    const {arenaId} = useParams<{arenaId: string}>();
    const [liveArena, setLiveArena] = useState<typeof MOCK_ARENAS[0] | null>(null);
    useEffect(() => {
        if (!arenaId){
            return;
        }
        let isMounted = true;

        customFetch(`/api/clash/arenas/${arenaId}`).then(async (res) => {
            if (res.ok) {
                const data = await res.json();
                if (isMounted && data?.arena) {
                    setLiveArena(data.arena);
                }
            }
        }).catch(() => {});
        return () => {
            isMounted = false;
        };
    }, [arenaId]);
    const arena = liveArena || MOCK_ARENAS.find((a) => a.id === arenaId) || MOCK_ARENAS[0];
    const currentUser = MOCK_ATHLETES.find((a) => a.id === CURRENT_USER_ID)!;
    const isDivisional = arena.type === 'divisional' || arena.id === 'weight-class-league';
    const currentUserBracket = getWeightClassBracket(currentUser.bodyweightKg); //todo: getweightclassbracket function

    const navigate = useNavigate();
    const isPrivate = arena.type?.toLowerCase() === 'private';
    const isCreator = (arena as {
        userRole?: string
    }).userRole === 'Owner' || arena.createdById === CURRENT_USER_ID;
    const [isShareModalOpen, setIsShareModalOpen] = useState(false);
    const [isLeaveConfirmOpen, setIsLeaveConfirmOpen] = useState(false);
    const [copied, setCopied] = useState(false);
    const [isLeaving, setIsLeaving] = useState(false);

    const [selectedMetric, setSelectedMetric] = useState<'dots' | 'volume' | 'squat' | 'bench' | 'deadlift'>('dots');
    const [selectedTimeframe, setSelectedTimeframe] = useState<'monthly' | 'all-time'>('monthly');
    const [selectedGender, setSelectedGender] = useState<'male' | 'female'>(currentUser.gender || 'female');
    const [selectedBracketId, setSelectedBracketId] = useState<string>(currentUserBracket.id);
    const [selectedAthlete, setSelectedAthlete] = useState<ClashAthlete | null>(null);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const [isRefreshing, setIsRefreshing] = useState(false);
    const [currentPage, setCurrentPage] = useState(1);

    const activeBracket = WEIGHT_CLASS_BRACKETS.find((b) => b.id === selectedBracketId) || WEIGHT_CLASS_BRACKETS[0];

    const handleShareClick = () => {
        if (isCreator) {
            setIsShareModalOpen(true);
        } else {
            if (!arena.code) {
                return;
            }
            navigator.clipboard.writeText(arena.code);
            setCopied(true);
            toast.success('Arena join code copied to clipboard', arena.code);
            setTimeout(() => setCopied(false), 2000);
        }
    };

    const handleLeaveArena = async () => {
        setIsLeaving(true);
        try {
            const res = await customFetch(`/api/clash/arenas/${arena.id}/leave`, {
                method: 'POST',
            });
            const data = await res.json().catch(() => null);
            if (res.ok){
                setIsLeaveConfirmOpen(false);
                toast.success(`You have left ${arena.name}`, 'Left Arena');
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
    
    const handleRefresh = () => {
        setIsRefreshing(true);
        setTimeout(() => {
            setIsRefreshing(false);
            toast.success('Leaderboard updated', 'Refreshed');
        }, 600); //todo: replace later along with everything else fake lol
    };

    const activeAthletes = MOCK_ATHLETES.filter((a) =>{
        if (selectedTimeframe === 'monthly'){
            const workoutDate = new Date(a.lastWorkoutDate);
            const now = new Date();
            const isThisMonth = workoutDate.getMonth() === now.getMonth() && workoutDate.getFullYear() === now.getFullYear();
            if (!isThisMonth){
                return false;
            }
        }
        if (isDivisional){
            const genderMatch = a.gender === selectedGender;
            const weightMatch = a.bodyweightKg >= activeBracket.minKg && a.bodyweightKg <= activeBracket.maxKg;
            return genderMatch && weightMatch;
        }
        return true;
    });

    //sort athletes based on metric - should i be doing this elsewhere?
    const sortedAthletes = [...activeAthletes].sort((a, b) => {
        switch (selectedMetric) {
            case 'volume':
                return b.weeklyVolumeKg - a.weeklyVolumeKg;
            case 'squat':
                return b.squat1RM - a.squat1RM;
            case 'bench':
                return b.bench1RM - a.bench1RM;
            case 'deadlift':
                return b.deadlift1RM - a.deadlift1RM;
            case 'dots':
            default:
                return b.dotsScore - a.dotsScore;
        }
    });

    const totalPages = Math.ceil(sortedAthletes.length / PAGE_SIZE);
    const startIndex = (currentPage-1) * PAGE_SIZE;
    const paginatedAthletes = sortedAthletes.slice(startIndex, startIndex + PAGE_SIZE);

    const getRankBadgeIcons = (index: number) => {
        if (index === 0) {
            return <Medal className="w-5 h-5 md:w-6 md:h-6 text-warning"/>;
        }
        if (index === 1){
            return <Medal className="w-5 h-5 md:w-6 md:h-6 text-slate-400" />;
        }
        if (index === 2){
            return <Medal className="w-5 h-5 md:w-6 md:h-6 text-amber-700" />;
        }
        return <span className="font-display text-base md:text-lg text-muted-foreground">#{index + 1}</span>;
    };

    const renderMetricValue = (athlete: ClashAthlete) => {
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
    const currentUserRankIndex = sortedAthletes.findIndex((a) => a.id === CURRENT_USER_ID);//todo: obv replace hardcoded

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
                                <span>{arena.name}</span>
                                {isCreator && (
                                    <span className="text-[11px] font-bold uppercase tracking-wider bg-brand-fill text-brand border border-brand/30 px-2.5 py-0.5 rounded-full font-sans">
                                        Squad Creator
                                    </span>
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
                                <span className={`px-1.5 py-0.2 rounded text-[9px] font-extrabold uppercase tracking-wider ${
                                    selectedMetric === 'dots' ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-brand/10 text-brand'
                                }`}>
                                    PRIMARY
                                </span>
                            </Button>

                            <Button variant={selectedMetric === 'volume' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('volume');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap shrink-0">
                                Total Volume
                            </Button>

                            <Button variant={selectedMetric === 'squat' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('squat');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap shrink-0">
                                Squat e1RM
                            </Button>

                            <Button variant={selectedMetric === 'bench' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('bench');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap shrink-0">
                                Bench e1RM
                            </Button>
                            <Button variant={selectedMetric === 'deadlift' ? 'default' : 'secondary'} size="sm" onClick={() =>{
                                setSelectedMetric('deadlift');
                                setCurrentPage(1);
                            }}
                            className="h-8 text-xs whitespace-nowrap shrink-0">
                                Deadlift e1RM
                            </Button>
                        </div>

                        {/* timeframe month vs all time */}
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
                                                {currentUserBracket.id === activeBracket.id && currentUser.gender === selectedGender ? ' (Your Class)' : ''}
                                            </span>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                                            {WEIGHT_CLASS_BRACKETS.map((bracket) => {
                                                const isUserBracket = currentUserBracket.id === bracket.id && currentUser.gender === selectedGender;
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
                                {paginatedAthletes.map((ath, pageIdx) => {
                                    const globalIdx = startIndex + pageIdx;
                                    const isCurrentUser = ath.id === CURRENT_USER_ID;
                                    const isMaster = globalIdx === 0 && ath.tier === 'Overload Master';
                                    return (
                                        <tr key={ath.id} onClick={() => {
                                            setSelectedAthlete(ath);
                                            setIsDrawerOpen(true);
                                        }}
                                        className={`cursor-pointer transition hover:bg-surface-2/60 ${
                                            isCurrentUser ? 'bg-brand-fill/40 border-l-4 border-l-brand' : ''
                                        } ${isMaster ? 'bg-overload-master/5' : ''}`}>
                                            {/* rank icon */}
                                            <td className="py-4 px-4 text-center">
                                                <div className="flex justify-center items-center">
                                                    {getRankBadgeIcons(globalIdx)}
                                                </div>
                                            </td>

                                            {/* athlete deets */}
                                            <td className="py-4 px-4">
                                                <div className="flex items-center gap-3">
                                                    <AthleteAvatar initials={ath.initials} name={ath.name} avatarUrl={ath.avatarUrl} isCurrentUser={isCurrentUser} size="md"/>
                                                    <div>
                                                        <strong className={`font-sans font-bold text-base md:text-lg block ${
                                                            isMaster ? 'text-overload-master' : isCurrentUser ? 'text-brand' : 'text-foreground'
                                                        }`}>{ath.name}</strong>
                                                    </div>
                                                </div>
                                            </td>

                                            {/* tier badge */}
                                            <td className="py-4 px-4 text-center">
                                                <TierBadge tier={ath.tier} size="md"/>
                                            </td>

                                            {/* bodyweight stats */}
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
                                                        <TrendingUp className="w-3.5 h-3.5"/> +{ath.rankTrend}
                                                    </span>
                                                )}
                                                {ath.rankTrend < 0 && (
                                                    <span className="inline-flex items-center gap-0.5 text-xs font-sans font-bold text-brand">
                                                        <TrendingDown className="w-3.5 h-3.5"/> {ath.rankTrend}
                                                    </span>
                                                )}
                                                {ath.rankTrend === 0 && (
                                                    <span className="inline-flex items-center text-muted-foreground">
                                                        <Minus className="w-3.5 h-3.5"/>
                                                    </span>
                                                )}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>

                    {/* table pagination controls */}
                    <div className="p-4 border-t border-border bg-surface-2/50 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs md:text-sm text-muted-foreground font-sans">
                        <div>
                            Showing <strong className="text-foreground">{startIndex + 1}</strong> to{' '}
                            <strong className="text-foreground">{Math.min(startIndex + PAGE_SIZE, sortedAthletes.length)}</strong> of{' '}
                            <strong className="text-foreground">{sortedAthletes.length}</strong> athletes
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
                                {/* todo: change this fake data as well */}
                                {currentUserRankIndex >= 0 ? `Rank #${currentUserRankIndex + 1} — Cailin Smith (You)` : `Cailin Smith (You) — Competing in Women's 66 kg Division`}
                            </strong>
                        </div>
                    </div>

                    <div className="flex items-center gap-4">
                        <div className="text-right">
                            <span className="text-xs font-sans font-bold text-brand">
                                {currentUser.dotsScore} DOTS
                            </span>
                            <span className="text-[10px] text-success block font-sans font-semibold">
                                {currentUserRankIndex === 0 ? 'Leading Division #1' : currentUserRankIndex > 0 ? `Rank #${currentUserRankIndex + 1}` : '64.0 kg Bodyweight'}
                            </span>
                        </div>
                        <Button variant="default" size="sm" onClick={() => {
                            setSelectedAthlete(currentUser);
                            setIsDrawerOpen(true);
                        }}
                        className="h-8 text-xs">
                            Profile
                        </Button>
                    </div>
                </div>
            </aside>

            <ProfileInspectorDrawer athlete={selectedAthlete} isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)}/>

            {/* f3: share arena modal and leave arena confirmation dialog */}
            <ShareArenaModal isOpen={isShareModalOpen} onClose={() => setIsShareModalOpen(false)} arena={arena}/>
            <ConfirmDialog isOpen={isLeaveConfirmOpen} onClose={() => setIsLeaveConfirmOpen(false)} onConfirm={handleLeaveArena} 
            isLoading={isLeaving} title={`Leave ${arena.name}?`} description="You will be removed from this arena's leaderboard. You can rejoin at any time using the arena invite code" confirmText="Leave Arena"
            cancelText="Cancel" variant="danger"/>
        </div>
    )
}