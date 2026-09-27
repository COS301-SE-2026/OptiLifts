import { AthleteAvatar } from "@/components/opticlash/athlete-avatar";
import { ProfileInspectorDrawer } from "@/components/opticlash/profile-inspector-drawer";
import { TierBadge } from "@/components/opticlash/tier-badge";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { getWeightClassBracket, WEIGHT_CLASS_BRACKETS, type ClashAthlete, type ClashArena } from "@/types/clash";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Clock, ArrowLeft, Award, ChevronLeft, ChevronRight, Filter, Medal, Minus, RotateCw, TrendingDown, TrendingUp, Trophy, Users, Copy, Check, LogOut, Share2 } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";
import { Link, useParams, useNavigate } from "react-router-dom";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { ShareArenaModal } from "@/components/opticlash/share-arena-modal";
import { customFetch } from "@/lib/custom-fetch";
import { useAuth } from "@/context/auth-context";
import { CreateDuelModal } from "@/components/opticlash/create-duel-modal";

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

type ArenaMetric = 'dots' | 'volume' | 'squat' | 'bench' | 'deadlift';
function getFrontendMetric(backendMetric?: string): ArenaMetric {
    const m = backendMetric?.toLowerCase() || '';
    if (m.includes('volume')) return 'volume';
    if (m.includes('squat')) return 'squat';
    if (m.includes('bench')) return 'bench';
    if (m.includes('deadlift')) return 'deadlift';
    return 'dots';
}

function getBackendMetricName(metric: string): string {
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
}

function getNormalisedBracketId(bracketId: string): string {
    if (bracketId.startsWith('u') || bracketId.endsWith('p')) {
        return bracketId;
    }
    return `u${bracketId}`;
}

