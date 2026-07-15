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

    test('displays the main heading and basic elements', async ({ page }) => {
        await expect(page.getByText('Create Workout')).toBeVisible();
        await expect(page.getByRole('button', { name: 'Back to Workouts' })).toBeVisible();
        await expect(page.getByText('Recommended')).toBeVisible();
        await expect(page.getByText('Exercises', { exact: true })).toBeVisible();
        await expect(page.getByRole('button', { name: '+ Create Exercise' })).toBeVisible();
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

        const [response] = await Promise.all([
            page.waitForResponse(res =>
                res.url().includes('/api/workouts') &&
                res.request().method() === 'POST' &&
                (res.status() === 200 || res.status() === 201)
            ),
            page.getByRole('button', { name: 'Save Workout' }).click()
        ]);

        const responseBody = await response.json();
        createdWorkoutId = responseBody.workoutId; //need the workout's id to delete it afterwards

        await page.getByRole('button', { name: `${uniqueWorkoutName} Options Primary` }).click();
    });

    test('can create and save a new unique exercise', async ({ page }) => {
        const uniqueExerciseName = `Seated Cable Row ${Date.now()}`;

        await page.getByRole('button', { name: '+ Create Exercise' }).click();
        await page.getByRole('textbox', { name: /Exercise name/i }).fill(uniqueExerciseName);
        await page.getByRole('button', { name: 'Select', exact: true }).click();
        await page.getByRole('button', { name: /Lats/i }).click();
        await page.getByRole('button', { name: 'Select (optional)' }).click();
        await page.getByRole('button', { name: /Biceps/i }).click();
        await page.getByRole('button', { name: /Middle Back/i }).click();
        await page.getByRole('button', { name: 'Back', exact: true }).click();

        const [response] = await Promise.all([
            page.waitForResponse(res => res.url().includes('/api/Exercises/custom') && res.request().method() === 'POST' && res.status() >= 200 && res.status() < 300),
            page.getByRole('button', { name: 'Save Exercise', exact: true }).click()
        ]);

        const responseBody = await response.json();
        createdExerciseId = responseBody.id; 

        //the newly created exercise should now be visible
        await expect(page.getByText(uniqueExerciseName)).toBeVisible();
    });

    test('exercise filters display the correct exercises', async ({ page }) => {
        await page.getByRole('button', { name: 'All Muscles' }).click();
        await page.getByRole('menuitem', { name: 'Lats' }).click();
        await page.getByText('Lat Pulldown', { exact: true }).click();
        await page.getByRole('button', { name: 'All Equipment' }).click();
        await page.getByRole('menuitem', { name: 'Bodyweight' }).nth(1).click();
        await page.getByText('Pull Up').nth(1).click();
    });
});