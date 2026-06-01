import CreateWorkoutPage from '@/pages/create-workout'
import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom'
import './App.css'
import { Navbar } from '@/components/ui/navbar'
import { PageTitle } from '@/components/ui/page-title'
import { useAuth } from '@/context/auth-context'
import { RegisterPage } from '@/pages/auth/RegisterPage'
import { LoginPage } from '@/pages/auth/LoginPage'
import WorkoutsPage from '@/pages/workouts'
import BrandStylePage from '@/pages/brand-style/brand-style'

function AppLayout() {
  return (
    <div className="min-h-dvh bg-background text-foreground">
      <Navbar />
      <main>
        <Outlet />
      </main>
    </div>
  )
}

function RequireAuth() {
  const { isAuthenticated, isHydrated } = useAuth()
  const location = useLocation()

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
  return (
    <Routes>
      <Route element={<AppLayout />}>
        <Route index element={<Navigate to="/register" replace />} />
        <Route path="register" element={<RegisterPage />} />
        <Route path="login" element={<LoginPage />} />
        <Route element={<RequireAuth />}>
          <Route path="dashboard" element={<PlaceholderPage title="Dashboard" description="Dashboard shell." />} />
          <Route path="workouts" element={<WorkoutsPage />} />
          <Route path="workouts/create" element={<CreateWorkoutPage />} />
          <Route path="schedule" element={<PlaceholderPage title="Schedule" description="Schedule shell." />} />
          <Route path="progress" element={<PlaceholderPage title="Progress" description="Progress shell." />} />
          <Route path="profile" element={<PlaceholderPage title="Profile" description="Profile shell." />} />
        </Route>        
      </Route>
      <Route path="brand-style" element={<BrandStylePage />} />
    </Routes>
  )
}

export default App