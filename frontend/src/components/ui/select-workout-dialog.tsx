import { useState, useEffect } from 'react'
import { Button } from './button'
import { X, Loader2, ChevronDown } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from './card'
import { SearchInput } from './search-input'
import type { Workout } from '@/types/workout'
import {
  DropdownMenu,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuContent
} from './dropdown-menu'

const formatDateToYMD = (date: Date) =>{
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2,'0')
    const day = String(date.getDate()).padStart(2,'0')
    return `${year}-${month}-${day}`
}

//trynna reduce complexity
function getRepeatDateConstraints(
    scheduledDate: Date | null,
    repeatType: 'Day' | 'Week' | 'Month',
    repeatInterval: number,
    isRepeating: boolean,
    repeatUntil: string
){
    let minDatestr = ''
    let maxDatestr = ''
    let showWarn = false
    if(!scheduledDate) {
        return {minDatestr, maxDatestr, showWarn}
        
    }
    const minDate = new Date(scheduledDate);
    minDate.setDate(minDate.getDate() + 1)
    minDatestr = formatDateToYMD(minDate)
    const maxDate = new Date(scheduledDate)
    maxDate.setFullYear(maxDate.getFullYear() + 1)
    maxDatestr = formatDateToYMD(maxDate)

    const first = new Date(scheduledDate)
    if(repeatType === 'Day') {
        first.setDate(first.getDate() + repeatInterval)
    } else if(repeatType === 'Week'){
        first.setDate(first.getDate() + repeatInterval*7);
    } else if(repeatType === 'Month'){
        first.setMonth(first.getMonth() + repeatInterval)
    }

    if(isRepeating&& repeatUntil){
        const untilDate = new Date(repeatUntil)
        untilDate.setHours(0,0,0,0)
        const firstOcc = new Date(first)
        firstOcc.setHours(0,0,0,0)
        if(untilDate < firstOcc){
            showWarn = true;
        }
    }
    return {minDatestr, maxDatestr, showWarn}
}

interface SelectWorkoutDialogProps {
    readonly isOpen: boolean
    readonly onClose: () => void
    readonly workouts: readonly Workout[]
    readonly isFetching: boolean
    readonly onSchedule: (workoutId: string,
        repeat?: string,
        interval?: number,
        until?: string
    ) => Promise<void> | void
    readonly isScheduling: boolean
    readonly scheduledDate: Date | null
}
export function SelectWorkoutDialog({
    isOpen, onClose, workouts, isFetching, onSchedule, isScheduling, scheduledDate
} : SelectWorkoutDialogProps) {
    const [selectedId, setSelectedId] = useState<string | null>(null)
    const [searchQuery, setSearchQuery] = useState('')

    //repeat configs
    const [isRepeating, setIsRepeating] = useState(false);
    const [repeatInterval, setRepeatInterval] = useState<number>(1)
    const [repeatUntil, setRepeatUntil] = useState<string>('')
    const [repeatType, setRepeatType] = useState<'Day' | 'Week'| 'Month'>('Week')

    useEffect(()=>{
        if(isOpen) {
            document.body.classList.add('overflow-hidden')
        } else {
            document.body.classList.remove('overflow-hidden')
        }
        return ()=>{
            document.body.classList.remove('overflow-hidden')
        }
    }, [isOpen])


    if (!isOpen) {
        return null
    }

    const filtered = workouts.filter(w => w.name.toLowerCase().includes(searchQuery.toLowerCase()) || w.primaryMuscleGroups.some((m: string) => m.toLowerCase().includes(searchQuery.toLowerCase())))
    const handleConfirm = async() => {
        if(selectedId) {
            if (isRepeating){
                await onSchedule(selectedId, repeatType, repeatInterval, repeatUntil)
            } else {
                await onSchedule(selectedId)
            }
            
        }
    }
    const {minDatestr, maxDatestr, showWarn} = getRepeatDateConstraints(scheduledDate, repeatType, repeatInterval, isRepeating, repeatUntil)
    
    const isScheduleDisabled = isScheduling || !selectedId || (isRepeating && !repeatUntil)
    
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
        <div className="fixed top-0 lg:top-20 inset-x-0 bottom-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs transition-opacity duration-200 animate-in fade-in">
            <dialog className="mx-auto w-full max-w-lg rounded-2xl border border-border bg-surface p-6 shadow-2xl flex flex-col max-h-[90%] animate-in fade-in zoom-in-95 duration-200 overflow-hidden z-50" 
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
                <div className="flex-1 overflow-y-auto px-2 py-1 space-y-4 min-h-[150px]">
                    {contentList}
                </div> 

                {/* repeating */}
                <div className="py-3.5 space-y-3.5 shrink-0">
                    <div className="flex items-center gap-2">
                        <input type="checkbox" id = "repeat-checkbox" checked={isRepeating} onChange={(e) => setIsRepeating(e.target.checked)}
                        className="h-4 w-4 rounded border-border text-brand focus:ring-brand accent-brand cursor-pointer"/>
                        <label htmlFor="repeat-checkbox" className="text-sm font-semibold text-foreground cursor-pointer select-none">
                            Repeat
                        </label>
                    </div>
                    {isRepeating&& (
                        <div className="space-y-3 border border-border/50 rounded-xl p-3 bg-surface-2/20 animate-in fade-in slide-in-from-top-2 duration-200">
                            <div className="flex flex-col sm:flex-row sm:items-center gap-2.5">
                                <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Repeat every:</span>
                                <div className="flex items-center gap-2">
                                    <input type="number" min="1" value={repeatInterval} onChange={(e) => setRepeatInterval(Math.max(1, Number.parseInt(e.target.value) || 1))}
                                    className="w-16 bg-surface border border-border text-foreground px-2 py-1.5 rounded-xl text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-brand shadow-sm text-center h-9"/>
                                    <DropdownMenu>
                                        <DropdownMenuTrigger variant="plain" className="bg-surface border border-border text-foreground px-3 py-1.5 rounded-xl text-sm font-semibold hover:bg-surface-2/40 transition-all cursor-pointer flex items-center justify-between gap-1 shadow-sm min-w-[100px] h-9">
                                        <span>
                                            {repeatInterval > 1 ? `${repeatType}s` :repeatType}
                                        </span>
                                        <ChevronDown size={14} className="text-muted-foreground ml-1"/>
                                        </DropdownMenuTrigger>
                                        <DropdownMenuContent className="bg-surface border border-border rounded-xl p-1 shadow-md z-[60]">
                                            <DropdownMenuItem onClick={() => setRepeatType('Day')}>{repeatInterval > 1 ? 'Days' : 'Day'}</DropdownMenuItem>
                                            <DropdownMenuItem onClick={() => setRepeatType('Week')}>{repeatInterval > 1 ? 'Weeks' : 'Week'}</DropdownMenuItem>
                                            <DropdownMenuItem onClick={() => setRepeatType('Month')}>{repeatInterval > 1 ? 'Months' : 'Month'}</DropdownMenuItem>
                                        </DropdownMenuContent>
                                    </DropdownMenu>
                                </div>
                            </div>
                            <div className="space-y-1">
                                <div className="flex items-center justify-between">
                                    <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">Until:</span> {showWarn && (
                                        <span className="text-[10px] font-bold text-destructive animate-pulse bg-destructive/5 px-2 py-0.5 rounded-md border border-destructive/10">Won't repeat as end date is before first repeat occurence.</span>
                                    )}
                                </div>
                                <input type ="date" min={minDatestr} max={maxDatestr} value={repeatUntil} onChange={(e)=> setRepeatUntil(e.target.value)}
                                className="w-full bg-surface border border-border text-foreground px-3 py-1.5 rounded-xl text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-brand shadow-sm cursor-pointer"/> 
                            </div>
                            </div>
                    )}
                </div>


        {/* buttons */}
        <div className="mt-6 flex justify-end gap-3 border-t border-border/60 pt-4">
                    <Button 
                        variant="secondary"
                        onClick={onClose}
                        disabled={isScheduling}
                        className ="text-xs uppercase tracking-wider">
                            Cancel
                    </Button>
                    <Button
                    disabled={isScheduleDisabled}
                    onClick={handleConfirm}
                    className="text-xs uppercase tracking-wider px-6">
                        {isScheduling ? 'Scheduling...' : 'Schedule Workout'}
                    </Button>
                    </div>
        </dialog>
        </div>
    )
}