function mapPrivateStandings(standings: unknown[]): LeaderboardAthleteDto[] {
    return (standings as Record<string, unknown>[]).map((s) => ({
        userId: String(s.userId),
        rank: Number(s.rank),
        displayName: String(s.displayName),
        avatarUrl: typeof s.avatarUrl === 'string' ? s.avatarUrl : undefined,
        bodyweightKg: Number(s.bodyweightKg ?? 0),
        tier: typeof s.tier === 'string' ? s.tier : 'Unranked',
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
}

function getRankBadgeIcons(index: number) {
    if (index === 1) {
        return <Medal className="w-5 h-5 md:w-6 md:h-6 text-warning"/>;
    }
    if (index === 2){
        return <Medal className="w-5 h-5 md:w-6 md:h-6 text-slate-400"/>;
    }
    if (index === 3){
        return <Medal className="w-5 h-5 md:w-6 md:h-6 text-amber-700"/>;
    }
    return <span className="font-display text-base md:text-lg text-muted-foreground">#{index}</span>;
}

function getAthleteNameClass(isMaster: boolean, isCurrent: boolean) {
    if (isMaster){
        return 'text-overload-master';
    }
    if (isCurrent) {
        return 'text-brand';
    }
    return 'text-foreground';
}

function getMetricScore(athlete: LeaderboardAthleteDto, metric: ArenaMetric): number {
    switch (metric) {
        case 'volume':
            return athlete.weeklyVolumeKg;
        case 'squat':
            return athlete.squat1RM;
        case 'bench':
            return athlete.bench1RM;
        case 'deadlift':
            return athlete.deadlift1RM;
        case 'dots':
        default:
            return athlete.dotsScore;
    }
}
function renderStandingScore(standing: LeaderboardAthleteDto | null, metric: ArenaMetric): string {
    if (!standing) return '0 DOTS';
    switch (metric) {
        case 'volume':
            return `${standing.weeklyVolumeKg.toLocaleString()} kg`;
        case 'squat':
            return `${standing.squat1RM} kg`;
        case 'bench':
            return `${standing.bench1RM} kg`;
        case 'deadlift':
            return `${standing.deadlift1RM} kg`;
        case 'dots':
        default:
            return `${standing.dotsScore} DOTS`;
    }
}

function getUserStandingStatus(currentUserStanding: LeaderboardAthleteDto | null): string {
    if (!currentUserStanding) {
        return 'Not ranked in this divisin yet';
    }
    if (currentUserStanding.rank === 1) {
        return 'Leading Division #1';
    }
    return `Rank #${currentUserStanding.rank}`;
}

function renderMetricValue(athlete: LeaderboardAthleteDto, selectedMetric: ArenaMetric) {
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
}

interface LeaderboardTableBodyProps {
    isLoading: boolean;
    standings: LeaderboardAthleteDto[];
    selectedMetric: ArenaMetric;
    onSelectAthlete: (athlete: LeaderboardAthleteDto) => void;
}

function LeaderboardTableBody({ isLoading, standings, selectedMetric, onSelectAthlete }: Readonly<LeaderboardTableBodyProps>) {
    if (isLoading) {
        return (
            <tr>
                <td colSpan={7} className="py-8 text-center text-xs text-muted-foreground font-sans">
                    Loading leaderboad standings...
                </td>
            </tr>
        );
    }
    if (standings.length === 0) {
        return (
            <tr>
                <td colSpan={7} className="py-8 text-center text-xs text-muted-foreground font-sans">
                    No athletes ranked yet in this division.
                </td>
            </tr>
        );
    }
    return standings.map((ath) => {
        const isCurrentUser = ath.isCurrentUser;
        const isMaster = ath.rank === 1 && ath.tier === 'Overload Master';
        const initials = ath.displayName.slice(0, 2).toUpperCase() || 'AT';
        return (
            <tr key={ath.userId} onClick={() => onSelectAthlete(ath)}
                className={`cursor-pointer transition hover:bg-surface-2/60 ${isCurrentUser ? 'bg-brand-fill/40 border-l-4 border-l-brand' : ''}
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
                    {renderMetricValue(ath, selectedMetric)}
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
    });
}
//deadlign w/ sonarqube complexity
interface ArenaHeaderProps {
    arenaName: string;
    isCreator: boolean;
    isPrivate: boolean;
    liveArena: ClashArena | null;
    isConcluded: boolean;
    isRefreshing: boolean;
    copied: boolean;
    onRefresh: () => void;
    onShareClick: () => void;
    onOpenLeaveConfirm: () => void;
}
function ArenaHeader({
    arenaName,
    isCreator,
    isPrivate,
    liveArena,
    isConcluded,
    isRefreshing,
    copied,
    onRefresh,
    onShareClick,
    onOpenLeaveConfirm,
}: Readonly<ArenaHeaderProps>) {
    return (
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
                <Button variant="outline" size="sm" onClick={onRefresh} disabled={isRefreshing}
                    className="h-8 text-xs flex items-center gap-1.5 border-border">
                    <RotateCw className={`w-3.5 h-3.5 ${isRefreshing ? 'animate-spin' : ''}`}/>
                    <span>{isRefreshing ? 'Refreshing...' : 'Refresh'}</span>
                </Button>

                {/* f3: share arena + leave arena btns */}
                {isPrivate && (
                    <>
                        <Button variant={isCreator ? 'default' : 'secondary'} size="sm" onClick={onShareClick}
                            className="h-8 text-xs flex items-center gap-2">
                            {isCreator ? (
                                <>
                                    <Share2 className="w-3.5 h-3.5"/>
                                    <span>Share Arena and Invite</span>
                                </>
                            ) : (
                                <>
                                    {copied ? <Check className="w-3.5 h-3.5 text-success"/> : <Copy className="w-3.5 h-3.5"/>}
                                    <span>{copied ? 'Code Copied' : 'Copy Code'}</span>
                                </>
                            )}
                        </Button>
                        <Button variant="outline" size="sm" onClick={onOpenLeaveConfirm}
                            className="h-8 text-xs flex items-center gap-1.5 border-border text-muted-foreground hover:text-destructive hover:border-destructive/40 transition-colors">
                            <LogOut className="w-3.5 h-3.5"/>
                            <span>Leave Arena</span>
                        </Button>
                    </>
                )}
            </div>
        </div>
    );
}

const METRIC_BUTTONS: { id: ArenaMetric; label: string }[] = [
    { id: 'dots', label: 'Overall DOTS' },
    { id: 'volume', label: 'Total Volume' },
    { id: 'squat', label: 'Squat e1RM' },
    { id: 'bench', label: 'Bench e1RM' },
    { id: 'deadlift', label: 'Deadlift e1RM' },
];
interface ArenaFilterBarProps {
    selectedMetric: ArenaMetric;
    primaryMetric: ArenaMetric;
    isPrivate: boolean;
    selectedTimeframe: 'monthly' | 'all-time';
    onSelectMetric: (metric: ArenaMetric) => void;
    onSelectTimeframe: (timeframe: 'monthly' | 'all-time') => void;
}

function ArenaFilterBar({
    selectedMetric, primaryMetric, isPrivate, selectedTimeframe, onSelectMetric,onSelectTimeframe,
}: Readonly<ArenaFilterBarProps>) {
    return (
        <Card className="bg-surface border-border p-4 shadow-sm">
            <CardContent className="p-0 flex flex-col md:flex-row md:items-center justify-between gap-4">
                <div className="flex items-center gap-1.5 overflow-x-auto pb-1 md:pb-0">
                    <span className="text-xs font-bold uppercase tracking-[1px] text-muted-foreground mr-2 flex items-center gap-1 shrink-0 font-sans">
                        <Filter className="w-3.5 h-3.5"/> Metric:
                    </span>
                    {METRIC_BUTTONS.map(({ id, label }) => {
                        const isSelected = selectedMetric === id;
                        const isPrimary = primaryMetric === id;
                        return (
                            <Button key={id} variant={isSelected ? 'default' : 'secondary'} size="sm"
                                onClick={() => onSelectMetric(id)} className="h-8 text-xs whitespace-nowrap flex items-center gap-1.5 shrink-0">
                                <span>{label}</span>
                                {isPrimary && (
                                    <span className={`px-1.5 py-0.2 rounded text-[9px] font-extrabold uppercase tracking-wider ${isSelected ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-brand/10 text-brand'}`}>
                                        PRIMARY
                                    </span>
                                )}
                            </Button>
                        );
                    })}
                </div>

                {/* timeframe month vs all time */}
                {!isPrivate && (
                    <div className="flex bg-surface-2 border border-border rounded-lg p-1 self-start md:self-auto shrink-0 overflow-x-auto">
                        <button type="button" onClick={() => onSelectTimeframe('monthly')}
                        className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans whitespace-nowrap shrink-0 ${
                            selectedTimeframe === 'monthly' ? 'bg-surface text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
                        }`}>
                            Monthly Season
                        </button>
                        <button type="button" onClick={() => onSelectTimeframe('all-time')}
                        className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans whitespace-nowrap shrink-0 ${
                            selectedTimeframe === 'all-time' ? 'bg-surface text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'
                        }`}>
                            All Time
                        </button>
                    </div>
                )}
            </CardContent>
        </Card>
    );
}

