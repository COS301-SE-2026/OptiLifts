export type ClashAthleteTier = 'Bronze' | 'Silver' | 'Gold' | 'Diamond' | 'Overload Master' | 'Unranked';

export interface WeightClassBracket {
    id: string;
    label: string;
    minKg: number;
    maxKg: number;
}
export const WEIGHT_CLASS_BRACKETS: WeightClassBracket[] = [
    { id: 'u59', label: '<59 kg Class', minKg: 0, maxKg: 59.0 },
    { id: 'u66', label: '66 kg Class', minKg: 59.1, maxKg: 66.0 },
    { id: 'u74', label: '74 kg Class', minKg: 66.1, maxKg: 74.0 },
    { id: 'u83', label: '83 kg Class', minKg: 74.1, maxKg: 83.0 },
    { id: 'u93', label: '93 kg Class', minKg: 83.1, maxKg: 93.0 },
    { id: 'u105', label: '105 kg Class', minKg: 93.1, maxKg: 105.0 },
    { id: 'u120', label: '120 kg Class', minKg: 105.1, maxKg: 120.0 },
    { id: '120p', label: '>120 kg Class', minKg: 120.1, maxKg: 999.0 },
];

export function getWeightClassBracket(weightKg: number): WeightClassBracket {
    return WEIGHT_CLASS_BRACKETS.find((b) => weightKg <= b.maxKg) || WEIGHT_CLASS_BRACKETS.at(-1)!;
}

export interface ClashArena {
    id: string;
    name: string;
    type: 'global' | 'divisional' | 'private';
    code?: string;
    createdById?: string; 
    creatorName?: string;
    memberCount: number;
    endsInText?: string;
    daysRemaining?: number;
    isActive?: boolean;
    seasonEndDate?: string;
    durationDays?: number;
    metricType: 'DOTS Overall' | 'Total Volume' | 'Bench e1RM' | 'Squat e1RM';
    userRole?: string;
}

export interface ClashAthlete {
    id: string;
    name: string;
    initials: string;
    avatarUrl?: string;
    code: string;
    gender: 'male' | 'female';
    bodyweightKg: number;
    squat1RM: number;
    bench1RM: number;
    deadlift1RM: number;
    totalE1RM: number;
    dotsScore: number;
    tier: ClashAthleteTier;
    tierLevel: number;
    rankTrend: number;
    weeklyVolumeKg: number;
    lastWorkoutDate: string;
    muscleBalance30d: {
        Chest: number;
        Core: number;
        Shoulders: number;
        Arms: number;
        Legs: number;
        Back: number;
    };
    trophies: {
        id: string;
        name: string;
        description: string;
        category: string;
        earnedAt: string;
    }[];
    recentWorkouts: {
        id: string;
        title: string;
        date: string;
        durationMins: number;
        volumeKg: number;
        exercises: {
            name: string;
            sets: string;
        }[];
    }[];
}

export interface ClashActivityItem {
    id: string;
    arenaId: string;
    arenaName: string;
    athleteId: string;
    athleteName: string;
    athleteInitials: string;
    athleteAvatarUrl?: string;
    timeAgo: string;
    eventText: string;
    details: string;
    kudosCount: number;
    isPr?: boolean;
    isPromotion?: boolean;
}

export interface DuelSummary {
    id: string;
    title: string;
    challengerUserId: string;
    challengerName: string;
    challengerAvatarUrl?: string | null;
    rivalUserId: string;
    rivalName: string;
    rivalAvatarUrl?: string | null;
    exerciseName: string;
    targetType: string; 
    status: string; //pending, active, finished, declined
    startDate?: string | null;
    endDate?: string | null;
    challengerCurrentValue: number | string;
    rivalCurrentValue: number | string;
    challengerBaselineValue?: number | string | null;
    rivalBaselineValue?: number | string | null;
    winnerUserId?: string | null;
    isDraw: boolean;
}

export interface DuelTimelineEventItem {
    id: string;
    userId: string;
    userName: string;
    eventText: string;
    isPr: boolean;
    createdAt: string;
}

export interface DuelDetail extends DuelSummary {
    timeline: DuelTimelineEventItem[];
}

export interface UserDuelsResponse {
    duels: DuelSummary[];
    won: number;
    lost: number;
    active: number;
    winRatePercent: number;
}

export interface DuelInviteItem {
    id: string;
    challengerUserId: string;
    challengerName: string;
    challengerAvatarUrl?: string | null;
    exerciseName: string;
    targetType: string;
    durationDays: number;
    createdAt: string;
}

export interface DuelMatchupState {
    isUserChallenger: boolean;
    userRawValue: number;
    rivalRawValue: number;
    rivalName: string;
    rivalId: string;
    rivalFirstName: string;
    rivalInitials: string;
    rivalAvatarUrl?: string | null;
    userInitials: string;
    userAvatarUrl?: string | null;
    userDisplayMetric: string;
    rivalDisplayMetric: string;
    userProgressPercent: number;
    rivalProgressPercent: number;
    isWinning: boolean;
    isTied: boolean;
    leadText: string;
    endsInText: string;
    isFinished: boolean;
    userWon: boolean;
}

