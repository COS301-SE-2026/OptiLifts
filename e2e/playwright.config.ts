import { defineConfig, devices } from '@playwright/test';
import * as path from 'node:path';

const STORAGE_STATE = path.join(__dirname, 'playwright/.auth/user.json');
const useExistingServices = process.env.E2E_USE_EXISTING_SERVICES === '1';

export default defineConfig({
    testDir: '.',
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: 4, //didn't allow for concurrency because my tests would fail then (might just be my laptop being slow lol ~E)

    reporter: [
        ['html', { outputFolder: './playwright-report' }]
    ],
    outputDir: './test-results',

    use: {
        baseURL: 'http://localhost:5173',
        trace: 'on-first-retry',
    },

    projects: [
        //runs tests for each browser, which first depends "setup" to finish, then loads the cookies
        {
            name: 'chromium',
            use: {
                ...devices['Desktop Chrome'],
            },
        },
        {
            name: 'firefox',
            use: {
                ...devices['Desktop Firefox'],
            },
        },
        {
            name: 'webkit',
            use: {
                ...devices['Desktop Safari'],
            },
        },
    ],

    webServer: useExistingServices ? undefined : {
        command: 'pnpm e2e:services:webserver',
        cwd: '..',
        url: 'http://localhost:5173/api/healthCheck', //prevents tests from starting if the backend and frontend isn'r ready yet
        reuseExistingServer: !process.env.CI, //if pnpm dev/prod is running it will just use that one, otherwise it spins up a containerised prod stack
        timeout: 10 * 60 * 1000,
    },
});