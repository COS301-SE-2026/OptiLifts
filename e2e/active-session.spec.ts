import { test, expect } from './test-utils';

test.describe('Active Session Page', () => {
    let createdEntryId: string | null = null;

    test.beforeEach(async ({ page }) => {
        await page.goto('/workouts');
        await expect(page.getByText('Loading workouts...')).toBeHidden({ timeout: 15000 });
    });

    test.afterEach(async ({ request }) => {
        if (createdEntryId) {
            const response = await request.delete(`/api/users/me/schedule/sessions/${createdEntryId}`);
            expect(response.ok()).toBeTruthy();
            createdEntryId = null;
        }
    });

    test('can start active session, modify sets and exercises, complete workout, and verify on profile page', async ({ page }) => {
        const startButton = page.locator('#start-workout-btn-0');
        await expect(startButton).toBeVisible();

        //to get the first workouts name
        const workoutCard = startButton.locator('xpath=ancestor::*[@data-slot="card"]');
        const workoutName = (await workoutCard.locator('[data-slot="card-title"]').innerText()).trim();

        await startButton.click();

        await expect(page.getByRole('heading', { name: workoutName }).first()).toBeVisible();
        await expect(page.locator('p', { hasText: /^Duration$/ })).toBeVisible();
        await expect(page.locator('p', { hasText: /^Volume$/ })).toBeVisible();
        await expect(page.locator('p', { hasText: /^Sets$/ })).toBeVisible();

        const finishButton = page.getByRole('button', { name: 'Finish' });
        await expect(finishButton).toBeDisabled();

        const addSetButtons = page.getByRole('button', { name: 'Add Set' });
        if (await addSetButtons.count() > 0)
            await addSetButtons.first().click();

        await page.getByRole('button', { name: 'Add Exercise' }).click();
        const dialog = page.getByRole('button', { name: 'Close exercise picker' }).locator('xpath=..');
        await expect(dialog).toBeVisible();

        await page.getByRole('button', { name: 'Add Barbell Back Squat' }).click();
        await expect(page.getByText('Barbell Back Squat').first()).toBeVisible();

        const squatCard = page.locator('[data-slot="card"]', { hasText: 'Barbell Back Squat' });
        const squatInputs = squatCard.getByRole('textbox');
        await squatInputs.nth(1).fill('60'); 
        await squatInputs.nth(2).fill('8'); 

        const squatCheckButton = squatCard.locator('button').filter({ has: page.locator('svg.lucide-check') });
        await squatCheckButton.first().click();


        await expect(finishButton).toBeEnabled();

        const [logResponse] = await Promise.all([page.waitForResponse(res => res.url().includes('/logs') && res.request().method() === 'POST' && (res.status() === 200 || res.status() === 201)), finishButton.click()]);
        expect(logResponse.ok()).toBeTruthy();

        const responseBody = await logResponse.json();
        createdEntryId = responseBody.entryId;

        await expect(page).toHaveURL(/\/workouts$/);
        await expect(page.getByText('Loading workouts...')).toBeHidden({ timeout: 15000 });

        //should show the recent workout on the user's profile page
        await page.goto('/profile');
        await expect(page.getByText('Loading profile...')).toBeHidden({ timeout: 15000 });
        await expect(page.getByRole('heading', { name: 'Recent Workouts' })).toBeVisible();
        await expect(page.getByRole('heading', { name: workoutName }).first()).toBeVisible();
    });
});
