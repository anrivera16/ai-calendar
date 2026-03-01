import { test, expect } from '@playwright/test';
import {
  createBusinessProfile,
  createService,
  createWeeklyAvailability,
  createBooking,
  cleanupTestData,
} from './helpers/test-data';

test.describe('Client Booking Management', () => {
  let profileSlug: string;
  let serviceId: number;
  let managementToken: string;

  test.beforeEach(async ({ request }) => {
    await cleanupTestData(request);

    const profile = await createBusinessProfile(request, {
      businessName: 'Client Manage Test Business',
      slug: 'client-manage-test',
    });
    profileSlug = profile.slug;

    const service = await createService(request, profile.id, {
      name: 'Test Service',
      durationMinutes: 60,
      price: 100,
    });
    serviceId = service.id;

    await createWeeklyAvailability(request, profile.id);
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test('loadsBookingDetails_byToken', async ({ page, request }) => {
    const booking = await createBooking(request, profileSlug, serviceId, {
      clientName: 'Token Test Client',
      clientEmail: 'token@test.com',
    });
    managementToken = booking.managementToken;

    await page.goto(`/book/manage/${managementToken}`);
    await expect(page.locator('.manage-container')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.booking-details')).toBeVisible();
  });

  test('showsBookingInfo', async ({ page, request }) => {
    const booking = await createBooking(request, profileSlug, serviceId, {
      clientName: 'Info Test Client',
      clientEmail: 'info@test.com',
    });

    await page.goto(`/book/manage/${booking.managementToken}`);
    await expect(page.locator('.manage-container')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.booking-info h2')).toContainText('Test Service');
    await expect(page.locator('.client-info')).toContainText('Info Test Client');
    await expect(page.locator('.client-info')).toContainText('info@test.com');
  });

  test('showsBusinessInfo', async ({ page, request }) => {
    const booking = await createBooking(request, profileSlug, serviceId, {
      clientName: 'Business Info Client',
      clientEmail: 'business-info@test.com',
    });

    await page.goto(`/book/manage/${booking.managementToken}`);
    await expect(page.locator('.manage-container')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.business-info')).toBeVisible();
    await expect(page.locator('.business-info h3')).toContainText('Client Manage Test Business');
  });

  test('cancelBooking_updatesStatus', async ({ page, request }) => {
    const booking = await createBooking(request, profileSlug, serviceId, {
      clientName: 'Cancel Test Client',
      clientEmail: 'cancel@test.com',
    });

    await page.goto(`/book/manage/${booking.managementToken}`);
    await expect(page.locator('.manage-container')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.status-banner')).toContainText('Confirmed');

    const cancelButton = page.locator('.cancel-btn');
    await expect(cancelButton).toBeVisible();
    await cancelButton.click();

    await expect(page.locator('.success-message')).toBeVisible({ timeout: 10000 });
  });

  test('showsCancelledStatus_afterCancellation', async ({ page, request }) => {
    const booking = await createBooking(request, profileSlug, serviceId, {
      clientName: 'Cancelled Status Client',
      clientEmail: 'cancelled-status@test.com',
    });

    await page.goto(`/book/manage/${booking.managementToken}`);
    await expect(page.locator('.manage-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.cancel-btn').click();
    await expect(page.locator('.success-message')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.status-banner')).toContainText('Cancelled');
    await expect(page.locator('.status-banner')).toHaveClass(/status-cancelled/);
  });

  test('disablesCancelButton_forAlreadyCancelled', async ({ page, request }) => {
    const booking = await createBooking(request, profileSlug, serviceId, {
      clientName: 'Already Cancelled Client',
      clientEmail: 'already-cancelled@test.com',
    });

    await page.goto(`/book/manage/${booking.managementToken}`);
    await expect(page.locator('.manage-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.cancel-btn').click();
    await expect(page.locator('.success-message')).toBeVisible({ timeout: 10000 });

    await page.reload();
    await expect(page.locator('.manage-container')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.status-banner')).toContainText('Cancelled');

    const cancelButton = page.locator('.cancel-btn');
    const isVisible = await cancelButton.isVisible().catch(() => false);
    expect(isVisible).toBe(false);
  });

  test('shows404_forInvalidToken', async ({ page }) => {
    await page.goto('/book/manage/invalid-token-xyz-123');
    await expect(page.locator('.error-state')).toBeVisible({ timeout: 10000 });
  });
});
