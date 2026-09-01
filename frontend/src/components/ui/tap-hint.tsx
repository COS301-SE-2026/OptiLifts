import { useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'

type TapHintProps = Readonly<{
  message: string
  children: ReactNode
}>

export function TapHint({ message, children }: TapHintProps) {
  const [open, setOpen] = useState(false)
  const [coords, setCoords] = useState({ top: 0, left: 0 })
  const triggerRef = useRef<HTMLButtonElement>(null)

  const toggleOpen = () => {
    if (!open) {
      const rect = triggerRef.current?.getBoundingClientRect()
      if (rect) {
        setCoords({ top: rect.top + rect.height / 2, left: rect.right + 8 })
      }
    }
    setOpen((current) => !current)
  }

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        className="inline-flex items-center"
        onClick={(event) => {
          event.stopPropagation()
          toggleOpen()
        }}
      >
        {children}
      </button>
      {open && createPortal(
        <>
          <div className="fixed inset-0 z-[100]" onClick={() => setOpen(false)} />
          <div
            className="fixed z-[101] w-56 -translate-y-1/2 rounded-lg border border-border bg-card px-3 py-2 text-xs font-normal normal-case leading-snug text-foreground shadow-lg"
            style={{ top: coords.top, left: coords.left }}
          >
            {message}
          </div>
        </>,
        document.body
      )}
    </>
  )
}
