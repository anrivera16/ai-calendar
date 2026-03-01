import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  retries: 1,
  workers: 1,
  reporter: 'html',
  timeout: 30_000,
  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
    { name: 'mobile-chrome', use: { ...devices['Pixel 5'] } },
  ],
  webServer: [
    {
      command: 'curl -s http://localhost:8080/health || true',
      url: 'http://localhost:8080/health',
      timeout: 10_000,
      reuseExistingServer: true,
    },
    {
      command: 'ng serve --port 4200',
      url: 'http://localhost:4200',
      timeout: 60_000,
      reuseExistingServer: true,
    },
  ],
});
