import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"
import { User } from "lucide-react"

export function CircularProfileImage() {
  return (
    <Avatar className="h-16 w-16 border border-gray-300">
      <AvatarImage src="/path/to/your/image.jpg" alt="Profile picture" />
      <AvatarFallback className="bg-white">
        <User className="h-8 w-8 text-gray-400" />
      </AvatarFallback>
    </Avatar>
  )
}
