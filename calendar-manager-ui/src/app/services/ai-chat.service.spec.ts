import { TestBed } from '@angular/core/testing';
import { AiChatService, ChatMessage } from './ai-chat.service';
import { ApiService } from './api';
import { CalendarService } from './calendar.service';
import { of, throwError } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';

describe('AiChatService', () => {
  let service: AiChatService;
  let apiServiceMock: {
    processChatMessage: ReturnType<typeof vi.fn>;
  };
  let calendarServiceMock: {
    loadEvents: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    apiServiceMock = {
      processChatMessage: vi.fn()
    };
    calendarServiceMock = {
      loadEvents: vi.fn()
    };

    TestBed.configureTestingModule({
      providers: [
        AiChatService,
        { provide: ApiService, useValue: apiServiceMock },
        { provide: CalendarService, useValue: calendarServiceMock }
      ]
    });

    service = TestBed.inject(AiChatService);
  });

  describe('initialState', () => {
    it('has greeting message', () => {
      const messages = service.messages();
      expect(messages.length).toBe(1);
      expect(messages[0].isUser).toBe(false);
      expect(messages[0].text).toContain('AI calendar assistant');
      expect(messages[0].type).toBe('info');
    });
  });

  describe('sendMessage', () => {
    it('adds user message to state', async () => {
      const mockResponse = { message: 'AI response', type: 'info' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const userMessage = messages.find(m => m.isUser);
      expect(userMessage?.text).toBe('Hello');
    });

    it('sets processing state during request', async () => {
      const mockResponse = { message: 'AI response', type: 'info' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.processing()).toBe(false);
    });

    it('calls api with message', async () => {
      const mockResponse = { message: 'AI response', type: 'info' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(apiServiceMock.processChatMessage).toHaveBeenCalledWith(
        'Hello',
        'test@example.com',
        undefined
      );
    });

    it('adds ai response to state', async () => {
      const mockResponse = { message: 'AI response text', type: 'success' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages.find(m => !m.isUser && m.text === 'AI response text');
      expect(aiMessage).toBeDefined();
      expect(aiMessage?.type).toBe('success');
    });

    it('sets processing false after response', async () => {
      const mockResponse = { message: 'AI response', type: 'info' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.processing()).toBe(false);
    });

    it('handles error with error message', async () => {
      apiServiceMock.processChatMessage.mockReturnValue(throwError(() => new Error('API error')));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const errorMessage = messages.find(m => m.type === 'error');
      expect(errorMessage).toBeDefined();
      expect(service.processing()).toBe(false);
    });
  });

  describe('handleBackendResponse', () => {
    it('maps message types correctly - info', async () => {
      const mockResponse = { message: 'Info message', type: 'info' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('info');
    });

    it('maps message types correctly - success', async () => {
      const mockResponse = { message: 'Success message', type: 'success' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('success');
    });

    it('maps message types correctly - warning', async () => {
      const mockResponse = { message: 'Warning message', type: 'warning' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('warning');
    });

    it('maps message types correctly - error', async () => {
      const mockResponse = { message: 'Error message', type: 'error' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('error');
    });

    it('handles numeric types - 0 (info)', async () => {
      const mockResponse = { message: 'Info message', type: 0 };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('info');
    });

    it('handles numeric types - 1 (success)', async () => {
      const mockResponse = { message: 'Success message', type: 1 };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('success');
    });

    it('handles numeric types - 2 (warning)', async () => {
      const mockResponse = { message: 'Warning message', type: 2 };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('warning');
    });

    it('handles numeric types - 3 (error)', async () => {
      const mockResponse = { message: 'Error message', type: 3 };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const aiMessage = messages[messages.length - 1];
      expect(aiMessage.type).toBe('error');
    });
  });

  describe('handleCalendarActions', () => {
    it('adds success message for executed actions', async () => {
      const mockResponse = {
        message: 'Done',
        type: 'info',
        actions: [
          { type: 'create_calendar_event', executed: true, result: 'Event created successfully' }
        ]
      };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Create event');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const successMessage = messages.find(m => m.type === 'success');
      expect(successMessage?.text).toContain('Event created successfully');
    });

    it('adds error message for failed actions', async () => {
      const mockResponse = {
        message: 'Done',
        type: 'info',
        actions: [
          { type: 'create_calendar_event', executed: false, errorMessage: 'Failed to create event' }
        ]
      };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Create event');
      await new Promise(resolve => setTimeout(resolve, 10));

      const messages = service.messages();
      const errorMessage = messages.find(m => m.type === 'error');
      expect(errorMessage?.text).toContain('Failed to create event');
    });

    it('refreshes calendar on event creation', async () => {
      const mockResponse = {
        message: 'Done',
        type: 'info',
        actions: [
          { type: 'create_calendar_event', executed: true, result: 'Event created' }
        ]
      };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Create event');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(calendarServiceMock.loadEvents).toHaveBeenCalled();
    });

    it('refreshes calendar on list events', async () => {
      const mockResponse = {
        message: 'Done',
        type: 'info',
        actions: [
          { type: 'list_calendar_events', executed: true, result: 'Events listed' }
        ]
      };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('List events');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(calendarServiceMock.loadEvents).toHaveBeenCalled();
    });
  });

  describe('clearChat', () => {
    it('removes all messages', async () => {
      const mockResponse = { message: 'AI response', type: 'info' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.messages().length).toBeGreaterThan(1);

      service.clearChat();

      expect(service.messages().length).toBe(0);
    });
  });

  describe('messages signal', () => {
    it('reflects current state', async () => {
      const mockResponse = { message: 'AI response', type: 'info' };
      apiServiceMock.processChatMessage.mockReturnValue(of(mockResponse));

      const initialCount = service.messages().length;

      service.sendMessage('Hello');
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(service.messages().length).toBe(initialCount + 2);
    });
  });
});
