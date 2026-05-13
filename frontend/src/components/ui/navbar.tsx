import { Link, useLocation } from 'react-router-dom'
import s from './navbar.module.css'

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
    <header className={s.header}>

      <Link to="/" aria-label="Home" className={s.logo}>
        <img src="/logo-light.svg" className="dark:hidden" alt="OptiLifts" />
        <img src="/logo-dark.svg"  className="hidden dark:block" alt="OptiLifts" />
        <span className={s.wordmark}>
          <span className={s.opti}>OPTI</span><span className={s.lifts}>LIFTS</span>
        </span>
      </Link>

      <nav className={s.nav}>
        {LINKS.map(({ to, label }) => (
          <Link
            key={to}
            to={to}
            className={pathname.startsWith(to) ? `${s.tab} ${s.tabActive}` : s.tab}
          >
            {label}
          </Link>
        ))}
      </nav>

    </header>
  )
}
