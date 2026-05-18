import * as React from "react"
import { Slot } from "@radix-ui/react-slot" 
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"

// Usage:
// <Button>Start Session</Button>
// <Button variant="secondary">Save Workout</Button>
// <Button variant="outline">+ Add Set</Button>
// <Button variant="icon" size="icon" aria-label="Add" />
// <Button variant="password" size="icon" aria-label="Show password" />
// <Button variant="text">Create Exercise</Button>

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const buttonVariants = cva(
  "inline-flex items-center justify-center gap-2 rounded-[0.5rem] border border-transparent bg-clip-padding whitespace-nowrap transition-all duration-200 outline-none cursor-pointer select-none font-sans font-bold uppercase tracking-[0.05em] leading-none text-sm focus-visible:outline-2 focus-visible:outline-brand focus-visible:outline-offset-2 disabled:opacity-50 disabled:pointer-events-none",
  {
    variants: {
      variant: {
        default: "h-12 px-7 py-4 bg-brand text-white border-brand hover:bg-brand-2 hover:border-brand-2",
        secondary: "h-12 px-7 py-4 bg-surface-2 text-foreground border-surface-2 hover:bg-border hover:border-border",
        outline: "h-8 px-6 py-4 bg-transparent text-foreground border-2 border-dashed border-border hover:bg-surface-2",
        ghost: "h-auto px-3 py-3 bg-transparent text-foreground border-0 hover:bg-surface-2",
        password: "h-8 w-8 p-0 bg-transparent text-foreground border-0 hover:bg-transparent focus-visible:bg-transparent focus-visible:text-brand focus-visible:outline-none",
        text: "h-auto p-0 bg-transparent text-foreground border-0 normal-case tracking-normal hover:bg-transparent",
        icon: "h-8 w-8 p-0 bg-surface text-foreground border border-border hover:bg-surface-2",
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
  ({ className, variant, size, asChild = false, onClick, children, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    const resolvedVariant = variant ?? "default"
    const resolvedSize = size ?? "default"
    const extraClasses: string[] = []
    const ariaLabel = props["aria-label"]

    const [isLoading, setIsLoading] = React.useState(false)

    if (resolvedVariant === "icon") {
      if (ariaLabel === "Add") extraClasses.push("border-2", "border-foreground", "hover:bg-border")
      if (ariaLabel === "Close") extraClasses.push("border-0", "bg-transparent", "hover:bg-transparent", "focus-visible:outline-none")
    }

    const handleClick = (e: React.MouseEvent<HTMLButtonElement>) => {
      if (resolvedVariant === "icon" && ariaLabel === "Add") {
        e.persist()
        setIsLoading(true)
        globalThis.setTimeout(() => {
          setIsLoading(false)
          onClick?.(e)
        }, 600)
      } else {
        onClick?.(e)
      }
    }

    const disabledProp = props.disabled || isLoading

    return (
      <Comp
        className={cn(buttonVariants({ variant: resolvedVariant, size: resolvedSize }), ...extraClasses, className)}
        data-variant={resolvedVariant}
        data-size={resolvedSize}
        ref={ref}
        onClick={handleClick}
        disabled={disabledProp}
        {...props}
      >
        {resolvedVariant === "icon" && ariaLabel === "Add" && isLoading ? (
          <span className="inline-block size-4 rounded-full border-2 border-gray-300 border-t-gray-500 animate-spin" />
        ) : (
          children
        )}
      </Comp>
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }