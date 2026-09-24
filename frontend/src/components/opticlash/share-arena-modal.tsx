import { Check, Copy, Loader2, Plus, Share2, UserCheck, Users, X } from "lucide-react";
import { useEffect, useState } from "react";
import { Button } from "../ui/button";
import { SearchInput } from "../ui/search-input";
import { Card } from "../ui/card";
import { AthleteAvatar } from "./athlete-avatar";
import { toast } from "../ui/alert";
import { customFetch } from "@/lib/custom-fetch";

export interface FriendsSummary {
    id: string;
    name: string;
    initials: string;
    avatarUrl?: string;
    code: string;
    dotsScore: number;
    tier: string;
}

export interface ShareableArena {
    id: string;
    name: string;
    code?: string | null;
}
interface ShareArenaModalProps {
    isOpen: boolean;
    onClose: () => void;
    arena: ShareableArena | null;
}

export function ShareArenaModal({
    isOpen,
    onClose,
    arena
}: Readonly<ShareArenaModalProps>) {
    const [copied, setCopied] = useState(false);
    const [searchQuery, setSearchQuery] = useState('');
    const [friends, setFriends] = useState<FriendsSummary[]>([]);
    const [isLoadingFriends, setIsLoadingFriends] = useState(false);
    const [invitingFriendIds, setInvitingFriendIds] = useState<Record<string, boolean>>({});
    const [invitedFriendIds, setInvitedFriendIds] = useState<Record<string, boolean>>({});

    useEffect(() => {
        if (!isOpen || !arena){
            return;
        }

    let isMounted = true;
    // eslint-disable-next-line react-hooks/set-state-in-effect -- fetch-on-mount, not a state-adjustment effect
    setIsLoadingFriends(true);
    customFetch('/api/clash/friends').then(async (res) => {
        if (res.ok){
            const data = await res.json();
            if (isMounted) {
                setFriends(Array.isArray(data) ? data : []);
            }
        }
    }).catch(() => {
        toast.error('Failed to load friends list');
    }).finally(() => {
        if (isMounted){
            setIsLoadingFriends(false);
        }
    });

        return () => {
            isMounted = false;
        };
    }, [isOpen, arena]);

    if (!isOpen || !arena){
        return null;
    }
    const handleCopyCode = () => {
        if (!arena.code){
            return;
        }
        navigator.clipboard.writeText(arena.code);
        setCopied(true);
        toast.success('Arena code copied to clipboard', arena.code);
        setTimeout(() => setCopied(false), 2000);
    };
    const handleInviteFriend = async (friendId: string) => {
        if (!arena.id || invitingFriendIds[friendId] || invitedFriendIds[friendId]){
            return;
        }
        setInvitingFriendIds((prev) => ({
            ...prev,
            [friendId]: true
        }));

        try {
            const res = await customFetch(`/api/clash/arenas/${arena.id}/invite`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    friendId
                }),
            });
            
            const data = await res.json().catch(() => null);
            if (res.ok && data?.success) {
                setInvitedFriendIds((prev) => ({
                    ...prev,
                    [friendId]: true
                }));
                toast.success(data.message ?? 'Invitation sent');
            } else {
                toast.error(data?.message ?? 'Failed to send invite');
                if (data?.message?.toLowerCase().includes('already')){
                    setInvitedFriendIds((prev) => ({
                        ...prev,
                        [friendId]: true
                    }));
                }
            }
        } catch {
            toast.error('Network error sending invite');
        } finally {
            setInvitingFriendIds((prev) => ({
                ...prev,
                [friendId]: false
            }));
        }
    };

    const filtFriends = friends.filter((f) => f.name.toLowerCase().includes(searchQuery.toLowerCase()));

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
            <Card className="bg-surface border-border max-w-md w-full rounded-2xl shadow-2xl relative animate-in zoom-in-95 duration-150 p-6 space-y-5">
                {/* modal header */}
                <div className="flex items-center justify-between border-b border-border pb-4">
                    <div>
                        <h3 className="font-display text-2xl tracking-wide text-foreground flex items-center gap-2.5">
                            <Share2 className="w-6 h-6" />
                            <span>Share Arena and Invite Friends</span>
                        </h3>
                    </div>
                    <Button variant="ghost" size="icon" onClick={onClose} className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-full" aria-label="Close">
                        <X className="w-4 h-4 hover:text-foreground" />
                    </Button>
                </div>
                    {/* invite code sec */}
                    <div>
                        <label className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5 font-sans">
                            Arena Join Code
                        </label>
                        <div className="flex items-center gap-2">
                            <div className="flex-1 bg-surface-2 border border-border rounded-lg px-3 py-2 font-mono text-sm font-bold text-brand tracking-widest uppercase">
                                {arena.code || '------'}
                            </div>
                            <Button variant="default" size="sm" onClick={handleCopyCode} className="flex items-center gap-1.5 h-9">
                                {copied ? <Check className="w-4 h-4"/> : <Copy className="w-4 h-4"/>}
                                <span>{copied ? 'Copied' : 'Copy'}</span>
                            </Button>
                        </div>
                    </div>

                    {/* direct invites to vriende */}
                    <div className="pt-3 border-t border-border">
                        <div className="flex items-center justify-between mb-2">
                            <label className="text-xs font-bold uppercase tracking-[1px] text-muted-foreground flex items-center gap-1.5 font-sans">
                                <Users className="w-3.5 h-3.5 text-brand"/>Direct Friend Invites
                            </label>
                            <span className="text-[11px] text-muted-foreground font-sans">
                                {friends.length} Friends
                            </span>
                        </div>
                        {/* search func */}
                        <div className="mb-3">
                            <SearchInput placeholder="Search friend by name" value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="h-9 text-xs bg-surface-2 border-border font-sans"/>
                        </div>

                        {/* friends */}
                        <div className="max-h-48 overflow-y-auto space-y-2 pr-1 divide-y divide-border/40">
                            {isLoadingFriends ? (
                                <div className="flex items-center justify-center py-6 text-muted-foreground text-xs font-sans gap-2">
                                    <Loader2 className="w-4 h-4 animate-spin text-brand"/>
                                    <span>Loading friends...</span>
                                </div>) : filtFriends.length === 0 ? (
                                    <p className="text-xs text-muted-foreground text-center py-4 font-sans">
                                        {searchQuery ? 'No matching friends found' : 'No friends found. Add friends using their private code.'}
                                    </p>
                                    ) : (
                                        filtFriends.map((f) => {
                                            const isInvited = !!invitedFriendIds[f.id];
                                            const isInviting = !!invitingFriendIds[f.id];

                                            return (
                                                <div key={f.id} className="pt-2 first:pt-0 flex items-center justify-between">
                                                    <div className="flex items-center gap-2.5">
                                                        <AthleteAvatar initials={f.initials} name={f.name} avatarUrl={f.avatarUrl} size="sm"/>
                                                        <div>
                                                            <strong className="text-xs font-bold text-foreground block font-sans">{f.name}</strong>
                                                            <span className="text-[10px] text-muted-foreground font-sans">{f.dotsScore} DOTS</span>
                                                        </div>
                                                    </div>

                                                    <Button variant={isInvited ? 'secondary': 'default'} size="sm"
                                                    onClick={() => handleInviteFriend(f.id)} disabled={isInvited || isInviting}
                                                    className="h-7 px-2.5 text-[11px]">
                                                        {isInviting ? (
                                                            <>
                                                                <Loader2 className="w-3 h-3 animate-spin mr-1"/>
                                                                <span>Inviting...</span>
                                                            </>
                                        ) : isInvited ? (
                                            <>
                                                <UserCheck className="w-3 h-3 text-success mr-1"/>
                                                <span>Invited</span>
                                            </>
                                        ) : (
                                            <>
                                                <Plus className="w-3 h-3 mr-1"/>
                                                <span>Invite</span>
                                            </>
                                        )}
                                                    </Button>
                                                    </div>
                                            );
                                        })
                                    )}
                        </div>
                    </div>
                <div className="pt-3 border-t border-border flex justify-end">
                    <Button variant="secondary" size="sm" onClick={onClose}
                    className="h-8 text-xs">
                        Done
                    </Button>
                </div>
            </Card>
        </div>
    );
}