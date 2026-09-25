export type ClashAthleteTier = 'Bronze' | 'Silver' | 'Gold' | 'Diamond' | 'Overload Master';

export interface WeightClassBracket {
    id: string;
    label: string;
    minKg: number;
    maxKg: number;
}
export const WEIGHT_CLASS_BRACKETS: WeightClassBracket[] = [
    { id: 'u59', label: '-59 kg Class', minKg: 0, maxKg: 59.0 },
    { id: '66', label: '66 kg Class', minKg: 59.1, maxKg: 66.0 },
    { id: '74', label: '74 kg Class', minKg: 66.1, maxKg: 74.0 },
    { id: '83', label: '83 kg Class', minKg: 74.1, maxKg: 83.0 },
    { id: '93', label: '93 kg Class', minKg: 83.1, maxKg: 93.0 },
    { id: '105', label: '105 kg Class', minKg: 93.1, maxKg: 105.0 },
    { id: '120', label: '120 kg Class', minKg: 105.1, maxKg: 120.0 },
    { id: '120p', label: '120+ kg Class', minKg: 120.1, maxKg: 999.0 },
];

export function getWeightClassBracket(weightKg: number): WeightClassBracket {
    return WEIGHT_CLASS_BRACKETS.find((b) => weightKg <= b.maxKg) || WEIGHT_CLASS_BRACKETS[WEIGHT_CLASS_BRACKETS.length - 1];
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
    tier: 'Bronze' | 'Silver' | 'Gold' | 'Diamond' | 'Overload Master';
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