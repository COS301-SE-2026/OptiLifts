import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { User } from "lucide-react"
import { cn, adaptImgUrl } from "@/lib/utils"
import type { ReactNode } from "react"

type CircularImageProps = Readonly<{
  src?: string
  alt?: string
  className?: string
  fallbackIcon?: ReactNode
}>

export function CircularProfileImage({ src, alt, className, fallbackIcon }: CircularImageProps) {
  const outSrc = src ? adaptImgUrl(src) : undefined;
  return (
    <Avatar className={cn("border border-border", className || "h-16 w-16")}> 
      {src ? <AvatarImage src={outSrc} alt={alt ?? ""} className="object-cover" /> : null}
      <AvatarFallback className="bg-background">
        {fallbackIcon ?? <User className="h-1/2 w-1/2 text-muted-foreground" />}
      </AvatarFallback>
    </Avatar>
  )
}
