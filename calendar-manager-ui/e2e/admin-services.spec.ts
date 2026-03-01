import { test, expect } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';
import {
  createBusinessProfile,
  createService,
  getServices,
  cleanupTestData,
} from './helpers/test-data';

test.describe('Service Management', () => {
  let profileId: number;

  test.beforeEach(async ({ page, request }) => {
    await setupDemoAuth(page, request);
    await cleanupTestData(request);
    const profile = await createBusinessProfile(request, { businessName: 'Services Test Business' });
    profileId = profile.id;
  });

  test.afterEach(async ({ request }) => {
    await cleanupTestData(request);
  });

  test('showsEmptyState_whenNoServices', async ({ page }) => {
    await page.goto('/admin/services');
    await expect(page.locator('.service-manager')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.empty-state')).toBeVisible();
    await expect(page.locator('.empty-state h3')).toContainText('No services yet');
  });

  test('createService_viaModal', async ({ page }) => {
    await page.goto('/admin/services');
    await expect(page.locator('.service-manager')).toBeVisible({ timeout: 10000 });

    const addButton = page.locator('button:has-text("+ Add Service")');
    await addButton.click();

    await expect(page.locator('.modal')).toBeVisible();

    await page.locator('input[name="name"]').fill('Test Consultation');
    await page.locator('textarea[name="description"]').fill('A test service for e2e');
    await page.locator('select[name="duration"]').selectOption('60');
    await page.locator('input[name="price"]').fill('150');

    await page.locator('.modal button[type="submit"]').click();

    await expect(page.locator('.modal')).not.toBeVisible({ timeout: 5000 });
    await expect(page.locator('.service-card:has-text("Test Consultation")')).toBeVisible({ timeout: 5000 });
  });

  test('createdService_appearsInList', async ({ page, request }) => {
    await createService(request, profileId, {
      name: 'Pre-created Service',
      description: 'Created via API',
      durationMinutes: 45,
      price: 75,
      color: '#10B981',
    });

    await page.goto('/admin/services');
    await expect(page.locator('.service-card:has-text("Pre-created Service")')).toBeVisible({ timeout: 10000 });
    await expect(page.locator('.service-card:has-text("Pre-created Service") .description')).toContainText('Created via API');
  });

  test('editService_viaModal', async ({ page, request }) => {
    await createService(request, profileId, {
      name: 'Service To Edit',
      price: 50,
    });

    await page.goto('/admin/services');
    await expect(page.locator('.service-card:has-text("Service To Edit")')).toBeVisible({ timeout: 10000 });

    const editButton = page.locator('.service-card:has-text("Service To Edit") button:has-text("✏️")');
    await editButton.click();

    await expect(page.locator('.modal')).toBeVisible();
    await expect(page.locator('.modal h2')).toContainText('Edit Service');

    await page.locator('input[name="name"]').fill('Edited Service Name');
    await page.locator('input[name="price"]').fill('99');

    await page.locator('.modal button[type="submit"]').click();

    await expect(page.locator('.modal')).not.toBeVisible({ timeout: 5000 });
    await expect(page.locator('.service-card:has-text("Edited Service Name")')).toBeVisible({ timeout: 5000 });
  });

  test('editedService_showsUpdatedData', async ({ page, request }) => {
    const service = await createService(request, profileId, {
      name: 'Original Name',
      durationMinutes: 30,
      price: 25,
    });

    await page.goto('/admin/services');
    await expect(page.locator('.service-card:has-text("Original Name")')).toBeVisible({ timeout: 10000 });

    const editButton = page.locator('.service-card:has-text("Original Name") button:has-text("✏️")');
    await editButton.click();

    await expect(page.locator('.modal')).toBeVisible();
    await page.locator('input[name="name"]').fill('Updated Name');
    await page.locator('input[name="price"]').fill('99');
    await page.locator('.modal button[type="submit"]').click();

    await expect(page.locator('.modal')).not.toBeVisible({ timeout: 5000 });

    const serviceCard = page.locator('.service-card:has-text("Updated Name")');
    await expect(serviceCard).toBeVisible();
    await expect(serviceCard.locator('.price')).toContainText('$99.00');
  });

  test('deleteService_withConfirmation', async ({ page, request }) => {
    await createService(request, profileId, { name: 'Service To Delete' });

    await page.goto('/admin/services');
    await expect(page.locator('.service-card:has-text("Service To Delete")')).toBeVisible({ timeout: 10000 });

    const deleteButton = page.locator('.service-card:has-text("Service To Delete") button:has-text("🗑️")');
    await deleteButton.click();

    await expect(page.locator('.confirm-modal')).toBeVisible();
    await page.locator('.confirm-modal button:has-text("Delete")').click();

    await expect(page.locator('.service-card:has-text("Service To Delete")')).not.toBeVisible({ timeout: 5000 });
  });

  test('cancelDelete_keepsService', async ({ page, request }) => {
    await createService(request, profileId, { name: 'Service Keep After Cancel' });

    await page.goto('/admin/services');
    await expect(page.locator('.service-card:has-text("Service Keep After Cancel")')).toBeVisible({ timeout: 10000 });

    const deleteButton = page.locator('.service-card:has-text("Service Keep After Cancel") button:has-text("🗑️")');
    await deleteButton.click();

    await expect(page.locator('.confirm-modal')).toBeVisible();
    await page.locator('.confirm-modal button:has-text("Cancel")').click();

    await expect(page.locator('.service-card:has-text("Service Keep After Cancel")')).toBeVisible();
  });

  test('serviceList_showsName_Duration_Price_Color', async ({ page, request }) => {
    await createService(request, profileId, {
      name: 'Full Info Service',
      durationMinutes: 90,
      price: 125,
      color: '#8B5CF6',
    });

    await page.goto('/admin/services');
    await expect(page.locator('.service-manager')).toBeVisible({ timeout: 10000 });

    const serviceCard = page.locator('.service-card:has-text("Full Info Service")');
    await expect(serviceCard).toBeVisible();
    await expect(serviceCard.locator('.duration')).toContainText('1.5 hours');
    await expect(serviceCard.locator('.price')).toContainText('$125');
    await expect(serviceCard.locator('.service-color')).toHaveCSS('background-color', 'rgb(139, 92, 246)');
  });

  test('createModal_hasColorPicker', async ({ page }) => {
    await page.goto('/admin/services');
    await expect(page.locator('.service-manager')).toBeVisible({ timeout: 10000 });

    await page.locator('button:has-text("+ Add Service")').click();
    await expect(page.locator('.modal')).toBeVisible();

    const colorPicker = page.locator('.color-picker');
    await expect(colorPicker).toBeVisible();

    const colorOptions = page.locator('.color-option');
    await expect(colorOptions).toHaveCount(8);

    const greenColor = page.locator('.color-option[title="Green"]');
    await greenColor.click();
    await expect(greenColor).toHaveClass(/selected/);
  });

  test('createMultipleServices', async ({ page, request }) => {
    await createService(request, profileId, { name: 'Service One', price: 10 });
    await createService(request, profileId, { name: 'Service Two', price: 20 });
    await createService(request, profileId, { name: 'Service Three', price: 30 });

    await page.goto('/admin/services');
    await expect(page.locator('.service-manager')).toBeVisible({ timeout: 10000 });

    const serviceCards = page.locator('.service-card');
    await expect(serviceCards).toHaveCount(3, { timeout: 5000 });

    await expect(page.locator('.service-card:has-text("Service One")')).toBeVisible();
    await expect(page.locator('.service-card:has-text("Service Two")')).toBeVisible();
    await expect(page.locator('.service-card:has-text("Service Three")')).toBeVisible();
  });
});
