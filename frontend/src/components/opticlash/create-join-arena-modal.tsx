import { customFetch } from "@/lib/custom-fetch";
import { useState, type FormEvent } from "react";
import { toast } from "../ui/alert";
import { useNavigate } from "react-router-dom";
import { Button } from "../ui/button";
import { Card } from "../ui/card";
import { KeyRound, Loader2, PlusCircle, Users, X } from "lucide-react";
import { Input } from "../ui/input";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu";

const ARENA_METRIC_OPTIONS = [
    {value: 'DOTSOverall', label: 'DOTS Strength-to-Weight (Recommended)'},
    { value: 'TotalVolume', label: 'Total Session Tonnage (Volume kg)'},
    { value: 'SquatE1RM', label: 'Squat e1RM Ranking'},
    { value: 'BenchE1RM', label: 'Bench Press e1RM Ranking'},
    { value: 'DeadliftE1RM', label: 'Deadlift e1RM Ranking'},
] as const;
const ARENA_DURATION_OPTIONS = [
    { value: '30', label: '30 Days (Full Calendar Season)'},
    { value: '365', label: 'Annual Season (1 Year)'},
] as const;

interface CreateJoinArenaModalProps {
    isOpen: boolean;
    onClose: () => void;
    onSuccess?: (arenaId: string) => void;
}

