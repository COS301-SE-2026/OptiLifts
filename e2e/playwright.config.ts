import { defineConfig, devices } from '@playwright/test';
import * as path from 'node:path';

const STORAGE_STATE = path.join(__dirname, 'playwright/.auth/user.json');

export default defineConfig({
    testDir: '.',
    fullyParallel: false,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: 1, //didn't allow for concurrency because my tests would fail then (might just be my laptop being slow lol ~E)

    reporter: [
        ['html', { outputFolder: './playwright-report' }]
    ],
    outputDir: './test-results',

    use: {
        baseURL: 'http://localhost:5173',
        trace: 'on-first-retry',
    },

    projects: [
        //runs the auth.setup.ts file to generate the cookies.
        {
            name: 'setup',
            testMatch: 'auth.setup.ts'
        },
        //runs tests for each browser, which first depends "setup" to finish, then loads the cookies
        {
            name: 'chromium',
            use: {
                ...devices['Desktop Chrome'],
                storageState: STORAGE_STATE,
            },
            dependencies: ['setup'],
        },
        {
            name: 'firefox',
            use: {
                ...devices['Desktop Firefox'],
                storageState: STORAGE_STATE,
            },
            dependencies: ['setup'],
        },
        {
            name: 'webkit',
            use: {
                ...devices['Desktop Safari'],
                storageState: STORAGE_STATE,
            },
            dependencies: ['setup'],
        },
    ],

    webServer: {
        command: 'pnpm e2e:services:webserver',
        cwd: '..',
        url: 'http://localhost:5173/api/healthCheck', //prevents tests from starting if the backend and frontend isn'r ready yet
        reuseExistingServer: !process.env.CI, //if pnpm dev/prod is running it will just use that one, otherwise it spins up a containerised prod stack
        timeout: 10 * 60 * 1000,
    },
});