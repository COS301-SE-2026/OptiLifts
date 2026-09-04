import { Button } from "@/components/ui/button";
import { Loader2, ArrowRight, Calendar, X } from "lucide-react";
import { useEffect } from "react";

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
    droppedItems?: readonly RescheduledItem[];
    onConfirm: () => Promise<void>;
    isConfirming: boolean;
    selectedMissedIds?: readonly string[]; 
}

export function ReschedulePreviewModal({
    isOpen, onClose, proposedItems, droppedItems, onConfirm, isConfirming, selectedMissedIds
}: Readonly<ReschedulePreviewModalProps>){
    //added keyboard accessibility
    useEffect(()=> {
        if (!isOpen) return;
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape") onClose();
        };
        window.addEventListener("keydown", handleKeyDown);
        return () => window.removeEventListener("keydown", handleKeyDown);
    }, [isOpen, onClose]);
    
    if (!isOpen){
        return null;
    }
    const formatDate = (iso: string) => {
        const d = new Date(iso);
        return d.toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-3 sm:p-4 animate-in fade-in duration-200">
            <button type="button" tabIndex={-1} aria-label="Close modal backdrop" onClick={onClose} className="fixed inset-0 cursor-default bg-transparent border-none p-0 w-full h-full"/>
            <div className="relative z-10 w-full max-w-2xl bg-surface border border-border rounded-2xl shadow-2xl p-4 sm:p-6 space-y-4 sm:space-y-6 max-h-[90vh] flex flex-col animate-in zoom-in-95 duration-200 font-sans overflow-hidden">
                <div className="flex items-center justify-between border-b border-border pb-3">
                    <div>
                        <h3 className="text-xl font-bold font-display uppercase tracking-wider text-foreground flex items-center gap-2">
                            <Calendar size={20} className="text-brand" />
                            Proposed Schedule Comparison
                        </h3>
                        <p className="text-sm text-muted-foreground mt-1">Review suggested workout schedule changes.</p>
                    </div>
                    <button type="button" onClick={onClose} aria-label="Close modal" className="text-muted-foreground hover:text-foreground cursor-pointer focus-visible:ring-2 focus-visible:ring-brand rounded-lg p-1">
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
                                
                            <div key={item.entryId} className="flex flex-col sm:grid sm:grid-cols-12 items-start sm:items-center gap-2.5 sm:gap-3 p-3.5 sm:p-4 bg-surface-2/40 border border-border rounded-xl text-sm">
                                <div className="sm:col-span-4 font-bold text-foreground text-sm flex items-center gap-1.5 min-w-0 w-full">
                                    <span className="truncate">{item.workoutName}</span>
                                    <span className={`text-xs font semibold shrink-0 ${isMissed ? 'text-warning' : 'text-brand'}`}>
                                        {isMissed ? '(Missed)' : '(Scheduled)'}
                                    </span>
                                    </div>
                                <div className="w-full sm:col-span-8 flex items-center justify-between sm:grid sm:grid-cols-8 gap-2 border-t sm:border-t-0 pt-2 sm:pt-0 border-border/50">
                                <div className="sm:col-span-3 text-muted-foreground text-xs sm:text-sm">
                                    <span className="block text-[10px] sm:text-[11px] uppercase font-extrabold tracking-wider text-muted-foreground/70">Current</span>
                                    {formatDate(item.originalScheduledAt)}
                                    </div>

                                <div className="sm:col-span-1 flex justify-center font-bold text-brand shrink-0">
                                    <ArrowRight size={18}/>
                                </div>
                                <div className="sm:col-span-4 font-semibold text-brand text-xs sm:text-sm text-right sm:text-left">
                                        <span className="block text-[10px] sm:text-[11px] uppercase font-extrabold tracking-wider text-brand/80">Proposed</span>
                                        {formatDate(item.newScheduledAt)}
                                    </div>
                                </div>
                                </div>
                            );
                        })
                    )}
                    {/* dropped entries */}
                    {droppedItems && droppedItems.length > 0 && (
                        <div className="pt-4 border-t border-border space-y-2">
                            <h4 className="text-xs sm:text-sm font-bold uppercase tracking-wider text-destructive flex items-center gap-1.5">Dropped Workouts ({droppedItems.length})</h4>
                            <p className="text-xs text-muted-foreground">These workouts could not be scheduled into the cycle window given your preferences:</p>
                            <div className="space-y-2">
                                {droppedItems.map((item) => (
                                    <div key={item.entryId} className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-3 bg-destructive/10 border border-destructive/20 rounded-xl text-sm">
                                        <div className="flex flex-col gap-0.5 min-w-0">
                                        <span className="font-bold text-foreground">{item.workoutName}</span>
                                        {item.originalScheduledAt && (
                                            <span className="text-xs text-muted-foreground flex items-center gap-1">
                                                <span className="font-medium text-muted-foreground/80">Originally:</span>{formatDate(item.originalScheduledAt)}
                                            </span>
                                        )}
                                        </div>
                                        <span className="text-xs font-semibold text-destructive px-2 py-0.5 bg-destructive/20 rounded-md">Dropped</span>
                                    </div>
                                ))}
                            </div>
                        </div>
                    )}
                </div>
                <div className="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-2.5 sm:gap-3 pt-3 sm:pt-4 border-t border-border shrink-0">
                    <Button type="button" variant="secondary" onClick={onClose} disabled={isConfirming} className="w-full sm:w-auto">Keep Current Schedule</Button>
                    {proposedItems.length > 0 && (
                        <Button type="button" onClick={onConfirm} disabled={isConfirming} className="w-full sm:w-auto">
                        {isConfirming ? <Loader2 className="animate-spin mr-2" size={16}/>: null}
                        Accept Proposed Schedule
                    </Button>
                    )}
                </div>
            </div>
        </div>
    );
}