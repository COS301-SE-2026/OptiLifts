import { AlertCircle, Check, KeyRound, UserPlus, X } from "lucide-react";
import React, { useState, useEffect } from "react";
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
}: Readonly<AddFriendModalProps>){
    const [friendCode, setFriendCode] = useState('');
    const [status, setStatus] = useState<'idle' |'loading' | 'success' | 'error'>('idle');
    const [errorMessage, setErrorMessage] = useState('');

    useEffect(() => {
        if(!isOpen) {
            return;
        }
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape"){
                onClose();
            };
        };
            window.addEventListener("keydown", handleKeyDown);
            return () => window.removeEventListener("keydown", handleKeyDown);
        }, [isOpen, onClose]);

        if (!isOpen){
            return null;
        }

    const handleSubmit = async (e: React.SyntheticEvent<HTMLFormElement>) =>{
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
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
            <button type="button" tabIndex={-1} aria-label="Close modal" onClick={onClose} className="fixed inset-0 cursor-default bg-transparent border-none p-0 w-full h-full"/>
            <Card className="bg-surface border-border max-w-md w-full rounded-2xl shadow-2xl relative animate-in zoom-in-95 duration-150 p-6 space-y-5 cursor-default">
                {/* modal header */}
                <div className="flex items-center justify-between border-b border-border pb-4">
                    <div>
                        <h3 className="font-display text-2xl tracking-wide text-foreground flex items-center gap-2.5">
                            <UserPlus className="w-6 h-6 text-brand"/>
                            <span>Add Friend by their Code</span>
                        </h3>
                    <p className="text-xs text-muted-foreground mt-0.5 font-sans">
                        Enter your friend's unique 6 character code to send them a friend request.
                    </p>
                </div>
                <Button type="button" variant="ghost" size="icon" onClick={onClose}
                className="h-8 w-8 text-muted-foreground hover:text-foreground rounded-full" aria-label="Close">
                    <X className="w-4 h-4"/>
                </Button>
            </div>

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
                                <label htmlFor="friend-code-input" className="block text-xs font-bold uppercase tracking-[1px] text-muted-foreground mb-1.5">
                                Friend&apos;s Unique Code
                                </label>
                                <div className="relative">
                                    <KeyRound className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground"/>
                                    <Input id="friend-code-input" type="text" value={friendCode} 
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
            </Card>
        </div>
    )
}