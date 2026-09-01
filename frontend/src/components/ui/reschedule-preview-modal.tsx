import { Button } from "@/components/ui/button";
import { Loader2, ArrowRight, Calendar, X } from "lucide-react";

export interface RescheduledItem {
    entryId: string;
    workoutId: string;
    workoutName: string;
    originalScheduledAt: string;
    newScheduledAt: string;
    action: string;
}
interface ReschedulePreviewModalProps {
    isOpen: boolean;
    onClose: () => void;
    proposedItems: readonly RescheduledItem[];
    onConfirm: () => Promise<void>;
    isConfirming: boolean;
    selectedMissedIds?: readonly string[]; 
}

export function ReschedulePreviewModal({
    isOpen, onClose, proposedItems, onConfirm, isConfirming, selectedMissedIds
}: Readonly<ReschedulePreviewModalProps>){
    if (!isOpen){
        return null;
    }
    const formatDate = (iso: string) => {
        const d = new Date(iso);
        return d.toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <button type="button" aria-label="Close modal backdrop" onClick={onClose} className="fixed inset-0 cursor-default bg-transparent border-none p-0 w-full h-full"/>
            <div className="relative z-10 w-full max-w-2xl bg-surface border border-border rounded-2xl shadow-2xl p-6 space-y-6 animate-in zoom-in-95 duration-200 font-sans">
                <div className="flex items-center justify-between border-b border-border pb-3">
                    <div>
                        <h3 className="text-xl font-bold font-display uppercase tracking-wider text-foreground flex items-center gap-2">
                            <Calendar size={20} className="text-brand" />
                            Proposed Schedule Comparison
                        </h3>
                        <p className="text-sm text-muted-foreground mt-1">Review suggested workout schedule changes.</p>
                    </div>
                    <button type="button" onClick={onClose} className="text-muted-foreground hover:text-foreground cursor-pointer">
                        <X size={20}/>
                    </button>
                </div>
                <div className="max-h-[50vh] overflow-y-auto space-y-3 pr-1">
                    {proposedItems.length === 0 ? (
                        <div className="p-6 text-center bg-warning/10 border border-warning/30 rounded-xl space-y-2">
                            <h4 className="font-bold text-sm text-warning">Unable to reschedule workouts</h4>
                            <p className="text-xs text-muted-foreground leading-relaxed">No valid schedule changes could be found within your current cycle window. Try increasing your daily workout limit or adjusting your fixed rest days in Schedule Settings</p>
                            </div>
                    ):(
                        [...proposedItems].sort((a,b)=> new Date(a.newScheduledAt).getTime() - new Date(b.newScheduledAt).getTime())
                        .map((item)=>{
                            const isMissed = selectedMissedIds?.includes(item.entryId) || item.action.toLowerCase().includes("missed");
                            return (
                                
                            <div key={item.entryId} className="grid grid-cols-12 items-center gap-3 p-4 bg-surface-2/40 border border-border rounded-xl text-sm">
                                <div className="col-span-4 font-bold text-foreground text-sm truncate flex items-center gap-1.5">
                                    <span className="truncate">{item.workoutName}</span>
                                    <span className={`text-xs font semibold shrink-0 ${isMissed ? 'text-warning' : 'text-brand'}`}>
                                        {isMissed ? '(Missed)' : '(Scheduled)'}
                                    </span>
                                    </div>
                                <div className="col-span-3 text-muted-foreground">
                                    <span className="block text-[11px] uppercase font-extrabold tracking-wider text-muted-foreground/70">Current</span>
                                    {formatDate(item.originalScheduledAt)}
                                </div>
                                <div className="col-span-1 flex justify-center font-bold text-brand">
                                    <ArrowRight size={18}/>
                                </div>
                                <div className="col-span-4 font-semibold text-brand">
                                        <span className="block text-[11px] uppercase font-extrabold tracking-wider text-brand/80">Proposed</span>
                                        {formatDate(item.newScheduledAt)}
                                    </div>
                                </div>
                            );
                        })
                    )}
                </div>
                <div className="flex items-center justify-end gap-3 pt-4 border-t border-border">
                    <Button type="button" variant="secondary" onClick={onClose} disabled={isConfirming}>Keep Current Schedule</Button>
                    {proposedItems.length > 0 && (
                        <Button type="button" onClick={onConfirm} disabled={isConfirming}>
                        {isConfirming ? <Loader2 className="animate-spin mr-2" size={16}/>: null}
                        Accept Proposed Schedule
                    </Button>
                    )}
                </div>
            </div>
        </div>
    );
}