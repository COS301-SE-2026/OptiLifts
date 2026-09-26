import { useAuth } from "@/context/auth-context";
import type { DuelSummary } from "@/types/clash";
import { Card, CardContent } from "../ui/card";
import { AthleteAvatar } from "./athlete-avatar";
import { Clock } from "lucide-react";
import { useState } from "react";

interface DuelCardProps {
    duel: DuelSummary;
    onClick?: () => void;
    className?: string;
    currentUserId?: string;
}
export function DuelCard({
    duel, onClick, className = '', currentUserId
}: Readonly<DuelCardProps>) {
    const { user } = useAuth();
    const [now] = useState(() => Date.now());
    const effuserId = currentUserId || user?.id;
    const isUserChallenger = !effuserId || duel.challengerUserId === effuserId;

    const userraw = Number(isUserChallenger ? duel.challengerCurrentValue : duel.rivalCurrentValue) || 0;
    const rivalraw = Number(isUserChallenger ? duel.rivalCurrentValue : duel.challengerCurrentValue) || 0;
    const rivalName = isUserChallenger ? duel.rivalName : duel.challengerName;
    const rivalAvatar = isUserChallenger ? duel.rivalAvatarUrl : duel.challengerAvatarUrl;

    const rivalInit = rivalName ? rivalName.split(' ').map((n) => n[0]).join('').slice(0, 2).toUpperCase() : 'OP';
    const userInit = (user?.name || (isUserChallenger ? duel.challengerName : duel.rivalName) || 'CS').split(' ').map((n) => n[0]).join('').slice(0,2).toUpperCase();

    const isVol = duel.targetType?.toLowerCase().includes('volume');
    const format = (val: number) => {
        if (isVol) {
            return `${val.toLocaleString()} kg`;
        }
        return `${val >= 0 ? '+' : ''}${val.toFixed(1)}%`;
    };

    const userDisMetric = format(userraw);
    const rivalDIsMetric = format(rivalraw);    
    const valA = Math.max(0, userraw);
    const valB = Math.max(0, rivalraw);
    let userprogPercent = 50;
    let rivalprogPercent = 50;
    if (valA + valB > 0) {
        userprogPercent = Math.round((valA/(valA+valB))*100);
        rivalprogPercent = 100 - userprogPercent;
    }

    const userWinning = userraw > rivalraw;
    const tied = userraw === rivalraw;

    const getLeadFormat = () => {
        if (tied){
            return 'All Tied';
        }
        const diff = Math.abs(userraw-rivalraw);
        const formatDif = isVol ? `${diff.toLocaleString()} kg` : `${diff.toFixed(1)}%`;
        return userWinning ? `+${formatDif} lead` : `-${formatDif} behind`;
    };
    const endsFormat = () => {
        if (!duel.endDate) {
            return 'Active';
        }
        const diffms = new Date(duel.endDate).getTime() - now;
        if (diffms <= 0){
            return '0h';
        }
        const difdays = Math.floor(diffms/(1000*60*60*24));
        if (difdays >= 1) {
            return `${difdays}d`;
        }
        const difhours = Math.max(1, Math.floor(diffms / (1000 * 60 * 60)));
        return `${difhours}h`;
    };

    const isEnded = duel.status?.toLowerCase() === 'finished';
    const temp = (isUserChallenger ? duel.winnerUserId === duel.challengerUserId : duel.winnerUserId === duel.rivalUserId);
    const userWon = duel.winnerUserId ? temp : userraw > rivalraw;

    let outcomeBadge = null;
    if(isEnded) {
        if (duel.isDraw) {
            outcomeBadge = (
                <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-surface-2 text-muted-foreground border border-border">
                    DRAW
                </span>
            );
        } else if (userWon){
            outcomeBadge = (
                <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-success/10 text-success border border-success/30">
                    VICTORY
                </span>
            );
        } else {
            outcomeBadge = (
                <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-surface-2 text-muted-foreground border border-border">
                    DEFEAT
                </span>
            );
        }
    }
    const rivalfirstname = rivalName ? rivalName.split(' ')[0] : 'Rival';
    return (
        <Card onClick={onClick} className={`bg-surface hover:bg-surface-2/40 border-border p-5 cursor-pointer transition shadow-sm group ${className}`}>
            <CardContent className="p-0">
                <div className="flex items-center justify-between text-xs mb-3 font-sans">
                    <span className="font-bold text-foreground uppercase tracking-wider">{duel.title}</span>
                    {isEnded ? (
                        outcomeBadge
                    ) : (
                        <span className="text-warning font-semibold flex items-center gap-1 text-[11px]">
                            <Clock className="3.5 h-3.5"/> {endsFormat()} left
                        </span>
                    )}
                </div>

                {/* vs match */}
                <div className="flex items-center justify-between my-3">
                    <div className="flex items-center gap-2.5">
                        <AthleteAvatar initials={userInit} isCurrentUser={true} size="md"/>
                        <div>
                            <span className="text-xs text-muted-foreground block sont-sans">You</span>
                            <strong className="text-sm font-bold text-foreground font-sans">{userDisMetric}</strong>
                        </div>
                    </div>

                    <div className="px-2.5 py-1 rounded bg-surface-2 text-[10px] font-sans font-bold text-muted-foreground border border-border">VS</div>
                    <div className="flex items-center gap-2.5 text-right">
                        <div>
                            <span className="text-xs text-muted-foreground block font-sans">{rivalName}</span>
                            <strong className="text-sm font-bold text-foreground font-sans">{rivalDIsMetric}</strong>
                        </div>
                        <AthleteAvatar initials={rivalInit} avatarUrl={rivalAvatar || undefined} size="md"/>
                    </div>
                </div>
                {/* progress bar */}
                <div className="mt-4">
                    <div className="h-3 w-full bg-surface-2 rounded-full overflow-hidden flex border border-border">
                        <div className={`${userWinning ? 'bg-brand' : 'bg-muted-foreground/30'} transition-all duration-500`}
                        style={{width: `${userprogPercent}%`}}/>
                        <div className={`${!userWinning && !tied ? 'bg-brand' : 'bg-muted-foreground/30'} transition-all duration-500`}
                        style={{width: `${rivalprogPercent}%`}}/>
                    </div>
                    <div className="flex justify-between items-center text-[11px] mt-1.5 font-sans">
                        <span className={userWinning ? 'text-brand font-bold' : 'text-muted-foreground'}>{getLeadFormat()}</span>
                        <span className={!userWinning && !tied ? 'text-brand font-bold' : 'text-muted-foreground'}>
                            {rivalprogPercent}% {rivalfirstname}
                        </span>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}