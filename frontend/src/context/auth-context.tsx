import * as React from 'react'
import { customFetch } from '@/lib/custom-fetch'

export type AuthUser = {
    id: string
    name: string
    email: string
    avatarUrl?: string
    metric: boolean
    lightTheme: boolean
}

export type AuthSession = {
    user: AuthUser | null
}

type AuthContextValue = {
    user: AuthUser | null
    isAuthenticated: boolean
    isHydrated: boolean
    login: (session: AuthSession) => void
    logout: () => void
}

const AuthContext = React.createContext<AuthContextValue | undefined>(undefined)

export function AuthProvider(props: Readonly<React.PropsWithChildren<unknown>>) {
    const { children } = props
    const [session, setSession] = React.useState<AuthSession | null>(null)
    const [isHydrated, setIsHydrated] = React.useState(false)

    const login = React.useCallback((nextSession: AuthSession) => {
        setSession(nextSession)
    }, [])

    const logout = React.useCallback(() => {
        setSession(null)
        customFetch('/api/auth/logout', { method: 'POST' }).catch(() => {
            //error handled in backend    
        })
    }, [])

    React.useEffect(() => {
        async function hydrateSession() {
            try {
                const loggedin = await customFetch('/api/auth/me')
                if (loggedin.ok) {
                    const user = await loggedin.json() as {
                        id: string;
                        name: string;
                        email: string;
                        avatarUrl?: string;
                        metric: boolean;
                        lightTheme: boolean;
                    };

                    const theme = user.lightTheme ? 'light' : 'dark';
                    localStorage.setItem('theme', theme);
                    if (user.lightTheme) {
                        document.documentElement.classList.remove('dark');
                    } else {
                        document.documentElement.classList.add('dark');
                    }

                    localStorage.setItem('metric', user.metric.toString());

                    login({
                        user: {
                            id: user.id,
                            name: user.name,
                            email: user.email,
                            metric: user.metric,
                            lightTheme: user.lightTheme,
                        }
                    });
                } else {
                    setSession(null);
                }
            } catch {
                setSession(null);
            } finally {
                setIsHydrated(true);
            }
        }
        hydrateSession();
    }, [login])

    //global listiner to log them out
    React.useEffect(() => {
        const handleGlobalLogout = () => {
            logout();
        };

        globalThis.addEventListener('logoutUser', handleGlobalLogout);
        return () => {
            globalThis.removeEventListener('logoutUser', handleGlobalLogout);
        };
    }, [logout]);

    const value = React.useMemo<AuthContextValue>(() => ({
        user: session?.user ?? null,
        isAuthenticated: Boolean(session?.user),
        isHydrated,
        login,
        logout,
    }), [session, isHydrated, login, logout])

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
    const context = React.useContext(AuthContext)

    if (!context) {
        throw new Error('useAuth must be used within an AuthProvider')
    }

    return context
}
