import { test, expect } from './test-utils';

test.describe('Workout Detail Page', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/workouts');
        await page.getByRole('button', { name: 'Push Day A Options Primary' }).first().click();//routes to the Push Day A's workout detail
        await page.waitForLoadState('networkidle');
    });

    test('User can view a workout\'s detail', async ({ page }) => {
        await expect (page.getByText('Push Day A').first()).toBeVisible();
        await expect (page.getByText('Volume1,080 KG')).toBeVisible();
        await expect (page.getByText('Sets2')).toBeVisible();

        await expect (page.getByText('Barbell Bench Press')).toBeVisible();
        await expect (page.getByRole('paragraph').filter({ hasText: 'Chest' })).toBeVisible();
        await expect (page.getByText('160 KG x 8 reps')).toBeVisible();

        await expect (page.getByText('Superset')).toBeVisible();
        await expect (page.getByText('Group Rest: 1:30 min')).toBeVisible();

        await expect (page.getByText('Barbell Back Squat')).toBeVisible();
        await expect (page.getByRole('paragraph').filter({ hasText: 'Quadriceps' })).toBeVisible();
        await expect (page.getByText('1120 KG x 5 reps')).toBeVisible();
        
        await expect (page.getByText('MuscleSetsChest1Quadriceps1')).toBeVisible();
    });
});