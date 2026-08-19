import type { Dispatch, SetStateAction } from 'react'
import { GoogleSignInButton, type GoogleSignInButtonProps } from './GoogleSignInButton'

export type SocialAuthSectionProps = Readonly<{
  text?: GoogleSignInButtonProps['text']
  dividerText?: string
  fromPath?: string
  setErrorMessage?: Dispatch<SetStateAction<string | null>>
  setIsSubmitting?: Dispatch<SetStateAction<boolean>>
}>

export function SocialAuthSection({
  text = 'signin_with',
  dividerText = 'Or continue with',
  fromPath,
  setErrorMessage,
  setIsSubmitting,
}: SocialAuthSectionProps) {
  return (
    <>
      <div className="relative my-2 flex items-center justify-center">
        <div className="absolute inset-0 flex items-center">
          <span className="w-full border-t border-border" />
        </div>
        <div className="relative flex justify-center text-xs uppercase">
          <span className="bg-card px-2 text-muted-foreground font-semibold">{dividerText}</span>
        </div>
      </div>

      <div className="flex justify-center w-full">
        <GoogleSignInButton
          text={text}
          fromPath={fromPath}
          setErrorMessage={setErrorMessage}
          setIsSubmitting={setIsSubmitting}
        />
      </div>
    </>
  )
}
