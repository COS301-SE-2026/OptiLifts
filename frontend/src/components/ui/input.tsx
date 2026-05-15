import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { Search } from "lucide-react"

import { cn } from "@/lib/utils"

const inputVariants = cva(
  "h-8 w-full min-w-0 bg-transparent py-1 text-base transition-colors outline-none file:inline-flex file:h-6 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none",
  {
    variants: {
      variant: {
        default:
          "rounded-lg border border-input px-2.5 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:bg-input/50 aria-invalid:border-destructive aria-invalid:ring-3 aria-invalid:ring-destructive/20 dark:bg-input/30 dark:disabled:bg-input/80 dark:aria-invalid:border-destructive/50 dark:aria-invalid:ring-destructive/40",
        underscore:
          "mx-auto w-10 rounded-none border-0 border-b-2 border-foreground px-0 text-center shadow-none focus-visible:ring-0 focus-visible:border-primary",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

export interface InputProps
  extends React.ComponentProps<"input">,
    VariantProps<typeof inputVariants> {}

function Input({ className, variant, type, ...props }: InputProps) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(inputVariants({ variant, className }))}
      {...props}
    />
  )
}

function SearchInput({ className, ...props }: InputProps) {
  return (
    <div className="relative w-full max-w-sm">
      <Input 
        type="text" 
        variant="default" 
        className={cn("pr-10", className)} 
        {...props} 
      />
      <button 
        type="button" 
        className="absolute right-0 top-0 flex h-full items-center justify-center px-3 text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:rounded-r-lg"
      >
        <Search className="h-4 w-4" />
      </button>
    </div>
  )
}

export { Input, SearchInput }