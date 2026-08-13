import { Link, useLocation } from 'react-router-dom'
import { useAuth } from '@/context/auth-context'
import { getDraftFromStorage } from '@/lib/session-drafts'
import { useEffect, useState } from 'react'
import { Menu, X } from 'lucide-react'

const PUBLIC_LINKS = [
  { to: '/register', label: 'Register' },
  { to: '/login', label: 'Login' },
]

const LINKS = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/workouts',  label: 'Workouts'  },
  { to: '/schedule',  label: 'Schedule'  },
  { to: '/help',  label: 'Help'  },
  { to: '/profile',   label: 'Profile'   },
]

export function Navbar() {
  const { pathname } = useLocation()
  const { isAuthenticated} = useAuth()
  const activeDraft = getDraftFromStorage()
  const [isMenuOpen, setMenuOpen] = useState(false)
  const [lastPathname, setLastPathname] = useState(pathname)

  if (pathname !== lastPathname) {
    setLastPathname(pathname)
    setMenuOpen(false)
  }

  const navigationLinks = isAuthenticated ? LINKS : PUBLIC_LINKS
  const homeLink = isAuthenticated ? '/dashboard' : '/'

  useEffect(() => {
    if (!isMenuOpen) {
      return
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setMenuOpen(false)
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isMenuOpen])

  const linkClass = (to: string) =>
    [
      'px-5 py-2 font-sans text-[13px] font-semibold uppercase tracking-[1px] whitespace-nowrap no-underline transition-colors duration-150 border-b-2 -mb-[2px]',
      pathname.startsWith(to) ? 'text-brand border-brand' : 'text-muted-foreground border-transparent hover:text-foreground',
    ].join(' ')

  const mobileLinkClass = (to: string) =>
    [
      'flex min-h-11 items-center border-l-4 px-6 font-sans text-[15px] font-semibold uppercase tracking-[1px] no-underline transition-colors duration-150',
      pathname.startsWith(to)
        ? 'text-brand border-brand bg-brand/5'
        : 'text-muted-foreground border-transparent hover:bg-surface-2 hover:text-foreground',
    ].join(' ')

  return (
    <>
    <header className="sticky top-0 z-[100] w-full h-20 bg-background border-b-2 border-brand hidden lg:flex items-center px-8 box-border">
      <Link to={homeLink} aria-label="Home" className="flex items-center gap-[14px] mr-auto no-underline flex-shrink-0">
        <img src="/logo-light.svg" className="h-12 w-auto dark:hidden" alt="OptiLifts" />
        <img src="/logo-dark.svg"  className="h-12 w-auto hidden dark:block" alt="OptiLifts" />
        <span className="font-display text-[36px] leading-none tracking-[2px] select-none">
          <span className="text-foreground">OPTI</span><span className="text-brand">LIFTS</span>
        </span>
      </Link>

      <nav className="flex items-center gap-2">
        {isAuthenticated && activeDraft && (
          <Link to="/active-session" state={{ workout: { id: activeDraft.workoutId, name: activeDraft.workoutName } }} className={linkClass('/active-session')}>
            Session
          </Link>
        )}

        {navigationLinks.map(({ to, label }) => (
          <Link key={to} to={to} className={linkClass(to)}>
            {label}
          </Link>
        ))}
        
    </nav>
    </header>

      <button
        type="button"
        aria-label={isMenuOpen ? 'Close menu' : 'Open menu'}
        aria-expanded={isMenuOpen}
        aria-controls="mobile-nav"
        onClick={() => setMenuOpen((open) => !open)}
        className="fixed right-4 top-4 z-[110] flex h-11 w-11 items-center justify-center rounded-lg border border-border bg-background/90 text-foreground shadow-lg backdrop-blur transition-colors hover:bg-surface-2 lg:hidden"
      >
        {isMenuOpen ? <X size={24} strokeWidth={2} /> : <Menu size={24} strokeWidth={2} />}
      </button>

      {isMenuOpen && (
        <>
          <button
            type="button"
            aria-label="Close menu"
            className="fixed inset-0 z-[105] bg-foreground/50 lg:hidden"
            onClick={() => setMenuOpen(false)}
          />
          <nav
            id="mobile-nav"
            className="fixed right-4 top-[4.25rem] z-[106] flex w-56 flex-col gap-1 overflow-hidden rounded-xl border border-border bg-background py-2 shadow-2xl lg:hidden"
          >
            {isAuthenticated && activeDraft && (
              <Link
                to="/active-session"
                state={{ workout: { id: activeDraft.workoutId, name: activeDraft.workoutName } }}
                className={mobileLinkClass('/active-session')}
                onClick={() => setMenuOpen(false)}
              >
                Session
              </Link>
            )}

            {navigationLinks.map(({ to, label }) => (
              <Link key={to} to={to} className={mobileLinkClass(to)} onClick={() => setMenuOpen(false)}>
                {label}
              </Link>
            ))}
          </nav>
        </>
      )}
    </>
  )
}

