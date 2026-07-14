import { test as setup, expect } from '@playwright/test';
import * as path from 'path';

const authFile = path.join(__dirname, 'playwright/.auth/user.json');

setup('authenticate', async ({ request }) => {
    const response = await request.post('http://localhost:5000/api/auth/login', {
        data: {
            email: 'gymgoer@gmail.com',
            password: 'GymGoer123!'
        }
    });

    expect(response.ok()).toBeTruthy();

    await request.storageState({ path: authFile });
});