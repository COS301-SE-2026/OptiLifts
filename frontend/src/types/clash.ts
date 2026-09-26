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
    challengerCurrentValue: string;
    rivalCurrentValue: string;
    challengerBaselineValue?: string | null;
    rivalBaselineValue?: string | null;
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