import { Navigate, Link, useLocation, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { useAuth } from '@/context/auth-context'
import { submitAuthRequest } from './auth-request'
import { PasswordRow } from './PasswordRow'
import { SocialAuthSection } from './SocialAuthSection'

function LoginHeading() {
  return (
    <h1 className="font-display text-[42px] leading-none tracking-[2px] text-foreground select-none border-b-4 border-brand pb-2 px-2 w-fit">
      LOGIN
    </h1>
  )
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

  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const handleSubmit = async (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!isFormValid) return

    await submitAuthRequest({
      endpoint: '/api/auth/login',
      body: { email: email.trim(), password },
      login,
      navigate,
      fromPath,
      setErrorMessage,
      setIsSubmitting,
      fallbackErrorMessage: 'Unable to log in. Please try again.',
      unauthorizedErrorMessage: 'Invalid email or password.',
    })
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
                autoComplete="current-password"
              />

              <Link to="/forgot-password" className="text-sm text-brand no-underline hover:underline text-right">
                Forgot Password?
              </Link>

              <Button
                type="submit"
                variant="default"
                disabled={!isFormValid || isSubmitting}
                className={`w-80 justify-center justify-self-center ${(isFormValid && !isSubmitting) ? '' : 'opacity-60 cursor-not-allowed'}`}
              >
                {isSubmitting ? 'LOGGING IN...' : 'LOGIN'}
              </Button>

              <SocialAuthSection
                text="signin_with"
                fromPath={fromPath}
                setErrorMessage={setErrorMessage}
                setIsSubmitting={setIsSubmitting}
              />

              {errorMessage && <p className="text-center text-sm text-destructive">{errorMessage}</p>}

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
