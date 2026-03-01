import { test, expect } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';
import {
  createBusinessProfile,
  createService,
  createBooking,
  createBookingForToday,
  cleanupTestData,
  getBusinessProfile,
  deleteBusinessProfile,
} from './helpers/test-data';

test.describe('Admin Dashboard', () => {
  test.beforeEach(async ({ page, request }) => {
    await setupDemoAuth(page, request);
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test.describe('No Profile State', () => {
    test('showsSetupPrompt_whenNoBusinessProfile', async ({ page, request }) => {
      const existing = await getBusinessProfile(request);
      if (existing) {
        await deleteBusinessProfile(request, existing.id);
      }

      await page.goto('/admin/dashboard');
      await expect(page.locator('.no-profile')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('.no-profile h2')).toContainText('Welcome! Set up your business');
    });

    test('setupLink_navigatesToBusinessSetup', async ({ page, request }) => {
      const existing = await getBusinessProfile(request);
      if (existing) {
        await deleteBusinessProfile(request, existing.id);
      }

      await page.goto('/admin/dashboard');
      await expect(page.locator('.no-profile')).toBeVisible({ timeout: 10000 });

      const setupLink = page.locator('a[routerLink="/admin/setup"]');
      await expect(setupLink).toBeVisible();
      await setupLink.click();

      await expect(page).toHaveURL(/.*admin\/setup.*/);
    });
  });

  test.describe('With Profile State', () => {
    test.beforeEach(async ({ request }) => {
      await cleanupTestData(request);
    });

    test('showsBusinessProfileSummary', async ({ page, request }) => {
      await createBusinessProfile(request, {
        businessName: 'Test Business Dashboard',
        description: 'A test business for dashboard tests',
      });

      await page.goto('/admin/dashboard');
      await expect(page.locator('.profile-overview')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('.profile-card h2')).toContainText('Test Business Dashboard');
    });

    test('showsBookingLink', async ({ page, request }) => {
      await createBusinessProfile(request, {
        businessName: 'Booking Link Test',
        slug: 'booking-link-test',
      });

      await page.goto('/admin/dashboard');
      await expect(page.locator('.booking-link')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('.booking-link .url')).toContainText('/book/');
    });

    test('copyBookingLink_copiesToClipboard', async ({ page, request }) => {
      await createBusinessProfile(request, { businessName: 'Copy Link Test' });

      await page.goto('/admin/dashboard');
      await expect(page.locator('.profile-card')).toBeVisible({ timeout: 10000 });

      const copyButton = page.locator('button:has-text("Copy Booking Link")');
      await expect(copyButton).toBeVisible();

      page.on('dialog', (dialog) => dialog.accept());
      await copyButton.click();

      await page.waitForTimeout(500);
    });

    test('showsTodaysBookings', async ({ page, request }) => {
      const profile = await createBusinessProfile(request, { businessName: 'Today Booking Test' });
      const service = await createService(request, profile.id, { name: 'Today Service' });
      await createBookingForToday(request, profile.slug, service.id, { clientName: 'Today Client' });

      await page.goto('/admin/dashboard');
      await expect(page.locator('.section:has-text("Today\'s Schedule")')).toBeVisible({ timeout: 10000 });

      const todaySection = page.locator('.section:has-text("Today\'s Schedule")');
      await expect(todaySection.locator('.booking-item')).toHaveCount(1, { timeout: 5000 });
    });

    test('showsUpcomingBookings', async ({ page, request }) => {
      const profile = await createBusinessProfile(request, { businessName: 'Upcoming Booking Test' });
      const service = await createService(request, profile.id, { name: 'Upcoming Service' });
      await createBooking(request, profile.slug, service.id, { clientName: 'Upcoming Client' });

      await page.goto('/admin/dashboard');
      await expect(page.locator('.section:has-text("Upcoming Bookings")')).toBeVisible({ timeout: 10000 });
    });

    test('navigationLinks_workCorrectly', async ({ page, request }) => {
      await createBusinessProfile(request, { businessName: 'Nav Links Test' });

      await page.goto('/admin/dashboard');
      await expect(page.locator('.profile-card')).toBeVisible({ timeout: 10000 });

      const servicesLink = page.locator('a[routerLink="/admin/services"]');
      await expect(servicesLink).toBeVisible();
      await servicesLink.click();
      await expect(page).toHaveURL(/.*admin\/services.*/);

      await page.goto('/admin/dashboard');
      const availabilityLink = page.locator('a[routerLink="/admin/availability"]');
      await expect(availabilityLink).toBeVisible();
      await availabilityLink.click();
      await expect(page).toHaveURL(/.*admin\/availability.*/);

      await page.goto('/admin/dashboard');
      const bookingsLink = page.locator('a[routerLink="/admin/bookings"]');
      await expect(bookingsLink).toBeVisible();
      await bookingsLink.click();
      await expect(page).toHaveURL(/.*admin\/bookings.*/);
    });
  });
});
