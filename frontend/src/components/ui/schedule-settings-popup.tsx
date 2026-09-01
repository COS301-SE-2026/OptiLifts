import { customFetch } from "@/lib/custom-fetch"
import { Calendar, CheckCircle2, Loader2, X } from "lucide-react"
import { useEffect, useState } from "react"
import { Button } from "./button"
import { ReschedulingConfig } from "./rescheduling-config"

type ScheduleSettingsPopupProps = Readonly<{
    isOpen: boolean
    onClose: () => void
}>

export function ScheduleSettingsPopup({
    isOpen, 
    onClose
}: ScheduleSettingsPopupProps){
    const [isLoading, setIsLoading] = useState(true)
    const [isToggling, setIsToggling] = useState(false)
    const [isConnected, setIsConnected] = useState(false)
    const [isConnecting, setIsConnecting] = useState(false)
    const [syncEnabled, setSyncEnabled] = useState(false)
    const [error, setError] = useState<string | null>(null)

    useEffect(()=> {
        if (!isOpen){
            return;
        }
        async function fetchSettings(){
            setIsLoading(true)
            setError(null)
            try{
                const res = await customFetch('/api/users/me/google-calendar/settings')
                if (res.ok){
                    const data = await res.json()
                    setIsConnected(data.isConnected)
                    setSyncEnabled(data.syncEnabled)
                }
            }catch{
                setError('Failed to load google calendar settings')
            } finally {
                setIsLoading(false)
            }
        }
        fetchSettings()
    },[isOpen])

    if (!isOpen){
        return null;
    }

    const handleConnectGoogle= () => {
        setIsConnecting(true)
        setError(null)
        const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID || import.meta.env.GOOGLE_CLIENT_ID || ''
        if (typeof window === 'undefined' || !window.google?.accounts?.oauth2){
            setError('Google OAuth client SDK is not loaded. Please refresh the page')
            setIsConnecting(false)
            return
        }

        const client = window.google.accounts.oauth2.initCodeClient({
            client_id: clientId,
            scope: 'https://www.googleapis.com/auth/calendar.events https://www.googleapis.com/auth/calendar',
            ux_mode: 'popup',
            error_callback: () => {
                setIsConnecting(false)
            },
            callback: async (response: { code?: string; error?: string }) => {
                if (response.error || !response.code) {
                    setError('Google calendar authorisation failed')
                    setIsConnecting(false)
                    return;
                }

                try {
                    const res = await customFetch('/api/users/me/google-calendar/connect',{
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            code: response.code,
                            redirectUri: 'postmessage'
                        })
                    })
                    if (!res.ok){
                        throw new Error('Could not connect google calendar')
                    }
                    setIsConnected(true)
                    setSyncEnabled(true)
                } catch (err) {
                    setError(err instanceof Error ? err.message: 'Error connecting google calendar')
                } finally {
                    setIsConnecting(false)
                }
            }
        })
        client.requestCode();
    }

    const handleToggleSync = async (enabled:boolean) => {
        setIsToggling(true)
        setError(null)
        try{
            const res = await customFetch('/api/users/me/google-calendar/toggle',{
                method: 'POST',
                headers: {
                    'Content-Type':'application/json'
                },
                body: JSON.stringify({enabled})
            })
            if (!res.ok){
                throw new Error('Failed to update sync settings')
            }
            setSyncEnabled(enabled)
        } catch (err){
            setError(err instanceof Error ? err.message: 'Error toggling sync')
        } finally {
            setIsToggling(false)
        }
    }

    const handleDisconnect = async () => {
        setIsToggling(true)
        setError(null)
        try {
            const res = await customFetch('/api/users/me/google-calendar/disconnect', {
                method: 'POST'
            })
            if (!res.ok){
                throw new Error("failed to disonnect google calendar")
            }
            setIsConnected(false)
            setSyncEnabled(false)
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Error disconnecting')
        } finally {
            setIsToggling(false)
        }
    }

    return (
        <div onClick={onClose} className="fixed top-0 lg:top-20 inset-x-0 bottom-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs transition-opacity duration-200 animate-in fade-in p-4">
            <div onClick={(e) => e.stopPropagation()} className="relative z-10 w-full max-w-md bg-surface border border-border rounded-2xl shadow-2xl overflow-hidden flex flex-col animate-in zoom-in-95 duration-200">
                <div className="flex items-center justify-between border-b border-border p-4">
                    <div className="flex items-center gap-2">
                        <Calendar size={20} className="text-brand"/>
                        <h2 className="text-xl font-bold font-display uppercase tracking-wider text-foreground">
                            Schedule Settings
                        </h2>
                    </div>
                    <button type="button" onClick={onClose} className="text-muted-foreground hover:text-foreground cursor-pointer">
                        <X size={20}/>
                    </button>
                </div>

                <div className="p-6 space-y-6 max-h-[75vh] overflow-y-auto font-sans">
                    {isLoading ? (
                        <div className="py-8 flex flex-col items-center justify-center gap-2 text-muted-foreground">
                            <Loader2 className="animate-spin text-brand" size={24}/>
                            <span className="text-sm">Loading calendar settings</span>
                        </div>
                    ): (
                        <>
                            <div className="space-y-3">
                                <h3 className="text-sm font-bold text-foreground">Google Calendar Integration</h3>
                                <p className="text-xs text-muted-foreground leading-relaxed">
                                    Sync your scheduled workouta to a OptiLifts calendar in Google calendar. Syncing applies to your future workouts only.
                                </p>
                            </div>

                            {error && (
                                <div className="p-3 text-xs bg-destructive/10 border border-destructive/20 text-destructive rounded-xl">
                                    {error}
                                </div>
                            )}

                            {!isConnected ? (
                                <div className="space-y-3 pt-2">
                                    <Button onClick={handleConnectGoogle} disabled={isConnecting}
                                    className="w-full flex items-center justify-center gap-2.5 bg-surface hover:bg-surface-2 border border-border text-foreground font-semibold py-2.5 rounded-xl shadow-sm cursor-pointer">
                                        {isConnecting ? (
                                            <Loader2 size={18} className="animate-spin text-brand"/>
                                        ):(
                                            <svg className="w-5 h-5" viewBox="0 0 24 24">
                                                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                                                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                                                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" />
                                                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" />
                                            </svg>
                                        )}
                                        <span>Sign in with Google to Sync Calendar</span>
                                    </Button>
                                </div>
                            ): (
                                <div className="p-4 bg-surface-2/30 border border-border rounded-xl space-y-4">
                                    <div className="flex items-center justify-between">
                                        <div className="flex items-center gap-2">
                                            <CheckCircle2 size={18} className="text-success"/>
                                            <span className="text-sm font-semibold text-foreground">Google Calendar Connected</span>
                                        </div>
                                        <button type="button" onClick={handleDisconnect} disabled={isToggling} className="text-xs text-destructive hover:underline font-medium cursor-pointer">Disconnect</button>
                                    </div>
                                    <div className="flex items-center justify-between border-t border-border/60 pt-3">
                                        <div className="flex items-center gap-2">
                                            <span className="text-sm font-semibold text-foreground">Sync Schedule to Google Calendar</span>
                                            {isToggling && <Loader2 size={16} className="animate-spin text-brand"/>}
                                        </div>                                        
                                        <label className="relative inline-flex items-center cursor-pointer">
                                            <span className="sr-only">Sync Schedule to Google Calendar</span>
                                            <input type="checkbox" checked={syncEnabled} disabled={isToggling} onChange={(e) => handleToggleSync(e.target.checked)} className="sr-only peer"/>
                                            <div className="w-11 h-6 bg-surface-2 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-brand"></div>
                                        </label>
                                    </div>
                                </div>
                            )}
                            {/* rescheduling section */}
                            <ReschedulingConfig/>
                        </>
                    )}
                </div>
            </div>
        </div>
    )
}