interface DivisionalClassSelectorProps {
    selectedGender: 'male' | 'female';
    activeBracket: (typeof WEIGHT_CLASS_BRACKETS)[number];
    userWeightBracket: (typeof WEIGHT_CLASS_BRACKETS)[number];
    currentUserStanding: LeaderboardAthleteDto | null;
    onSelectGender: (gender: 'male' | 'female') => void;
    onSelectBracketId: (bracketId: string) => void;
}

function DivisionalClassSelector({
    selectedGender, activeBracket, userWeightBracket, currentUserStanding, onSelectGender, onSelectBracketId,
}: Readonly<DivisionalClassSelectorProps>) {
    return (
        <Card className="bg-surface border-border p-4 shadow-sm">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="flex items-center gap-2.5">
                    <Users className="w-4 h-4 text-brand"/>
                    <div>
                        <h3 className="text-sm font-bold font-sans uppercase tracking-[1px] text-foreground">
                            Divisional Weight Class
                        </h3>
                        <span className="text-xs text-muted-foreground font-sans block mt-0.5">
                            {selectedGender === 'male' ? "Men's" : "Women's"} {activeBracket.label} (
                            {activeBracket.minKg > 0 ? `${activeBracket.minKg} - ` : 'Up to '}
                            {activeBracket.maxKg < 999 ? `${activeBracket.maxKg} kg` : '+ kg'})
                        </span>
                    </div>
                </div>

                <div className="flex flex-wrap items-center gap-3">
                    {/* gender toggle */}
                    <div className="flex bg-surface-2 border border-border rounded-lg p-1 self-start sm:self-auto shrink-0">
                        <button type="button" onClick={() => onSelectGender('male')}
                        className={`px-3 py-1 text-xs font-bold uppercase tracking-[1px] rounded-md transition font-sans ${
                            selectedGender === 'male' ? 'bg-surface text-brand shadow-sm font-extrabold' : 'text-muted-foreground hover:text-foreground'
                        }`}>
                            Men's
                        </button>
                        <button type="button" onClick={() => onSelectGender('female')}
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
                                        <DropdownMenuItem key={bracket.id} onSelect={() => onSelectBracketId(bracket.id)}
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
                            </DropdownMenuContent>
                        </DropdownMenu>
                    </div>
                </div>
            </div>
        </Card>
    );
}

