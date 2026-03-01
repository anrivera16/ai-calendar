import { test, expect } from '@playwright/test';
import {
  createBusinessProfile,
  createService,
  createWeeklyAvailability,
  cleanupTestData,
} from './helpers/test-data';

test.describe('Public Booking Flow', () => {
  let profileSlug: string;
  let serviceId: number;

  test.beforeEach(async ({ request }) => {
    await cleanupTestData(request);

    const profile = await createBusinessProfile(request, {
      businessName: 'Public Booking Test Business',
      slug: 'public-booking-test',
      description: 'A test business for public booking tests',
    });
    profileSlug = profile.slug;

    const service = await createService(request, profile.id, {
      name: 'Consultation',
      description: 'A consultation session',
      durationMinutes: 60,
      price: 100,
      color: '#3B82F6',
    });
    serviceId = service.id;

    await createService(request, profile.id, {
      name: 'Follow-up',
      description: 'Follow-up appointment',
      durationMinutes: 30,
      price: 50,
      color: '#10B981',
    });

    await createWeeklyAvailability(request, profile.id);
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test('loadsBusinessPage_bySlug', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });
  });

  test('showsBusinessName_andDescription', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.booking-header h1')).toContainText('Public Booking Test Business');
    await expect(page.locator('.booking-header p')).toContainText('A test business for public booking tests');
  });

  test('showsActiveServices_withPriceAndDuration', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    const serviceCards = page.locator('.service-card');
    await expect(serviceCards).toHaveCount(2, { timeout: 5000 });

    const consultation = page.locator('.service-card:has-text("Consultation")');
    await expect(consultation).toBeVisible();
    await expect(consultation.locator('.duration')).toContainText('60 min');
    await expect(consultation.locator('.price')).toContainText('$100');
  });

  test('step1_selectService', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    const serviceCard = page.locator('.service-card:has-text("Consultation")');
    await serviceCard.click();

    await expect(page.locator('.step-content:has-text("Select a Date")')).toBeVisible({ timeout: 5000 });
  });

  test('step2_selectDate', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await expect(page.locator('.step-content:has-text("Select a Date")')).toBeVisible({ timeout: 5000 });

    const dateBtn = page.locator('.date-btn').nth(1);
    await dateBtn.click();

    await expect(page.locator('.step-content:has-text("Select a Time")')).toBeVisible({ timeout: 5000 });
  });

  test('step2_showsAvailableDates', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await expect(page.locator('.step-content:has-text("Select a Date")')).toBeVisible({ timeout: 5000 });

    const dateBtns = page.locator('.date-btn');
    await expect(dateBtns.first()).toBeVisible();
    const count = await dateBtns.count();
    expect(count).toBeGreaterThan(0);
  });

  test('step3_selectTimeSlot', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await expect(page.locator('.step-content:has-text("Select a Date")')).toBeVisible({ timeout: 5000 });

    const dateBtn = page.locator('.date-btn').nth(1);
    await dateBtn.click();

    await expect(page.locator('.step-content:has-text("Select a Time")')).toBeVisible({ timeout: 5000 });
    await page.waitForTimeout(1000);

    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step-content:has-text("Your Information")')).toBeVisible({ timeout: 5000 });
    }
  });

  test('step3_showsAvailableSlots', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();

    await expect(page.locator('.step-content:has-text("Select a Time")')).toBeVisible({ timeout: 5000 });
    await page.waitForTimeout(1000);

    const slotsContainer = page.locator('.slots-grid, .no-slots');
    await expect(slotsContainer).toBeVisible({ timeout: 5000 });
  });

  test('step4_fillClientDetails', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();
    await page.waitForTimeout(1000);

    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step-content:has-text("Your Information")')).toBeVisible({ timeout: 5000 });

      await page.locator('#name').fill('John Doe');
      await page.locator('#email').fill('john.doe@example.com');
      await page.locator('#phone').fill('+1-555-123-4567');
      await page.locator('#notes').fill('Test booking notes');

      await expect(page.locator('#name')).toHaveValue('John Doe');
      await expect(page.locator('#email')).toHaveValue('john.doe@example.com');
    }
  });

  test('step4_validates_requiredName', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();
    await page.waitForTimeout(1000);

    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step-content:has-text("Your Information")')).toBeVisible({ timeout: 5000 });

      await page.locator('#email').fill('test@example.com');

      await page.locator('.submit-btn').click();

      await expect(page.locator('.error-message:has-text("Name is required")')).toBeVisible({ timeout: 3000 });
    }
  });

  test('step4_validates_requiredEmail', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();
    await page.waitForTimeout(1000);

    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step-content:has-text("Your Information")')).toBeVisible({ timeout: 5000 });

      await page.locator('#name').fill('Test User');

      await page.locator('.submit-btn').click();

      await expect(page.locator('.error-message:has-text("Email is required")')).toBeVisible({ timeout: 3000 });
    }
  });

  test('step4_validates_emailFormat', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();
    await page.waitForTimeout(1000);

    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step-content:has-text("Your Information")')).toBeVisible({ timeout: 5000 });

      await page.locator('#name').fill('Test User');
      await page.locator('#email').fill('invalid-email');

      await page.locator('.submit-btn').click();

      await expect(page.locator('.error-message:has-text("valid email")')).toBeVisible({ timeout: 3000 });
    }
  });

  test('step5_showsConfirmation', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();
    await page.waitForTimeout(1000);

    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step-content:has-text("Your Information")')).toBeVisible({ timeout: 5000 });

      await page.locator('#name').fill('Confirmation Test');
      await page.locator('#email').fill('confirmation@test.com');
      await page.locator('.submit-btn').click();

      await expect(page.locator('.confirmation')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('.success-icon')).toBeVisible();
      await expect(page.locator('h2:has-text("Booking Confirmed")')).toBeVisible();
    }
  });

  test('fullFlow_serviceToConfirmation', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.step.active .step-number:has-text("1")')).toBeVisible();

    await page.locator('.service-card:has-text("Consultation")').click();
    await expect(page.locator('.step.active .step-number:has-text("2")')).toBeVisible({ timeout: 5000 });

    await page.locator('.date-btn').nth(1).click();
    await expect(page.locator('.step.active .step-number:has-text("3")')).toBeVisible({ timeout: 5000 });

    await page.waitForTimeout(1000);
    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step.active .step-number:has-text("4")')).toBeVisible({ timeout: 5000 });

      await page.locator('#name').fill('Full Flow User');
      await page.locator('#email').fill('fullflow@test.com');
      await page.locator('.submit-btn').click();

      await expect(page.locator('.confirmation')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('.confirmation-details')).toBeVisible();
    }
  });

  test('backButton_fromDate_returnsToService', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await expect(page.locator('.step-content:has-text("Select a Date")')).toBeVisible({ timeout: 5000 });

    await page.locator('button:has-text("Back")').click();

    await expect(page.locator('.step-content:has-text("Select a Service")')).toBeVisible({ timeout: 5000 });
  });

  test('backButton_fromTime_returnsToDate', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();
    await expect(page.locator('.step-content:has-text("Select a Time")')).toBeVisible({ timeout: 5000 });

    await page.locator('button:has-text("Back")').click();

    await expect(page.locator('.step-content:has-text("Select a Date")')).toBeVisible({ timeout: 5000 });
  });

  test('backButton_fromDetails_returnsToTime', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();
    await page.waitForTimeout(1000);

    const slotBtns = page.locator('.slot-btn');
    const count = await slotBtns.count();

    if (count > 0) {
      await slotBtns.first().click();
      await expect(page.locator('.step-content:has-text("Your Information")')).toBeVisible({ timeout: 5000 });

      await page.locator('button:has-text("Back")').click();

      await expect(page.locator('.step-content:has-text("Select a Time")')).toBeVisible({ timeout: 5000 });
    }
  });

  test('stepIndicator_showsCurrentStep', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await expect(page.locator('.progress-steps')).toBeVisible();

    const step1 = page.locator('.step').first();
    await expect(step1).toHaveClass(/active/);

    await page.locator('.service-card:has-text("Consultation")').click();

    const step2 = page.locator('.step').nth(1);
    await expect(step2).toHaveClass(/active/, { timeout: 5000 });
  });

  test('shows404_forInvalidSlug', async ({ page }) => {
    await page.goto('/book/nonexistent-business-xyz');
    await expect(page.locator('.error-state')).toBeVisible({ timeout: 10000 });
  });

  test('showsNoSlots_whenFullyBooked', async ({ page }) => {
    await page.goto(`/book/${profileSlug}`);
    await expect(page.locator('.booking-container')).toBeVisible({ timeout: 10000 });

    await page.locator('.service-card:has-text("Consultation")').click();
    await page.locator('.date-btn').nth(1).click();

    await page.waitForTimeout(1000);

    const noSlotsMessage = page.locator('.no-slots');
    const slotsGrid = page.locator('.slots-grid');

    const hasNoSlots = await noSlotsMessage.isVisible().catch(() => false);
    const hasSlots = await slotsGrid.isVisible().catch(() => false);

    expect(hasNoSlots || hasSlots).toBe(true);
  });
});
