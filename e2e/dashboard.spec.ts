import { test, expect } from '@playwright/test';

test.describe('Dashboard Page', () => {

    test.beforeEach(async ({ page }) => {
        await page.goto('/dashboard');

        //guarantees react has finished rendering the final state of your data so that there is no race conditions
        await page.waitForLoadState('networkidle');

        await expect(page.getByText('Loading dashboard data')).toBeHidden();
        await expect(page.getByText('Loading...')).toBeHidden();
    });

    test('displays the main user greeting', async ({ page }) => {
        await expect(page.getByText(/Good Day, Test Athlete/i)).toBeVisible();
    });
});