export default function ArenaLeaderboardPage() {//
    const {arenaId} = useParams<{arenaId: string}>();
    const navigate = useNavigate();
    const {user} = useAuth();
    
    const [liveArena, setLiveArena] = useState<ClashArena | null>(null);
    const [rawPrivateStandings, setRawPrivateStandings] = useState<LeaderboardAthleteDto[]>([]);
    const [publicStandings, setPublicStandings] = useState<LeaderboardAthleteDto[]>([]);
    const [currentUserStanding, setCurrentUserStanding] = useState<LeaderboardAthleteDto | null>(null);
    const [publicTotalAthletes, setPublicTotalAthletes] = useState<number>(0);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [selectedMetric, setSelectedMetric] = useState<ArenaMetric>('dots');
    const initializedMetricForArenaRef = useRef<string | null | undefined>(null);
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

    const [isDuelModalOpen, setIsDuelModalOpen] = useState<boolean>(false);

    const isDivisional = arenaId === 'weight-class-league';
    const isGlobal = arenaId === 'global-league';
    const isPrivate = !isDivisional && !isGlobal;
    const isCreator = (liveArena as { userRole?: string; createdById?: string})?.userRole === 'Owner' || liveArena?.createdById === user?.id;

    const isConcluded = isPrivate && liveArena ? (liveArena.isActive === false || (liveArena.daysRemaining !== undefined && liveArena.daysRemaining <= 0)): false;
    const primaryMetric = isPrivate ? getFrontendMetric(liveArena?.metricType) : 'dots';

    const activeBracket = WEIGHT_CLASS_BRACKETS.find((b) => b.id === selectedBracketId) || WEIGHT_CLASS_BRACKETS[0];
    const userWeightBracket = getWeightClassBracket(currentUserStanding?.bodyweightKg ?? 74);

    const fetchPrivateLeaderboard = async () => {
        const res = await customFetch(`/api/clash/arenas/${arenaId}`);
        if (!res.ok) return;
        const data = await res.json();
        if (data?.arena) {
            setLiveArena(data.arena);
            if (data.arena.metricType && initializedMetricForArenaRef.current !== arenaId) {
                initializedMetricForArenaRef.current = arenaId;
                setSelectedMetric(getFrontendMetric(data.arena.metricType));
            }
        }
        if (Array.isArray(data?.standings)) {
            const mapped = mapPrivateStandings(data.standings);
            setRawPrivateStandings(mapped);
        }
        if (data?.userStanding) {
            setCurrentUserStanding(data.userStanding as LeaderboardAthleteDto);
        }
    };

    const fetchPublicLeaderboard = async () => {
        const metricParam = getBackendMetricName(selectedMetric);
        const normalisedBracket = getNormalisedBracketId(selectedBracketId);
        const url = isDivisional ? `/api/clash/leaderboard/divisional?gender=${selectedGender}&bracketId=${normalisedBracket}&metric=${metricParam}&timeframe=${selectedTimeframe}&page=${currentPage}&pageSize=${PAGE_SIZE}` : `/api/clash/leaderboard/global?metric=${metricParam}&timeframe=${selectedTimeframe}&page=${currentPage}&pageSize=${PAGE_SIZE}`;
        const res = await customFetch(url);
        if (res.ok) {
            const data: LeaderboardApiResponse = await res.json();
            setPublicStandings(data.entries ?? []);
            setPublicTotalAthletes(data.totalCount ?? 0);
            setCurrentUserStanding(data.currentUserEntry ?? null);
        }
    };

    const fetchLeaderboard = async () => {
        if (!arenaId) return;
        setIsLoading(true);
        try {
            if (isPrivate) {
                await fetchPrivateLeaderboard();
            } else {
                await fetchPublicLeaderboard();
            }
        } catch {
            toast.error('Failed to load leaderboard data', 'Connection Error');
        } finally {
            setIsLoading(false);
        }
    };

    const sortedStandings = useMemo(() => {
        if (!isPrivate) return publicStandings;
        const sorted = [...rawPrivateStandings].sort((a, b) => {
            const scoreA = getMetricScore(a, selectedMetric);
            const scoreB = getMetricScore(b, selectedMetric);
            if (scoreB !== scoreA) return scoreB - scoreA;
            return a.displayName.localeCompare(b.displayName);
        });
        return sorted.map((ath, idx) => ({ ...ath, rank: idx + 1 }));
    }, [isPrivate, publicStandings, rawPrivateStandings, selectedMetric]);

    const totalAthletes = isPrivate ? rawPrivateStandings.length : publicTotalAthletes;
    const totalPages = Math.max(1, Math.ceil(totalAthletes / PAGE_SIZE));
    const startIndex = (currentPage - 1) * PAGE_SIZE;

    const displayStandings = useMemo(() => {
        if (!isPrivate) return sortedStandings;
        return sortedStandings.slice(startIndex, startIndex + PAGE_SIZE);
    }, [isPrivate, sortedStandings, startIndex]);

    const displayCurrentUserStanding = useMemo(() => {
        if (!isPrivate) return currentUserStanding;
        return sortedStandings.find((s) => s.isCurrentUser || s.userId === user?.id) || currentUserStanding;
    }, [isPrivate, currentUserStanding, sortedStandings, user?.id]);

    useEffect(() => {
        if (isPrivate) {
            void fetchLeaderboard();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [arenaId]);

    useEffect(() => {
        if (!isPrivate) {
            void fetchLeaderboard();
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [arenaId, selectedMetric, selectedGender, selectedBracketId, currentPage, selectedTimeframe]);

    useEffect(() => {
        if (!isPrivate || !arenaId) return;
        let isCancelled = false;
        const connection = new signalR.HubConnectionBuilder()
            .withUrl('/api/hubs/clash')
            .withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        connection.start().then(() => {
            if (isCancelled) {
                void connection.stop();
                return;
            }
            connection.invoke('JoinArena', arenaId).catch(() => {});
        }).catch(() => {});

        connection.on('ReceiveUserJoined', (joinedArenaId: string, userName: string) => {
            if (joinedArenaId === arenaId) {
                toast.success(`${userName} joined the arena!`, 'New Member');
                void fetchPrivateLeaderboard();
            }
        });

        connection.on('ReceiveUserLeft', (leftArenaId: string, userName: string) => {
            if (leftArenaId === arenaId) {
                toast.info(`${userName} left the arena`, 'Member Left');
                void fetchPrivateLeaderboard();
            }
        });

        return () => {
            isCancelled = true;
            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke('LeaveArena', arenaId).catch(() => {});
                void connection.stop();
            }
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [isPrivate, arenaId]);

    //sonqorqube nested ternary issues
    const arenaName = liveArena?.name || (isDivisional ? 'Divisional Weight-Class League': 'Global Season League');

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


    return (
        <div className="min-h-screen bg-background text-foreground pb-24">
            <div className="max-w-6xl mx-auto px-4 pt-8 pb-4">
                <Link to="/clash" className="inline-flex items-center gap-2 text-xs font-bold text-muted-foreground hover:text-brand uppercase tracking-[1px] mb-4 transition font-sans">
                    <ArrowLeft className="w-4 h-4"/>Back to Arenas
                </Link>
                <ArenaHeader arenaName={arenaName} isCreator={isCreator} isPrivate={isPrivate} liveArena={liveArena} isConcluded={isConcluded} isRefreshing={isRefreshing}
                copied={copied} onRefresh={handleRefresh} onShareClick={handleShareClick} onOpenLeaveConfirm={() => setIsLeaveConfirmOpen(true)}/>
            </div>

            <div className="max-w-6xl mx-auto px-4 py-4 space-y-6">
                <ArenaFilterBar selectedMetric={selectedMetric} primaryMetric={primaryMetric} isPrivate={isPrivate} selectedTimeframe={selectedTimeframe}
                    onSelectMetric={(metric) => {
                        setSelectedMetric(metric);
                        setCurrentPage(1);
                    }}
                    onSelectTimeframe={(timeframe) => {
                        setSelectedTimeframe(timeframe);
                        setCurrentPage(1);
                    }} />
                {isDivisional && (
                    <DivisionalClassSelector selectedGender={selectedGender} activeBracket={activeBracket} userWeightBracket={userWeightBracket} currentUserStanding={currentUserStanding}
                        onSelectGender={(gender) => {
                            setSelectedGender(gender);
                            setCurrentPage(1);
                        }}
                        onSelectBracketId={(bracketId) => {
                            setSelectedBracketId(bracketId);
                            setCurrentPage(1);
                        }} />
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
                                <LeaderboardTableBody isLoading={isLoading} standings={displayStandings} selectedMetric={selectedMetric} onSelectAthlete={handleOpenAthleteDrawer}/>
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
                                {displayCurrentUserStanding ? `Rank #${displayCurrentUserStanding.rank} — ${displayCurrentUserStanding.displayName} (You)` : `${user?.name ?? 'You'} — Not ranked in this category yet`}
                            </strong>
                        </div>
                    </div>

                    <div className="flex items-center gap-4">
                        <div className="text-right">
                            <span className="text-xs font-sans font-bold text-brand">
                                {renderStandingScore(displayCurrentUserStanding, selectedMetric)}
                            </span>
                            <span className="text-[10px] text-success block font-sans font-semibold">
                                {getUserStandingStatus(displayCurrentUserStanding)}
                            </span>
                        </div>
                        {displayCurrentUserStanding && (
                        <Button variant="default" size="sm" onClick={() => handleOpenAthleteDrawer(displayCurrentUserStanding)}
                        className="h-8 text-xs">
                            Profile
                        </Button>

                        )}
                    </div>
                </div>
            </aside>

            <ProfileInspectorDrawer athlete={selectedAthlete} isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)}
            onChallengeDuel={() => {
                setIsDrawerOpen(false);
                setIsDuelModalOpen(true);
            }}/>
            <CreateDuelModal isOpen={isDuelModalOpen} onClose={() => setIsDuelModalOpen(false)} defaultFriend={selectedAthlete}/>

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