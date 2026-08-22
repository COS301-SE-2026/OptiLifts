import { test, expect } from './test-utils';

test.describe('Schedule Page', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/schedule');
        await page.waitForLoadState('networkidle');
    });

    test('can dynamically schedule a recurring workout', async ({ page }) => {
        const today = new Date();

        const futureDate = new Date();
        futureDate.setDate(today.getDate() + 14);

        //reformat the future date as yyyy-mm-dd for the date-time-picker
        const year = futureDate.getFullYear();
        const month = String(futureDate.getMonth() + 1).padStart(2, '0');
        const day = String(futureDate.getDate()).padStart(2, '0');
        const formattedFutureDate = `${year}-${month}-${day}`;

        //race condition fix
        const nextWeekButton = page.locator('button:has(svg.lucide-chevron-right)');
        await nextWeekButton.click();

        const addWorkoutButton = page.getByRole('button', { name: /^Add workout for / }).first();

        await expect(addWorkoutButton).toBeVisible();
        await addWorkoutButton.click();

        const selectWorkoutDialog = page.getByRole('dialog', { name: 'Select Workout' });
        await expect(selectWorkoutDialog).toBeVisible();
        await selectWorkoutDialog.getByRole('button', { name: 'Push Day A' }).first().click();

        await page.getByRole('checkbox', { name: 'Repeat' }).check();
        await page.locator('input[type="date"]').fill(formattedFutureDate);

        await page.getByRole('button', { name: 'Schedule Workout' }).click();

        const scheduledWorkoutCard = page.getByRole('button', { name: 'Push Day A' }).first();
        await expect(scheduledWorkoutCard).toBeVisible();
        
        const scheduledWorkoutRow = scheduledWorkoutCard.locator('xpath=..');
        await scheduledWorkoutRow.getByRole('button', { name: 'Delete Push Day A from schedule' }).click();
        await page.getByRole('button', { name: 'Delete', exact: true }).click();
    });
});