import { submitAuthRequest } from "@/pages/auth/auth-request";
import { LoginPage } from "@/pages/auth/LoginPage";
import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from "vitest";
import { cleanup, render, screen, fireEvent } from '@testing-library/react';
import { useAuth } from '@/context/auth-context';
import type { ReactNode } from 'react';

//mocking dependencies -> prevents network requests + isolates component

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
describe('LoginPage', () => {
    const mockAuth = useAuth as unknown as Mock;

    afterEach(() => {
        cleanup();
    });

    beforeEach(() => { //runs each 'it' test block + resets spy functions
        vi.clearAllMocks();
    });

    it('redirects to / when not hydrated', () => {
        //arrange: mock loading user session ie not hydrated
        mockAuth.mockReturnValue({
            isHydrated: false,
            isAuthenticated: false
        });

        //act: render loginpage comp
        render(<LoginPage />);

        //assert: verify correct redirect
        expect(screen.getByTestId('navigate').getAttribute('data-to')).toBe('/');
    });

    it('redirects to fromPath when authenticated', () => {
        //arrange: sim already logged in user
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: true
        });

        //act: render loginpage comp
        render(<LoginPage />);

        //assert: verify correct redirect
        expect(screen.getByTestId('navigate').getAttribute('data-to')).toBe('/workouts');
    });

    it('renders login form and handles input changes', () => {
        //arrage: simulate not logged in user
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: false
        });

        //act: render comp
        render(<LoginPage />);

        //find inputs in dom using user-facing text ie placeholders
        const emailInput = screen.getByPlaceholderText('you@example.com') as HTMLInputElement;
        const passwordInput = screen.getByPlaceholderText('Enter password') as HTMLInputElement;
        //simulate user typing
        fireEvent.change(emailInput, {
            target: {
                value: 'user@test.com'
            }
        });
        fireEvent.change(passwordInput, {
            target: {
                value: 'password123'
            }
        });

        //assert:check is state correctly updated w/ input
        expect(emailInput.value).toBe('user@test.com');
        expect(passwordInput.value).toBe('password123');
    });

    it('toggles password visibility', () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: false
        });

        //act: render comp
        render(<LoginPage />);
        const passwordInput = screen.getByPlaceholderText('Enter password');
        const toggleBtn = screen.getByLabelText('Show password');
        
        expect(passwordInput.getAttribute('type')).toBe('password');
        fireEvent.click(toggleBtn);

        expect(passwordInput.getAttribute('type')).toBe('text');

    })

    it('submits form w/ valid data', () => {
        mockAuth.mockReturnValue({
            isHydrated: true,
            isAuthenticated: false,
            login: vi.fn()
        });

        //act: render comp
        render(<LoginPage />);

        const emailInput = screen.getByPlaceholderText('you@example.com');
        const passwordInput = screen.getByPlaceholderText('Enter password');
        //simulate user typing
        fireEvent.change(emailInput, {
            target: {
                value: 'user@test.com'
            }
        });
        fireEvent.change(passwordInput, {
            target: {
                value: 'password123'
            }
        });

        const submitBtn = screen.getByRole('button', {
            name: 'LOGIN'
        }) as HTMLButtonElement;
        expect(submitBtn.disabled).toBe(false);

        fireEvent.click(submitBtn);

        //assert: verify submit helper called
        expect(submitAuthRequest).toHaveBeenCalledWith(expect.objectContaining({
            endpoint: '/api/auth/login',
            body: {
                email: 'user@test.com',
                password: 'password123'
            },
        }));
    });

});

