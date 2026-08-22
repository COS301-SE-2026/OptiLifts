import { test, expect } from './test-utils';

test.describe('Profile Page', () => {
  
  test.beforeEach(async ({ page }) => {
    await page.goto('/profile');
    await page.waitForLoadState('networkidle');
  });

  test.afterEach(async ({ request }) => {
    //reset Test Athlete's account after the test has completed
    const response = await request.patch('http://localhost:5036/api/users/me/profileDetails', {
      data: {
        displayName: "Test Athlete",
        bio: "Powerlifting enthusiast and OptiLifts demo account.",
        sex: "Male",
        dateOfBirth: "1998-04-23",
        weight: 82.5,
        height: 180
      }
    });
    
    expect(response.ok()).toBeTruthy();
  });

  test('can view and edit profile details', async ({ page }) => {
    await expect(page.getByText('Test Athlete', { exact: true })).toBeVisible();
    await expect(page.getByText(/Email: test\d*@optilifts\.com/)).toBeVisible();
    await expect(page.getByText('Bio: Powerlifting enthusiast and OptiLifts demo account.')).toBeVisible();

    await page.getByRole('button', { name: 'Settings' }).click();

    const nameInput = page.getByRole('textbox').first();
    const bioTextarea = page.locator('textarea');
    
    await expect(nameInput).toHaveValue('Test Athlete');
    await expect(bioTextarea).toHaveValue('Powerlifting enthusiast and OptiLifts demo account.');

    //change name to Axel
    await nameInput.click();
    await nameInput.fill('Axel');

    await Promise.all([page.waitForResponse(res => res.url().includes('api/users/me/profileDetails') && (res.request().method() === 'PATCH') && res.status() >= 200 && res.status() < 300), page.getByRole('button', { name: 'Save Changes' }).click()]);

    await page.waitForLoadState('networkidle');

    await expect(page.getByText('Axel', { exact: true })).toBeVisible();
  });
});