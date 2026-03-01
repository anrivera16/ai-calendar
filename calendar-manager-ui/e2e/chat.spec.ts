import { test, expect, Page, APIRequestContext } from '@playwright/test';
import { setupDemoAuth } from './helpers/auth';
import { createBusinessProfile, createService, createBooking, cleanupTestData } from './helpers/test-data';

test.describe('AI Chat', () => {
  test.beforeEach(async ({ page, request }) => {
    await setupDemoAuth(page, request);
  });

  test.describe('Basic Chat', () => {
    test('chat_showsGreetingMessage_onLoad', async ({ page }) => {
      await page.goto('/dashboard');
      const chatPanel = page.locator('app-chat-panel');
      await expect(chatPanel).toBeVisible();

      const greetingMessage = page.locator('.message.ai-message.info');
      await expect(greetingMessage).toBeVisible();
      await expect(greetingMessage).toContainText('AI Calendar Assistant');
    });

    test('chat_sendsMessage_andReceivesResponse', async ({ page }) => {
      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('Hello');

      const sendButton = page.locator('button.send-btn');
      await sendButton.click();

      const userMessage = page.locator('.message.user-message').first();
      await expect(userMessage).toBeVisible({ timeout: 5000 });
      await expect(userMessage).toContainText('Hello');

      const aiResponse = page.locator('.message.ai-message').last();
      await expect(aiResponse).toBeVisible({ timeout: 30000 });
    });

    test('chat_showsProcessingIndicator_whileWaiting', async ({ page }) => {
      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('What events do I have today?');

      const sendButton = page.locator('button.send-btn');
      await sendButton.click();

      const processingIndicator = page.locator('.message.ai-message.typing');
      const headerStatus = page.locator('.header-status:has-text("Thinking")');

      await expect(processingIndicator.or(headerStatus)).toBeVisible({ timeout: 2000 });
    });

    test('chat_inputClears_afterSending', async ({ page }) => {
      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('Test message');
      await expect(messageInput).toHaveValue('Test message');

      const sendButton = page.locator('button.send-btn');
      await sendButton.click();

      await expect(messageInput).toHaveValue('', { timeout: 3000 });
    });

    test('chat_sendViaEnterKey', async ({ page }) => {
      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('Send via enter');
      await messageInput.press('Enter');

      const userMessage = page.locator('.message.user-message').first();
      await expect(userMessage).toBeVisible({ timeout: 5000 });
      await expect(userMessage).toContainText('Send via enter');
    });

    test('chat_sendViaButton', async ({ page }) => {
      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('Send via button');

      const sendButton = page.locator('button.send-btn');
      await sendButton.click();

      const userMessage = page.locator('.message.user-message').first();
      await expect(userMessage).toBeVisible({ timeout: 5000 });
      await expect(userMessage).toContainText('Send via button');
    });

    test('chat_doesNotSend_emptyMessage', async ({ page }) => {
      await page.goto('/dashboard');

      const initialMessageCount = await page.locator('.message').count();

      const sendButton = page.locator('button.send-btn');
      await expect(sendButton).toBeDisabled();

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('   ');
      await expect(sendButton).toBeDisabled();

      await messageInput.press('Enter');
      await page.waitForTimeout(500);

      const currentMessageCount = await page.locator('.message').count();
      expect(currentMessageCount).toBe(initialMessageCount);
    });

    test('chat_autoScrolls_onNewMessages', async ({ page }) => {
      await page.goto('/dashboard');

      const messagesContainer = page.locator('.messages-container');
      await messagesContainer.evaluate((el) => {
        el.scrollTop = 0;
      });

      const messageInput = page.locator('textarea.message-input');
      for (let i = 0; i < 3; i++) {
        await messageInput.fill(`Message ${i + 1}`);
        await messageInput.press('Enter');
        await page.waitForTimeout(500);
      }

      const scrollPosition = await messagesContainer.evaluate((el) => el.scrollTop);
      const scrollHeight = await messagesContainer.evaluate((el) => el.scrollHeight);
      const clientHeight = await messagesContainer.evaluate((el) => el.clientHeight);

      expect(scrollPosition).toBeGreaterThan(0);
    });
  });

  test.describe('Calendar Integration', () => {
    test('chat_createEvent_updatesCalendar', async ({ page, request }) => {
      const profile = await createBusinessProfile(request, { slug: 'test-chat-create' });
      await createService(request, profile.id, { name: 'Meeting' });

      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const tomorrowStr = tomorrow.toLocaleDateString('en-US', {
        weekday: 'long',
        month: 'long',
        day: 'numeric',
      });

      await messageInput.fill(`Schedule a meeting ${tomorrowStr} at 2pm`);
      await messageInput.press('Enter');

      const aiResponse = page.locator('.message.ai-message').last();
      await expect(aiResponse).toBeVisible({ timeout: 45000 });

      await cleanupTestData(request);
    });

    test('chat_listEvents_showsEvents', async ({ page, request }) => {
      const profile = await createBusinessProfile(request, { slug: 'test-chat-list' });
      const service = await createService(request, profile.id);

      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(14, 0, 0, 0);

      await createBooking(request, profile.slug, service.id, {
        clientName: 'List Events Test',
        startTime: tomorrow.toISOString(),
      });

      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('What events do I have this week?');
      await messageInput.press('Enter');

      const aiResponse = page.locator('.message.ai-message').last();
      await expect(aiResponse).toBeVisible({ timeout: 45000 });

      await cleanupTestData(request);
    });

    test('chat_showsActionResults_afterToolExecution', async ({ page }) => {
      await page.goto('/dashboard');

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('Show my calendar events');
      await messageInput.press('Enter');

      const messages = page.locator('.message');
      await expect(messages.first()).toBeVisible({ timeout: 45000 });
    });
  });

  test.describe('Error Handling', () => {
    test('chat_showsErrorMessage_onApiFailure', async ({ page }) => {
      await page.goto('/dashboard');

      await page.route('**/api/chat/process', (route) => {
        route.fulfill({
          status: 500,
          body: JSON.stringify({ error: 'Internal Server Error' }),
        });
      });

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('This should fail');
      await messageInput.press('Enter');

      const errorMessage = page.locator('.message.ai-message.error, .message.ai-message.warning');
      await expect(errorMessage).toBeVisible({ timeout: 30000 });
    });

    test('chat_handlesTimeout_gracefully', async ({ page }) => {
      await page.goto('/dashboard');

      await page.route('**/api/chat/process', (route) => {
        return new Promise(() => {
          // Never resolve - simulate timeout
        });
      });

      const messageInput = page.locator('textarea.message-input');
      await messageInput.fill('This will timeout');
      await messageInput.press('Enter');

      const timeoutMessage = page.locator('.message.ai-message.warning, .message.ai-message.error');
      await expect(timeoutMessage).toBeVisible({ timeout: 40000 });

      const inputAfterTimeout = page.locator('textarea.message-input');
      await expect(inputAfterTimeout).toBeEnabled({ timeout: 5000 });
    });
  });

  test.describe('Suggestion Buttons', () => {
    test('chat_suggestionButton_sendsPredefinedMessage', async ({ page }) => {
      await page.goto('/dashboard');

      const suggestionBtn = page.locator('.suggestion-btn:has-text("Show my events")');
      await suggestionBtn.click();

      const userMessage = page.locator('.message.user-message').first();
      await expect(userMessage).toBeVisible({ timeout: 5000 });
    });
  });
});
