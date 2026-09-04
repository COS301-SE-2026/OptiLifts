import { test as base, expect, type APIRequestContext } from '@playwright/test';

type StorageState = Awaited<ReturnType<APIRequestContext['storageState']>>;
const cachedStorageByWorker: Record<number, StorageState> = {};
 
export const test = base.extend({
  request: async ({ playwright, baseURL }, use, testInfo) => {
    const workerId = testInfo.workerIndex % 4;
    const apiBase = baseURL || 'http://localhost:5173';

    if (!cachedStorageByWorker[workerId]) {
      const authRequest = await playwright.request.newContext({ baseURL: apiBase });
      const response = await authRequest.post('/api/auth/login', {
        data: { email: `test${workerId}@optilifts.com`, password: 'TestPassword123!' }
      });

      expect(response.ok(), `Login failed for worker ${workerId} with status ${response.status()}: ${await response.text()}`).toBeTruthy();

      cachedStorageByWorker[workerId] = await authRequest.storageState();
      await authRequest.dispose();
    }

    const context = await playwright.request.newContext({
      baseURL: apiBase,
      storageState: cachedStorageByWorker[workerId]
    });

    await use(context);
    await context.dispose();
  },

  page: async ({ page, request }, use, testInfo) => {
    const workerId = testInfo.workerIndex % 4;
    const storage = cachedStorageByWorker[workerId];
    if (storage) {
      await page.context().addCookies(storage.cookies);
    }

    await use(page);
  }
});

export { expect };