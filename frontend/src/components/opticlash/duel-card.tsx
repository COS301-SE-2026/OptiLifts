import { useAuth } from "@/context/auth-context";
import { getDuelMatchupState, type DuelSummary } from "@/types/clash";
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
    const m = getDuelMatchupState(duel, effuserId, user?.name, now);

    let outcomeBadge = null;
    if(m.isFinished) {
        if (duel.isDraw) {
            outcomeBadge = (
                <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-surface-2 text-muted-foreground border border-border">
                    DRAW
                </span>
            );
        } else if (m.userWon){
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
    const rivalfirstname = m.rivalName ? m.rivalName.split(' ')[0] : 'Rival';
    return (
        <Card onClick={onClick} className={`bg-surface hover:bg-surface-2/40 border-border p-5 cursor-pointer transition shadow-sm group ${className}`}>
            <CardContent className="p-0">
                <div className="flex items-center justify-between text-xs mb-3 font-sans">
                    <span className="font-bold text-foreground uppercase tracking-wider">{duel.title}</span>
                    {m.isFinished ? (
                        outcomeBadge
                    ) : (
                        <span className="text-warning font-semibold flex items-center gap-1 text-[11px]">
                            <Clock className="w-3.5 h-3.5"/> {m.endsInText} left
                        </span>
                    )}
                </div>

                {/* vs match */}
                <div className="flex items-center justify-between my-3">
                    <div className="flex items-center gap-2.5">
                        <AthleteAvatar initials={m.userInitials} isCurrentUser={true} size="md"/>
                        <div>
                            <span className="text-xs text-muted-foreground block font-sans">You</span>
                            <strong className="text-sm font-bold text-foreground font-sans">{m.userDisplayMetric}</strong>
                        </div>
                    </div>

                    <div className="px-2.5 py-1 rounded bg-surface-2 text-[10px] font-sans font-bold text-muted-foreground border border-border">VS</div>
                    <div className="flex items-center gap-2.5 text-right">
                        <div>
                            <span className="text-xs text-muted-foreground block font-sans">{m.rivalName}</span>
                            <strong className="text-sm font-bold text-foreground font-sans">{m.rivalDisplayMetric}</strong>
                        </div>
                        <AthleteAvatar initials={m.rivalInitials} avatarUrl={m.rivalAvatarUrl || undefined} size="md"/>
                    </div>
                </div>
                {/* progress bar */}
                <div className="mt-4">
                    <div className="h-3 w-full bg-surface-2 rounded-full overflow-hidden flex border border-border">
                        <div className={`${m.isWinning ? 'bg-brand' : 'bg-muted-foreground/30'} transition-all duration-500`}
                        style={{width: `${m.userProgressPercent}%`}}/>
                        <div className={`${!m.isWinning && !m.isTied ? 'bg-brand' : 'bg-muted-foreground/30'} transition-all duration-500`}
                        style={{width: `${m.rivalProgressPercent}%`}}/>
                    </div>
                    <div className="flex justify-between items-center text-[11px] mt-1.5 font-sans">
                        <span className={m.isWinning ? 'text-brand font-bold' : 'text-muted-foreground'}>{m.leadText}</span>
                        <span className={!m.isWinning && !m.isTied ? 'text-brand font-bold' : 'text-muted-foreground'}>
                            {m.rivalProgressPercent}% {rivalfirstname}
                        </span>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}