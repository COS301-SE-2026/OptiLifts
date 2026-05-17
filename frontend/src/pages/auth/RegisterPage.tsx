import { PageTitle } from '@/components/ui/page-title'

export function RegisterPage() {
  return (
    <section className="mx-auto min-h-[calc(100dvh-5rem)] max-w-3xl px-6 pt-4 pb-10">
      <div className="flex min-h-[calc(100dvh-7rem)] flex-col items-center justify-center">
        <p className="mb-4 text-sm font-semibold uppercase tracking-[0.2em] text-brand">Route ready</p>
        <PageTitle title="REGISTER" />
        <p className="mt-4 max-w-2xl text-lg text-muted-foreground">Register shell. Revert to full form later.</p>
      </div>
    </section>
  )
}