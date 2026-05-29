import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { User } from "lucide-react"
import { cn } from "@/lib/utils"
import type { ReactNode } from "react"

type CircularImageProps = Readonly<{
  src?: string
  alt?: string
  className?: string
  fallbackIcon?: ReactNode
}>

export function CircularProfileImage({ src, alt, className, fallbackIcon }: CircularImageProps) {
  return (
    <Avatar className={cn("border border-gray-300", className || "h-16 w-16")}> 
      {src ? <AvatarImage src={src} alt={alt ?? ""} className="object-cover" /> : null}
      <AvatarFallback className="bg-white">
        {fallbackIcon ?? <User className="h-1/2 w-1/2 text-gray-400" />}
      </AvatarFallback>
    </Avatar>
  )
}
