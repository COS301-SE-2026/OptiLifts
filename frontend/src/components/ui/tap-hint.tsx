import { useEffect, useRef, useState, type ReactNode } from 'react'
import { createPortal } from 'react-dom'

type TapHintProps = Readonly<{
  message: string
  children: ReactNode
}>

export function TapHint({ message, children }: TapHintProps) {
  const [open, setOpen] = useState(false)
  const [coords, setCoords] = useState({ top: 0, left: 0 })
  const triggerRef = useRef<HTMLButtonElement>(null)
  const tooltipRef = useRef<HTMLDivElement>(null)

  const toggleOpen = () => {
    if (!open) {
      const rect = triggerRef.current?.getBoundingClientRect()
      if (rect) {
        setCoords({ top: rect.top + rect.height / 2, left: rect.right + 8 })
      }
    }
    setOpen((current) => !current)
  }

  useEffect(() => {
    if (!open) {
      return
    }

    const handleOutsideEvent = (event: MouseEvent | KeyboardEvent) => {
      if (event instanceof KeyboardEvent) {
        if (event.key === 'Escape') {
          setOpen(false)
        }
        return
      }

      const target = event.target as Node
      if (!triggerRef.current?.contains(target) && !tooltipRef.current?.contains(target)) {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', handleOutsideEvent)
    document.addEventListener('keydown', handleOutsideEvent)
    return () => {
      document.removeEventListener('mousedown', handleOutsideEvent)
      document.removeEventListener('keydown', handleOutsideEvent)
    }
  }, [open])

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
        <div
          ref={tooltipRef}
          className="fixed z-[101] w-56 -translate-y-1/2 rounded-lg border border-border bg-card px-3 py-2 text-xs font-normal normal-case leading-snug text-foreground shadow-lg"
          style={{ top: coords.top, left: coords.left }}
        >
          {message}
        </div>,
        document.body
      )}
    </>
  )
}
