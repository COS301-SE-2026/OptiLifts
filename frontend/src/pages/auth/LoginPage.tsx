import { PageTitle } from '@/components/ui/page-title'

export function LoginPage() {
  return (
    <section className="mx-auto min-h-[calc(100dvh-5rem)] max-w-3xl px-6 pt-4 pb-10">
      <div className="flex min-h-[calc(100dvh-7rem)] flex-col items-center justify-center">
        <p className="mb-4 text-sm font-semibold uppercase tracking-[0.2em] text-brand">Route ready</p>
        <PageTitle title="LOGIN" />
        <p className="mt-4 max-w-2xl text-lg text-muted-foreground">Login shell. Build form components here.</p>
      </div>
    </section>
  )
}
