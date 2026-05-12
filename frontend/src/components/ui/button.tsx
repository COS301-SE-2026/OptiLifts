import * as React from "react"
import { Slot } from "@radix-ui/react-slot" 
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

// Usage:
// <Button>Start Session</Button>
// <Button variant="secondary">Save Workout</Button>
// <Button variant="outline">+ Add Set</Button>
// <Button variant="icon" size="icon" aria-label="Add" />
// <Button variant="text">Create Exercise</Button>

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const BUTTON_STYLES = `
.optilifts-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  border-radius: 0.5rem;
  border: 1px solid transparent;
  font-family: 'Barlow', sans-serif;
  font-weight: 700;
  font-size: 0.875rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  line-height: 1;
  transition: all 0.2s;
  cursor: pointer;
  user-select: none;
  white-space: nowrap;
  outline: none;
}

.optilifts-btn:disabled { opacity: 0.5; pointer-events: none }
.optilifts-btn:focus-visible { outline: 2px solid var(--brand); outline-offset: 2px }

.optilifts-btn[data-variant="default"] { height: 3rem; padding: 1rem 1.75rem; background: var(--brand); color: white; border-color: var(--brand) }
.optilifts-btn[data-variant="default"]:hover:not(:disabled) { background: var(--brand-2); border-color: var(--brand-2) }

.optilifts-btn[data-variant="secondary"] { height: 3rem; padding: 1rem 1.75rem; background: var(--surface-2); color: var(--foreground); border-color: var(--surface-2) }
.optilifts-btn[data-variant="secondary"]:hover:not(:disabled) { background: var(--border); border-color: var(--border) }

.optilifts-btn[data-variant="outline"] { height: 2rem; padding: 1rem 1.5rem; background: transparent; color: var(--foreground); border-color: var(--border); border-width: 2px; border-style: dashed }
.optilifts-btn[data-variant="outline"]:hover:not(:disabled) { background: var(--surface-2) }

.optilifts-btn[data-variant="ghost"] { height: auto; padding: 0.75rem 0.75rem; background: transparent; color: var(--foreground); border: none }
.optilifts-btn[data-variant="ghost"]:hover:not(:disabled) { background: var(--surface-2) }

.optilifts-btn[data-variant="text"] { height: auto; padding: 0; background: transparent; color: var(--foreground); border: 0; text-transform: none; letter-spacing: normal }
.optilifts-btn[data-variant="text"]:hover:not(:disabled) { background: transparent }

.optilifts-btn[data-variant="icon"] { width: 2rem; height: 2rem; padding: 0; background: var(--surface); color: var(--foreground); border-color: var(--border); border-width: 1px }
.optilifts-btn[data-variant="icon"]:hover:not(:disabled) { background: var(--surface-2) }

.optilifts-btn[data-variant="icon"][aria-label="Add"] { border-width: 2px; border-color: var(--foreground) }
.optilifts-btn[data-variant="icon"][aria-label="Close"] { border-width: 0; border-color: transparent; background: transparent }
.optilifts-btn[data-variant="icon"][aria-label="Close"]:hover:not(:disabled) { background: transparent }
.optilifts-btn[data-variant="icon"][aria-label="Close"]:focus-visible { outline: none }

.optilifts-btn[data-size="sm"] { height: 2.5rem; padding: 0.75rem 1rem; font-size: 0.8rem }
.optilifts-btn[data-size="lg"] { height: 3.5rem; padding: 1.25rem 1.75rem; font-size: 1rem }
.optilifts-btn[data-size="icon"] { width: 2rem; height: 2rem; padding: 0 }
`

if (typeof window !== "undefined") {
  const __btnStylesId = "optilifts-button-styles"
  if (!document.getElementById(__btnStylesId)) {
    const s = document.createElement("style")
    s.id = __btnStylesId
    s.textContent = BUTTON_STYLES
    document.head.appendChild(s)
  }
}

const buttonVariants = cva(
  "optilifts-btn inline-flex items-center justify-center rounded-lg border bg-clip-padding whitespace-nowrap transition-all outline-none select-none font-sans font-bold uppercase tracking-[0.05em] leading-none focus-visible:ring-3 focus-visible:ring-ring/50 disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        default: "h-12 px-7 py-4 bg-brand text-white border-brand hover:bg-brand-2 hover:border-brand-2",
        secondary: "h-14 px-10 py-4 bg-surface-2 text-foreground border-surface-2 hover:bg-border hover:border-border",
        outline: "h-8 px-6 py-4 bg-transparent text-foreground border-2 border-dashed border-border hover:bg-surface-2",
        ghost: "h-auto px-3 py-3 bg-transparent text-foreground border-0 hover:bg-surface-2",
        text: "h-auto p-0 bg-transparent text-foreground border-0 hover:bg-transparent normal-case tracking-normal",
        icon: "h-8 w-8 p-0 bg-surface text-foreground border border-border hover:bg-surface-2 rounded-lg",
      },
      size: {
        default: "",
        sm: "h-10 px-4 py-2.5 text-[0.8rem]",
        lg: "h-14 px-7 py-5 text-base",
        icon: "h-8 w-8 p-0",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    const resolvedVariant = variant ?? "default"
    const resolvedSize = size ?? "default"
    return (
      <Comp
        className={cn(buttonVariants({ variant: resolvedVariant, size: resolvedSize, className }))}
        data-variant={resolvedVariant}
        data-size={resolvedSize}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }