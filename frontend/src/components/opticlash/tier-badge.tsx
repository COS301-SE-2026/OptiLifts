import {Award} from 'lucide-react';

export type TierType = 'Bronze' | 'Silver' | 'Gold' | 'Diamond' | 'Overload Master';

interface TierBadgeProps{
    tier: string;
    size?: 'sm' | 'md' | 'lg';
    className?: string;
}

export function TierBadge({
    tier,
    size="md",
    className = ''
}: Readonly<TierBadgeProps>){
    const sizeClasses ={
        sm: 'px-2 py-0.5 text-[10px] gap-1',
        md: 'px-3 py-1 text-xs gap-1.5',
        lg: 'px-4 py-1.5 text-sm gap-2',
    }[size];
    const iconClasses = {
        sm: 'w-3 h-3',
        md: 'w-3.5 h-3.5',
        lg: 'w-4 h-4',
    }[size];
    switch(tier){
        case 'Overload Master':
            return(
                <span className={`rounded-full font-bold bg-overload-master/15 text-overload-master border border-overload-master/50 inline-flex items-center justify-center uppercase tracking-wide font-sans ${sizeClasses} ${className}`}>
                    <Award className={iconClasses}/>
                        <span>Overload Master</span>
                </span>
            );
        case 'Diamond':
             return(
                <span className={`rounded-full font-bold bg-cyan-500/15 text-cyan-600 dark:text-cyan-400 border border-cyan-500/50 inline-flex items-center justify-center uppercase tracking-wide font-sans ${sizeClasses} ${className}`}>
                    <span>Diamond</span>
                </span>
            );
        case 'Gold':
            return (
                <span className={`rounded-full font-bold bg-amber-500/15 text-amber-600 dark:text-amber-400 border border-amber-500/50 inline-flex items-center justify-center uppercase tracking-wide font-sans ${sizeClasses} ${className}`}>
                    <span>Gold</span>
                </span>
            );
        case 'Silver':
            return (
                <span className={`rounded-full font-bold bg-slate-500/15 text-slate-600 dark:text-slate-300 border border-slate-500/50 inline-flex items-center justify-center uppercase tracking-wide font-sans ${sizeClasses} ${className}`}>
                    <span>Silver</span>
                </span>
            );
        default:
            return (
                <span className={`rounded-full font-bold bg-amber-500/15 text-amber-800 dark:text-amber-600 border border-amber-700/50 inline-flex items-center justify-center uppercase tracking-wide font-sans ${sizeClasses} ${className}`}>
                    <span>Bronze</span>
                </span>
            );
    }
}