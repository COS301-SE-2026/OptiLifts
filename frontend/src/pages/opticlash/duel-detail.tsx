import { ProfileInspectorDrawer } from "@/components/opticlash/profile-inspector-drawer";
import { toast } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useAuth } from "@/context/auth-context";
import { customFetch } from "@/lib/custom-fetch";
import { type DuelDetail, type DuelTimelineEventItem, type ClashAthlete, getDuelMatchupState } from "@/types/clash";
import confetti from "canvas-confetti";
import { ArrowLeft, Clock, Loader2, Sparkles, Swords } from "lucide-react";
import { useState, useRef, useEffect, type MouseEvent } from "react";
import { Link, useParams } from "react-router-dom";
import * as signalR from "@microsoft/signalr";
import { CreateDuelModal } from "@/components/opticlash/create-duel-modal";

interface DuelUpdatePaylod {
    duelId: string;
    challengerCurrentValue: number;
    rivalCurrentValue: number;
    latestEventText: string;
    isPr: boolean;
}

export default function DuelArenaPage() {
    const {duelId} = useParams<{ duelId: string }>();
    const {user} = useAuth();
    const [duel, setDuel] = useState<DuelDetail | null>(null);
    const [timelineEvents, setTimelineEvents] = useState<DuelTimelineEventItem[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [selectedAthlete, setSelectedAthlete] = useState<ClashAthlete | null>(null);
    const [isDrawerOpen, setIsDrawerOpen] = useState(false);
    const [reactionSent, setReactionSent] = useState(false);
    const [renderTimestamp] = useState(() => Date.now());

    const [isDuelModalOpen, setIsDuelModalOpen] = useState(false);

    const hubRef = useRef<signalR.HubConnection | null>(null);
    const userRef = useRef(user);
    useEffect(() => {
        userRef.current = user;
    }, [user]);

    const fetchDetail = async () => {
        if (!duelId) return;
        try {
            const res = await customFetch(`/api/clash/duels/${duelId}`);
            if (res.ok) {
                const data: DuelDetail = await res.json();
                setDuel(data);
                setTimelineEvents(data.timeline || []);
            }
        } catch {
            //fallback gracefully
        } finally {
            setIsLoading(false);
        }
    };
    useEffect(() => {
        // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch-on-mount
        void fetchDetail();
        if(!duelId) {
            return;
        }
        const connection = new signalR.HubConnectionBuilder().withUrl('/api/hubs/clash').withAutomaticReconnect().build();

        hubRef.current = connection;
        connection.start().then(async () => {
            await connection.invoke('JoinDuel', duelId);
        }).catch(() => {
            //signalr fallback
        });

        connection.on('ReceiveDuelUpdate', (update: DuelUpdatePaylod) => {
            setDuel((prev) => {
                if (!prev) {
                    return prev;
                }
                return {
                    ...prev,
                    challengerCurrentValue: update.challengerCurrentValue,
                    rivalCurrentValue: update.rivalCurrentValue,
                };
            });            
            if (update.latestEventText) {
                toast.info(update.latestEventText, 'Live Duel Activity');
                const newEv: DuelTimelineEventItem = {
                    id: `live-${Date.now()}`,
                    userId: '',
                    userName: 'Athlete',
                    eventText: update.latestEventText,
                    isPr: update.isPr,
                    createdAt: new Date().toISOString(),
                };
                setTimelineEvents((prev) => [
                    newEv, 
                    ...prev
                ]);
        }
    });

        connection.on('ReceiveDuelHype', (_dId: string, senderName: string) => {
            const currentUserName = userRef.current?.name;
            if (currentUserName && senderName?.trim().toLowerCase() === currentUserName.trim().toLowerCase()) {
                return;
            }
            toast.success(`${senderName} cheered on this duel!`, 'Hype Received');
            confetti({
                particleCount: 25,
                spread: 50,
                origin: { x: 0.5, y: 0.5 },
                colors: ['#CC0022', '#FF9800', '#FFFFFF'],
            });
        });

        return () => {
            if (connection.state === signalR.HubConnectionState.Connected) {
                connection.invoke('LeaveDuel', duelId).catch(() => { });
            }
            connection.stop().catch(() => { });
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [duelId]);

    const handleSendReaction = async (e: MouseEvent<HTMLButtonElement>) => {
        if (!duelId || !duel) {
            return;
        }
        setReactionSent(true);

        const rect = e.currentTarget.getBoundingClientRect();
        const x = (rect.left + rect.width / 2) / window.innerWidth;
        const y = (rect.top + rect.height / 2) / window.innerHeight;
        confetti({
            particleCount: 35,
            spread: 60,
            origin: { x, y },
            colors: ['#CC0022', '#FF9800', '#1B6E1F', '#FFFFFF'],
        });

        try {
            await customFetch(`/api/clash/duels/${duelId}/hype`, {
                method: 'POST',
            });
            toast.success(`You hyped up your 1v1 duel against ${duel.rivalName}!`, 'Hype Delivered');
        } catch {
            //ignored
        } finally {
            setTimeout(() => {
                setReactionSent(false);
            }, 2000);
        }
    };

    const handleOpenAthlete = (athleteId: string, name: string, initials: string, avatarUrl?: string | null) => {
        const fallbackAthlete: ClashAthlete = {
            id: athleteId,
            name,
            initials,
            avatarUrl: avatarUrl || undefined,
            code: '',
            gender: 'male',
            bodyweightKg: 0,
            squat1RM: 0,
            bench1RM: 0,
            deadlift1RM: 0,
            totalE1RM: 0,
            dotsScore: 0,
            tier: 'Unranked',
            tierLevel: 1,
            rankTrend: 0,
            weeklyVolumeKg: 0,
            lastWorkoutDate: new Date().toISOString(),
            muscleBalance30d: { Chest: 0, Core: 0, Shoulders: 0, Arms: 0, Legs: 0, Back: 0 },
            trophies: [],
            recentWorkouts: [],
        };
        setSelectedAthlete(fallbackAthlete);
        setIsDrawerOpen(true);
    };

    if (isLoading && !duel) {
        return (
            <div className="min-h-screen bg-background text-foreground flex items-center justify-center">
                <Loader2 className="w-8 h-8 animate-spin text-brand"/>
            </div>
        );
    }
    if (!duel) {
        return (
            <div className="min-h-screen bg-background text-foreground flex flex-col items-center justify-center p-4">
                <h2 className="font-display text-2xl mb-2">Duel Not Found</h2>
                <Link to="/clash/duels" className="text-xs text-brand font-bold uppercase tracking-wider">
                    Back to 1v1 Duels
                </Link>
            </div>
        );
    }
    
    const m = getDuelMatchupState(duel, user?.id, user?.name, renderTimestamp);
    const isVolume = duel.targetType?.toLowerCase().includes('volume');

    const userPill = m.isWinning ? 'bg-brand-fill border border-brand/30 text-brand' : 'bg-surface-2 border border-border text-muted-foreground';

    const rivalPill = !m.isWinning && !m.isTied ? 'bg-brand-fill border border-brand/30 text-brand' : 'bg-surface-2 border border-border text-muted-foreground';

    return (
        <div className="min-h-screen bg-background text-foreground pb-20">
            <div className="max-w-4xl mx-auto px-4 pt-8 pb-4">
                <Link to="/clash/duels" className="inline-flex items-center gap-2 text-xs font-bold text-muted-foreground hover:text-brand uppercase tracking-[1px] mb-4 transition font-sans">
                    <ArrowLeft className="w-4 h-4"/> Back to 1v1 Duels
                </Link>

                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                        <div className="flex items-center gap-2">
                            <span className="px-2.5 py-0.5 rounded-full text-[11px] font-sans font-bold bg-brand-fill text-brand border border-brand/40 flex items-center gap-1 uppercase tracking-[1px]">
                                <Swords className="w-3.5 h-3.5"/> 1V1 HEAD-TO-HEAD DUEL
                            </span>
                            <span className="text-xs text-muted-foreground font-sans flex items-center gap-1 font-semibold">
                                <Clock className="w-3.5 h-3.5 text-warning"/> {m.endsInText} remaining
                            </span>
                        </div>
                        <h1 className="font-display text-3xl md:text-4xl tracking-wide text-foreground mt-1.5">{duel.title}</h1>
                        <p className="text-xs text-muted-foreground mt-1 font-sans">
                            Target Exercise: <strong className="text-foreground">{duel.exerciseName}</strong> - Evaluated on <strong>{isVolume ? 'Total Volume (kg)' : '% e1RM Overload'}</strong>
                        </p>
                    </div>
                </div>
            </div>
            <div className="max-w-4xl mx-auto px-4 py-4 space-y-6">
                {/* hv card */}
                <Card className="bg-surface border-border p-6 md:p-8 shadow-sm overflow-hidden">
                    <CardContent className="p-0">
                    <div className="grid grid-cols-3 items-center text-center gap-2 md:gap-4 relative z-10">
                        <div className="flex flex-col items-center">
                        <button type="button" onClick={() => handleOpenAthlete(user?.id || '', user?.name || 'You', m.userInitials)}
                        className="w-16 h-16 md:w-20 md:h-20 rounded-2xl bg-brand text-primary-foreground flex items-center justify-center font-display text-2xl md:text-3xl font-bold shadow-md hover:opacity-90 transition cursor-pointer"
                        aria-label="Inspect Your Profile">
                            {m.userInitials}
                        </button>
                        <strong className="font-sans text-base md:text-lg font-bold text-foreground mt-2 block">
                            {user?.name || 'You'} (You)
                        </strong>
                        <span className="text-xs text-muted-foreground font-sans">
                            {duel.exerciseName}
                        </span>
                        <div className={`mt-2 px-3 py-1 rounded-full font-sans font-bold text-sm ${userPill}`}>
                            {m.userDisplayMetric}
                        </div>
                    </div>

                    {/* vs center ding */}
                    <div className="flex flex-col items-center justify-center">
                        <div className="w-12 h-12 rounded-full bg-surface-2 border border-border flex items-center justify-center text-muted-foreground font-display text-xl font-bold">
                            VS
                        </div>
                        <span className="text-[11px] font-sans font-bold text-muted-foreground uppercase tracking-[1px] mt-2">
                            OVERLOAD DUEL
                        </span>
                    </div>

                    <div className="flex flex-col items-center">
                        <button type="button" onClick={() => handleOpenAthlete(m.rivalId, m.rivalName, m.rivalInitials)}
                        className="w-16 h-16 md:w-20 md:h-20 rounded-2xl bg-surface-2 border border-border text-foreground flex items-center justify-center font-display text-2xl md:text-3xl font-bold shadow-sm hover:opacity-90 transition cursor-pointer"
                        aria-label={`Inspect ${m.rivalName}'s Profile`}>
                            {m.rivalInitials}
                        </button>
                        <strong className="font-sans text-base md:text-lg font-bold text-foreground mt-2 block">
                            {m.rivalName}
                        </strong>
                        <span className="text-xs text-muted-foreground font-sans">
                            Opponent
                        </span>
                        <div className={`mt-2 px-3 py-1 rounded-full font-sans font-bold text-sm ${rivalPill}`}>
                            {m.rivalDisplayMetric}
                        </div>
                    </div>
                    </div>

                    <div className="mt-8 pt-6 border-t border-border">
                        <div className="flex justify-between text-xs font-sans mb-2 font-bold">
                            <span className={m.isWinning ? 'text-brand' : 'text-muted-foreground'}>
                                {m.userProgressPercent}% Progress (You)
                            </span>
                            <span className="text-foreground">{m.leadText}</span>
                            <span className={!m.isWinning && !m.isTied ? 'text-brand' : 'text-muted-foreground'}>
                                {m.rivalProgressPercent}% Progress ({m.rivalFirstName})
                            </span>
                        </div>

                        <div className="h-4 w-full bg-surface-2 rounded-full overflow-hidden flex border border-border p-0.5">
                            <div className={`${m.isWinning ? 'bg-brand' : 'bg-muted-foreground/30'} rounded-l-full transition-all duration-500`}
                                style={{ width: `${m.userProgressPercent}%` }} />
                            <div className={`${!m.isWinning && !m.isTied ? 'bg-brand' : 'bg-muted-foreground/30'} rounded-r-full transition-all duration-500`}
                                style={{ width: `${m.rivalProgressPercent}%` }} />
                        </div>

                        <div className="mt-5 flex flex-wrap items-center justify-center gap-3">
                            <Button variant="secondary" size="sm" onClick={handleSendReaction} disabled={reactionSent}
                                className="flex items-center gap-1.5">
                                <Sparkles className="w-4 h-4 text-warning" />
                                <span>{reactionSent ? 'Reaction Sent' : 'Send Hype Reaction'}</span>
                            </Button>
                            <Button variant="outline" size="sm" onClick={() => handleOpenAthlete(m.rivalId, m.rivalName, m.rivalInitials)}
                                className="flex items-center gap-1.5 border-border">
                                <span>Inspect Rival Profile</span>
                            </Button>
                        </div>
                    </div>
                </CardContent>
            </Card>

            <Card className="bg-surface border-border p-6 shadow-sm">
                <CardContent className="p-0">
                    <h3 className="font-display text-xl tracking-wide text-foreground flex items-center gap-2 mb-4">
                        Live Duel Timeline
                    </h3>
                    {timelineEvents.length > 0 ? (
                            <div className="space-y-4 relative before:absolute before:inset-0 before:left-3.5 before:w-0.5 before:bg-border">
                    {timelineEvents.map((evt) => {
                        const isUserEvent = evt.userId === user?.id || (user?.name && evt.userName?.includes(user.name));
                        const dotColor = evt.isPr ? 'bg-brand border-brand ring-4 ring-brand/20' : 'bg-surface border-border';
                        const nameColor = isUserEvent ? 'text-brand' : 'text-foreground';
                        const eventTime = evt.createdAt ? new Date(evt.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : 'Just now';

                        return (
                            <div key={evt.id} className="flex items-start gap-4 relative pl-8">
                                <div className={`absolute left-2 top-1.5 w-3.5 h-3.5 rounded-full border-2 ${dotColor}`} />

                                <div className="flex-1 bg-surface-2 border border-border rounded-xl p-3.5">
                                    <div className="flex items-center justify-between text-xs font-sans">
                                        <strong className={`font-bold ${nameColor}`}>
                                            {evt.userName || (isUserEvent ? 'You' : 'Opponent')}
                                        </strong>
                                        <span className="text-muted-foreground text-[11px]">
                                            {eventTime}
                                        </span>
                                    </div>
                                    <p className="text-xs text-foreground mt-1 font-sans">
                                        {evt.eventText}
                                    </p>
                                </div>
                            </div>
                        );
                    })}
                </div>
                ) : (
                <div className="text-center py-6 text-xs text-muted-foreground font-sans">
                    No sets logged for this duel yet.
                </div>
                )}
            </CardContent>
        </Card>
            </div >

            <ProfileInspectorDrawer athlete={selectedAthlete} isOpen={isDrawerOpen} onClose={() => setIsDrawerOpen(false)}
                onChallengeDuel={() => {
                    setIsDrawerOpen(false);
                    setIsDuelModalOpen(true);
                }} />
            <CreateDuelModal isOpen={isDuelModalOpen} onClose={() => setIsDuelModalOpen(false)} defaultFriend={selectedAthlete} />
        </div >
    );

}