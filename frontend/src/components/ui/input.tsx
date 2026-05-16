import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"

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

function DefaultTextBox({ className, type = "text", ...props }: InputProps) {
  return (
    <div className="relative w-full max-w-sm">
      <Input type={type} variant="default" className={cn(className)} {...props} />
    </div>
  )
}

function NumericalUnderscoreInput({ className, type = "text", onChange, ...props }: InputProps) {
  const [val, setVal] = React.useState<string>(
    props.value !== undefined ? String(props.value) : props.defaultValue !== undefined ? String(props.defaultValue) : ""
  )

  React.useEffect(() => {
    if (props.value !== undefined) setVal(String(props.value))
  }, [props.value])

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const digits = e.target.value.replace(/\D+/g, "")
    setVal(digits)
    if (onChange) {
      const syntheticEvent = Object.assign({}, e, {
        target: Object.assign({}, e.target, { value: digits }),
      })
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      onChange(syntheticEvent)
    }
  }

  return (
    <div className="relative w-full max-w-sm">
      <Input
        type={type}
        variant="underscore"
        className={cn(className)}
        value={val}
        onChange={handleChange}
        inputMode="numeric"
        pattern="\\d*"
        {...props}
      />
    </div>
  )
}

export { Input, DefaultTextBox, NumericalUnderscoreInput }