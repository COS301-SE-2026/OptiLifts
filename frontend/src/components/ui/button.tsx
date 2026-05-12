import * as React from "react"
import { Slot } from "@radix-ui/react-slot" 
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "@/lib/utils"




const buttonVariants = cva(
  "inline-flex w-fit items-center justify-center whitespace-nowrap rounded-[10px] font-sans transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand disabled:pointer-events-none disabled:opacity-50",
  {
    variants: {
      variant: {
        //eg. save workout button
        primary:
          "bg-surface-2 text-foreground hover:bg-border font-extrabold uppercase tracking-[0.12em]",
        brand:
          "bg-brand text-white hover:bg-brand-2 font-extrabold uppercase tracking-[0.12em]",
        //eg. add set button -> dotted button
        outlineDotted:
          "border border-dashed border-foreground/45 bg-transparent text-foreground hover:border-brand hover:text-brand font-medium normal-case tracking-[0.04em]",
        
        //eg. create exercise button -> text only
        ghost:
          "bg-transparent text-foreground hover:text-brand font-normal normal-case tracking-[0.06em]",
        //eg. icon buttons
        icon:
          "bg-surface border-[4px] border-foreground text-foreground hover:border-brand rounded-[14px]",
        // default: "bg-brand text-white hover:bg-brand-2",
        
      },
      size: {
        default: "h-12 px-8 gap-3 text-2xl leading-none",
        sm: "h-10 px-4 gap-2 text-xl leading-none",
        lg: "h-14 px-10 gap-3 text-3xl leading-none",
        icon: "size-11",
      },
    },
    defaultVariants: {
      variant: "primary",
      size: "default",
    },
  }
)

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement>,
    VariantProps<typeof buttonVariants> {
  asChild?: boolean
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant, size, asChild = false, ...props }, ref) => {
    const Comp = asChild ? Slot : "button"
    return (
      <Comp
        className={cn(buttonVariants({ variant, size, className }))}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button, buttonVariants }