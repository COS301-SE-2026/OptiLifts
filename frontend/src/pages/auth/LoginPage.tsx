import { Navigate, Link, useLocation, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { useAuth, type AuthSession, type AuthUser } from '@/context/auth-context'

function createToken(email: string) {
  //placeholder for now till backend is setup
  const encode = (value: string) => globalThis.btoa(value).replaceAll('+', '-').replaceAll('/', '_').replaceAll('=', '')

  const header = encode(JSON.stringify({ alg: 'none', typ: 'JWT' }))
  const payload = encode(JSON.stringify({ email, demo: true, iat: Math.floor(Date.now() / 1000) }))
  const signature = encode(`${email}-${Date.now()}`)

  return `${header}.${payload}.${signature}`
}

function LoginHeading() {
  return (
    <h1 className="font-display text-[42px] leading-none tracking-[2px] text-foreground select-none border-b-4 border-brand pb-2 px-2 w-fit">
      LOGIN
    </h1>
  )
}

type PasswordRowProps = Readonly<{
  label: string
  value: string
  onChange: (nextValue: string) => void
  showValue: boolean
  onToggle: () => void
  placeholder: string
}>

function PasswordRow({ label, value, onChange, showValue, onToggle, placeholder }: PasswordRowProps) {
  const inputId = `password-row-${label.toLowerCase().replace(/[^a-z0-9]+/g, '-')}`

  return (
    <div className="grid gap-1">
      <label htmlFor={inputId} className="text-sm font-semibold uppercase tracking-[0.08em] text-foreground">
        {label}
      </label>
      <div className="relative w-full">
        <Input
          id={inputId}
          required
          type={showValue ? 'text' : 'password'}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          autoComplete="current-password"
          placeholder={placeholder}
          className="pr-11"
        />
        <Button
          type="button"
          variant="password"
          size="icon"
          aria-label={showValue ? `Hide ${label.toLowerCase()}` : `Show ${label.toLowerCase()}`}
          onClick={onToggle}
          className="absolute right-1 top-1/2 -translate-y-1/2"
        >
          {showValue ? <Eye size={16} /> : <EyeOff size={16} />}
        </Button>
      </div>
    </div>
  )
}

const generateUser = (email: string): AuthUser => {
  const id = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}`
  const name = email.split('@')[0] || 'Member'
  return { id, name, email: email.trim() }
}

export function LoginPage() {
  const { login, isAuthenticated, isHydrated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)

  const emailRegex = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/
  
  const isEmailValid = email.trim().length > 0 && emailRegex.test(email.trim())
  const isPasswordValid = password.length > 0
  const isFormValid = isEmailValid && isPasswordValid

  const fromPath = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/workouts'

  const handleSubmit = (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault()
    
    if (!isFormValid) {
      return
    }

    //just create a user, will change when backend is implemented
    const nextUser = generateUser(email)

    const nextSession: AuthSession = {
      token: createToken(nextUser.email), 
      user: nextUser,
    }

    login(nextSession)
    navigate(fromPath, { replace: true })
  }

  if (!isHydrated) {
    return <Navigate to="/" replace />
  }

  if (isAuthenticated) {
    return <Navigate to={fromPath} replace />
  }

  return (
    <section className="mx-auto min-h-[calc(100dvh-5rem)] max-w-3xl px-6 pt-4 pb-10">
      <div className="flex min-h-[calc(100dvh-7rem)] flex-col items-center justify-center">
        <LoginHeading />

        <Card className="mt-6 w-full max-w-md">
          <CardContent>
            <form onSubmit={handleSubmit} className="grid gap-4">
              <label className="grid gap-1">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-foreground">Email Address</span>
                <Input
                  required
                  type="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  autoComplete="email"
                  placeholder="you@example.com"
                />
              </label>

              <PasswordRow
                label="Password"
                value={password}
                onChange={setPassword}
                showValue={showPassword}
                onToggle={() => setShowPassword((current) => !current)}
                placeholder="Enter password"
              />

              <Link to="/forgot-password" className="text-sm text-brand no-underline hover:underline text-right">
                Forgot Password?
              </Link>

              <Button
                type="submit"
                variant="default"
                disabled={!isFormValid}
                className={`w-80 justify-center justify-self-center ${isFormValid ? '': 'opacity-60 cursor-not-allowed'}`}
              >
                LOGIN
              </Button>

              <p className="text-center text-sm text-muted-foreground">
                Don't have an account?{' '}
                <Link to="/register" className="font-bold text-brand no-underline hover:underline">
                  Register
                </Link>
              </p>
            </form>
          </CardContent>
        </Card>
      </div>
    </section>
  )
}
