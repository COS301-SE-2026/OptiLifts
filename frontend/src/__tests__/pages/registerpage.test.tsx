import { submitAuthRequest } from "@/pages/auth/auth-request";
import { RegisterPage } from "@/pages/auth/RegisterPage";
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, render, screen, fireEvent } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import type { ReactNode } from 'react';

const mockNavigate = vi.fn(); //mock trackin function (spies on nav calls)
vi.mock('react-router-dom', async() => {
    const actual = await vi.importActual<typeof import('react-router-dom')>('react-router-dom');
    return {
        ...actual,
        useNavigate: () => mockNavigate, //inject trackable spy func
        useLocation: () => ({
            state: {
                from: {pathname: '/workouts'} //mock url loc state
            }
        }),
        Link: ({
            children, to
        }: Readonly<{ children: ReactNode; to: string }>) => <a href={to}>{children}</a>,
        Navigate: ({to}: Readonly<{ to: string }>) => <div data-testid="navigate" data-to={to}/>,
    };
});
 

//mock auth context hook -> simulate logged in/out/loadin user
vi.mock('@/context/auth-context', () =>({
    useAuth: vi.fn(),
}));

//mock submit auth utility
vi.mock('@/pages/auth/auth-request', () => ({
    submitAuthRequest: vi.fn()
}));

//'describe' defines suite of related tests
describe('RegisterPage', () => {
    const mockAuth = useAuth as unknown as Mock;

    afterEach(() => {
        cleanup();
    });

    beforeEach(() => { //runs each 'it' test block + resets spy functions
        vi.clearAllMocks();
    });

    it('redirects when not hydrated or authenticated', () => {
        mockAuth.mockReturnValue({
            isHydrated: false,
            isAuthenticated: false
        });

        const {rerender} = render(<RegisterPage />);
        expect(screen.getByTestId('navigate').getAttribute('data-to')).toBe('/');

        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });
        rerender(<RegisterPage />)
        expect(screen.getByTestId('navigate').getAttribute('data-to')).toBe('/workouts');
    });

    it('shows error messages for invalid inputs', () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: false
        });

        render(<RegisterPage />)

        const userIn = screen.getByPlaceholderText('your username');
        const emailIn = screen.getByPlaceholderText('you@example.com');
        const passIn = screen.getByPlaceholderText('Enter password');
        const confirmIn = screen.getByPlaceholderText('Confirm password');

        fireEvent.change(userIn, {
            target: {
                value: 'a'.repeat(31) //beyond char limit ie invalid
            }
        });
        expect(screen.queryByText(/Username must be 1-30 characters/i)).not.toBeNull();

        fireEvent.change(emailIn, {
            target: {
                value: 'invalid'
            }
        });
        expect(screen.queryByText(/Please enter a valid email address/i)).not.toBeNull();

        fireEvent.change(passIn, {
            target: {
                value: 'short' //weak password
            }
        });
        expect(screen.queryByText(/Password does not meet complexity requirements/i)).not.toBeNull();

        fireEvent.change(passIn, {
            target: {
                value: 'ValidPass123!'
            }
        });
        fireEvent.change(confirmIn, {
            target: {
                value: 'DiffPass123!'
            }
        });
        expect(screen.queryByText(/Passwords do not match/i)).not.toBeNull();
        
    });
    
    it('submits form with valid data', () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: false,
            login: vi.fn()
        });

        render(<RegisterPage />)

        const userIn = screen.getByPlaceholderText('your username');
        const emailIn = screen.getByPlaceholderText('you@example.com');
        const passIn = screen.getByPlaceholderText('Enter password');
        const confirmIn = screen.getByPlaceholderText('Confirm password');

        fireEvent.change(userIn, {
            target: {
                value: 'validusername'
            }
        });
        fireEvent.change(emailIn, {
            target: {
                value: 'yuser@test.com'
            }
        });
        fireEvent.change(passIn, {
            target: {
                value: 'ValidPass123!'
            }
        });
        fireEvent.change(confirmIn, {
            target: {
                value: 'ValidPass123!'
            }
        });

        const registerBtn = screen.getByRole('button', {
            name: 'REGISTER'
        }) as HTMLButtonElement;
        expect(registerBtn.disabled).toBe(false);
        fireEvent.click(registerBtn);

        expect(submitAuthRequest).toHaveBeenCalledWith(expect.objectContaining({
            endpoint: '/api/auth/register',
            body: {
                displayName: 'validusername',
                email: 'yuser@test.com',
                password: 'ValidPass123!'
            },
        }));
    });    
});