import { useState } from "react";

interface AthleteAvatarProps{
    initials: string;
    name?: string;
    avatarUrl?: string;
    isCurrentUser?:boolean;
    size?: 'sm' | 'md' | 'lg' | 'xl';
    className?: string;
}

export function AthleteAvatar({
    initials, name, avatarUrl, isCurrentUser = false,
    size = 'md', className= '',
}: Readonly<AthleteAvatarProps>){
    const [imageError, setImageError] = useState(false);
    const sizeMap = {
        sm: 'w-8 h-8 rounded-lg text-xs',
        md: 'w-10 h-10 rounded-xl text-base',
        lg: 'w-12 h-12 rounded-xl text-xl',
        xl: 'w-16 h-16 rounded-2xl text-2xl',
    }[size];

    const colourStyles = isCurrentUser ? 'bg-brand text-primary-foreground shadow-sm' : 'bg-surface-2 border border-border text-foreground';
    //pfp available + no error
    if (avatarUrl && !imageError){
        return (
            <div className={`${sizeMap} overflow-hidden shrink-0 border border-border rounded-xl ${isCurrentUser ? 'border-brand ring-2 ring-brand/30':''}
            ${className}`}>
                <img src={avatarUrl} alt={name ||initials} onError={() => setImageError(true)}
                className="w-full h-full object-cover"/>
            </div>
        );
    }

    //no pfp or error --> just do their initials
    return (
        <div className={`${sizeMap} ${colourStyles} flex items-center justify-center font-display font-bold shrink-0 ${className}`}
        title={name || initials}>
            {initials}
        </div>
    );
}