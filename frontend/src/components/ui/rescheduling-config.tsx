import { useEffect, useState } from "react";
import { customFetch } from "@/lib/custom-fetch";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { toast } from "@/components/ui/alert";

interface ScheduleConfig {
    dynamicSchedulerEnabled: boolean;
    maxWorkoutsPerDay: number;
    minMuscleRestHours: number;
    restDays: string[];
    cycleWindowLengthDays: number;
    cycleStartDate: string;
}

const ALL_DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

export function ReschedulingConfig() {
    const [config, setConfig] = useState<ScheduleConfig>({
        dynamicSchedulerEnabled: false,
        maxWorkoutsPerDay: 1,
        minMuscleRestHours: 48,
        restDays: ["Sunday"],
        cycleWindowLengthDays: 7,
        cycleStartDate: new Date().toISOString().split("T")[0],
    });

    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);

    useEffect(()=>{
        async function loadConfig(){
            try {
                const res = await customFetch("/api/users/me/schedule/config");
                if (res.ok){
                    const data = await res.json();
                    setConfig({
                        ...data,
                        cycleStartDate: data.cycleStartDate ? data.cycleStartDate.split("T")[0] : new Date().toISOString().split("T")[0],
                    });
                }
            } catch (err){
                toast.error(err instanceof Error ? err.message : 'Failed to open schedule settings', 'Error')
            } finally {
                setIsLoading(false);
            }
        }
        loadConfig();
    },[]);

    const handleSave = async ()=>{
        setIsSaving(true);
        try {
            const res = await customFetch("api/users/me/schedule/config", {
                method: "PUT",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(config),
            });
            if (res.ok){
                toast.success("Rescheduling preferences updated", "Success");
            } else {
                toast.error("Failed to update preferences", "Error");
            }
        } catch {
            toast.error("Error saving preferences", "Error");
        } finally {
            setIsSaving(false);
        }
    };

    const toggleRestDay = (day: string) => {
        setConfig((prev) => ({
            ...prev,
            restDays: prev.restDays.includes(day)
                ? prev.restDays.filter((d) => d !== day)
                : [...prev.restDays, day],
        }));
    };

    if (isLoading){
        return <div className="text-xs text-muted-foreground font-sans">Loading preferences...</div>;
    }

    return (
        <div className="space-y-4 pt-4 border-t border-border font-sans">
            <div className="flex items-center justify-between">
                <div>
                    <h4 className="font-display font-bold text-base uppercase tracking-wider text-foreground">Dynamic Rescheduling</h4>
                    <p className="text-xs text-muted-foreground">Automatically re-schedules missed workouts within active cycles</p>
                </div>
                <input type="checkbox" checked={config.dynamicSchedulerEnabled} onChange={(e) => setConfig({
                    ...config,
                    dynamicSchedulerEnabled: e.target.checked
                })}
                className="h-4 w-4 rounded border-border text-brand focus:ring-brand accent-brand cursor-pointer"/>
            </div>
            
            {config.dynamicSchedulerEnabled && (
                <div className="space-y-3 bg-surface-2/40 p-4 rounded-2xl border border-border text-xs">
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div>
                            <span className="font-bold text-muted-foreground uppercase text-[11px] block mb-1">Max Workouts/Day</span>
                            <Input type="number" min={1} max={5} value={config.maxWorkoutsPerDay} onChange={(e) => setConfig({
                                ...config,
                                maxWorkoutsPerDay: Number.parseInt(e.target.value) || 1
                            })}/>
                        </div>
                        <div>
                            <span className="font-bold text-muted-foreground uppercase text-[11px] block mb-1.5">Min Muscle Rest (Hours)</span>
                            <Input type="number" min={0} max={120} value={config.minMuscleRestHours} onChange={(e) => setConfig({
                                ...config,
                                minMuscleRestHours: Number.parseInt(e.target.value) || 48
                            })}/>
                        </div>
                    </div>
                    <div>
                        <span className="font-bold text-muted-foreground uppercase text-[11px] block mb-1.5">Fixed Rest Days</span>
                        <div className="flex flex-wrap gap-1.5">
                            {ALL_DAYS.map((day)=> {
                                const active = config.restDays.includes(day);
                                return (
                                    <button key={day} type="button" onClick={() => toggleRestDay(day)} aria-pressed={active} aria-label={`Toggle ${day} as rest day`}
                                        className={`px-2.5 py-1 rounded-lg text-[11px] font-semibold transition-all cursor-pointer focus-visible:ring-2 focus-visible:ring-brand outline-none ${active ? "bg-brand text-white shadow-xs" : "bg-surface border border-border text-muted-foreground hover:bg-surface-2"}`}>
                                            {day.slice(0,3)}
                                    </button>
                                );
                            })}
                        </div>
                    </div>
                    
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                        <div>
                            <span className="font-bold text-muted-foreground uppercase text-[11px] block mb-1">Cycle Length (Days)</span>
                            <Input type="number" min={1} max={30} value={config.cycleWindowLengthDays} onChange={(e) => setConfig({
                                ...config,
                                cycleWindowLengthDays: Number.parseInt(e.target.value) || 7
                            })}/>
                        </div>
                        <div>
                            <span className="font-bold text-muted-foreground uppercase text-[11px] block mb-1">Cycle Start Date</span>
                            <Input type="date" value={config.cycleStartDate} onChange={(e) => setConfig({
                                ...config, cycleStartDate: e.target.value
                            })}/>
                        </div>
                    </div>
                    


                </div>
            )}
            <Button type="button" onClick={handleSave} disabled={isSaving} className="w-full">
                {isSaving ? "Saving..." : "Save Rescheduling Preferences"}
            </Button>

        </div>
    );
    
}