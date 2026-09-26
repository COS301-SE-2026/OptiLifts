import { useState, useEffect } from "react";
import type { MouseEvent } from "react";
import { toast } from "../ui/alert";
import confetti from "canvas-confetti";
import { AthleteAvatar } from "./athlete-avatar";
import { TierBadge } from "./tier-badge";
import { Button } from "../ui/button";
import { Activity, ChevronDown, ChevronUp, Dumbbell, Heart, Medal, Swords, Trophy, UserCheck, UserPlus, X } from "lucide-react";
import { Card, CardContent } from "../ui/card";
import SpiderGraph from "../ui/spider-graph";
import { type ClashAthlete } from "@/types/clash";
import { useAuth } from "@/context/auth-context";
import { customFetch } from "@/lib/custom-fetch";

interface MuscleBalanceDto {
    muscleGroup: string;
    volumeKg: number;
}
interface TrophyDto {
    id: string;
    name: string;
    category: string;
    description: string;
    iconUrl?: string;
    earnedAt: string;
}
interface RecentWorkoutExerciseDto {
    exerciseName: string;
    sets: number;
}

interface RecentWorkoutDto {
    logId: string;
    title: string;
    notes?: string;
    completedAt?: string;
    volumeKg: number;
    exercises: RecentWorkoutExerciseDto[];
}
interface AthleteProfileApiResponse {
    userId: string;
    displayName: string;
    bodyweightKg: number;
    tier: string;
    tierLevel: number;
    dotsScore: number;
    squat1RM: number;
    bench1RM: number;
    deadlift1RM: number;
    totalE1RM: number;
    weeklyVolumeKg: number;
    muscleBalance30d: MuscleBalanceDto[];
    trophies: TrophyDto[];
    recentWorkouts: RecentWorkoutDto[];
    kudosCount: number;
    hasSentKudos: boolean;
    isFriend?: boolean;
}
interface ProfileInspectorDrawerProps {
    athlete: ClashAthlete | null;
    isOpen: boolean;
    onClose: () => void;
    onChallengeDuel?: (athlete: ClashAthlete) => void; //f4
}
export function ProfileInspectorDrawer({
    athlete, isOpen, onClose, onChallengeDuel,
}: Readonly<ProfileInspectorDrawerProps>) {
    const {user} = useAuth();
    const [profile, setProfile] = useState<AthleteProfileApiResponse | null>(null);
    const [isSubmittingKudos, setIsSubmittingKudos] = useState<boolean>(false);

    const [activeTab, setActiveTab] = useState<'overview' | 'workouts'>('overview');
    const [kudosCount, setKudosCount] = useState<number>(0)
    const [hasGivenKudos, setHasGivenKudos] = useState<boolean>(false);
    const [expandedWorkoutId, setExpandedWorkoutId] = useState<string | null>(null);
    const [showTrophiesModal, setShowTrophiesModal] = useState<boolean>(false);
    const [sentFriendRequest, setSentFriendRequest] = useState<boolean>(false);

    useEffect (() => {
        if (!isOpen || !athlete?.id) {
            // eslint-disable-next-line react-hooks/set-state-in-effect -- reset profile on drawer close
            setProfile(null);
            return;
        }
        let isMounted = true;
        customFetch(`/api/clash/athletes/${athlete.id}/profile`).then(async (res) => {
            if (res.ok) {
                const data: AthleteProfileApiResponse = await res.json();
                if (isMounted) {
                    setProfile(data);
                    setKudosCount(data.kudosCount ?? 0);
                    setHasGivenKudos(data.hasSentKudos ?? false);
                }
            }
        }).catch(() => {}).finally(() => {});

        return () => {
            isMounted = false;
        };
    }, [isOpen, athlete?.id]);

    if(!isOpen || !athlete){
        return null;
    }

    const isCurrentUser = user?.id === athlete.id;
    const isAlreadyFriend = profile?.isFriend ?? false;

    const handleKudos = async (e: MouseEvent<HTMLButtonElement>) => {
        if(hasGivenKudos || isSubmittingKudos) {
            return;
        }
        const rect = e.currentTarget?.getBoundingClientRect();
        setIsSubmittingKudos(true);
        try {
            const res = await customFetch(`/api/clash/athletes/${athlete.id}/kudos`, {
                method: 'POST',
            });
            if (res.ok) {
                setKudosCount((prev) => prev + 1);
                setHasGivenKudos(true);
                toast.success(`You sent kudos to ${athlete.name}`, 'Kudos Delivered');
                if (rect) {
                    const x = (rect.left + rect.width / 2) / window.innerWidth;
                    const y = (rect.top + rect.height / 2) / window.innerHeight;
                    confetti({
                        particleCount: 35,
                        spread: 60,
                        origin: {x,y},
                        colors: ['#CC0022', '#B35C00', '#FF9800', '#FFFFFF'],
                    });
                }
            } else {
                const errData = await res.json().catch(() => null);
                toast.error(errData?.message ?? 'Failed to send kudos');
            }
        } catch {
            toast.error('Network error sending kudos');
        } finally {
            setIsSubmittingKudos(false);
        }
    };

    const handleSendFriendRequest = async () => {
        if (!athlete.code || sentFriendRequest) return;
        try {
            const res = await customFetch('/api/clash/friends/requests', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    friendCode: athlete.code
                })
            });
            const data = await res.json().catch(() => null);
            if (res.ok) {
                setSentFriendRequest(true);
                toast.success(`Friend request sent to ${athlete.name}`, 'Request Dispatched');
            } else {
                toast.error(data?.message ?? 'Could not send friend request');
            }
        } catch {
            toast.error('Network error sending friend request');
        }
    };

    const muscleBalanceData: Record<string, number> = {
        Chest: 0,
        Core: 0,
        Shoulders: 0,
        Arms: 0,
        Legs: 0,
        Back: 0,
    };
    if (profile?.muscleBalance30d) {
        for (const item of profile.muscleBalance30d) {
            if (item.muscleGroup in muscleBalanceData) {
                muscleBalanceData[item.muscleGroup] = Number(item.volumeKg);
            }
        }
    } else if (athlete.muscleBalance30d) {
        Object.assign(muscleBalanceData, athlete.muscleBalance30d);
    }
    const workoutsList = profile?.recentWorkouts?.map((w) => {
        const dateStr = w.completedAt ? new Date(w.completedAt).toLocaleDateString(): 'Recent';
        return {
            id: w.logId,
            title: w.title,
            date: dateStr,
            durationMins:0,
            volumeKg: Number(w.volumeKg),
            exercises: w.exercises.map((ex) => ({
                name: ex.exerciseName,
                sets: `${ex.sets} sets`,
            })),
        };
    }) ?? athlete.recentWorkouts;

    const trophiesList = profile?.trophies?.map((t) => ({
        id: t.id,
        name: t.name,
        category: t.category,
        description: t.description,
        earnedAt: t.earnedAt,
    })) ?? athlete.trophies;

    return (
        <div className="fixed inset-x-0 bottom-0 top-20 z-40 flex justify-end bg-black/60 backdrop-blur-sm transition-opacity duration-200">
            <button type="button" className="flex-1 cursor-default bg-transparent border-0 outline-none" onClick={onClose} aria-label="Close drawer backdrop"/>
            <section aria-label={`Athlete profile for ${athlete.name}`}
            className="w-full max-w-lg h-full bg-surface border-l border-border shadow-2xl flex flex-col overflow-hidden animate-in slide-in-from-right duration-200 text-foreground">
                <header className="relative bg-surface-2 p-5 border-b border-border">
                    <Button variant="ghost" size="icon" onClick={onClose} className="absolute top-4 right-4 h-8 w-8 rounded-lg" aria-label="Close">
                        <X className="w-4 h-4 text-muted-foreground hover:text-foreground"/>
                    </Button>
                    <div className="flex items-center gap-4">
                        <AthleteAvatar initials={athlete.initials} name={athlete.name} avatarUrl={athlete.avatarUrl} isCurrentUser={isCurrentUser} size="xl"/>
                        <div>
                            <h2 className="font-display text-2xl tracking-wide text-foreground leading-tight">
                                {athlete.name}
                            </h2>
                            <div className="text-xs text-muted-foreground mt-0.5 font-sans">
                                Bodyweight: <strong className="text-foreground">{athlete.bodyweightKg} kg</strong>
                            </div>
                            {/* tier + dots badges */}
                            <div className="flex items-center gap-2 mt-2 font-sans">
                                <TierBadge tier={profile?.tier || athlete.tier || 'Unranked'} size="md"/>
                                <span className="px-3 py-1 rounded-full text-xs font-bold bg-surface text-brand border border-border">
                                    {athlete.dotsScore} DOTS
                                </span>
                            </div>
                        </div>
                    </div>

                    {/* action bar */}
                    <div className="flex flex-wrap items-center gap-2 mt-4 pt-3 border-t border-border">
                        <Button variant="secondary" size="sm" onClick={handleKudos} disabled={hasGivenKudos || isSubmittingKudos || isCurrentUser}
                        className={`flex-1 min-w-[100px] h-8 text-xs font-semibold ${hasGivenKudos ? 'border border-brand text-brand' : ''}`}>
                            <Heart className={`w-3.5 h-3.5 mr-1 ${hasGivenKudos ? 'fill-brand text-brand': ''}`}/>
                            <span>{kudosCount} Kudos</span>
                        </Button>

                        {!isCurrentUser && (
                            <>
                            {/* f4: 1v1 challenge duel btn */}
                                {isAlreadyFriend ? (
                                    <Button variant="default" size="sm" onClick={() => onChallengeDuel?.(athlete)}
                                    className="flex-1 min-w-[100px] h-8 text-xs flex items-center justify-center gap-1.5">
                                        <Swords className="w-3.5 h-3.5" />
                                        <span>1v1 Duel</span>
                                    </Button>
                                ) : (
                                    <Button variant={sentFriendRequest ? 'secondary' : 'default'} size="sm" disabled={sentFriendRequest} onClick={handleSendFriendRequest}
                                    className="flex-1 min-w-[100px] h-8 text-xs flex items-center justify-center gap-1.5">
                                        {sentFriendRequest ? (
                                            <>
                                                <UserCheck className="w-3.5 h-3.5 text-success"/>
                                                <span>Request Sent</span>
                                            </>
                                        ) : (
                                            <>
                                                <UserPlus className="w-3.5 h-3.5"/>
                                                <span>Add Friend</span>
                                            </>
                                        )}
                                    </Button>
                            )}
                            </>
                        )}

                        <Button variant="outline" size="sm" onClick={() => setShowTrophiesModal(true)}
                            className="flex-1 min-w-[90px] h-8 text-xs flex items-center justify-center gap-1.5 border-border">
                            <Trophy className="w-3.5 h-3.5 text-warning"/>
                            <span>Trophies</span>
                        </Button>
                    </div>
                </header>

                {/* tab switcher */}
                <div className="flex border-b border-border bg-surface px-4 overflow-x-auto">
                    <button type="button" onClick={() => setActiveTab('overview')}
                        className={`flex-1 py-3 font-sans text-xs font-bold uppercase tracking-[1px] border-b-2 transition whitespace-nowrap shrink-0 ${
                            activeTab === 'overview' ? 'border-brand text-brand' : 'border-transparent text-muted-foreground hover:text-foreground'
                        }`}>
                        Overview
                    </button>
                    <button type="button" onClick={() => setActiveTab('workouts')}
                        className={`flex-1 py-3 font-sans text-xs font-bold uppercase tracking-[1px] border-b-2 transition whitespace-nowrap shrink-0 ${
                            activeTab === 'workouts' ? 'border-brand text-brand' : 'border-transparent text-muted-foreground hover:text-foreground'
                        }`}>
                        Recent Workouts
                    </button>
                </div>

                {/* drawer body */}
                <div className="flex-1 overflow-y-auto p-5 space-y-5 bg-background">
                    {activeTab === 'overview' && (
                        <>
                            {/* strength benchmarks card */}
                            <Card className="bg-surface border-border p-4 shadow-sm">
                                <CardContent className="p-0 space-y-3">
                                    <div className="flex items-center gap-1.5 text-xs font-bold uppercase tracking-[1px] text-muted-foreground font-sans">
                                        <Activity className="w-4 h-4 text-brand"/> Best Estimated 1-Rep Maxes (e1RM)
                                    </div>

                                    <div className="grid grid-cols-3 gap-2.5">
                                        <div className="p-2.5 bg-surface-2 rounded-lg border border-border text-center">
                                            <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider block font-sans">Squat</span>
                                            <span className="font-display text-xl font-bold text-foreground">{profile?.squat1RM ?? athlete.squat1RM} kg</span>
                                        </div>
                                        <div className="p-2.5 bg-surface-2 rounded-lg border border-border text-center">
                                            <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider block font-sans">Bench</span>
                                            <span className="font-display text-xl font-bold text-foreground">{profile?.bench1RM ?? athlete.bench1RM} kg</span>
                                        </div>
                                        <div className="p-2.5 bg-surface-2 rounded-lg border border-border text-center">
                                            <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider block font-sans">Deadlift</span>
                                            <span className="font-display text-xl font-bold text-foreground">{profile?.deadlift1RM ?? athlete.deadlift1RM} kg</span>
                                        </div>
                                    </div>

                                    <div className="pt-2.5 border-t border-border flex justify-between items-center text-xs text-muted-foreground font-sans">
                                        <span>Total e1RM: <strong className="text-foreground">{ profile?.totalE1RM ?? athlete.totalE1RM} kg</strong></span>
                                        {/* pls note im aware the variable says weekly volume but i made an exec 
                                        decision to make it monthly volume instead but dont want to change everywhere 
                                        it says 'weekly'. in terms of correctness, weekly actually holds monthly so its fine */}
                                        <span>Monthly Volume: <strong className="text-brand font-bold">{ (profile?.weeklyVolumeKg ?? athlete.weeklyVolumeKg).toLocaleString()} kg</strong></span>
                                    </div>
                                </CardContent>
                            </Card>

                            {/* spidergraph */}
                            <Card className="bg-surface border-border p-4 shadow-sm">
                                <CardContent className="p-0">
                                    <div className="flex items-center justify-between text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-3 font-sans">
                                        <span>Muscle Volume Balance</span>
                                        <span className="text-[11px] text-brand font-semibold lowercase">last 30 days</span>
                                    </div>
                                    <div className="flex justify-center items-center py-2">
                                        <SpiderGraph data={muscleBalanceData} className="max-w-[280px] max-h-[280px]"/>
                                    </div>
                                </CardContent>
                            </Card>
                        </>
                    )}

                    {activeTab === 'workouts' && (
                        <div className="space-y-3">
                            {workoutsList.length === 0 ? (
                                <div className="text-center py-10 text-muted-foreground text-xs font-sans">
                                    No public workout logs shared yet for this user.
                                </div>
                            ):(
                                workoutsList.map((w) => {
                                    const isExpanded = expandedWorkoutId === w.id;
                                    return (
                                        <Card key={w.id} className="bg-surface border-border shadow-sm overflow-hidden transition">
                                            <button type="button" onClick={() => setExpandedWorkoutId(isExpanded ? null : w.id)}
                                            className="w-full flex items-center justify-between p-4 text-left hover:bg-surface-2 transition">
                                                <div>
                                                    <h4 className="font-sans text-sm font-bold text-foreground">
                                                        {w.title}
                                                    </h4>
                                                    <p className="text-xs text-muted-foreground flex items-center gap-2 mt-0.5 font-sans">
                                                        {w.date}
                                                    </p>
                                                </div>
                                                <div className="flex items-center gap-2">
                                                    <span className="text-xs font-bold text-brand bg-brand-fill px-2 py-0.5 rounded border border-brand/20">
                                                        {w.volumeKg.toLocaleString()} kg
                                                    </span>
                                                    {isExpanded ? <ChevronUp className="w-4 h-4 text-muted-foreground"/> : <ChevronDown className="w-4 h-4 text-muted-foreground"/>}
                                                </div>
                                            </button>

                                            {isExpanded && (
                                                <div className="p-4 pt-0 border-t border-border space-y-2 bg-surface-2/40">
                                                    <h5 className="text-[11px] font-bold uppercase text-muted-foreground tracking-wider mt-3 font-sans">
                                                        Exercise Logs & Sets:
                                                    </h5>
                                                    {w.exercises.map((ex, idx) => (
                                                        <div key={idx} className="flex justify-between items-center text-xs py-1 border-b border-border/60 last:border-0 font-sans">
                                                            <span className="text-foreground font-medium flex items-center gap-1.5">
                                                                <Dumbbell className="w-3.5 h-3.5 text-brand"/>{ex.name}
                                                            </span>
                                                            <span className="text-muted-foreground text-[11px] font-mono">{ex.sets}</span>
                                                        </div>
                                                    ))}
                                                </div>
                                            )}
                                        </Card>
                                    );
                                })
                            )}
                        </div>
                    )}
                </div>
            </section >

            {/* trophies + achievments modal */}
            {showTrophiesModal && (
                <div className="fixed inset-0 z-60 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
                    <Card className="bg-surface border-border max-w-md w-full p-6 text-center relative shadow-2xl">
                        <Button variant="ghost" size="icon" onClick={() => setShowTrophiesModal(false)}
                            className="absolute top-4 right-4 h-8 w-8" aria-label="Close">
                            <X className="w-4 h-4"/>
                        </Button>

                        <div className="w-14 h-14 rounded-full bg-brand-fill border border-brand/30 flex items-center justify-center mx-auto mb-3 text-brand">
                            <Trophy className="w-7 h-7"/>
                        </div>
                        <h3 className="font-display text-2xl tracking-wide text-foreground">
                            {athlete.name}&apos;s Trophies
                        </h3>
                        <p className="text-xs text-muted-foreground mt-0.5 font-sans">
                            Earned milestones and competitive season badges
                        </p>

                        <div className="my-5 space-y-2.5 text-left">
                            {trophiesList.length > 0 ? (
                                athlete.trophies.map((t) => (
                                    <div key={t.id} className="p-3 bg-surface-2 rounded-xl border border-border flex items-start gap-3">
                                        <div className="w-9 h-9 rounded-lg bg-surface border border-border flex items-center justify-center text-brand shrink-0">
                                            <Medal className="w-5 h-5"/>
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <div className="flex items-center justify-between text-xs">
                                                <strong className="font-bold text-foreground font-sans truncate">{t.name}</strong>
                                                <span className="text-[11px] text-muted-foreground font-sans">{t.category}</span>
                                            </div>
                                            <p className="text-[11px] text-muted-foreground font-sans mt-0.5">{t.description}</p>
                                        </div>
                                    </div>
                                ))
                            ):(
                                <div className="p-4 bg-surface-2 rounded-xl border border-border text-center text-xs text-muted-foreground font-sans">
                                    No trophy milestones earned yet for this athlete
                                </div>
                            )}
                        </div>

                        <Button variant="default" onClick={() => setShowTrophiesModal(false)} 
                        className="w-full">
                            Close Trophies
                        </Button>
                    </Card>
                </div>
            )}

        </div>
    );
}