import { BrowserRouter } from 'react-router-dom'
import { Navbar } from '@/components/ui/navbar'
import { Toaster, toast } from '@/components/ui/alert'
import { ThemeToggle } from '@/components/ui/theme-toggle'
import { PageTitle } from '@/components/ui/page-title'
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuItem,
  DropdownMenuSeparator,
} from '@/components/ui/dropdown-menu'

function App() {
  return (
    <BrowserRouter>
      <div style={{ background: 'var(--background)', minHeight: '100vh', fontFamily: "'Barlow', sans-serif" }}>
        <Navbar />
        <div style={{ padding: '36px', display: 'flex', flexDirection: 'column', gap: '24px' }}>

          <PageTitle title="Dashboard" />

          {/* Page header */}
          <div style={{ borderLeft: '4px solid var(--brand)', paddingLeft: '20px' }}>
            <div style={{ fontFamily: "'Bebas Neue', sans-serif", fontSize: '42px', letterSpacing: '2px', color: 'var(--foreground)' }}>
              Good Evening, Alex
            </div>
            <div style={{ fontSize: '14px', color: 'var(--muted-text)', marginTop: '6px' }}>
              Push Day B · AI-optimised session ready
            </div>
            <div style={{ display: 'flex', gap: '10px', marginTop: '16px', alignItems: 'center' }}>
              <button style={{ padding: '10px 24px', borderRadius: '4px', fontSize: '13px', fontWeight: 700, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer', background: 'transparent', color: 'var(--foreground)', border: '1px solid var(--border)', fontFamily: "'Barlow', sans-serif" }}>
                View Plan
              </button>
              <button style={{ padding: '10px 24px', borderRadius: '4px', fontSize: '13px', fontWeight: 700, letterSpacing: '1px', textTransform: 'uppercase', cursor: 'pointer', background: 'var(--brand)', color: '#fff', border: 'none', fontFamily: "'Barlow', sans-serif" }}>
                Start Session
              </button>
              <ThemeToggle />
            </div>
          </div>

          {/* Dropdown demo */}
          <DropdownMenu>
            <DropdownMenuTrigger>Options</DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuLabel>Actions</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem>View Plan</DropdownMenuItem>
              <DropdownMenuItem>Start Session</DropdownMenuItem>
              <DropdownMenuItem variant="destructive">Delete</DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          {/* Alert test buttons */}
          <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
            <button onClick={() => toast.info('Increase bench press by 2.5kg next week.', 'AI Coach')}
              style={{ padding: '8px 16px', background: 'var(--surface)', border: '1px solid var(--border)', cursor: 'pointer', fontFamily: "'Barlow', sans-serif", fontSize: '12px', fontWeight: 700, letterSpacing: '1px', textTransform: 'uppercase', color: 'var(--foreground)' }}>
              Info Toast
            </button>
            <button onClick={() => toast.success('Session logged successfully.', 'Session Complete')}
              style={{ padding: '8px 16px', background: 'var(--surface)', border: '1px solid var(--border)', cursor: 'pointer', fontFamily: "'Barlow', sans-serif", fontSize: '12px', fontWeight: 700, letterSpacing: '1px', textTransform: 'uppercase', color: 'var(--foreground)' }}>
              Success Toast
            </button>
            <button onClick={() => toast.warning('RPE averaged 9.2. Consider a deload.', 'Fatigue Warning')}
              style={{ padding: '8px 16px', background: 'var(--surface)', border: '1px solid var(--border)', cursor: 'pointer', fontFamily: "'Barlow', sans-serif", fontSize: '12px', fontWeight: 700, letterSpacing: '1px', textTransform: 'uppercase', color: 'var(--foreground)' }}>
              Warning Toast
            </button>
            <button onClick={() => toast.error('Bench press stalled for 3 sessions.', 'Plateau Alert')}
              style={{ padding: '8px 16px', background: 'var(--surface)', border: '1px solid var(--border)', cursor: 'pointer', fontFamily: "'Barlow', sans-serif", fontSize: '12px', fontWeight: 700, letterSpacing: '1px', textTransform: 'uppercase', color: 'var(--foreground)' }}>
              Error Toast
            </button>
          </div>

        </div>
      </div>
      <Toaster />
    </BrowserRouter>
  )
}

export default App
