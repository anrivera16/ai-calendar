import { test, expect } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';
import {
  createBusinessProfile,
  getBusinessProfile,
  deleteBusinessProfile,
  cleanupTestData,
} from './helpers/test-data';

test.describe('Business Setup', () => {
  test.beforeEach(async ({ page, request }) => {
    await setupDemoAuth(page, request);
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test.describe('Create Profile', () => {
    test.beforeEach(async ({ request }) => {
      const existing = await getBusinessProfile(request);
      if (existing) {
        await deleteBusinessProfile(request, existing.id);
      }
    });

    test('showsCreateForm_whenNoProfile', async ({ page }) => {
      await page.goto('/admin/setup');
      await expect(page.locator('.business-setup')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('h1')).toContainText('Set Up Your Business');
      await expect(page.locator('#businessName')).toBeVisible();
    });

    test('validates_requiredBusinessName', async ({ page }) => {
      await page.goto('/admin/setup');
      await expect(page.locator('#businessName')).toBeVisible({ timeout: 10000 });

      const submitButton = page.locator('button[type="submit"]');
      await submitButton.click();

      await expect(page.locator('.error-message')).toBeVisible({ timeout: 5000 });
      await expect(page.locator('.error-message')).toContainText('Business name is required');
    });

    test('createsProfile_withAllFields', async ({ page, request }) => {
      await page.goto('/admin/setup');
      await expect(page.locator('#businessName')).toBeVisible({ timeout: 10000 });

      await page.locator('#businessName').fill('Full Profile Business');
      await page.locator('#description').fill('A complete business profile for testing');
      await page.locator('#phone').fill('+1-555-999-8888');
      await page.locator('#website').fill('https://testbusiness.example.com');
      await page.locator('#address').fill('456 Test Ave, Test City, TC 67890');

      const submitButton = page.locator('button[type="submit"]');
      await submitButton.click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 10000 });

      const profile = await getBusinessProfile(request);
      expect(profile).not.toBeNull();
      expect(profile?.businessName).toBe('Full Profile Business');
    });

    test('generatesSlug_fromBusinessName', async ({ page, request }) => {
      await page.goto('/admin/setup');
      await expect(page.locator('#businessName')).toBeVisible({ timeout: 10000 });

      await page.locator('#businessName').fill('My Test Business 123');
      await page.locator('button[type="submit"]').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 10000 });

      const profile = await getBusinessProfile(request);
      expect(profile).not.toBeNull();
      expect(profile?.slug).toMatch(/^my-test-business/);
    });

    test('navigatesToAdminDashboard_afterSave', async ({ page }) => {
      await page.goto('/admin/setup');
      await expect(page.locator('#businessName')).toBeVisible({ timeout: 10000 });

      await page.locator('#businessName').fill('Navigation Test Business');
      await page.locator('button[type="submit"]').click();

      await expect(page).toHaveURL(/.*admin\/dashboard.*/, { timeout: 15000 });
    });
  });

  test.describe('Edit Profile', () => {
    test.beforeEach(async ({ request }) => {
      await cleanupTestData(request);
    });

    test('showsEditForm_withExistingData', async ({ page, request }) => {
      await createBusinessProfile(request, {
        businessName: 'Existing Business Edit',
        description: 'Original description',
        phone: '+1-555-111-2222',
      });

      await page.goto('/admin/setup');
      await expect(page.locator('.business-setup')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('h1')).toContainText('Edit Business Profile');

      await expect(page.locator('#businessName')).toHaveValue('Existing Business Edit');
      await expect(page.locator('#description')).toHaveValue('Original description');
      await expect(page.locator('#phone')).toHaveValue('+1-555-111-2222');
    });

    test('updatesProfile_successfully', async ({ page, request }) => {
      await createBusinessProfile(request, {
        businessName: 'Business To Update',
        description: 'Will be updated',
      });

      await page.goto('/admin/setup');
      await expect(page.locator('#businessName')).toBeVisible({ timeout: 10000 });

      await page.locator('#businessName').fill('Updated Business Name');
      await page.locator('#description').fill('Description has been updated');

      await page.locator('button[type="submit"]').click();

      await expect(page.locator('.success-message')).toBeVisible({ timeout: 10000 });
      await expect(page.locator('.success-message')).toContainText('updated successfully');
    });

    test('cancelButton_navigatesBack_withoutSaving', async ({ page, request }) => {
      await createBusinessProfile(request, {
        businessName: 'Cancel Test Business',
        description: 'Should not change',
      });

      await page.goto('/admin/setup');
      await expect(page.locator('#businessName')).toBeVisible({ timeout: 10000 });

      await page.locator('#businessName').fill('Changed Name That Should Not Save');

      const cancelButton = page.locator('button:has-text("Cancel")');
      await cancelButton.click();

      await expect(page).toHaveURL(/.*admin\/dashboard.*/);

      const profile = await getBusinessProfile(request);
      expect(profile?.businessName).toBe('Cancel Test Business');
    });
  });
});
