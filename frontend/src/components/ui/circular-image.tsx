import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { User } from "lucide-react"

type CircularImageProps = Readonly<{
  src?: string
  alt?: string
}>

export function CircularProfileImage({ src, alt }: CircularImageProps) {
  return (
    <Avatar className="h-16 w-16 border border-gray-300">
      {src ? <AvatarImage src={src} alt={alt ?? ""} /> : null}
      <AvatarFallback className="bg-white">
        <User className="h-8 w-8 text-gray-400" />
      </AvatarFallback>
    </Avatar>
  )
}
