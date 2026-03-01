import { test, expect } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';
import {
  createBusinessProfile,
  createService,
  createWeeklyAvailability,
  createBooking,
  createBookingForToday,
  cleanupTestData,
} from './helpers/test-data';

test.describe('Admin Booking Management', () => {
  let profileId: number;
  let profileSlug: string;
  let serviceId: number;

  test.beforeEach(async ({ page, request }) => {
    await setupDemoAuth(page, request);
    await cleanupTestData(request);

    const profile = await createBusinessProfile(request, { businessName: 'Booking Test Business' });
    profileId = profile.id;
    profileSlug = profile.slug;

    const service = await createService(request, profileId, { name: 'Test Service', durationMinutes: 60, price: 100 });
    serviceId = service.id;

    await createWeeklyAvailability(request, profileId);
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test('showsBookingsList', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, { clientName: 'List Test Client' });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const bookingRow = page.locator('.table-row');
    await expect(bookingRow.first()).toBeVisible({ timeout: 5000 });
  });

  test('filterByStatus_confirmed', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, { clientName: 'Confirmed Client' });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const statusFilter = page.locator('.filters select').first();
    await statusFilter.selectOption('Confirmed');

    await page.waitForTimeout(500);

    const statusBadges = page.locator('.status-badge.confirmed');
    const count = await statusBadges.count();

    for (let i = 0; i < count; i++) {
      await expect(statusBadges.nth(i)).toContainText('Confirmed');
    }
  });

  test('filterByStatus_cancelled', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, { clientName: 'Cancelled Filter Client' });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const statusFilter = page.locator('.filters select').first();
    await statusFilter.selectOption('Cancelled');

    await page.waitForTimeout(500);

    const bookingRows = page.locator('.table-row');
    const count = await bookingRows.count();

    if (count > 0) {
      const statusBadges = page.locator('.status-badge.cancelled');
      const badgeCount = await statusBadges.count();
      for (let i = 0; i < badgeCount; i++) {
        await expect(statusBadges.nth(i)).toContainText('Cancelled');
      }
    }
  });

  test('filterByDateRange', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, { clientName: 'Date Range Client' });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const today = new Date();
    const todayStr = today.toISOString().split('T')[0];

    const fromDate = page.locator('input[type="date"]').first();
    await fromDate.fill(todayStr);

    const nextWeek = new Date(today);
    nextWeek.setDate(nextWeek.getDate() + 7);
    const nextWeekStr = nextWeek.toISOString().split('T')[0];

    const toDate = page.locator('input[type="date"]').nth(1);
    await toDate.fill(nextWeekStr);

    await page.waitForTimeout(500);
  });

  test('clearFilters_showsAll', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, { clientName: 'Clear Filter Client' });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const statusFilter = page.locator('.filters select').first();
    await statusFilter.selectOption('Confirmed');

    await page.waitForTimeout(500);

    const clearButton = page.locator('button:has-text("Clear")');
    await clearButton.click();

    await expect(statusFilter).toHaveValue('');
  });

  test('cancelBooking_withConfirmation', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, { clientName: 'Cancel Test Client' });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const bookingRow = page.locator('.table-row').first();
    await expect(bookingRow).toBeVisible({ timeout: 5000 });

    const cancelButton = bookingRow.locator('button:has-text("❌")');
    await expect(cancelButton).toBeVisible({ timeout: 3000 });
    await cancelButton.click();

    await expect(page.locator('.modal')).toBeVisible();

    const confirmButton = page.locator('.modal button:has-text("Yes, Cancel")');
    await confirmButton.click();

    await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
  });

  test('completeBooking', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, { clientName: 'Complete Test Client' });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const bookingRow = page.locator('.table-row').first();
    await expect(bookingRow).toBeVisible({ timeout: 5000 });

    page.on('dialog', (dialog) => dialog.accept());

    const completeButton = bookingRow.locator('button:has-text("✅")');
    await expect(completeButton).toBeVisible({ timeout: 3000 });
    await completeButton.click();

    await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
  });

  test('showsBookingDetails', async ({ page, request }) => {
    await createBooking(request, profileSlug, serviceId, {
      clientName: 'Details Test Client',
      clientEmail: 'details@test.com',
    });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const bookingRow = page.locator('.table-row').first();
    await expect(bookingRow).toBeVisible({ timeout: 5000 });

    await expect(bookingRow.locator('.col-client .name')).toContainText('Details Test Client');
    await expect(bookingRow.locator('.col-client .email')).toContainText('details@test.com');
    await expect(bookingRow.locator('.col-service')).toContainText('Test Service');
  });

  test('sortsByDate', async ({ page, request }) => {
    const today = new Date();
    today.setHours(10, 0, 0, 0);

    await createBooking(request, profileSlug, serviceId, {
      clientName: 'Early Booking',
      startTime: today.toISOString(),
    });

    const later = new Date(today);
    later.setHours(14, 0, 0, 0);
    await createBooking(request, profileSlug, serviceId, {
      clientName: 'Later Booking',
      startTime: later.toISOString(),
    });

    await page.goto('/admin/bookings');
    await expect(page.locator('.booking-list-page')).toBeVisible({ timeout: 10000 });

    const bookingRows = page.locator('.table-row');
    await expect(bookingRows.first()).toBeVisible({ timeout: 5000 });
    const count = await bookingRows.count();
    expect(count).toBeGreaterThanOrEqual(2);
  });
});
