import { defineConfig, devices } from '@playwright/test';
import * as path from 'path';

const STORAGE_STATE = path.join(__dirname, 'playwright/.auth/user.json');

export default defineConfig({
    testDir: '.',
    fullyParallel: true,
    forbidOnly: !!process.env.CI,
    retries: process.env.CI ? 2 : 0,
    workers: process.env.CI ? 1 : undefined,

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
            testMatch: /.*\.setup\.ts/,
        },
        //runs tests in parallel for each browser, which first depends "setup" to finish, then loads the cookies
        {
            name: 'chromium',
            use: {
                ...devices['Desktop Chrome'],
                storageState: STORAGE_STATE,
            },
            dependencies: ['setup'],
        },
        // {
        //     name: 'firefox',
        //     use: {
        //         ...devices['Desktop Firefox'],
        //         storageState: STORAGE_STATE,
        //     },
        //     dependencies: ['setup'],
        // },
        // {
        //     name: 'webkit',
        //     use: {
        //         ...devices['Desktop Safari'],
        //         storageState: STORAGE_STATE,
        //     },
        //     dependencies: ['setup'],
        // },
    ],

    //will run "pnpm dev" if it is not already running
    webServer: {
        command: 'pnpm dev',
        cwd: '..',
        url: 'http://localhost:5173',
        reuseExistingServer: !process.env.CI,
        timeout: 120 * 1000,
    },
});