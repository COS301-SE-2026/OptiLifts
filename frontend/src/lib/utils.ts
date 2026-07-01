import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function adaptImgUrl(url: string | null | undefined): string {
  if (!url) return '';
  
  if (url.includes('http://azurite:10000')) {
    return url.replace('http://azurite:10000', 'http://127.0.0.1:10000');
  }
  
  return url;
}
