import { test, expect } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';

test.describe('Authentication Flow', () => {
  test.describe('Login Page', () => {
    test('loginPage_accessible_withDemoUser', async ({ page }) => {
      await page.goto('/login');
      await expect(page).toHaveURL(/.*login.*/, { timeout: 10000 });
      const userInfo = page.locator('.user-info, .demo-user-info');
      if (await userInfo.isVisible().catch(() => false)) {
        await expect(userInfo).toContainText('test@example.com');
      }
    });

    test('renders_googleLoginButton', async ({ page }) => {
      await page.goto('/login');
      const googleButton = page.locator('button.google-login-btn');
      await expect(googleButton).toBeVisible();
      await expect(googleButton).toContainText('Connect Google Calendar');
    });

    test('renders_featureDescriptions', async ({ page }) => {
      await page.goto('/login');
      const featuresList = page.locator('.features ul');
      await expect(featuresList).toBeVisible();
      await expect(featuresList.locator('li')).toHaveCount(4);
      await expect(page.locator('.features')).toContainText('View your calendar events');
      await expect(page.locator('.features')).toContainText('Create new meetings');
      await expect(page.locator('.features')).toContainText('Find optimal meeting times');
      await expect(page.locator('.features')).toContainText('Natural language commands');
    });

    test('protectedRoutes_autoAuthenticate_inDemoMode', async ({ page }) => {
      await page.goto('/dashboard');
      await expect(page).toHaveURL(/.*dashboard.*/, { timeout: 10000 });

      await page.goto('/admin/dashboard');
      await expect(page).toHaveURL(/.*admin\/dashboard.*/, { timeout: 10000 });
    });
  });

  test.describe('Demo Mode Auth', () => {
    test('dashboard_isAccessible_inDemoMode', async ({ page, request }) => {
      await setupDemoAuth(page, request);
      await page.goto('/dashboard');
      await expect(page).toHaveURL(/.*dashboard.*/, { timeout: 10000 });
      await expect(page.locator('.dashboard-container')).toBeVisible();
    });

    test('adminDashboard_isAccessible_inDemoMode', async ({ page, request }) => {
      await setupDemoAuth(page, request);
      await page.goto('/admin/dashboard');
      await expect(page).toHaveURL(/.*admin\/dashboard.*/, { timeout: 10000 });
      await expect(page.locator('app-admin-dashboard')).toBeVisible();
    });
  });
});
