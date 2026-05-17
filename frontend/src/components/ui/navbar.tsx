import { Link, useLocation } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { useAuth } from '@/context/auth-context'

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

function getInitials(name: string = '') {
  const matches = name.match(/\b\p{L}/gu) || [] //uses word boundary to get first letter of each unicode word
  return matches.slice(0, 2).join('').toUpperCase()
}

export function Navbar() {
  const { pathname } = useLocation()
  const { user, isHydrated, isAuthenticated, logout } = useAuth()

  const navigationLinks = isAuthenticated ? LINKS : PUBLIC_LINKS
  const activeUser = isAuthenticated ? user : null
  const authControls = isHydrated && activeUser ? (
    <div className="ml-3 flex items-center gap-3 border-l border-border pl-4">
      <Link to="/profile" className="flex items-center gap-3 no-underline text-foreground">
        <Avatar className="size-8">
          <AvatarFallback className="bg-brand text-brand-foreground text-xs font-bold">
            {getInitials(activeUser.name)}
          </AvatarFallback>
        </Avatar>
        <span className="hidden max-w-[12rem] truncate text-sm font-semibold text-foreground sm:block">
          {activeUser.name}
        </span>
      </Link>

      <Button
        type="button"
        variant="outline"
        size="sm"
        className="px-4"
        onClick={logout}
      >
        <LogOut size={14} />
        Logout
      </Button>
    </div>
  ) : null

  return (
    <header className="sticky top-0 z-[100] w-full h-20 bg-background border-b-2 border-brand flex items-center px-8 box-border">

      <Link to="/" aria-label="Home" className="flex items-center gap-[14px] mr-auto no-underline flex-shrink-0">
        <img src="/logo-light.svg" className="h-12 w-auto dark:hidden" alt="OptiLifts" />
        <img src="/logo-dark.svg"  className="h-12 w-auto hidden dark:block" alt="OptiLifts" />
        <span className="font-display text-[36px] leading-none tracking-[2px] select-none">
          <span className="text-foreground">OPTI</span><span className="text-brand">LIFTS</span>
        </span>
      </Link>

      <nav className="flex items-center gap-2">
        {navigationLinks.map(({ to, label }) => (
          <Link
            key={to}
            to={to}
            className={[
              'px-5 py-2 font-sans text-[13px] font-semibold uppercase tracking-[1px] whitespace-nowrap no-underline transition-colors duration-150 border-b-2 -mb-[2px]',
              pathname.startsWith(to)
                ? 'text-brand border-brand'
                : 'text-muted-foreground border-transparent hover:text-foreground',
            ].join(' ')}
          >
            {label}
          </Link>
        ))}
        
        
        {authControls} {/* shows avatar and logout if they're logged in and state is hydrated */}
      </nav>

    </header>
  )
}
