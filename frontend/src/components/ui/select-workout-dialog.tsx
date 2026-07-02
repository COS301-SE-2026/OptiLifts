import { useState, useEffect } from 'react'
import { Button } from './button'
import { X, Loader2 } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from './card'
import { SearchInput } from './search-input'

interface SelectWorkoutDialogProps {
    readonly isOpen: boolean
    readonly onClose: () => void
    readonly workouts: any[]
    readonly isFetching: boolean
    readonly onSchedule: (workoutId: string) => Promise<void> | void
    readonly isScheduling: boolean
}
export function SelectWorkoutDialog({
    isOpen, onClose, workouts, isFetching, onSchedule, isScheduling
} : SelectWorkoutDialogProps) {
    const [selectedId, setSelectedId] = useState<string | null>(null)
    const [searchQuery, setSearchQuery] = useState('')
    useEffect(() => {
        if (isOpen) {
            setSelectedId(null)
            setSearchQuery('')
        }
    }, [isOpen])

    if (!isOpen) {
        return null
    }

    const filtered = workouts.filter(w => w.name.toLowerCase().includes(searchQuery.toLowerCase()) || w.primaryMuscleGroups.some((m: string) => m.toLowerCase().includes(searchQuery.toLowerCase())))
    const handleConfirm = async() => {
        if(selectedId) {
            await onSchedule(selectedId)
        }
    }

    let contentList;
    if (isFetching) {
        contentList = (
            <div className="h-48 flex flex-col items-center justify-center gap-2">
                <Loader2 className="animate-spin text-brand" size={24} />
                <span className="text-sm text-muted-foreground">Loading your workouts</span>
            </div>
        );
    } else if (filtered.length === 0) {
        contentList = (
            <div className="h-48 flex flex-col items-center justify-center text-center">
                <span className="text-sm text-muted-foreground">No workouts found.</span>
            </div>
        );
    } else {
        contentList = filtered.map((w) => {
            const isSelected = w.id === selectedId
            return (
                <Card
                    key={w.id}
                    role="button"
                    tabIndex={0}
                    aria-pressed={isSelected}
                    className={`cursor-pointer transition-shadow hover:border-brand/40 focus-visible:ring-2 focus-visible:ring-brand ${isSelected ? 'ring-1 ring-brand border-brand bg-brand/5' : ''}`}
                    onClick={() => setSelectedId(w.id)}
                    onKeyDown={(event) => {
                        if (event.key === 'Enter' || event.key === ' ') {
                            event.preventDefault()
                            setSelectedId(w.id)
                        }
                    }}>
                    <CardHeader className="flex flex-row justify-between items-start">
                        <CardTitle className="font-bold text-foreground text-base leading-snuf">
                            {w.name}
                        </CardTitle>
                        {isSelected && (
                            <div className="w-5 h-5 rounded-full bg-brand flex items-center justify-center text-brand-foreground text-[10px] font-bold shrink-0">
                                ✓
                            </div>
                        )}
                    </CardHeader>
                    <CardContent className="space-y-1 pb-4">
                        <p className="text-xs text-foreground">
                            <span className="font-semibold">Primary Muscle Groups:</span>{w.primaryMuscleGroups.join(', ') || 'None'}
                        </p>
                        <p className="text-xs text-foreground">
                            <span className="font-semibold">Exercises:</span> {w.exercisePreview.join(', ') || 'None'}
                        </p>
                    </CardContent>
                </Card>

            );
        });

    }

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs transition-opacity duration-200 animate-in fade-in">
            <dialog className="w-full max-w-lg rounded-2xl border border-border bg-surface p-6 shadow-2xl mx-4 flex flex-col max-h-[85vh] animate-in fade-in zoom-in-95 duration-200" 
            open aria-modal="true" 
                aria-labelledby="select-workout-title">
                <div className="flex items-center justify-between border-b border-border/60 pb-4 mb-4">
                    <h2 id="select-workout-title" className="font-display text-2xl tracking-wide text-foreground">Select Workout</h2>
                    <Button variant="ghost" size="icon" className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-full transition-all" 
                        onClick={onClose}
                        disabled={isScheduling}
                        aria-label="Close dialog"><X size={18}/></Button>
                </div>
                {/* searching */}
                <div className="mb-4">
                    <SearchInput
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    placeholder="Search workouts or muscle groups"/>
                </div>

                {/* list if workouts */}
                <div className="flex-1 overflow-y-auto pr-1 space-y-3 min-h-[250px] max-h-[400px]">
                    {contentList}
        </div> 

        {/* buttons */}
        <div className="mt-6 flex justify-end gap-3 border-t border-border/60 pt-4">
                    <Button 
                        variant="ghost"
                        onClick={onClose}
                        disabled={isScheduling}
                        className ="text-xs uppercase tracking-wider">
                            Cancel
                    </Button>
                    <Button
                    variant="secondary"
                    disabled={isScheduling || !selectedId}
                    onClick={handleConfirm}
                    className="text-xs uppercase tracking-wider px-6">
                        {isScheduling ? 'Scheduling...' : 'Schedule Workout'}
                    </Button>
                    </div>
        </dialog>
        </div>
    )
}