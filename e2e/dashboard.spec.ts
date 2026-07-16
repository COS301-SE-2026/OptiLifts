import { test, expect } from '@playwright/test';

test.describe('Dashboard Page', () => {

    test.beforeEach(async ({ page }) => {
        await page.goto('/dashboard');

        //guarantees react has finished rendering the final state of your data so that there is no race conditions
        await page.waitForLoadState('networkidle');

        await expect(page.getByText('Loading dashboard data')).toBeHidden();
        await expect(page.getByText('Loading...')).toBeHidden();
    });

    test('displays the main user greeting and workout status', async ({ page }) => {
        await expect(page.getByRole('heading', { name: /Good Day, Test Athlete/i })).toBeVisible();
        await expect(page.getByText(/Today's Workout:/i)).toBeVisible();
    });

    test('renders all four statistics cards', async ({ page }) => {
        await expect(page.getByRole('heading', { name: 'Favorite exercise' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Days exercised this week' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Personal records hit this week' })).toBeVisible();
        await expect(page.getByRole('heading', { name: 'Muscle Balance' })).toBeVisible();
    });

    test('validates action button states based on upcoming workouts', async ({ page }) => {
        const viewWorkoutBtn = page.getByRole('button', { name: 'View Workout' });
        const startSessionBtn = page.getByRole('button', { name: 'Start Session' });

        const hasNoWorkout = await page.getByText('No workout scheduled').isVisible();

        if (hasNoWorkout) {
            await expect(viewWorkoutBtn).toBeDisabled();
            await expect(startSessionBtn).toBeDisabled();
        } else {
            await expect(viewWorkoutBtn).toBeEnabled();
            await expect(startSessionBtn).toBeEnabled();
        }
    });
});