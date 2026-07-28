import { Link, useLocation } from 'react-router-dom'
import { useEffect, useState } from 'react'
import { useAuth } from '@/context/auth-context'
import { getDraftFromStorage } from '@/lib/session-drafts'

const PUBLIC_LINKS = [
  { to: '/register', label: 'Register' },
  { to: '/login', label: 'Login' },
]

const LINKS = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/workouts',  label: 'Workouts'  },
  { to: '/schedule',  label: 'Schedule'  },
  { to: '/progress',  label: 'Progress'  },
  { to: '/profile',   label: 'Profile'   },
]

export function Navbar() {
  const { pathname } = useLocation()
  const { isAuthenticated} = useAuth()
  const [ activeDraft, setActiveDraft] = useState<{ workoutId: string; workoutName: string } | null>(null)

  useEffect(() => {
    setActiveDraft(getDraftFromStorage())
  }, [pathname])

  const navigationLinks = isAuthenticated ? LINKS : PUBLIC_LINKS
  const homeLink = isAuthenticated ? '/workouts' : '/register'

  const linkClass = (to: string) =>
    [
      'px-5 py-2 font-sans text-[13px] font-semibold uppercase tracking-[1px] whitespace-nowrap no-underline transition-colors duration-150 border-b-2 -mb-[2px]',
      pathname.startsWith(to) ? 'text-brand border-brand' : 'text-muted-foreground border-transparent hover:text-foreground',
    ].join(' ')

  return (
    <header className="sticky top-0 z-[100] w-full h-20 bg-background border-b-2 border-brand flex items-center px-8 box-border">

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
  )
}
