import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { PasswordRow } from '@/pages/auth/PasswordRow'
import { SocialAuthSection } from '@/pages/auth/SocialAuthSection'

vi.mock('@/pages/auth/GoogleSignInButton', () => ({
  GoogleSignInButton: ({ text, theme }: { text?: string; theme?: string }) => (
    <div data-testid="mock-google-button" data-button-text={text} data-button-theme={theme} />
  ),
}))

describe('PasswordRow', () => {
  it('renders password row with label and toggles visibility', () => {
    const onToggle = vi.fn()
    const onChange = vi.fn()

    const { rerender } = render(
      <PasswordRow
        label="Password"
        value="secret123"
        onChange={onChange}
        showValue={false}
        onToggle={onToggle}
        placeholder="Enter password"
        error={<span>Invalid password</span>}
        disclaimer={<span>8+ chars</span>}
      />
    )

    expect(screen.getByText('Password')).toBeDefined()
    expect(screen.getByText('8+ chars')).toBeDefined()
    expect(screen.getByText('Invalid password')).toBeDefined()

    const input = screen.getByPlaceholderText('Enter password') as HTMLInputElement
    expect(input.type).toBe('password')
    expect(input.value).toBe('secret123')

    fireEvent.change(input, { target: { value: 'newpassword' } })
    expect(onChange).toHaveBeenCalledWith('newpassword')

    const toggleBtn = screen.getByLabelText('Show password')
    fireEvent.click(toggleBtn)
    expect(onToggle).toHaveBeenCalledTimes(1)

    rerender(
      <PasswordRow
        label="Password"
        value="secret123"
        onChange={onChange}
        showValue={true}
        onToggle={onToggle}
        placeholder="Enter password"
      />
    )

    const updatedInput = screen.getByPlaceholderText('Enter password') as HTMLInputElement
    expect(updatedInput.type).toBe('text')
    expect(screen.getByLabelText('Hide password')).toBeDefined()
  })
})

describe('SocialAuthSection', () => {
  it('renders divider and GoogleSignInButton with default and custom props', () => {
    const { rerender } = render(<SocialAuthSection />)

    expect(screen.getByText('Or continue with')).toBeDefined()
    expect(screen.getByTestId('mock-google-button').getAttribute('data-button-text')).toBe('signin_with')
    expect(screen.getByTestId('mock-google-button').getAttribute('data-button-theme')).toBeNull()

    rerender(<SocialAuthSection text="signup_with" theme="filled_black" dividerText="Or register with" />)
    expect(screen.getByText('Or register with')).toBeDefined()
    expect(screen.getByTestId('mock-google-button').getAttribute('data-button-text')).toBe('signup_with')
    expect(screen.getByTestId('mock-google-button').getAttribute('data-button-theme')).toBe('filled_black')
  })
})
