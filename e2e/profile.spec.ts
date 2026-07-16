import { test, expect } from '@playwright/test';

test.describe('Profile Page', () => {
  
  test.beforeEach(async ({ page }) => {
    await page.goto('/profile');
    await page.waitForLoadState('networkidle');
  });

  test.afterEach(async ({ request }) => {
    //reset Alex's account after the test has completed
    const response = await request.patch('http://localhost:5036/api/users/me/profileDetails', {
      data: {
        displayName: "Alex",
        bio: "Loves to gym every day all day. This is their favourite app ever.",
        sex: "Male",
        dateOfBirth: "1999-02-14",
        weight: 78,
        height: 182
      }
    });
    
    expect(response.ok()).toBeTruthy();
  });

  test('can view and edit profile details', async ({ page }) => {
    await expect(page.getByText('Alex', { exact: true })).toBeVisible();
    await expect(page.getByText('Email: gymgoer@gmail.com')).toBeVisible();
    await expect(page.getByText('Bio: Loves to gym every day')).toBeVisible();

    await page.getByRole('button', { name: 'Settings' }).click();

    const nameInput = page.getByRole('textbox').first();
    const bioTextarea = page.locator('textarea');
    
    await expect(nameInput).toHaveValue('Alex');
    await expect(bioTextarea).toHaveValue('Loves to gym every day all day. This is their favourite app ever.');

    //change name to Axel
    await nameInput.click();
    await nameInput.fill('Axel');

    await Promise.all([page.waitForResponse(res => res.url().includes('api/users/me/profileDetails') && (res.request().method() === 'PATCH') && res.status() >= 200 && res.status() < 300), page.getByRole('button', { name: 'Save Changes' }).click()]);

    await page.goto('/profile');
    await page.waitForLoadState('networkidle');

    await expect(page.getByText('Axel', { exact: true })).toBeVisible();
  });
});