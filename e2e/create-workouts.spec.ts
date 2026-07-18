import { test, expect } from '@playwright/test';

test.describe('Create Workouts Page', () => {
    let createdExerciseId: string | null = null;
    let createdWorkoutId: string | null = null;

    test.beforeEach(async ({ page }) => {
        await page.goto('/workouts/create');
        await page.waitForLoadState('networkidle');
    });

    test.afterEach(async ({ request }) => {
        if (createdExerciseId) {
            const response = await request.delete(`http://localhost:5036/api/exercises/custom/${createdExerciseId}`);
            expect(response.ok()).toBeTruthy();
            createdExerciseId = null;
        }

        if (createdWorkoutId) {
            const response = await request.delete(`http://localhost:5036/api/workouts/${createdWorkoutId}`);
            expect(response.ok()).toBeTruthy();
            createdWorkoutId = null;
        }
    });

    test('can create and save a new workout', async ({ page }) => {
        const uniqueWorkoutName = `Deadlift Day ${Date.now()}`; //the date is appended so that if the test crashes the next run won't try to create a duplicate workout

        await page.getByRole('button', { name: 'Add Deadlift' }).click();
        await page.getByRole('button', { name: '+ Add Set' }).click();
        await page.getByRole('textbox').nth(2).click();
        await page.getByRole('textbox').nth(2).fill('100');
        await page.getByRole('textbox').nth(2).press('Tab');
        await page.getByRole('textbox').nth(3).fill('8');
        await page.getByRole('spinbutton', { name: 'Rest (seconds)' }).click();
        await page.getByRole('spinbutton', { name: 'Rest (seconds)' }).fill('60');
        await page.getByRole('textbox', { name: 'Workout Name' }).click();
        await page.getByRole('textbox', { name: 'Workout Name' }).fill(uniqueWorkoutName);

        const [response] = await Promise.all([page.waitForResponse(res => res.url().includes('/api/workouts') && res.request().method() === 'POST' && (res.status() === 200 || res.status() === 201)), page.getByRole('button', { name: 'Save Workout' }).click()]);

        const responseBody = await response.json();
        createdWorkoutId = responseBody.workoutId; //need the workout's id to delete it afterwards

        await expect(page.getByRole('button', { name: `${uniqueWorkoutName} Options Primary` })).toBeVisible();
    });
});