import { Navigate, Link, useLocation, useNavigate } from 'react-router-dom'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { useAuth } from '@/context/auth-context'
import { submitAuthRequest } from './auth-request'
import { PasswordRow } from './PasswordRow'
import { SocialAuthSection } from './SocialAuthSection'

function RegisterHeading() {
  return (
    <h1 className="font-display text-[42px] leading-none tracking-[2px] text-foreground select-none border-b-4 border-brand pb-2 px-2 w-fit">
      REGISTER
    </h1>
  )
}

export function RegisterPage() {
  const { login, isAuthenticated, isHydrated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  
  const [displayName, setDisplayName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  const DISPLAY_NAME_MAX = 30
  const emailRegex = /^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$/
  const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/

  const isDisplayNameValid = displayName.trim().length > 0 && displayName.trim().length <= DISPLAY_NAME_MAX
  const isEmailValid = email.trim().length > 0 && emailRegex.test(email.trim())
  const isPasswordValid = password.length > 0 && passwordRegex.test(password)
  const doPasswordsMatch = confirmPassword.length > 0 && password === confirmPassword
  const isFormValid = isDisplayNameValid && isEmailValid && isPasswordValid && doPasswordsMatch

  const showDisplayNameError = !isDisplayNameValid && displayName.length > 0
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

    await submitAuthRequest({
      endpoint: '/api/auth/register',
      body: { displayName: displayName.trim(), email: email.trim(), password },
      login,
      navigate,
      fromPath,
      setErrorMessage,
      setIsSubmitting,
      fallbackErrorMessage: 'Unable to register. Please try again.',
      conflictErrorMessage: 'That email is already in use.',
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
        <RegisterHeading />

        <Card className="mt-6 w-full max-w-md">
          <CardContent>
            <form onSubmit={handleSubmit} className="grid gap-4">
              <label className="grid gap-1">
                <span className="text-sm font-semibold uppercase tracking-[0.08em] text-foreground">Display Name</span>
                <Input
                  required
                  value={displayName}
                  onChange={(event) => setDisplayName(event.target.value)}
                  maxLength={DISPLAY_NAME_MAX}
                  autoComplete="name"
                  placeholder="your display name"
                />
                <span className="text-sm text-muted-foreground">Maximum {DISPLAY_NAME_MAX} characters.</span>
                
                {showDisplayNameError && (
                  <span className="text-sm text-destructive -mt-2">Display name must be 1-{DISPLAY_NAME_MAX} characters.</span>
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
                autoComplete="new-password"
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
                autoComplete="new-password"
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

              <SocialAuthSection
                text="signup_with"
                fromPath={fromPath}
                setErrorMessage={setErrorMessage}
                setIsSubmitting={setIsSubmitting}
              />

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