import { MAX_VIDEO_BYTES } from '@/constants/optivision'

export function validateVideoFile(file: File): string | null {
  //some browsers leave file.type empty for .mov, only reject if clearly not video
  if (file.type && !file.type.startsWith('video/')) {
    return 'Please choose a video file.'
  }
  if (file.size > MAX_VIDEO_BYTES) {
    return `That video is too large. The limit is ${MAX_VIDEO_BYTES / 1024 / 1024} MB.`
  }
  return null
}
