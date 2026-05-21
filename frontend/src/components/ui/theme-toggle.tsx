import { useEffect, useState } from 'react'
import { Sun, Moon } from 'lucide-react'

export function ThemeToggle() {
  const [dark, setDark] = useState(() =>
    document.documentElement.classList.contains('dark')
  )

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark)
  }, [dark])

  return (
    <button
      onClick={() => setDark(d => !d)}
      aria-label="Toggle theme"
      style={{
        background: 'none',
        border: '1px solid var(--border)',
        borderRadius: 6,
        padding: '6px 10px',
        cursor: 'pointer',
        color: 'var(--foreground)',
        display: 'flex',
        alignItems: 'center',
        gap: 6,
      }}
    >
      {dark ? <Sun size={16} /> : <Moon size={16} />}
    </button>
  )
}
