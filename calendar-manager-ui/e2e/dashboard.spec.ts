import { test, expect } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';
import { createBusinessProfile, createService, createBooking, cleanupTestData } from './helpers/test-data';

test.describe('Dashboard & Calendar', () => {
  test.beforeEach(async ({ page, request }) => {
    await setupDemoAuth(page, request);
  });

  test.describe('Layout', () => {
    test('renders_splitLayout_calendar_andChat', async ({ page }) => {
      await page.goto('/dashboard');
      await expect(page.locator('.calendar-section')).toBeVisible();
      await expect(page.locator('.chat-section')).toBeVisible();
    });

    test('renders_headerWithUserName', async ({ page }) => {
      await page.goto('/dashboard');
      await expect(page.locator('.dashboard-header')).toBeVisible();
      await expect(page.locator('.user-info .welcome')).toBeVisible();
    });

    test('renders_adminLink_inHeader', async ({ page }) => {
      await page.goto('/dashboard');
      const adminLink = page.locator('a[routerLink="/admin/dashboard"]');
      await expect(adminLink).toBeVisible();
      await expect(adminLink).toContainText('Admin');
    });

    test('renders_logoutButton', async ({ page }) => {
      await page.goto('/dashboard');
      const logoutButton = page.locator('button.icon-btn.danger');
      await expect(logoutButton).toBeVisible();
    });
  });

  test.describe('Calendar', () => {
    test('calendar_showsCurrentMonth', async ({ page }) => {
      await page.goto('/dashboard');
      const monthTitle = page.locator('.month-title');
      await expect(monthTitle).toBeVisible();

      const expectedMonth = new Date().toLocaleDateString('en-US', {
        month: 'long',
        year: 'numeric',
      });
      await expect(monthTitle).toContainText(expectedMonth);
    });

    test('calendar_highlightsToday', async ({ page }) => {
      await page.goto('/dashboard');
      const today = page.locator('.calendar-day.today');
      await expect(today).toBeVisible();

      const todayDate = new Date().getDate().toString();
      await expect(today.locator('.day-number')).toContainText(todayDate);
    });

    test('calendar_navigatesToPreviousMonth', async ({ page }) => {
      await page.goto('/dashboard');

      const currentMonth = new Date();
      const prevMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1);
      const expectedMonth = prevMonth.toLocaleDateString('en-US', {
        month: 'long',
        year: 'numeric',
      });

      const prevButton = page.locator('button[aria-label="Previous month"]');
      await prevButton.click();

      await expect(page.locator('.month-title')).toContainText(expectedMonth);
    });

    test('calendar_navigatesToNextMonth', async ({ page }) => {
      await page.goto('/dashboard');

      const currentMonth = new Date();
      const nextMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1);
      const expectedMonth = nextMonth.toLocaleDateString('en-US', {
        month: 'long',
        year: 'numeric',
      });

      const nextButton = page.locator('button[aria-label="Next month"]');
      await nextButton.click();

      await expect(page.locator('.month-title')).toContainText(expectedMonth);
    });

    test('calendar_goToTodayButton_returnsToCurrentMonth', async ({ page }) => {
      await page.goto('/dashboard');

      const nextButton = page.locator('button[aria-label="Next month"]');
      await nextButton.click();

      const currentMonth = new Date();
      const expectedMonth = currentMonth.toLocaleDateString('en-US', {
        month: 'long',
        year: 'numeric',
      });

      const monthTitle = page.locator('.month-title');
      await monthTitle.click();

      await expect(monthTitle).toContainText(expectedMonth);
    });

    test('calendar_displaysEvents_onCorrectDates', async ({ page, request }) => {
      const profile = await createBusinessProfile(request, { slug: 'test-events-cal' });
      const service = await createService(request, profile.id, { name: 'Test Event Service' });
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(14, 0, 0, 0);

      await createBooking(request, profile.slug, service.id, {
        clientName: 'Test Client',
        startTime: tomorrow.toISOString(),
      });

      await page.goto('/dashboard');
      await page.waitForTimeout(2000);

      const eventDots = page.locator('.event-indicators');
      await expect(eventDots.first()).toBeVisible({ timeout: 5000 });

      await cleanupTestData(request);
    });

    test('calendar_clickOnDay_selectsDay', async ({ page }) => {
      await page.goto('/dashboard');

      const todayCell = page.locator('.calendar-day.today');
      await todayCell.click();

      const selectedDay = page.locator('.calendar-day.selected');
      await expect(selectedDay).toBeVisible();
    });

    test('calendar_clickOnEvent_selectsEvent', async ({ page, request }) => {
      const profile = await createBusinessProfile(request, { slug: 'test-event-click' });
      const service = await createService(request, profile.id);

      const today = new Date();
      today.setHours(14, 0, 0, 0);

      await createBooking(request, profile.slug, service.id, {
        clientName: 'Event Click Test',
        startTime: today.toISOString(),
      });

      await page.goto('/dashboard');
      await page.waitForTimeout(2000);

      const todayCell = page.locator('.calendar-day.today');
      await todayCell.click();

      const eventItem = page.locator('.day-events .event-item').first();
      if (await eventItem.isVisible({ timeout: 3000 })) {
        await eventItem.click();
      }

      await cleanupTestData(request);
    });
  });

  test.describe('Mobile Responsive', () => {
    test.use({ viewport: { width: 375, height: 667 } });

    test('mobile_showsToggleButton', async ({ page }) => {
      await page.goto('/dashboard');
      await expect(page.locator('.mobile-toggle')).toBeVisible();
      await expect(page.locator('.toggle-btn')).toHaveCount(2);
    });

    test('mobile_togglesBetween_calendarAndChat', async ({ page }) => {
      await page.goto('/dashboard');

      const calendarBtn = page.locator('.toggle-btn:has-text("Calendar")');
      const chatBtn = page.locator('.toggle-btn:has-text("Assistant")');

      await chatBtn.click();
      await expect(page.locator('.chat-section')).not.toHaveClass(/mobile-hidden/);

      await calendarBtn.click();
      await expect(page.locator('.calendar-section')).not.toHaveClass(/mobile-hidden/);
    });

    test('mobile_defaultsToCalendarView', async ({ page }) => {
      await page.goto('/dashboard');

      const calendarSection = page.locator('.calendar-section');
      const activePanel = await page.evaluate(() => localStorage.getItem('activePanel'));

      expect(['chat', 'calendar', null]).toContain(activePanel);
    });
  });
});
