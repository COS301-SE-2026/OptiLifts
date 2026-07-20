import { Component, type ErrorInfo, type ReactNode } from 'react'

type Props = { children: ReactNode }
type State = { hasError: boolean }

export class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false }

  static getDerivedStateFromError(): State {
    return { hasError: true }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('App crashed:', error, info)
  }

  render() {
    if (this.state.hasError) {
      return (
        <section className="mx-auto flex min-h-[calc(100dvh-4rem)] max-w-md flex-col items-center justify-center gap-4 px-6 py-16 text-center">
          <h2 className="text-lg font-bold text-foreground">Couldn’t load this page</h2>
          <p className="text-sm text-muted-foreground">
            If you’re offline, reconnect and try again — your workout is saved on this device and will sync automatically.
          </p>
          <button
            type="button"
            onClick={() => window.location.reload()}
            className="rounded-md bg-brand px-4 py-2 text-sm font-semibold text-white hover:bg-brand-2"
          >
            Reload
          </button>
        </section>
      )
    }
    return this.props.children
  }
}
