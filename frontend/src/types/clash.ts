export type ClashAthleteTier = 'Bronze' | 'Silver' | 'Gold' | 'Diamond' | 'Overload Master' | 'Unranked';

export interface WeightClassBracket {
    id: string;
    label: string;
    minKg: number;
    maxKg: number;
}
export const WEIGHT_CLASS_BRACKETS: WeightClassBracket[] = [
    { id: 'u59', label: '-59 kg Class', minKg: 0, maxKg: 59.0 },
    { id: 'u66', label: '66 kg Class', minKg: 59.1, maxKg: 66.0 },
    { id: 'u74', label: '74 kg Class', minKg: 66.1, maxKg: 74.0 },
    { id: 'u83', label: '83 kg Class', minKg: 74.1, maxKg: 83.0 },
    { id: 'u93', label: '93 kg Class', minKg: 83.1, maxKg: 93.0 },
    { id: 'u105', label: '105 kg Class', minKg: 93.1, maxKg: 105.0 },
    { id: 'u120', label: '120 kg Class', minKg: 105.1, maxKg: 120.0 },
    { id: '120p', label: '120+ kg Class', minKg: 120.1, maxKg: 999.0 },
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

export function getDuelMatchupState(
    duel: DuelSummary,
    currentUserId?: string,
    currentUserName?: string,
    now: number = Date.now()
) : DuelMatchupState {
    const isUserChallenger = !currentUserId || duel.challengerUserId === currentUserId;

    const userRawValue = Number(isUserChallenger ? duel.challengerCurrentValue : duel.rivalCurrentValue) || 0;
    const rivalRawValue = Number(isUserChallenger ? duel.rivalCurrentValue : duel.challengerCurrentValue) || 0;
    const rivalName = (isUserChallenger ? duel.rivalName : duel.challengerName) || 'Rival';
    const rivalId = (isUserChallenger ? duel.rivalUserId : duel.challengerUserId) || '';
    const rivalAvatarUrl = isUserChallenger ? duel.rivalAvatarUrl : duel.challengerAvatarUrl;

    const rivalInitials = rivalName ? rivalName.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase() : 'OP';
    const userInitials = (currentUserName || (isUserChallenger ? duel.challengerName : duel.rivalName) || 'You').split(' ').map((n) => n[0]).join('').slice(0,2).toUpperCase();

    const isVolume = duel.targetType?.toLowerCase().includes('volume');
    const format = (val: number) => {
        if (isVolume) {
            return `${val.toLocaleString()} kg`;
        }
        return `${val >= 0 ? '+' : ''}${val.toFixed(1)}%`;
    };

    const userDisplayMetric = format(userRawValue);
    const rivalDisplayMetric = format(rivalRawValue);    
    const valA = Math.max(0, userRawValue);
    const valB = Math.max(0, rivalRawValue);
    let userProgressPercent = 50;
    let rivalProgressPercent = 50;
    if (valA + valB > 0) {
        userProgressPercent = Math.round((valA/(valA+valB))*100);
        rivalProgressPercent = 100 - userProgressPercent;
    }

    const isWinning = userRawValue > rivalRawValue;
    const isTied = userRawValue === rivalRawValue;

    let leadText = 'All Tied';
    if (!isTied) {
        const diff = Math.abs(userRawValue - rivalRawValue);
        const formattedDiff = isVolume ? `${diff.toLocaleString()} kg` : `${diff.toFixed(1)}%`;
        leadText = isWinning ? `+${formattedDiff} lead` : `-${formattedDiff} behind`;
    }

    let endsInText = 'Active'
    if (duel.endDate) {
        const diffMs = new Date(duel.endDate).getTime() - now;
        if (diffMs <= 0) {
            endsInText = 'Concluded';
        } else {
            const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));
            if (diffDays >= 1) {
                endsInText = `${diffDays}d`;
            } else {
                const diffHours = Math.max(1, Math.floor(diffMs / (1000 * 60 * 60)));
                endsInText = `${diffHours}h`;
            }
        }
    }

    const isFinished = duel.status?.toLowerCase() === 'finished';
    const temp = (isUserChallenger ? duel.winnerUserId === duel.challengerUserId : duel.winnerUserId === duel.rivalUserId);
    const userWon = duel.winnerUserId ? temp : userRawValue > rivalRawValue;

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
    }
}

