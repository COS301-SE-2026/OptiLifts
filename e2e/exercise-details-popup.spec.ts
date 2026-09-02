import { test, expect } from './test-utils';

test.describe('Standard Exercise Details Popup', () => {
    test('testing if a user can view an exercise\'s details', async ({ page }) => {
        await page.goto('/workouts');
        await page.getByRole('button', { name: 'Push Day A Options Primary' }).click();
        await page.waitForLoadState('networkidle');
        await page.getByTestId('exercise-item-Barbell Bench Press').click();

        await expect(page.getByRole('heading', { name: 'Exercise Details' })).toBeVisible();

        await expect(page.getByRole('paragraph').filter({ hasText: 'Barbell Bench Press' })).toBeVisible();
        await expect(page.getByText('Chest').first()).toBeVisible();
        await expect(page.getByText('Barbell', { exact: true })).toBeVisible();
        await expect(page.getByText('compound')).toBeVisible();
        await expect(page.locator('span').filter({ hasText: 'Shoulders' })).toBeVisible();
        await expect(page.locator('span').filter({ hasText: 'Triceps' })).toBeVisible();

        await page.getByRole('button', { name: 'Close', exact: true }).click();
    });
});

test.describe('Custom Exercise Details Popup', () => {
    test('testing if a user can view, edit, and delete a custom exercise', async ({ page }) => {
        const customExerciseName = `Custom Curl ${Date.now()}`;
        const updatedExerciseName = `Updated ${customExerciseName}`;
        const customWorkoutName = `Custom Workout ${Date.now()}`;

        //this also tests the create-exercise-popup
        await page.goto('/workouts/create');
        await page.waitForLoadState('networkidle');

        await page.getByRole('button', { name: '+ Create Exercise' }).click();
        await page.getByPlaceholder('e.g. Seated Cable Row').fill(customExerciseName);
        await page.getByRole('button', { name: 'Select', exact: true }).click();
        await page.getByRole('button', { name: 'Biceps', exact: true }).click();
        await page.getByRole('button', { name: 'Save Exercise' }).click();

        await page.getByRole('button', { name: `Add ${customExerciseName}` }).click();
        await page.getByRole('textbox', { name: 'Workout Name' }).fill(customWorkoutName);

        await Promise.all([page.waitForResponse(res => res.url().includes('/api/workouts') && (res.status() === 200 || res.status() === 201)), page.getByRole('button', { name: 'Save Workout' }).click()]);

        await page.goto('/workouts');
        await page.waitForLoadState('networkidle');
        await expect(page.getByText('Loading workouts...')).toBeHidden({ timeout: 15000 });
        await page.getByTestId(`workout-card-${customWorkoutName}`).click();
        await page.waitForLoadState('networkidle');

        await page.getByTestId(`exercise-item-${customExerciseName}`).click();

        await expect(page.getByRole('heading', { name: 'Exercise Details' })).toBeVisible();
        await expect(page.getByRole('paragraph').filter({ hasText: customExerciseName })).toBeVisible();
        await expect(page.getByText('Custom exercise')).toBeVisible();

        //testing the edit a custom exercise functionality
        await page.getByRole('button', { name: 'Edit' }).click();
        await page.getByPlaceholder('e.g. Seated Cable Row').fill(updatedExerciseName);
        await page.getByRole('button', { name: 'Save Exercise' }).click();  
        await page.waitForLoadState('networkidle');

        // Re-open details popup for updated exercise
        await page.getByTestId(`exercise-item-${updatedExerciseName}`).click();
        await expect(page.getByRole('paragraph').filter({ hasText: updatedExerciseName })).toBeVisible();

        //testing the delete a custom exercise functionality
        await page.getByRole('button', { name: 'Delete' }).click();
        await expect(page.getByRole('heading', { name: 'Delete Exercise' })).toBeVisible();
        await page.getByRole('button', { name: 'Delete', exact: true }).last().click();

        //exercise details popup updates immediately upon deletion
        await expect(page.getByRole('heading', { name: 'Exercise Details' })).toBeVisible();
        await expect(page.getByText('Deleted custom exercise')).toBeVisible();
        await expect(page.getByText('This exercise has been deleted and cannot be edited or deleted.')).toBeVisible();
        await expect(page.getByRole('button', { name: 'Edit' })).not.toBeVisible();
        await expect(page.getByRole('button', { name: 'Delete' })).not.toBeVisible();
        await page.getByRole('button', { name: 'Close', exact: true }).click();
        await expect(page.getByRole('heading', { name: 'Exercise Details' })).not.toBeVisible();
    });
});