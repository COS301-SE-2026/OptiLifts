import { test as base } from '@playwright/test';

//to log in the 4 accounts 
export const test = base.extend({
  page: async ({ page, request }, use, testInfo) => {
    const workerId = testInfo.workerIndex % 4; //so can run all 4
    
    await request.post('/api/auth/login', {
      data: { email: `test${workerId}@optilifts.com`, password: 'TestPassword123!' }
    });
    
    const state = await request.storageState();
    await page.context().addCookies(state.cookies);
    
    await use(page);
  }
});
export { expect } from '@playwright/test';