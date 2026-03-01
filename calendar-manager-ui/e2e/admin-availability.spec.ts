import { test, expect } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';
import {
  createBusinessProfile,
  createWeeklyAvailability,
  cleanupTestData,
} from './helpers/test-data';

test.describe('Availability Management', () => {
  let profileId: number;

  test.beforeEach(async ({ page, request }) => {
    await setupDemoAuth(page, request);
    await cleanupTestData(request);
    const profile = await createBusinessProfile(request, { businessName: 'Availability Test Business' });
    profileId = profile.id;
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test.describe('Weekly Schedule', () => {
    test('showsWeeklySchedule_forAll7Days', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const dayRows = page.locator('.day-row');
      await expect(dayRows).toHaveCount(7, { timeout: 5000 });

      await expect(page.locator('.day-name:has-text("Sunday")')).toBeVisible();
      await expect(page.locator('.day-name:has-text("Monday")')).toBeVisible();
      await expect(page.locator('.day-name:has-text("Tuesday")')).toBeVisible();
      await expect(page.locator('.day-name:has-text("Wednesday")')).toBeVisible();
      await expect(page.locator('.day-name:has-text("Thursday")')).toBeVisible();
      await expect(page.locator('.day-name:has-text("Friday")')).toBeVisible();
      await expect(page.locator('.day-name:has-text("Saturday")')).toBeVisible();
    });

    test('saveWeeklyAvailability_forMonday', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const mondayRow = page.locator('.day-row').nth(1);
      await expect(mondayRow.locator('.day-name')).toContainText('Monday');

      const toggle = mondayRow.locator('input[type="checkbox"]');
      if (!(await toggle.isChecked())) {
        await toggle.click();
      }

      await page.waitForTimeout(500);

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
    });

    test('savedAvailability_persistsOnReload', async ({ page, request }) => {
      await createWeeklyAvailability(request, profileId, [
        { dayOfWeek: 1, startTime: '09:00:00', endTime: '17:00:00' },
      ]);

      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const mondayRow = page.locator('.day-row').nth(1);
      const toggle = mondayRow.locator('input[type="checkbox"]');
      await expect(toggle).toBeChecked({ timeout: 5000 });

      await page.reload();

      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });
      await expect(toggle).toBeChecked();
    });

    test('canDisableDay', async ({ page, request }) => {
      await createWeeklyAvailability(request, profileId, [
        { dayOfWeek: 2, startTime: '09:00:00', endTime: '17:00:00' },
      ]);

      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const tuesdayRow = page.locator('.day-row').nth(2);
      await expect(tuesdayRow.locator('.day-name')).toContainText('Tuesday');

      const toggle = tuesdayRow.locator('input[type="checkbox"]');
      await expect(toggle).toBeChecked({ timeout: 5000 });

      await toggle.click();

      await expect(toggle).not.toBeChecked();
      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('Date Overrides', () => {
    test('openOverrideForm_clickButton', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const addButton = page.locator('button:has-text("+ Add Override")');
      await addButton.click();

      await expect(page.locator('.override-form')).toBeVisible();
    });

    test('createDateOverride_markDayUnavailable', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      await page.locator('button:has-text("+ Add Override")').click();
      await expect(page.locator('.override-form')).toBeVisible();

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0];

      await page.locator('.override-form input[type="date"]').fill(dateStr);

      const availableCheckbox = page.locator('.override-form input[type="checkbox"]');
      if (await availableCheckbox.isChecked()) {
        await availableCheckbox.click();
      }

      await page.locator('.override-form button:has-text("Save Override")').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
    });

    test('createDateOverride_customHours', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      await page.locator('button:has-text("+ Add Override")').click();
      await expect(page.locator('.override-form')).toBeVisible();

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0];

      await page.locator('.override-form input[type="date"]').fill(dateStr);

      const availableCheckbox = page.locator('.override-form input[type="checkbox"]');
      if (!(await availableCheckbox.isChecked())) {
        await availableCheckbox.click();
      }

      const startSelect = page.locator('.override-form select').first();
      await startSelect.selectOption('10:00');

      const endSelect = page.locator('.override-form select').nth(1);
      await endSelect.selectOption('14:00');

      await page.locator('.override-form button:has-text("Save Override")').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
    });

    test('overrideAppears_inOverrideList', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      await page.locator('button:has-text("+ Add Override")').click();

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0];

      await page.locator('.override-form input[type="date"]').fill(dateStr);
      await page.locator('.override-form button:has-text("Save Override")').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
      await expect(page.locator('.rules-list .rule-item')).toBeVisible({ timeout: 5000 });
    });

    test('deleteOverride_removesFromList', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      await page.locator('button:has-text("+ Add Override")').click();

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0];

      await page.locator('.override-form input[type="date"]').fill(dateStr);
      await page.locator('.override-form button:has-text("Save Override")').click();

      await expect(page.locator('.rules-list .rule-item')).toBeVisible({ timeout: 5000 });

      page.on('dialog', (dialog) => dialog.accept());

      const deleteButton = page.locator('.rules-list .rule-item button:has-text("🗑️")');
      await deleteButton.click();

      await expect(page.locator('.rules-list .rule-item')).not.toBeVisible({ timeout: 5000 });
    });
  });

  test.describe('Break Slots', () => {
    test('openBreakForm_clickButton', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const addButton = page.locator('section:has-text("Breaks") button:has-text("+ Add Break")');
      await addButton.click();

      await expect(page.locator('section:has-text("Breaks") .override-form')).toBeVisible();
    });

    test('createBreak_forSpecificDate', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const addButton = page.locator('section:has-text("Breaks") button:has-text("+ Add Break")');
      await addButton.click();

      const breakForm = page.locator('section:has-text("Breaks") .override-form');
      await expect(breakForm).toBeVisible();

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0];

      await breakForm.locator('input[type="date"]').fill(dateStr);

      const startSelect = breakForm.locator('select').first();
      await startSelect.selectOption('12:00');

      const endSelect = breakForm.locator('select').nth(1);
      await endSelect.selectOption('13:00');

      await breakForm.locator('button:has-text("Save Break")').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });
    });

    test('breakAppears_inBreakList', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const addButton = page.locator('section:has-text("Breaks") button:has-text("+ Add Break")');
      await addButton.click();

      const breakForm = page.locator('section:has-text("Breaks") .override-form');

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0];

      await breakForm.locator('input[type="date"]').fill(dateStr);
      await breakForm.locator('button:has-text("Save Break")').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });

      const breakSection = page.locator('section:has-text("Breaks")');
      await expect(breakSection.locator('.rules-list .rule-item')).toBeVisible({ timeout: 5000 });
    });

    test('deleteBreak_removesFromList', async ({ page }) => {
      await page.goto('/admin/availability');
      await expect(page.locator('.availability-manager')).toBeVisible({ timeout: 10000 });

      const addButton = page.locator('section:has-text("Breaks") button:has-text("+ Add Break")');
      await addButton.click();

      const breakForm = page.locator('section:has-text("Breaks") .override-form');

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const dateStr = tomorrow.toISOString().split('T')[0];

      await breakForm.locator('input[type="date"]').fill(dateStr);
      await breakForm.locator('button:has-text("Save Break")').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 5000 });

      const breakSection = page.locator('section:has-text("Breaks")');
      await expect(breakSection.locator('.rules-list .rule-item')).toBeVisible({ timeout: 5000 });

      page.on('dialog', (dialog) => dialog.accept());

      const deleteButton = breakSection.locator('.rules-list .rule-item button:has-text("🗑️")');
      await deleteButton.click();

      await expect(breakSection.locator('.rules-list .rule-item')).not.toBeVisible({ timeout: 5000 });
    });
  });
});