export function CreateJoinArenaModal({
    isOpen, onClose, onSuccess
}: Readonly<CreateJoinArenaModalProps>){
    const [tab, setTab] = useState<'join' | 'create'>('join');
    const [joinCode, setJoinCode] = useState('');
    const [error, setError] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const navigate = useNavigate();
    const [arenaName, setArenaName] = useState('');
    const [metric, setMetric] = useState<string>(ARENA_METRIC_OPTIONS[0].value);
    const [duration, setDuration] = useState<string>(ARENA_DURATION_OPTIONS[0].value);

    if (!isOpen){
        return null;
    }

    const handleJoin = async (e: FormEvent) => {
        e.preventDefault();
        const cleanCode = joinCode.trim().toUpperCase();
        if (!cleanCode){
            setError('Please enter a 6 character arena code');
            return;
        }
        if (cleanCode.length < 6) {
            setError('Arena code must be 6 characters');
            return;
        }

        setError('');
        setIsSubmitting(true);

        try {
            const res = await customFetch('/api/clash/arenas/join', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    code: cleanCode
                }),
            });
            const data = await res.json().catch(() => null);
            if (res.ok && data?.success) {
                toast.success(data.message ?? 'Joined squad successfully');
                const targetId = data.arena?.id ?? cleanCode;
                onSuccess?.(targetId);
                onClose();
                navigate(`/clash/${targetId}`);
            } else {
                setError(data?.message ?? 'Arena not found or you are already a member');
            }
        } catch {
            setError('Error connecting to arena hub');
        } finally {
            setIsSubmitting(false);
        }
    };

    const handleCreate = async (e: FormEvent) => {
        e.preventDefault();
        const cleanName = arenaName.trim();
        if (!cleanName) {
            setError('Please provide an arena name');
            return;
        }
        setError('');
        setIsSubmitting(true);

        try {
            const res = await customFetch('/api/clash/arenas', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    name: cleanName,
                    metricType: metric,
                    durationDays: Number.parseInt(duration, 10) || 30,
                }),
            });
            const data = await res.json().catch(() => null);
            if (res.ok && data?.success) {
                toast.success(data.message ?? 'Arena created successfully');
                const targetId = data.arena?.id;
                if (targetId){
                    onSuccess?.(targetId);
                    onClose();
                    navigate(`/clash/${targetId}`);
                } else {
                    onClose();
                }
            } else {
                setError(data?.message ?? 'Failed to create arena');
            }
        } catch {
            setError('Error creating arena');
        } finally {
            setIsSubmitting(false);
        }
    };

    const selectedMetricobj = ARENA_METRIC_OPTIONS.find((m) => m.value === metric) || ARENA_METRIC_OPTIONS[0];
    const selectedDurobj = ARENA_DURATION_OPTIONS.find((d) => d.value === duration) || ARENA_DURATION_OPTIONS[0];

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
            <Card className="bg-surface border-border max-w-md w-full rounded-2xl shadow-2xl relative animate-in zoom-in-95 duration-150 p-6 space-y-5">
                <div className="flex items-center justify-between border-b border-border pb-4">
                    <div>
                        <h3 className="font-display text-2xl tracking-wide text-foreground flex items-center gap-2.5">
                            <Users className="w-6 h-6" />
                            <span>Gym Arenas</span>
                        </h3>
                    </div>
                    <Button variant="ghost" size="icon" onClick={onClose}
                        className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-full" aria-label="Close" disabled={isSubmitting}>
                        <X className="w-4 h-4 hover:text-foreground" />
                    </Button>
                </div>                    

                    {/* tab switch */}
                    <div className="flex bg-surface-2 border border-border rounded-lg p-1">
                        <button type="button" disabled={isSubmitting} onClick={() => {
                            setTab('join');
                            setError('');
                        }}
                        className={`flex-1 py-1.5 text-xs font-bold uppercase tracking-[1px] rounded-md transition ${
                            tab === 'join' ? 'bg-surface text-foreground shadow-sm font-semibold' : 'text-muted-foreground hover:text-foreground'
                        }`}>
                            Join with code
                        </button>
                        <button type="button" disabled={isSubmitting} onClick={() => {
                            setTab('create');
                            setError('');
                        }}
                        className={`flex-1 py-1.5 text-xs font-bold uppercase tracking-[1px] rounded-md transition ${
                            tab === 'create' ? 'bg-surface text-foreground shadow-sm font-semibold' : 'text-muted-foreground hover:text-foreground'
                        }`}>
                            Create New Arena
                        </button>
                    </div>
                    {/* // */}
                    {error && (
                        <div className="mb-4 p-3 bg-brand-fill border border-brand/40 rounded-lg text-xs text-brand font-medium">
                            {error}
                        </div>
                    )}

                    {tab === 'join' ? (
                        <form onSubmit={handleJoin} className="space-y-4">
                            <div>
                                <label htmlFor="join-code-input" className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">
                                    Private Arena Invite Code
                                </label>
                                <div className="relative">
                                    <KeyRound className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"/>
                                    <Input id="join-code-input" type="text" value={joinCode} onChange={(e) => setJoinCode(e.target.value.toUpperCase())} 
                                    placeholder="e.g. IRON99" maxLength={10} disabled={isSubmitting}
                                    className="pl-9 font-mono tracking-widest uppercase bg-surface-2 border-border"/>
                                </div>
                                <p className="text-[11px] text-muted-foreground mt-1.5">
                                    Ask your arena leader or friend for their code.
                                </p>
                            </div>

                            <Button type="submit" variant="default" disabled={isSubmitting}
                            className="w-full flex items-center justify-center gap-1.5">
                                {isSubmitting ? (
                                    <>
                                    <Loader2 className="w-4 h-4 animate-spin"/>
                                    <span>Joining...</span>
                                    </>
                                    ): (
                                        <span>Join Arena</span>
                                    )}
                            </Button>
                        </form>
                    ): (
                        <form onSubmit={handleCreate} className="space-y-4">
                            <div>
                                <label htmlFor="arena-name-input" className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">Arena Name</label>
                                <Input id="arena-name-input" type="text" value={arenaName} onChange={(e) => setArenaName(e.target.value)}
                                placeholder="e.g. Hazelwood Overloaders" disabled={isSubmitting} 
                                className="bg-surface-2 border-border"/>
                            </div>

                            <div>
                                <span className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">
                                    Primary Ranking Metric
                                </span>
                                <DropdownMenu>
                                    <DropdownMenuTrigger variant="filter" className="w-full bg-surface-2" aria-label="Primary Rankign Metric">
                                        <span className="min-w-0 truncate">
                                            {selectedMetricobj.label}
                                        </span>
                                    </DropdownMenuTrigger>
                                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                                        {ARENA_METRIC_OPTIONS.map((m) => (
                                            <DropdownMenuItem key={m.value} onSelect={() => setMetric(m.value)} className="cursor-pointer">
                                                {m.label}
                                            </DropdownMenuItem>
                                        ))}
                                    </DropdownMenuContent>
                                </DropdownMenu>
                            </div>

                            <div>
                                <span className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">
                                    Duration
                                </span>
                                <DropdownMenu>
                                    <DropdownMenuTrigger variant="filter" disabled={isSubmitting}
                                    className="w-full bg-surface-2" aria-label="Arena Duration">
                                        <span className="min-w-0 truncate">{selectedDurobj.label}</span>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                                            {ARENA_DURATION_OPTIONS.map((d) => (
                                                <DropdownMenuItem key={d.value} onSelect={() => setDuration(d.value)} className="cursor-pointer">
                                                    {d.label}
                                                </DropdownMenuItem>
                                            ))}
                                        </DropdownMenuContent>
                                </DropdownMenu>
                            </div>

                            <Button type="submit" variant="default" disabled={isSubmitting} className="w-full flex items-center justify-center gap-1.5">
                                {isSubmitting ? (
                                    <>
                                        <Loader2 className="w-4 h-4 animate-spin"/>
                                        <span>Creating...</span>
                                    </>
                                    ) :(
                                        <>
                                            <PlusCircle className="w-4 h-4"/>
                                            <span>Create Arena</span>
                                        </>
                                    )}
                            </Button>
                        </form>
                    )}
            </Card>
        </div>
    );
}