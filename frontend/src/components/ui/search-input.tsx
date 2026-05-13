import { Input } from "@/components/ui/input"
import { Search } from "lucide-react"

export function SearchInput() {
  return (
    <div className="relative w-full max-w-sm">
      <Input 
        type="text" 
        placeholder="Search" 
        className="pr-10" 
      />
      <Search className="absolute right-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
    </div>
  )
}