import { customFetch } from "@/lib/custom-fetch";
import { useEffect, useState, type SyntheticEvent } from "react";
import { toast } from "../ui/alert";
import { Check, Swords, X, Activity, Calendar, Loader2 } from "lucide-react";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";
import { Button } from "../ui/button";
import { Card } from "../ui/card";

const EXERCISE_OPTIONS = [
    'Barbell Back Squat',
    'Barbell Bench Press',
    'Barbell Deadlift',
    'All Compound Lifts Combined',
] as const;

interface FriendOption {
    id: string;
    name: string;
    dotsScore?: number;
    tier?: string;
}
interface CreateDuelModalProps {
    isOpen: boolean;
    onClose: () => void;
    defaultFriend?: { //for adding from profile
        id: string;
        name: string;
        dotsScore?: number
    } | null;
    onDuelCreated?: (result?: unknown) => void;
}

export function CreateDuelModal({
    isOpen, onClose, defaultFriend, onDuelCreated,
}:Readonly<CreateDuelModalProps>){
    const [friends, setFriends] = useState<FriendOption[]>([]);
    const [loadingFriends, setLoadingFriends] = useState<boolean>(false);
    const [selectedFriendId, setSelectedFriendId] = useState<string>('');

    const [exercise, setExercise] = useState<string>('Barbell Back Squat');
    const [targetType, setTargetType] = useState<'E1RMGainPercent' | 'TotalVolumeKg'>('E1RMGainPercent');
    const [durationDays, setDurationDays] = useState<number>(7);

    const [submitted, setSubmitted] = useState<boolean>(false);
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    useEffect(() => {
        if (!isOpen) {
            return;
        }

        const fetchFriends = async () => {
            setLoadingFriends(true);
            try {
                const res = await customFetch('/api/clash/friends');
                if (res.ok) {
                    const data: FriendOption[] = await res.json();
                    setFriends(data);
                    if (defaultFriend?.id){
                        setSelectedFriendId(defaultFriend.id);
                    } else if (data.length > 0) {
                        setSelectedFriendId(data[0].id);
                    }
                }
            } catch {
                //fallback gracefully
            } finally {
                setLoadingFriends(false);
            }
        };
        void fetchFriends();
    }, [isOpen, defaultFriend]);

    if (!isOpen){
        return null;
    }
    const selectedFriend = friends.find((f) => f.id === selectedFriendId) || defaultFriend;
    
    const getSelectedFriendLabel = ()=> {
        if (loadingFriends) {
            return 'Loading friends...';
        }
        if (!selectedFriend) {
            return 'Select a friend';
        }
        const dotsPart = selectedFriend.dotsScore ? ` (${selectedFriend.dotsScore} DOTS)` : '';
        return `${selectedFriend.name}${dotsPart}`;
    }
    const handleSubmit = async (e: SyntheticEvent) => {
        e.preventDefault();
        if (!selectedFriendId) {
            toast.error('Please select a friend to challenge');
            return;
        }
        setIsSubmitting(true);
        try {
            const res = await customFetch('/api/clash/duels', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    rivalUserId: selectedFriendId,
                    exerciseName: exercise,
                    exerciseId: null,
                    targetType,
                    durationDays,
                }),
            });
            const data = await res.json().catch(() => null);
            if (res.ok && data?.success) {
                setSubmitted(true);
                if (onDuelCreated) {
                    onDuelCreated(data);
                }
                setTimeout(() => {
                    setSubmitted(false);
                    onClose();
                }, 1200);
            } else {
                toast.error(data?.message || 'Failed to send duel challenge', 'Challenge Error');
            }
        } catch {
            toast.error('Network error creating duel challenge');
        } finally {
            setIsSubmitting(false);
        }
    };


    const renderFriendDropdown = () => {
        if (friends.length === 0 && !loadingFriends) {
            return (
                <div className="p-2 text-xs text-muted-foreground text-center">
                    No friends available to challenge to a duel.
                </div>
            );
        }
        return friends.map((f) => (
            <DropdownMenuItem key={f.id} onSelect={() => setSelectedFriendId(f.id)} className="cursor-pointer">
                {f.name} {f.dotsScore ? `(${f.dotsScore} DOTS)` : ''}
            </DropdownMenuItem>
        ));
    };

    const renderDuration = () => [7,14,30].map((days) => {
        return (
            <button key={days} type="button" onClick={() => setDurationDays(days)}
                className={`flex-1 py-2 text-xs font-bold uppercase tracking-[1px] rounded-lg border transition ${durationDays === days ? 'border-brand bg-brand-fill text-brand font-bold' : 'border-border bg-surface-2 text-foreground'
                    }`}>
                {days} Days
            </button>
        )

    })

    const handleModalClose = () => {
        setSubmitted(false);
        setIsSubmitting(false);
        onClose();
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
            <Card className="bg-surface border-border max-w-md w-full overflow-hidden shadow-2xl relative animate-in zoom-in-95 duration-150 p-0">
                <Button variant="ghost" size="icon" onClick={handleModalClose}
                className="absolute top-4 right-4 h-8 w-8 z-10" aria-label="Close">
                    <X className="w-4 h-4 text-muted-foreground hover:text-foreground"/>
                </Button>

                {/* modal header */}
                <div className="p-6 pb-4 border-b border-border bg-surface-2">
                    <div className="w-12 h-12 rounded-xl bg-brand-fill border border-brand/30 text-brand flex items-center justify-center mb-3">
                        <Swords className="w-6 h-6"/>
                    </div>
                    <h3 className="font-display text-2xl tracking-wide text-foreground">
                        Challenge Friend to 1v1 Duel
                    </h3>
                    <p className="text-xs text-muted-foreground mt-0.5 font-sans">
                        Start a progressive overload duel. The duel will begin when your friend accepts your invitation.
                    </p>
                </div>

                {/* modal body */}
                <form onSubmit={handleSubmit} className="p-6 space-y-4 bg-surface">
                    {submitted ? (
                        <div className="p-6 text-center space-y-2">
                            <div className="w-12 h-12 rounded-full bg-success/10 text-success border border-success/30 flex items-center justify-center mx-auto">
                                <Check className="w-6 h-6"/>
                            </div>
                            <h4 className="font-sans font-bold text-base text-foreground">
                                Duel Invite Sent
                            </h4>
                            <p className="text-xs text-muted-foreground">
                                Your friend will be notified. Once accepted, your {durationDays} day battle will begin.
                            </p>
                        </div>
                        ) : (
                            <>
                            {/* select yo friend */}
                            <div>
                                <label htmlFor="duel-friend-trigger" className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">
                                    Select Friend
                                </label>
                                <DropdownMenu>
                                    <DropdownMenuTrigger id="duel-friend-trigger" variant="filter" className="w-full bg-surface-2" aria-label="Select Friend">
                                        <span className="min-w-0 truncate">
                                            {getSelectedFriendLabel()}
                                        </span>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)] max-h-60 overflow-y-auto">
                                        {renderFriendDropdown()}
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            </div>
                            {/* metric type */}
                            <div>
                                <span className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">
                                    Duel Metric Target
                                </span>
                                <div className="grid grid-cols-2 gap-2">
                                    <button type="button" onClick={() => setTargetType('E1RMGainPercent')} 
                                    className={`p-3 rounded-lg border text-left transition ${
                                        targetType === 'E1RMGainPercent' ? 'border-brand bg-brand-fill text-brand font-bold' : 'border-border bg-surface-2 text-foreground'}`}>
                                            <span className="block text-xs font-bold uppercase">% e1RM Overload</span>
                                            <span className="text-[11px] text-muted-foreground font-normal">Relative strength gain</span>
                                        </button>

                                    <button type="button" onClick={() => setTargetType('TotalVolumeKg')} 
                                    className={`p-3 rounded-lg border text-left transition ${
                                        targetType === 'TotalVolumeKg' ? 'border-brand bg-brand-fill text-brand font-bold' : 'border-border bg-surface-2 text-foreground'}`}>
                                            <span className="block text-xs font-bold uppercase">Total Volume (kg)</span>
                                            <span className="text-[11px] text-muted-foreground font-normal">Accumulated tonnage</span>
                                        </button>
                                </div>
                            </div>

                            {/* target exercise */}
                            <div>
                                <label htmlFor="duel-exercise-trigger"
                                className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5 flex items-center gap-1.5">
                                    <Activity className="w-3.5 h-3.5 text-brand"/> Target Exercise
                                </label>
                                <DropdownMenu>
                                    <DropdownMenuTrigger id="duel-exercise-trigger" variant="filter" className="w-full bg-surface-2" aria-label="Target Exercise">
                                        <span className="min-w-0 truncate">{exercise}</span>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)] max-h-60 overflow-y-auto">
                                        {EXERCISE_OPTIONS.map((ex) => (
                                            <DropdownMenuItem key={ex} onSelect={() => setExercise(ex)} className="cursor-pointer">
                                                {ex}
                                            </DropdownMenuItem>
                                            ))}
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            </div>

                            {/* duration */}
                            <div>
                                <span className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5 flex items-center gap-1.5">
                                    <Calendar className="w-3.5 h-3.5 text-brand" /> Duel Duration
                                </span>
                                <div className="flex gap-2">
                                    {renderDuration()}
                                </div>
                            </div>
                            <Button type="submit" variant="default" disabled={isSubmitting || !selectedFriendId} className="w-full mt-2">
                                {isSubmitting ? (
                                    <span className="flex items-center gap-2">
                                        <Loader2 className="w-4 h-4 animate-spin"/>
                                        Sending challenge...
                                    </span>
                                    ) : (
                                        'Send Challenge'
                                    )}
                            </Button>
                            </>
                        )}
                </form>
            </Card>
        </div>
    );
}