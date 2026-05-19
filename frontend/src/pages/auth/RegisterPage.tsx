import { Navigate, Link, useLocation, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { Eye, EyeOff } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { useAuth, type AuthSession, type AuthUser } from '@/context/auth-context'

function RegisterHeading() {
  return (
    <h1 className="font-display text-[42px] leading-none tracking-[2px] text-foreground select-none border-b-4 border-brand pb-2 px-2 w-fit">
      REGISTER
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
  error?: React.ReactNode
  disclaimer?: React.ReactNode
}>

function PasswordRow({ label, value, onChange, showValue, onToggle, placeholder, error, disclaimer }: PasswordRowProps) {
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
          autoComplete="new-password"
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
      {disclaimer}
      {error}
    </div>
  )
}

//map backend user DTO to frontend AuthUser
function mapBackendUserToAuthUser(dto: { id: string; displayName: string; email: string }) {
  return {
    id: dto.id,
    name: dto.displayName,
    email: dto.email,
  } as AuthUser
}

export function RegisterPage() {
  const { login, isAuthenticated, isHydrated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const USERNAME_MAX = 30
  const emailRegex = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/
  const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/

  const isUsernameValid = username.trim().length > 0 && username.trim().length <= USERNAME_MAX
  const isEmailValid = email.trim().length > 0 && emailRegex.test(email.trim())
  const isPasswordValid = password.length > 0 && passwordRegex.test(password)
  const doPasswordsMatch = confirmPassword.length > 0 && password === confirmPassword
  const isFormValid = isUsernameValid && isEmailValid && isPasswordValid && doPasswordsMatch

  const showUsernameError = !isUsernameValid && username.length > 0
  const showEmailError = !isEmailValid && email.length > 0
  const showPasswordError = !isPasswordValid && password.length > 0
  const showConfirmError = !doPasswordsMatch && confirmPassword.length > 0

  const fromPath = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname ?? '/workouts'

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const handleSubmit = async (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault()
    
    if (!isFormValid) {
      return
    }

    setIsSubmitting(true)
    setErrorMessage(null)

    try {
      const res = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName: username.trim(), email: email.trim(), password }),
      })

      if (!res.ok) {
        const payload = await res.json().catch(() => null)
        if (res.status === 409) setErrorMessage('That email is already in use.')
        else setErrorMessage(payload?.title ?? 'Unable to register. Please try again.')
        return
      }

      const data = await res.json()

      const session: AuthSession = {
        token: data.token,
        user: mapBackendUserToAuthUser(data.user),
      }

      login(session)
      navigate(fromPath, { replace: true })
    } catch (err) {
      setErrorMessage(err instanceof Error ? err.message : 'Network error - please try again.')
    } finally {
      setIsSubmitting(false)
    }
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
        <RegisterHeading />

        <Card className="mt-6 w-full max-w-md">
          <CardContent>
            <form onSubmit={handleSubmit} className="grid gap-4">
              <label className="grid gap-1">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-foreground">Username</span>
                <Input
                  required
                  value={username}
                  onChange={(event) => setUsername(event.target.value)}
                  maxLength={USERNAME_MAX}
                  autoComplete="username"
                  placeholder="your username"
                />
                <span className="text-sm text-muted-foreground">Maximum {USERNAME_MAX} characters.</span>
                
                {showUsernameError && (
                  <span className="text-sm text-destructive -mt-2">Username must be 1-{USERNAME_MAX} characters.</span>
                )}
              </label>

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
                {showEmailError && <div className="text-sm text-destructive">Please enter a valid email address.</div> }
              </label>

              <PasswordRow
                label="Password"
                value={password}
                onChange={setPassword}
                showValue={showPassword}
                onToggle={() => setShowPassword((current) => !current)}
                placeholder="Enter password"
                disclaimer={<div className="text-sm text-muted-foreground"> 8 or more characters containing uppercase, lowercase, numbers, and special characters</div>}
                error={showPasswordError && <div className="text-sm text-destructive">Password does not meet complexity requirements.</div>}
              />

              <PasswordRow
                label="Re-enter Password"
                value={confirmPassword}
                onChange={setConfirmPassword}
                showValue={showConfirmPassword}
                onToggle={() => setShowConfirmPassword((current) => !current)}
                placeholder="Confirm password"
                error={showConfirmError && <div className="text-sm text-destructive">Passwords do not match.</div>}
              />

              <Button
                type="submit"
                variant="default"
                disabled={!isFormValid || isSubmitting}
                className={`w-80 justify-center justify-self-center ${(isFormValid && !isSubmitting) ? '' : 'opacity-60 cursor-not-allowed'}`}
              >
                {isSubmitting ? 'REGISTERING...' : 'REGISTER'}
              </Button>

              {errorMessage && <p className="text-center text-sm text-destructive">{errorMessage}</p>}

              <p className="text-center text-sm text-muted-foreground">
                Already have an account?{' '}
                <Link to="/login" className="font-bold text-brand no-underline hover:underline">
                  Login
                </Link>
              </p>
            </form>
          </CardContent>
        </Card>
      </div>
    </section>
  )
}