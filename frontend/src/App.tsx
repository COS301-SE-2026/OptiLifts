import { Outlet, Route, Routes } from 'react-router-dom'
import './App.css'
import { Navbar } from '@/components/ui/navbar'
import { PageTitle } from '@/components/ui/page-title'
import { Button } from '@/components/ui/button'
import { ThemeToggle } from '@/components/ui/theme-toggle'
import { DefaultTextBox, NumericalUnderscoreInput } from '@/components/ui/input'
import { SearchInput as SimpleSearchInput } from '@/components/ui/search-input'
import { CircularProfileImage } from '@/components/ui/circular-image'
import { CreateExercise, type CreateExerciseFormData } from '@/components/ui/create-exercise'
import { MoreHorizontal, Plus, X } from 'lucide-react'
import edwinImg from '../../docs/images/Edwin_circle.svg'
import { useState } from 'react'

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

function HomePage() {
  const [isCreateExerciseOpen, setIsCreateExerciseOpen] = useState(false)

  const handleSaveExercise = (values: CreateExerciseFormData) => {
    console.log('Create exercise payload:', values)
    setIsCreateExerciseOpen(false)
  }

  return (
    <section className="mx-auto max-w-5xl px-6 py-16">
      <PageTitle title="Welcome to OptiLifts" />
      <p className="mt-4 text-muted-foreground">This is a minimal demo shell. Add your pages under the routes.</p>

      <div className="mt-6">
        <Button variant="default" onClick={() => setIsCreateExerciseOpen(true)}>
          Create custom exercise
        </Button>
      </div>

      <section style={{ padding: '2rem', display: 'grid', gap: '1rem' }}>
        <h2>Buttons</h2>
        <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
          <Button variant="default">Start Session</Button>
          <Button variant="secondary">Save Workout</Button>
          <Button variant="outline">+ Add Set</Button>
          <Button variant="text">+ Create Exercise</Button>
          <ThemeToggle />
          <Button variant="icon" size="icon" aria-label="Add">
            <Plus size={16} />
          </Button>
          <Button variant="icon" size="icon" aria-label="More options">
            <MoreHorizontal size={16} />
          </Button>
          <Button variant="icon" size="icon" aria-label="Close">
            <X size={16} />
          </Button>
        </div>

        <div style={{ padding: '1rem', display: 'grid', gap: '1rem' }}>
          <h3>Edwin's components</h3>
          <SimpleSearchInput />
          <DefaultTextBox />
          <div style={{ justifySelf: 'start' }}>
            <NumericalUnderscoreInput className="mx-0" />
          </div>
          <CircularProfileImage src={edwinImg} alt="Edwin" />
        </div>
      </section>

      <CreateExercise
        isOpen={isCreateExerciseOpen}
        onCancel={() => setIsCreateExerciseOpen(false)}
        onSave={handleSaveExercise}
      />
    </section>
  )
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
        <Route index element={<HomePage />} />
        <Route path="dashboard" element={<PlaceholderPage title="Dashboard" description="Dashboard shell." />} />
        <Route path="workouts" element={<PlaceholderPage title="Workouts" description="Workouts shell." />} />
        <Route path="schedule" element={<PlaceholderPage title="Schedule" description="Schedule shell." />} />
        <Route path="progress" element={<PlaceholderPage title="Progress" description="Progress shell." />} />
        <Route path="profile" element={<PlaceholderPage title="Profile" description="Profile shell." />} />
      </Route>
    </Routes>
  )
}

export default App