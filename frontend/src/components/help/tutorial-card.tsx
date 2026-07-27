import { useState } from "react";
import { Play, Clock, Film, X } from "lucide-react";

export interface TutorialVideo {
    id: string
    title: string
    description: string
    duration: string
    youtubeId?: string
    fallbackVideoUrl?: string
    thumbnailUrl?: string
}

interface TutorialCardProps{
    readonly video: TutorialVideo
}

export function TutorialCard({video}: TutorialCardProps){
    const [isModalOpen, setIsModelOpen] = useState(false)
    const [useLocalFallback, setUseLocalFallback] = useState(false)
    return (
        <>
        <div className="group rounded-xl border border-border bg-surface overflow-hidden flex flex-col hover:border-brand transition-all duration-200 shadow-sm hover:shadow-md">
            <div className="relative aspect-video bg-surface-2 flex items-center justify-center overflow-hidden">
                {video.thumbnailUrl ? (
                    <img src={video.thumbnailUrl} alt={video.title} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"/>
                ):(
                    <div className="flex flex-col items-center gap-2 text-muted-foreground">
                        <Film className="h-10 w-10 text-brand/60"/>
                        <span className="text-xs uppercase tracking-widest font-display">Video Tutorial</span>
                    </div>
                )}

                <span className="absolute bottom-2 right-2 bg-background/90 backdrop-blur-xs text-foreground px-2 py-0.5 rounded text-xs font-mono flex items-center gap-1 border border-border">
                    <Clock className="h-3 w-3 text-brand"/>{video.duration}
                </span>

                <button type="button" onClick={() => setIsModelOpen(true)}
                className="absolute inset-0 bg-background/40 group-hover:bg-background/20 transition-colors flex items-center justify-center focus:outline-none"
                aria-label={`Play ${video.title}`}>
                    <div className="h-12 w-12 rounded-full bg-brand text-white flex items-center justify-center shadow-lg group-hover:scale-110 transition-transform">
                        <Play className="h-6 w-6 fill-current ml-0.5"/>
                    </div>
                </button>
            </div>
            <div className="p-5 flex-1 flex flex-col justify-between gap-4">
                <div>
                    <h3 className="font-display text-xl text-foreground leading-snug mb-1">{video.title}</h3>
                    <p className="text-sm text-muted-foreground line-clamp-2 leading-relaxed">{video.description}</p>
                </div>

                <button type="button" onClick={() => setIsModelOpen(true)}
                className="w-full py-2.5 px-4 rounded-lg bg-surface-2 hover:bg-brand hover:text-white text-foreground text-xs font-semibold uppercase tracking-wider transition-colors flex items-center justify-center gap-2 border border-border hover:border-brand">
                    <Play className="h-3.5 w-3.5 fill-current"/>Watch Tutorial
                </button>
            </div>
        </div>

        {/* video modal */}
        {isModalOpen && (
            <div className="fixed inset-0 z-[200] bg-black/80 backdrop-blur-sm flex items-center justify-center p-4">
                <div className="bg-surface border border-border rounded-xl w-full max-w-4xl overflow-hidden shadow-2xl flex flex-col animate-in fade-in zoom-in-95 duration-200">
                    <div className="p-4 border-b border-border flex items-center justify-between bg-surface-2">
                        <div className="flex items-center gap-3">
                            <span className="w-1 h-6 bg-brand rounded-full"/>
                            <h2 className="font-display text-2xl text-foreground tracking-wide">{video.title}</h2>
                        </div>
                        <button type="button" onClick={() => setIsModelOpen(false)}
                        className="p-1 rounded-lg text-muted-foreground hover:text-foreground hover:bg-surface transition-colors"
                        aria-label="Close modal">
                            <X className="h-6 w-6"/>
                        </button>
                    </div>

                    <div className="relative aspect-video bg-black flex items-center justify-center">
                        {!useLocalFallback && video.youtubeId ? (
                            <iframe title={video.title} src={`https://www.youtube-nocookie.com/embed/${video.youtubeId}?autoplay=1`}
                            className="w-full h-full" allow="accelerometer; autoplay; clipboard-write encrypted-media; gyroscope; picture-in-picture" allowFullScreen/>
                        ):(
                            <video controls autoPlay src={video.fallbackVideoUrl || '/videos/sample-tutorial.mp4'} //should probbaly make this - will we have fallbacks in the repo?
                            className="w-full h-full object-contain">Your browser does not support the video player</video>
                        )}
                    </div>

                    <div className="p-4 border-t border-border bg-surface flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-muted-foreground">
                        <p className="line-clamp-1">{video.description}</p>

                        {/* TODO: put the fallback btn here if we decide to have one*/}

                    </div>
                </div>
            </div>
        )}
        </>
    )
}