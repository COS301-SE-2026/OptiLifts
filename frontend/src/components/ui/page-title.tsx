interface PageTitleProps {
  readonly title: string;
}

export function PageTitle({ title }: PageTitleProps) {
  return (
    <div className="inline-flex items-center gap-4">
      <span className="w-1 h-9 bg-brand rounded-full flex-shrink-0" />
      <span className="font-display text-[42px] leading-none tracking-[2px] text-foreground select-none">
        {title}
      </span>
    </div>
  )
}
