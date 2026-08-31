import { Button } from "@/components/ui/button";
import { Loader2, ArrowRight, Calendar } from "lucide-react";

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
}

export function ReschedulePreviewModal({
    isOpen, onClose, proposedItems, onConfirm, isConfirming,
}: ReschedulePreviewModalProps){
    if (!isOpen){
        return null;
    }
    const formatDate = (iso: string) => {
        const d = new Date(iso);
        return d.toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric", hour: "2-digit", minute: "2-digit" });
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4 animate-in fade-in duration-200">
            <div className="relative z-10 w-full max-w-2xl bg-surface border border-border rounded-2xl shadow-2xl p-6 space-y-6 animate-in zoom-in-95 duration-200 font-sans">
                <div className="border-b border-border pb-3">
                    <h3 className="text-xl font-bold font-display uppercase tracking-wider text-foreground flex items-center gap-2">
                        <Calendar size={20} className="text-brand"/>
                        Proposed Schedule Comparison
                    </h3>
                    <p className="text-xs text-muted-foreground mt-1">Review suggested workout schedule changes.</p>
                </div>
                <div className="max-h-[50vh] overflow-y-auto space-y-3 pr-1">
                    {proposedItems.length === 0 ? (
                        <div className="p-8 text-center text-muted-foreground text-xs">
                            No schedule changes were suggested for the selected cycle.
                            </div>
                    ):(
                        proposedItems.map((item)=>(
                            <div key={item.entryId} className="grid grid-cols-12 items-center gap-3 p-3.5 bg-surface-2/40 border border-border rounded-xl text-xs">
                                <div className="col-span-4 font-bold text-foreground truncate">{item.workoutName}</div>
                                <div className="col-span-3 text-muted-foreground">
                                    <span className="block text-[10px] uppercase font-extrabold tracking-wider text-muted-foreground/70">Current</span>
                                    {formatDate(item.originalScheduledAt)}
                                </div>
                                <div className="col-span-1 flex justify-center font-bold text-brand">
                                    <ArrowRight size={16}/>
                                </div>
                                <div className="col-span-4 font-semibold text-brand">
                                    <span className="block text-[10px] uppercase font-extrabold tracking-wider text-brand/80">Proposed</span>
                                    {formatDate(item.newScheduledAt)}
                                </div>
                            </div>
                        ))
                    )}
                </div>
                <div className="flex items-center justify-end gap-3 pt-3 border-t border-border">
                    <Button variant="outline" onClick={onClose} disabled={isConfirming} className="rounded-xl font-semibold">Keep Current Schedule</Button>
                    <Button onClick={onConfirm} disabled={isConfirming} className="bg-brand hover:bg-brand-2 text-white font-display uppercase tracking-wider rounded-xl">
                        {isConfirming ? <Loader2 className="animate-spin mr-2" size={16}/>: null}
                        Accept Proposed Schedule
                    </Button>
                </div>
            </div>
        </div>
    );
}