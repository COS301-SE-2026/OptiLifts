import { test, expect } from './test-utils';

test.describe('Workout Log Detail Page', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/past-workouts?week=2026-06-10');
        await page.getByText('Push Day A').first().click();
    });

    test('User can view a workout log\'s detail', async ({ page }) => {
        await expect (page.getByText('Push Day A')).toBeVisible();
        await expect (page.getByText('Duration55m')).toBeVisible();
        await expect (page.getByText('Volume1,080 KG')).toBeVisible();
        await expect (page.getByText('Sets2')).toBeVisible();

        await expect (page.getByText('Barbell Bench Press')).toBeVisible();
        await expect (page.getByRole('paragraph').filter({ hasText: 'Chest' })).toBeVisible();
        await expect (page.locator('div').filter({ hasText: /^160 KG x 8 reps @ 8 RPE$/ }).first()).toBeVisible();

        await expect (page.getByText('Barbell Back Squat')).toBeVisible();
        await expect (page.getByRole('paragraph').filter({ hasText: 'Quadriceps' })).toBeVisible();
        await expect (page.locator('div').filter({ hasText: /^1120 KG x 5 reps @ 8\.5 RPE$/ }).first()).toBeVisible();

        await expect (page.getByText('MuscleSetsChest1Quadriceps1')).toBeVisible();
    });
});