import { test, expect } from '@playwright/test';

test.describe('Schedule Page', () => {
    let scheduledSessionId: string | null = null;

    test.beforeEach(async ({ page }) => {
        await page.goto('/schedule');
        await page.waitForLoadState('networkidle');
    });

    test('can dynamically schedule a recurring workout', async ({ page }) => {
        //calculate today's day of the week. Used for the button name
        const today = new Date();
        const currentDayName = today.toLocaleDateString('en-US', { weekday: 'long' });
        const dynamicAddButtonName = `Add workout for ${currentDayName}`;

        const futureDate = new Date();
        futureDate.setDate(today.getDate() + 14);

        //reformat the future date as yyyy-mm-dd for the date-time-picker
        const year = futureDate.getFullYear();
        const month = String(futureDate.getMonth() + 1).padStart(2, '0');
        const day = String(futureDate.getDate()).padStart(2, '0');
        const formattedFutureDate = `${year}-${month}-${day}`;

        await page.getByRole('button', { name: dynamicAddButtonName }).click();

        await page.getByRole('button', { name: 'Full Body' }).first().click();

        await page.getByRole('checkbox', { name: 'Repeat' }).check();
        await page.locator('input[type="date"]').fill(formattedFutureDate);

        await page.getByRole('button', { name: 'Schedule Workout' }).click();

        const scheduledWorkoutCard = page.getByRole('button', { name: 'Full Body' }).first();
        await expect(scheduledWorkoutCard).toBeVisible();
        
        await page.getByRole('button', { name: 'Delete Full Body from schedule' }).click();
        await page.getByRole('button', { name: 'Delete', exact: true }).click();
    });
});