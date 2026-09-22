import type { ComponentProps } from "react"
import { Input } from "@/components/ui/input"
import { Search } from "lucide-react"
import { cn } from "@/lib/utils"

type SearchInputProps = Readonly<
  ComponentProps<typeof Input> & {
    containerClassName?: string
  }
>

export function SearchInput({ className, containerClassName, ...props }: SearchInputProps) {
  return (
    <div className={cn("relative w-full max-w-sm", containerClassName)}>
      <Input 
        type="text" 
        className={className ? `pr-10 ${className}` : "pr-10"} 
        {...props} 
      />
      <Search className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
    </div>
  )
}