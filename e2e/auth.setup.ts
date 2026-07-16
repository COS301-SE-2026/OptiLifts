import { test as setup, expect } from '@playwright/test';
import * as path from 'path';

const authFile = path.join(__dirname, 'playwright/.auth/user.json');

setup('authenticate user via API', async ({ request }) => {
    const loginResponse = await request.post('http://localhost:5036/api/auth/login', {
        data: {
            email: 'test@optilifts.com',
            password: 'TestPassword123!'
        }
    });

    expect(loginResponse.ok()).toBeTruthy();

    //automatically reads the "Set-Cookie" headers from the response
    await request.storageState({ path: authFile });
});