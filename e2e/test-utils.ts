import { test as base, expect, type BrowserContext } from '@playwright/test';

type Cookie = Parameters<BrowserContext['addCookies']>[0][number];
let cachedCookies: Cookie[] | null = null;

//to log in the 4 accounts 
export const test = base.extend({
  page: async ({ page, request }, use, testInfo) => {
    const workerId = testInfo.workerIndex % 4; //so can run all 4

    if (!cachedCookies) {
      const response = await request.post('/api/auth/login', {
        data: { email: `test${workerId}@optilifts.com`, password: 'TestPassword123!' }
      });

      expect(response.ok(), `Login failed for worker ${workerId} with status ${response.status()}: ${await response.text()}`).toBeTruthy();

      const state = await request.storageState();
      cachedCookies = state.cookies;
    }

    if (cachedCookies) {
      await page.context().addCookies(cachedCookies);
    }

    await use(page);
  }
});

export { expect };