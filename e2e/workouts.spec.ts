import { test, expect } from '@playwright/test';

test.describe('Workouts Page', () => {

    test.beforeEach(async ({ page }) => {
        await page.goto('/workouts');

        //guarantees react has finished rendering the final state of your data so that there is no race conditions
        await page.waitForLoadState('networkidle');
    });
});