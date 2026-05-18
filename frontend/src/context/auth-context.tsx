import * as React from 'react'

export type AuthUser = {
    id: string
    name: string
    email: string
    avatarUrl?: string
}

export type AuthSession = {
    token: string
    user: AuthUser | null
}

type AuthContextValue = {
    token: string | null
    user: AuthUser | null
    isAuthenticated: boolean
    isHydrated: boolean
    login: (session: AuthSession) => void
    logout: () => void
}

const STORAGE_KEY = 'optilifts.auth.session'

const AuthContext = React.createContext<AuthContextValue | undefined>(undefined)

function readStoredSession(): AuthSession | null {
    //the second condition checks if window exists as it's only in browswer enviroments and not in SSR or during build time
    if (typeof globalThis === 'undefined' || !('window' in globalThis)) 
        return null

    try {
        const raw = globalThis.localStorage.getItem(STORAGE_KEY)
        return raw ? (JSON.parse(raw) as AuthSession) : null
    } catch {
        return null
    }
}

function saveSession(session: AuthSession) {
    if (typeof globalThis === 'undefined' || !('window' in globalThis)) 
        return
    try {
        globalThis.localStorage.setItem(STORAGE_KEY, JSON.stringify(session))
    } catch {
        return
    }
}

function clearSession() {
    if (typeof globalThis === 'undefined' || !('window' in globalThis)) 
        return
    try {
        globalThis.localStorage.removeItem(STORAGE_KEY)
    } catch {
        return
    }
}

export function AuthProvider(props: Readonly<React.PropsWithChildren<unknown>>) {
    const { children } = props
    const [session, setSession] = React.useState<AuthSession | null>(() => readStoredSession())
    const [isHydrated] = React.useState(() => typeof globalThis !== 'undefined' && 'window' in globalThis)

    const login = React.useCallback((nextSession: AuthSession) => {
        setSession(nextSession)
        saveSession(nextSession)
    }, [])

    const logout = React.useCallback(() => {
        setSession(null)
        clearSession()
    }, [])

    const value = React.useMemo<AuthContextValue>(() => ({
        token: session?.token ?? null,
        user: session?.user ?? null,
        isAuthenticated: Boolean(session?.token),
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
