import type { ReactNode } from 'react'

interface PageTitleProps {
  readonly title?: ReactNode;
  readonly children?: ReactNode;
}

export function PageTitle({ title, children }: PageTitleProps) {
  return (
    <div className="inline-flex items-center gap-4">
      <span className="w-1 h-9 bg-brand rounded-full flex-shrink-0" />
      <h1 className="font-display text-[42px] leading-none tracking-[2px] text-foreground select-none uppercase">
        {title || children}
      </h1>
    </div>
  )
}

