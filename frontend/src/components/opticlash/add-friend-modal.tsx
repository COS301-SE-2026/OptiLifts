import { AlertCircle, Check, KeyRound, UserPlus, X } from "lucide-react";
import { useState, type FormEvent } from "react";
import { Button } from "../ui/button";
import { Card } from "../ui/card";
import { Input } from "../ui/input";
import { customFetch } from "@/lib/custom-fetch";

interface AddFriendModalProps{
    isOpen: boolean;
    onClose: () => void;
    onFriendAdded?: (code: string) => void;
}

export function AddFriendModal({
    isOpen,
    onClose,
    onFriendAdded
}: AddFriendModalProps){
    const [friendCode, setFriendCode] = useState('');
    const [status, setStatus] = useState<'idle' |'loading' | 'success' | 'error'>('idle');
    const [errorMessage, setErrorMessage] = useState('');

    if(!isOpen) {
        return null;
    }

    const handleSubmit = async (e: FormEvent) =>{
        e.preventDefault();
        const clean = friendCode.trim().toUpperCase();
        if(!clean || clean.length <5){
            setStatus('error');
            setErrorMessage('Please enter a valid 6-character friend code');
            return;
        }

        setStatus('loading');
        try {
            const res = await customFetch('/api/clash/friends/requests', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    friendCode: clean
                }),
            });
            const data = await res.json();
            if (!res.ok || !data.success){
                setStatus('error');
                setErrorMessage(data.message || 'Failed to send friend request');
                return;
            }

            setStatus('success');
            if (onFriendAdded){
                onFriendAdded(clean);
            }
            setTimeout(() => {
                setStatus('idle');
                setFriendCode('');
                onClose();
            }, 1500);
        } catch {
            setStatus('error');
            setErrorMessage('Network error. Please try again');
        }

    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm cursor-pointer"
        onClick={onClose}>
            <Card className="bg-surface border-border max-w-md w-full overflow-hidden shadow-2xl relative animate-in zoom-in-95 duration-150 p-0 cursor-default" onClick={(e) => e.stopPropagation()}>
                <Button type="button" variant="ghost" size="icon" onClick={onClose}
                className="absolute top-4 right-4 h-8 w-8 z-10" aria-label="Close">
                    <X className="w-4 h-4 text-muted-foreground hover:text-foreground"/>
                </Button>

                {/* modal header */}
                <div className="p-6 pb-4 border-b border-border bg-surface-2">
                    <div className="w-12 h-12 rounded-xl bg-brand-fill border border-brand/30 text-brand flex items-center justify-center mb-3">
                        <UserPlus className="w-6 h-6"/>
                    </div>
                    <h3 className="font-display text-2xl tracking-wide text-foreground">
                        Add Friend by their Code
                    </h3>
                    <p className="text-xs text-muted-foreground mt-0.5 font-sans">
                        Enter your friend's unique 6 character code to send them a friend request.
                    </p>
                </div>

                {/* modal bod */}
                <div className="p-6 bg-surface">
                    {status === 'success' ? (
                        <div className="py-6 text-center space-y-2">
                            <div className="w-12 h-12 rounded-full bg-success/10 text-success border border-success/30 flex items-center justify-center mx-auto">
                                <Check className="w-6 h-6"/>
                            </div>
                            <h4 className="font-sans font-bold text-base text-foreground">
                            Friend Request Sent
                            </h4>
                            <p className="text-xs text-muted-foreground">
                            Your friend request to <strong className="text-brand font-mono">{friendCode}</strong> is now pending acceptance
                            </p>
                        </div>
                    ):(
                        <form onSubmit={handleSubmit} className="space-y-4">
                            {status === 'error' && (
                                <div className="p-3 bg-brand-fill border border-brand/40 rounded-lg text-xs text-brand font-medium flex items-center gap-2">
                                    <AlertCircle className="w-4 h-4 shrink-0"/>
                                    <span>{errorMessage}</span>
                                </div>
                            )}

                            <div>
                                <label className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">
                                Friend&apos;s Unique Code
                                </label>
                                <div className="relative">
                                    <KeyRound className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"/>
                                    <Input type="text" value={friendCode} 
                                    onChange={(e) => {
                                        setFriendCode(e.target.value.toUpperCase());
                                        setStatus('idle');
                                    }}
                                    placeholder="eg. AB1234" maxLength={8}
                                    className="pl-9 font-mono tracking-widest uppercase bg-surface-2 border-border"/>
                                </div>
                            </div>
                            <Button type="submit" variant="default" className="w-full">Send Friend Request</Button>
                        </form>
                    )}
                </div>
            </Card>
        </div>
    )
}