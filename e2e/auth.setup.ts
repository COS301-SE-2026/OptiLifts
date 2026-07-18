import { test as setup, expect } from '@playwright/test';
import * as path from 'node:path';

const authFile = path.join(__dirname, 'playwright/.auth/user.json');

setup('authenticate user via API', async ({ request }) => {
    const loginResponse = await request.post('http://localhost:5036/api/auth/login', {
        data: {
            email: 'test@optilifts.com', //NOSONAR
            password: 'TestPassword123!' //NOSONAR
        }
    });

    expect(loginResponse.ok()).toBeTruthy();

    //automatically reads the "Set-Cookie" headers from the response
    await request.storageState({ path: authFile });
});