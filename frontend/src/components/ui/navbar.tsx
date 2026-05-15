import { Link, useLocation } from 'react-router-dom'

const LINKS = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/workouts',  label: 'Workouts'  },
  { to: '/schedule',  label: 'Schedule'  },
  { to: '/progress',  label: 'Progress'  },
  { to: '/profile',   label: 'Profile'   },
]

export function Navbar() {
  const { pathname } = useLocation()

  return (
    <header className="sticky top-0 z-[100] w-full h-16 bg-background border-b-2 border-brand flex items-center px-8 box-border">

      <Link to="/" aria-label="Home" className="flex items-center gap-[14px] mr-auto no-underline flex-shrink-0">
        <img src="/logo-light.svg" className="h-9 w-auto dark:hidden" alt="OptiLifts" />
        <img src="/logo-dark.svg"  className="h-9 w-auto hidden dark:block" alt="OptiLifts" />
        <span className="font-display text-[28px] leading-none tracking-[2px] select-none">
          <span className="text-foreground">OPTI</span><span className="text-brand">LIFTS</span>
        </span>
      </Link>

      <nav className="flex items-center">
        {LINKS.map(({ to, label }) => (
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
      </nav>

    </header>
  )
}
