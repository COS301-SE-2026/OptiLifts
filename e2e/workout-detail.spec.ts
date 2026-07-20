import { test, expect } from '@playwright/test';

test.describe('Workout Detail Page', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('/workouts/33333333-3333-3333-3333-333333333333');//push day A's workoutId
        await page.waitForLoadState('networkidle');
    });

    test('User can view a workout\'s details', async ({ page }) => {
        await expect (page.getByText('Push Day A')).toBeVisible();
        await expect (page.getByText('Volume1,080 KG')).toBeVisible();
        await expect (page.getByText('Sets2')).toBeVisible();

        await expect (page.getByText('Barbell Bench Press')).toBeVisible();
        await expect (page.getByRole('paragraph').filter({ hasText: 'Chest' })).toBeVisible();
        await expect (page.getByText('Rest time: 1:30 min')).toBeVisible();
        await expect (page.getByText('160 KG x 8 reps')).toBeVisible();
        await expect (page.getByText('Back Squat')).toBeVisible();
        
        await expect (page.getByRole('paragraph').filter({ hasText: 'Quadriceps' })).toBeVisible();
        await expect (page.getByText('Rest time: 2 min')).toBeVisible();
        await expect (page.getByText('1120 KG x 5 reps')).toBeVisible();
        await expect (page.getByText('MuscleSetsChest1Quadriceps1')).toBeVisible();
    });
});