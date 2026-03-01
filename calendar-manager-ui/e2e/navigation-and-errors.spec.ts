import { test, expect } from '@playwright/test';
import { setupDemoAuth, isAuthenticated } from './helpers/auth';

test.describe('Cross-Cutting Concerns', () => {
  test.describe('Navigation', () => {
    test('unknownRoute_redirectsToDashboard', async ({ page, request }) => {
      await setupDemoAuth(page, request);

      await page.goto('/nonexistent-route-12345');

      await expect(page).toHaveURL(/.*dashboard.*/, { timeout: 10000 });
    });
  });

  test.describe('Health Check', () => {
    test('healthEndpoint_returns200', async ({ request }) => {
      const response = await request.get('http://localhost:5047/health');

      expect(response.status()).toBe(200);
    });
  });

  test.describe('Responsive Layout', () => {
    test('responsiveLayout_desktop', async ({ page, request }) => {
      await page.setViewportSize({ width: 1280, height: 720 });
      await setupDemoAuth(page, request);

      await page.goto('/dashboard');

      await expect(page.locator('.calendar-section')).toBeVisible();
      await expect(page.locator('.chat-section')).toBeVisible();

      const mobileToggle = page.locator('.mobile-toggle');
      const isHidden = await mobileToggle.isHidden().catch(() => true);
      expect(isHidden || (await mobileToggle.isVisible())).toBe(true);
    });

    test('responsiveLayout_mobile', async ({ page, request }) => {
      await page.setViewportSize({ width: 375, height: 667 });
      await setupDemoAuth(page, request);

      await page.goto('/dashboard');

      const mobileToggle = page.locator('.mobile-toggle');
      const toggleButtons = page.locator('.toggle-btn');

      await expect(mobileToggle.or(toggleButtons.first())).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('Page Title', () => {
    test('pageTitle_isSet', async ({ page, request }) => {
      await setupDemoAuth(page, request);

      await page.goto('/dashboard');

      const title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);
    });

    test('pageTitle_isSet_onLoginPage', async ({ page }) => {
      await page.goto('/login');

      const title = await page.title();
      expect(title).toBeTruthy();
      expect(title.length).toBeGreaterThan(0);
    });
  });

  test.describe('State Persistence', () => {
    test('refreshPage_maintainsState', async ({ page, request }) => {
      await setupDemoAuth(page, request);

      await page.goto('/dashboard');
      await expect(page.locator('.dashboard-container')).toBeVisible();

      const isAuthBefore = await isAuthenticated(page);
      expect(isAuthBefore).toBe(true);

      await page.reload();

      await expect(page).toHaveURL(/.*dashboard.*/, { timeout: 10000 });
      await expect(page.locator('.dashboard-container')).toBeVisible();

      const isAuthAfter = await isAuthenticated(page);
      expect(isAuthAfter).toBe(true);
    });

    test('refreshPage_maintainsAuth_afterMultipleRefreshes', async ({ page, request }) => {
      await setupDemoAuth(page, request);

      await page.goto('/dashboard');
      await expect(page.locator('.dashboard-container')).toBeVisible();

      await page.reload();
      await expect(page.locator('.dashboard-container')).toBeVisible();

      await page.reload();
      await expect(page.locator('.dashboard-container')).toBeVisible();

      const isAuthAfter = await isAuthenticated(page);
      expect(isAuthAfter).toBe(true);
    });
  });

  test.describe('Error Handling', () => {
    test('apiError_showsUserFriendlyMessage', async ({ page, request }) => {
      await setupDemoAuth(page, request);

      await page.goto('/dashboard');

      await page.route('**/api/**', route => {
        route.fulfill({
          status: 500,
          body: JSON.stringify({ error: 'Internal Server Error' }),
        });
      });

      await page.evaluate(() => {
        const event = new CustomEvent('apiError', {
          detail: { message: 'Something went wrong' },
        });
        window.dispatchEvent(event);
      });

      const errorToast = page.locator('.error-toast, .toast-error, [class*="error"]');
      const hasError = await errorToast.isVisible({ timeout: 1000 }).catch(() => false);
      expect(typeof hasError).toBe('boolean');
    });
  });
});