function getDuelParticipantValues(duel: DuelSummary, isUserChallenger: boolean) {
    if (isUserChallenger) {
        return {
            userRawValue: Number(duel.challengerCurrentValue) || 0,
            rivalRawValue: Number(duel.rivalCurrentValue) || 0,
            userBaseline: Number(duel.challengerBaselineValue) || 0,
            rivalBaseline: Number(duel.rivalBaselineValue) || 0,
            rivalName: duel.rivalName || 'Rival',
            rivalId: duel.rivalUserId || '',
            rivalAvatarUrl: duel.rivalAvatarUrl,
            userAvatarUrl: duel.challengerAvatarUrl,
        };
    }
    return {
        userRawValue: Number(duel.rivalCurrentValue) || 0,
        rivalRawValue: Number(duel.challengerCurrentValue) || 0,
        userBaseline: Number(duel.rivalBaselineValue) || 0,
        rivalBaseline: Number(duel.challengerBaselineValue) || 0,
        rivalName: duel.challengerName || 'Rival',
        rivalId: duel.challengerUserId || '',
        rivalAvatarUrl: duel.challengerAvatarUrl,
        userAvatarUrl: duel.rivalAvatarUrl,
    };
}

function getInitials(name?: string, fallback: string = 'OP'): string {
    if (!name) {
        return fallback;
    }
    return name.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase();
}

function calculateGain(current: number, baseline: number): number {
    if (baseline <= 0 || current <= 0) {
        return 0;
    }
    return ((current - baseline) / baseline) * 100;
}

function formatDuelMetric(score: number, rawKg: number, isVolume: boolean): string {
    if (isVolume) {
        return `${score.toLocaleString()} kg`;
    }
    if (rawKg <= 0) {
        return '+0.0%';
    }
    return `${score >= 0 ? '+' : ''}${score.toFixed(1)}% (${rawKg.toFixed(1)} kg)`;
}

function getProgressPercents(userScore: number, rivalScore: number) {
    const valA = Math.max(0, userScore);
    const valB = Math.max(0, rivalScore);
    if (valA + valB > 0) {
        const userProgressPercent = Math.round((valA / (valA + valB)) * 100);
        return { userProgressPercent, rivalProgressPercent: 100 - userProgressPercent,};
    }
    return { userProgressPercent: 50, rivalProgressPercent: 50, };
}

function getLeadText(userScore: number, rivalScore: number, isVolume: boolean): string {
    if (userScore === rivalScore) {
        return 'All Tied';
    }
    const diff = Math.abs(userScore - rivalScore);
    const formattedDiff = isVolume ? `${diff.toLocaleString()} kg` : `${diff.toFixed(1)}%`;
    return userScore > rivalScore ? `+${formattedDiff} lead` : `-${formattedDiff} behind`;
}

function getEndsInText(endDate?: string | null, now: number = Date.now()): string {
    if (!endDate) {
        return 'Active';
    }
    const diffMs = new Date(endDate).getTime() - now;
    if (diffMs <= 0) {
        return 'Concluded';
    }
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
    if (diffDays >= 1) {
        return `${diffDays}d`;
    }
    const diffHours = Math.max(1, Math.floor(diffMs / (1000 * 60 * 60)));
    return `${diffHours}h`;
}

function checkUserWon(duel: DuelSummary, isUserChallenger: boolean, userRawValue: number, rivalRawValue: number): boolean {
    if (duel.winnerUserId) {
        return isUserChallenger ? duel.winnerUserId === duel.challengerUserId : duel.winnerUserId === duel.rivalUserId;
    }
    return userRawValue > rivalRawValue;
}

export function getDuelMatchupState(
    duel: DuelSummary,
    currentUserId?: string,
    currentUserName?: string,
    now: number = Date.now()
) : DuelMatchupState {
    const isUserChallenger = !currentUserId || duel.challengerUserId === currentUserId;

    const { userRawValue, rivalRawValue, userBaseline, rivalBaseline, rivalName, rivalId, rivalAvatarUrl, userAvatarUrl,} = getDuelParticipantValues(duel, isUserChallenger);
    const rivalInitials = getInitials(rivalName, 'OP');
    const fallbackUserName = isUserChallenger ? duel.challengerName : duel.rivalName;
    const userInitials = getInitials(currentUserName || fallbackUserName || 'You', 'You');

    const isVolume = Boolean(duel.targetType?.toLowerCase().includes('volume'));
    const userScore = isVolume ? userRawValue : calculateGain(userRawValue, userBaseline);
    const rivalScore = isVolume ? rivalRawValue : calculateGain(rivalRawValue, rivalBaseline);

    const userDisplayMetric = formatDuelMetric(userScore, userRawValue, isVolume);
    const rivalDisplayMetric = formatDuelMetric(rivalScore, rivalRawValue, isVolume);
    const { userProgressPercent, rivalProgressPercent } = getProgressPercents(userScore, rivalScore);

    const isWinning = userScore > rivalScore;
    const isTied = userScore === rivalScore;

    const leadText = getLeadText(userScore, rivalScore, isVolume);
    const endsInText = getEndsInText(duel.endDate, now);

    const isFinished = duel.status?.toLowerCase() === 'finished';
    const userWon = checkUserWon(duel, isUserChallenger, userRawValue, rivalRawValue);

    return {
        isUserChallenger,
        userRawValue,
        rivalRawValue,
        rivalName,
        rivalId,
        rivalFirstName: rivalName.split(' ')[0] || 'Rival',
        rivalInitials,
        rivalAvatarUrl,
        userInitials,
        userAvatarUrl,
        userDisplayMetric,
        rivalDisplayMetric,
        userProgressPercent,
        rivalProgressPercent,
        isWinning,
        isTied,
        leadText,
        endsInText,
        isFinished,
        userWon,
    };
}

