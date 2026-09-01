import { lazy, Suspense, useEffect } from 'react'
import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom'
import './App.css'
import { Navbar } from '@/components/ui/navbar'
import { PageTitle } from '@/components/ui/page-title'
import { useAuth } from '@/context/auth-context'
import { RegisterPage } from '@/pages/auth/RegisterPage'
import { LoginPage } from '@/pages/auth/LoginPage'
import ActiveSessionPage from '@/pages/active-session'
import { Loader2 } from 'lucide-react'
import { Toaster } from '@/components/ui/alert'
import { initOfflineWorkoutLogSync } from '@/lib/offline/workout-logs'
import { ErrorBoundary } from '@/components/ui/error-boundary'
import { warmOfflineCache } from '@/lib/offline/workouts-cache'

const CreateWorkoutPage = lazy(() => import('@/pages/create-workout'))
const WorkoutsPage = lazy(() => import('@/pages/workouts'))
const WorkoutDetailPage = lazy(() => import('@/pages/workout-detail'))
const BrandStylePage = lazy(() => import('@/pages/brand-style/brand-style'))
const WorkoutLogDetailPage = lazy(() => import('@/pages/workout-log-detail'))
const ProfilePage = lazy(() => import('@/pages/profile'))
const PastWorkoutsPage = lazy(() => import('@/pages/past-workouts'))
const SchedulePage = lazy(() => import('@/pages/schedule'))
const DashboardPage = lazy(() => import('@/pages/dashboard'))
const LandingPage = lazy(() => import('@/pages/landing'))
const HelpPage= lazy(() => import('@/pages/help'))
const PlateauPage = lazy(() => import('@/pages/plateau'))

function AppLayout() {
  return (
    <div className="min-h-dvh bg-background text-foreground">
      <Navbar />
      <Toaster />
      <main>
        <ErrorBoundary>
          <Suspense fallback={
            <section className="mx-auto flex min-h-[calc(100dvh-4rem)] items-center justify-center py-16">
              <div className="flex flex-col items-center gap-4">
                <Loader2 className="h-8 w-8 animate-spin text-brand" />
                <p className="text-sm uppercase tracking-[0.2em] text-muted-foreground animate-pulse">
                  Loading...
                </p>
              </div>
            </section>
          }>
            <Outlet />
          </Suspense>
        </ErrorBoundary>
      </main>
    </div>
  )
}

function RequireAuth() {
  const { isAuthenticated, isHydrated } = useAuth()
  const location = useLocation()

  useEffect(() => {
    if (isAuthenticated) {
      void warmOfflineCache()
    }
  }, [isAuthenticated])

  if (!isHydrated) {
    return (
      <section className="mx-auto flex min-h-[calc(100dvh-4rem)] max-w-5xl items-center justify-center px-6 py-16">
        <p className="text-sm uppercase tracking-[0.2em] text-muted-foreground">Checking session</p>
      </section>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/register" replace state={{ from: location }} />
  }

  return <Outlet />
}

function RequireGuest({ children }: Readonly<{ children: React.ReactNode }>) {
  const { isAuthenticated, isHydrated } = useAuth()

  if (!isHydrated) {
    return (
      <section className="mx-auto flex min-h-[calc(100dvh-4rem)] max-w-5xl items-center justify-center px-6 py-16">
        <p className="text-sm uppercase tracking-[0.2em] text-muted-foreground">Checking session</p>
      </section>
    )
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />
  }

  return children
}


type PlaceholderPageProps = Readonly<{
  title: string
  description: string
}>

function PlaceholderPage({ title, description }: PlaceholderPageProps) {
  return (
    <section className="mx-auto flex min-h-[calc(100dvh-4rem)] max-w-5xl flex-col justify-center px-6 py-16">
      <p className="mb-4 text-sm font-semibold uppercase tracking-[0.2em] text-brand">Route ready</p>
      <PageTitle title={title} />
      <p className="mt-4 max-w-2xl text-lg text-muted-foreground">{description}</p>
    </section>
  )
}

function App() {
  useEffect(() => initOfflineWorkoutLogSync(), [])
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route path="register" element={<RequireGuest><RegisterPage /></RequireGuest>} />
        <Route path="login" element={<RequireGuest><LoginPage /></RequireGuest>} />
        <Route element={<RequireAuth />}>
          <Route path="dashboard" element={<DashboardPage />} />
          <Route path="workouts" element={<WorkoutsPage />} />
          <Route path="workouts/:workoutId" element={<WorkoutDetailPage />} />
          <Route path="workouts/:workoutId/logs/:logId" element={<WorkoutLogDetailPage />} />
          <Route path="workouts/:workoutId/logs/:logId/edit" element={<ActiveSessionPage mode="edit" />} />
          <Route path="workouts/create" element={<CreateWorkoutPage />} />
          <Route path="workouts/edit/:id" element={<CreateWorkoutPage />} />
          <Route path="active-session" element={<ActiveSessionPage />} />
          <Route path="schedule" element={<SchedulePage />} />
          <Route path="progress" element={<PlaceholderPage title="Progress" description="Progress shell." />} />
          <Route path="plateau" element={<PlateauPage />} />
          <Route path="help" element={<HelpPage />} />
          <Route path="profile" element={<ProfilePage />} />
          <Route path="past-workouts" element={<PastWorkoutsPage />} />
        </Route>
      </Route>

      <Route path="/" element={
        <RequireGuest>
          <Suspense fallback={
            <div className="flex min-h-dvh items-center justify-center bg-background">
              <Loader2 className="h-8 w-8 animate-spin text-brand" />
            </div>
          }>
            <LandingPage />
          </Suspense>
        </RequireGuest>
      } />

      <Route path="brand-style" element={
        <Suspense fallback={
          <div className="flex min-h-dvh items-center justify-center bg-background">
            <Loader2 className="h-8 w-8 animate-spin text-brand" />
          </div>
        }>
          <BrandStylePage />
        </Suspense>
      } />
    </Routes>
  )
}

export default App