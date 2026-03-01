import { Page, APIRequestContext } from '@playwright/test';

const DEMO_USER_EMAIL = 'test@example.com';
const API_BASE_URL = 'http://localhost:8080';

export async function setupDemoAuth(page: Page, request: APIRequestContext): Promise<void> {
  await page.route('**/api/auth/status', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        authenticated: true,
        user: { id: 1, email: DEMO_USER_EMAIL, displayName: 'Test User' },
        tokenCount: 1,
        nextExpiry: new Date(Date.now() + 3600000).toISOString()
      })
    });
  });

  await page.route('**/api/auth/test-token', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        message: 'Access token retrieved successfully',
        tokenLength: 100,
        startsWithBearer: false
      })
    });
  });

  await page.goto('/');
  await page.waitForTimeout(1000);
  await page.goto('/dashboard');
}

export async function loginAsDemoUser(page: Page): Promise<void> {
  await page.goto('/login');

  const demoButton = page.locator('button:has-text("Demo"), a:has-text("Demo")');

  if (await demoButton.isVisible({ timeout: 2000 }).catch(() => false)) {
    await demoButton.click();
  } else {
    await page.evaluate(() => {
      localStorage.setItem('demoMode', 'true');
      localStorage.setItem('userEmail', 'test@example.com');
    });
    await page.goto('/dashboard');
  }
}

export async function logout(page: Page): Promise<void> {
  const logoutButton = page.locator('button:has-text("Logout"), button:has-text("Log out")');
  if (await logoutButton.isVisible({ timeout: 2000 }).catch(() => false)) {
    await logoutButton.click();
  } else {
    await page.evaluate(() => {
      localStorage.removeItem('demoMode');
      localStorage.removeItem('userEmail');
    });
    await page.goto('/login');
  }
}

export async function isAuthenticated(page: Page): Promise<boolean> {
  const email = await page.evaluate(() => localStorage.getItem('userEmail'));
  const demoMode = await page.evaluate(() => localStorage.getItem('demoMode'));
  return email === 'test@example.com' || demoMode === 'true';
}

export async function ensureAuthenticated(page: Page, request: APIRequestContext): Promise<void> {
  if (!(await isAuthenticated(page))) {
    await setupDemoAuth(page, request);
  }
}

export async function getDemoUserEmail(): string {
  return DEMO_USER_EMAIL;
}
